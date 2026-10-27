using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// A ferramenta consultar_balanco com balanços falsos da AEPL Participações (coligada 17) em
/// set/2026 e ago/2026, no layout das linhas da tela: as linhas, o subtotal do grupo, o total do
/// bloco e o total do lado. Os valores são de teste, não os do TOTVS.
/// </summary>
public class ConsultarBalancoTests
{
    private static readonly DateTime Setembro = new(2026, 9, 1), Agosto = new(2026, 8, 1);

    private static BalancoLinhaViewModel L(string tipo, string descricao, decimal valor) => new() { Tipo = tipo, Descricao = descricao, Valor = valor };

    private static BalancoViewModel Balanco(decimal caixa, decimal bancos, decimal duplicatas, decimal emprestimos, decimal resultado)
    {
        var disponivel = caixa + bancos;
        var circulante = disponivel + duplicatas;
        const decimal imobilizado = 650;
        var ativo = circulante + imobilizado;
        const decimal fornecedores = 300;
        var passivoCirculante = fornecedores + emprestimos;
        const decimal capital = 500;
        var pl = ativo - passivoCirculante;
        var lucros = pl - capital - resultado;

        // Linha zerada não aparece, como no service.
        IEnumerable<BalancoLinhaViewModel> Se(decimal valor, BalancoLinhaViewModel linha) => valor == 0 ? [] : [linha];

        return new BalancoViewModel
        {
            Ativo =
            [
                .. Se(caixa, L("linha", "Caixa", caixa)),
                .. Se(bancos, L("linha", "Bancos c/ Movimento", bancos)),
                L("grupo", "Disponível", disponivel),
                .. Se(duplicatas, L("linha", "Duplicatas a Receber", duplicatas)),
                L("grupo", "Realizável a Curto Prazo", duplicatas),
                L("bloco", "Ativo Circulante", circulante),
                L("grupo", "Realizável a Longo Prazo", 0),
                L("linha", "Imobilizado", imobilizado),
                L("bloco", "Ativo Não Circulante", imobilizado),
                L("total", "ATIVO", ativo),
            ],
            Passivo =
            [
                L("linha", "Fornecedores", fornecedores),
                .. Se(emprestimos, L("linha", "Empréstimos e Financiamentos", emprestimos)),
                L("grupo", "Exigível a Curto Prazo", passivoCirculante),
                L("bloco", "Passivo Circulante", passivoCirculante),
                L("grupo", "Exigível a Longo Prazo", 0),
                L("bloco", "Passivo Não Circulante", 0),
                L("linha", "Capital Social", capital),
                .. Se(lucros, L("linha", "Lucros/Prejuízos Acumulados", lucros)),
                L("linha", "Resultado do Exercício", resultado),
                L("bloco", "Patrimônio Líquido", pl),
                L("total", "PASSIVO", ativo),
            ],
            TotalAtivo = ativo,
            TotalPassivo = ativo,
        };
    }

    // Setembro: ativo 1.000 = fornecedores 300 + PL 700. Agosto: tinha empréstimo de 100 e não tinha duplicatas.
    private static Dictionary<DateTime, BalancoViewModel> DoisMeses() => new()
    {
        [Setembro] = Balanco(caixa: 100, bancos: 200, duplicatas: 50, emprestimos: 0, resultado: 200),
        [Agosto] = Balanco(caixa: 80, bancos: 220, duplicatas: 0, emprestimos: 100, resultado: 150),
    };

    private static (ConsultarBalanco Ferramenta, BalancoFalso Servico) Criar(Dictionary<DateTime, BalancoViewModel> porMes)
    {
        var servico = new BalancoFalso(porMes);
        var ferramenta = new ConsultarBalanco(
            EmpresasFalsas.DoPainel(), servico, Ferramenta.Escopos<IBalancoService>(servico), NullLogger<ConsultarBalanco>.Instance);
        return (ferramenta, servico);
    }

    private static JsonElement Bloco(JsonElement saida, string nome) =>
        saida.GetProperty("blocos").EnumerateArray().Single(b => b.GetProperty("bloco").GetString() == nome);

    [Fact]
    public async Task Parametros_validos_trazem_os_blocos_e_os_totais_com_o_mes_anterior()
    {
        var (ferramenta, servico) = Criar(DoisMeses());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2026-09"}""");

        Assert.Equal(17, saida.GetProperty("coligada").GetInt32());
        Assert.Equal("2026-09-30", saida.GetProperty("periodo").GetProperty("fim").GetString());
        Assert.Equal("2026-08", saida.GetProperty("periodoAnterior").GetProperty("mes").GetString());
        Assert.False(saida.GetProperty("periodoAnterior").GetProperty("semDados").GetBoolean());

        var totais = saida.GetProperty("totais");
        Assert.Equal(1_000m, totais.GetProperty("ativo").GetProperty("valor").GetDecimal());
        Assert.Equal(950m, totais.GetProperty("ativo").GetProperty("valorAnterior").GetDecimal());
        Assert.Equal(50m, totais.GetProperty("ativo").GetProperty("variacao").GetDecimal());
        Assert.Equal(300m, totais.GetProperty("passivoExigivel").GetProperty("valor").GetDecimal());
        Assert.Equal(400m, totais.GetProperty("passivoExigivel").GetProperty("valorAnterior").GetDecimal());
        Assert.Equal(700m, totais.GetProperty("patrimonioLiquido").GetProperty("valor").GetDecimal());
        Assert.Equal(550m, totais.GetProperty("patrimonioLiquido").GetProperty("valorAnterior").GetDecimal());
        Assert.Equal(150m, totais.GetProperty("patrimonioLiquido").GetProperty("variacao").GetDecimal());
        Assert.Equal(1_000m, totais.GetProperty("passivoMaisPl").GetProperty("valor").GetDecimal());
        Assert.Equal(0m, saida.GetProperty("diferencaAtivoMenosPassivo").GetDecimal());

        Assert.Equal(
            new[] { "Ativo Circulante", "Ativo Não Circulante", "Passivo Circulante", "Passivo Não Circulante", "Patrimônio Líquido" },
            saida.GetProperty("blocos").EnumerateArray().Select(b => b.GetProperty("bloco").GetString()));
        var circulante = Bloco(saida, "Ativo Circulante");
        Assert.Equal("ativo", circulante.GetProperty("lado").GetString());
        Assert.Equal(350m, circulante.GetProperty("valor").GetDecimal());
        Assert.Equal(300m, circulante.GetProperty("valorAnterior").GetDecimal());
        // Sem detalhado, só os blocos.
        Assert.False(circulante.TryGetProperty("grupos", out _));

        // Os dois meses foram pedidos ao service.
        Assert.Equal(new[] { Agosto, Setembro }, servico.Chamadas.Select(c => c.Mes).Order());
    }

    [Fact]
    public async Task Detalhado_traz_grupos_e_linhas_inclusive_a_que_zerou_no_mes()
    {
        var (ferramenta, _) = Criar(DoisMeses());

        var saida = await Ferramenta.Executar(
            ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2026-09","detalhado":true}""");

        var disponivel = Bloco(saida, "Ativo Circulante").GetProperty("grupos").EnumerateArray()
            .Single(g => g.GetProperty("grupo").GetString() == "Disponível");
        Assert.Equal(300m, disponivel.GetProperty("valor").GetDecimal());
        var caixa = disponivel.GetProperty("linhas").EnumerateArray().Single(l => l.GetProperty("linha").GetString() == "Caixa");
        Assert.Equal(100m, caixa.GetProperty("valor").GetDecimal());
        Assert.Equal(80m, caixa.GetProperty("valorAnterior").GetDecimal());
        Assert.Equal(20m, caixa.GetProperty("variacao").GetDecimal());

        // Duplicatas não existia em agosto: o anterior é zero, porque a tela esconde linha zerada.
        var duplicatas = Bloco(saida, "Ativo Circulante").GetProperty("grupos").EnumerateArray()
            .Single(g => g.GetProperty("grupo").GetString() == "Realizável a Curto Prazo")
            .GetProperty("linhas").EnumerateArray().Single();
        Assert.Equal(0m, duplicatas.GetProperty("valorAnterior").GetDecimal());

        // O empréstimo foi quitado: aparece com zero no mês e 100 no anterior.
        var emprestimo = Bloco(saida, "Passivo Circulante").GetProperty("grupos")[0].GetProperty("linhas").EnumerateArray()
            .Single(l => l.GetProperty("linha").GetString() == "Empréstimos e Financiamentos");
        Assert.Equal(0m, emprestimo.GetProperty("valor").GetDecimal());
        Assert.Equal(100m, emprestimo.GetProperty("valorAnterior").GetDecimal());

        // Linhas sem subtotal (Imobilizado, Capital Social) ficam direto no bloco.
        Assert.Equal("Imobilizado", Bloco(saida, "Ativo Não Circulante").GetProperty("linhas")[0].GetProperty("linha").GetString());
        Assert.Contains("Capital Social", Bloco(saida, "Patrimônio Líquido").GetProperty("linhas").EnumerateArray()
            .Select(l => l.GetProperty("linha").GetString()));
    }

    [Fact]
    public async Task Mes_anterior_sem_saldos_deixa_o_valor_anterior_nulo_e_nao_zero()
    {
        var (ferramenta, _) = Criar(new() { [Setembro] = DoisMeses()[Setembro] });

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2026-09"}""");

        Assert.True(saida.GetProperty("periodoAnterior").GetProperty("semDados").GetBoolean());
        var ativo = saida.GetProperty("totais").GetProperty("ativo");
        Assert.Equal(1_000m, ativo.GetProperty("valor").GetDecimal());
        Assert.Equal(JsonValueKind.Null, ativo.GetProperty("valorAnterior").ValueKind);
        Assert.Equal(JsonValueKind.Null, ativo.GetProperty("variacao").ValueKind);
    }

    [Fact]
    public async Task Sem_saldos_no_mes_volta_semDados()
    {
        var (ferramenta, _) = Criar(new());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2015-12"}""");

        Assert.True(saida.GetProperty("semDados").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(saida.GetProperty("motivo").GetString()));
        Assert.Equal("2015-12-01", saida.GetProperty("periodo").GetProperty("inicio").GetString());
        Assert.False(saida.TryGetProperty("totais", out _));
    }

    [Theory]
    [InlineData("""{"anoMes":"2026-09"}""", "Empresa não informada")]
    [InlineData("""{"empresa":"aepl participacoes"}""", "Período não informado")]
    [InlineData("""{"empresa":"aepl participacoes","anoMes":"2026-09","detalhado":"sim"}""", "Parâmetro inválido: detalhado")]
    [InlineData("""{"empresa":"AEPL","anoMes":"2026-09"}""", "Mais de uma empresa")]
    public async Task Parametro_faltando_ou_invalido_volta_erro_sem_chamar_o_service(string entrada, string trecho)
    {
        var (ferramenta, servico) = Criar(DoisMeses());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, entrada);

        Assert.Contains(trecho, Ferramenta.Erro(saida));
        Assert.Empty(servico.Chamadas);
    }
}
