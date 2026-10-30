using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// A consultar_historico_indicadores com o service mensal falso. Março de 2026 é o caso AEPL
/// Participações (coligada 17) combinado com a contabilidade; fevereiro é de teste — a mesma base
/// com o passivo circulante dobrado; janeiro não tem lançamentos.
/// </summary>
public class ConsultarHistoricoIndicadoresTests
{
    private static BaseCalculoIndicadoresViewModel ComPassivoCirculanteDobrado()
    {
        var b = RegrasIndicadoresTests.BaseAepl();
        b.PassivoCirculante *= 2;
        return b;
    }

    private static Dictionary<string, BaseCalculoIndicadoresViewModel> PrimeiroTrimestre() => new()
    {
        ["2026-02"] = ComPassivoCirculanteDobrado(),
        ["2026-03"] = RegrasIndicadoresTests.BaseAepl(),
    };

    private static (ConsultarHistoricoIndicadores Ferramenta, IndicadoresMensaisFalso Servico) Criar(
        Dictionary<string, BaseCalculoIndicadoresViewModel> porMes)
    {
        var servico = new IndicadoresMensaisFalso(porMes);
        return (new ConsultarHistoricoIndicadores(
            EmpresasFalsas.DoPainel(), servico, NullLogger<ConsultarHistoricoIndicadores>.Instance), servico);
    }

    private static JsonElement Indicador(JsonElement saida, string codigo) =>
        saida.GetProperty("indicadores").EnumerateArray().Single(i => i.GetProperty("codigo").GetString() == codigo);

    private static string Mes(DateTime data) => data.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Parametros_validos_trazem_cada_mes_com_valor_e_status_da_tela()
    {
        var (ferramenta, servico) = Criar(PrimeiroTrimestre());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync,
            """{"empresa":"aepl participacoes","anoMesInicial":"2026-01","anoMes":"2026-03","indicadores":["liquidez corrente"]}""");

        Assert.Equal(17, saida.GetProperty("coligada").GetInt32());
        Assert.Equal("2026-01-01", saida.GetProperty("periodo").GetProperty("inicio").GetString());
        Assert.Equal(3, saida.GetProperty("periodo").GetProperty("meses").GetInt32());
        var liquidez = Assert.Single(saida.GetProperty("indicadores").EnumerateArray());
        Assert.Equal("LIQ_CORRENTE", liquidez.GetProperty("codigo").GetString());
        Assert.Equal("numero", liquidez.GetProperty("unidade").GetString());

        var meses = liquidez.GetProperty("meses").EnumerateArray().ToList();
        Assert.Equal(new[] { "2026-01", "2026-02", "2026-03" }, meses.Select(m => m.GetProperty("mes").GetString()));
        // Janeiro sem lançamentos: valor nulo com o motivo, nunca zero.
        Assert.Equal(JsonValueKind.Null, meses[0].GetProperty("valor").ValueKind);
        Assert.Equal(JsonValueKind.Null, meses[0].GetProperty("status").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(meses[0].GetProperty("motivo").GetString()));
        // Fevereiro com o passivo circulante dobrado; março, o caso AEPL (1,02 Atenção na tela).
        Assert.Equal(0.51, meses[1].GetProperty("valor").GetDouble());
        Assert.Equal("risco", meses[1].GetProperty("status").GetString());
        Assert.Equal(1.02, meses[2].GetProperty("valor").GetDouble());
        Assert.Equal("atencao", meses[2].GetProperty("status").GetString());

        Assert.Equal((new DateTime(2026, 1, 1), new DateTime(2026, 4, 1), (short)17, (short?)null), Assert.Single(servico.Chamadas));
    }

    [Fact]
    public async Task Sem_lista_vem_os_13_indicadores_na_ordem_da_tela()
    {
        var (ferramenta, _) = Criar(PrimeiroTrimestre());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync,
            """{"empresa":"aepl participacoes","anoMesInicial":"2026-03","anoMes":"2026-03"}""");

        var codigos = saida.GetProperty("indicadores").EnumerateArray().Select(i => i.GetProperty("codigo").GetString()).ToList();
        Assert.Equal(13, codigos.Count);
        Assert.Equal("LIQ_CORRENTE", codigos[0]);
        Assert.Equal("EBITDA", codigos[^1]);
        // ROE: o valor é o do mês, e o status usa o anualizado, como a matriz da tela.
        Assert.Equal(-0.79, Indicador(saida, "ROE").GetProperty("meses")[0].GetProperty("valor").GetDouble());
        Assert.Equal("risco", Indicador(saida, "ROE").GetProperty("meses")[0].GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(Indicador(saida, "ROE").GetProperty("observacao").GetString()));
        // Dívida líquida / EBITDA: no mês, o EBITDA de 12 meses é o do mês × 12, e a saída avisa.
        Assert.Contains("× 12", Indicador(saida, "DIV_LIQ_EBITDA_12M").GetProperty("observacao").GetString());
        Assert.Equal(JsonValueKind.Null, Indicador(saida, "LIQ_CORRENTE").GetProperty("observacao").ValueKind);
    }

    [Fact]
    public async Task Caso_especial_vem_com_o_texto_da_tela_e_nao_parece_mes_sem_dados()
    {
        var b = RegrasIndicadoresTests.BaseAepl();
        b.DividaBruta = 500_000m;
        b.DividaLiquida = 500_000m - 984_563.27m;
        var (ferramenta, _) = Criar(new() { ["2026-03"] = b });

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync,
            """{"empresa":"aepl participacoes","anoMesInicial":"2026-03","anoMes":"2026-03","indicadores":"DIV_LIQ_EBITDA_12M"}""");

        var mes = Indicador(saida, "DIV_LIQ_EBITDA_12M").GetProperty("meses")[0];
        Assert.Equal(JsonValueKind.Null, mes.GetProperty("valor").ValueKind);
        Assert.Equal("bom", mes.GetProperty("status").GetString());
        Assert.Equal("Caixa líquido", mes.GetProperty("texto").GetString());
        Assert.False(mes.TryGetProperty("motivo", out _));
    }

    [Fact]
    public async Task Meses_no_futuro_saem_nulos_e_nem_vao_ao_banco()
    {
        var mesAtual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var (ferramenta, servico) = Criar(new() { [Mes(mesAtual)] = RegrasIndicadoresTests.BaseAepl() });

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync,
            $$"""{"empresa":"aepl participacoes","anoMesInicial":"{{Mes(mesAtual)}}","anoMes":"{{Mes(mesAtual.AddMonths(2))}}","indicadores":["EBITDA"]}""");

        var meses = Indicador(saida, "EBITDA").GetProperty("meses").EnumerateArray().ToList();
        Assert.Equal(3, meses.Count);
        Assert.Equal(JsonValueKind.Number, meses[0].GetProperty("valor").ValueKind);
        Assert.All(meses.Skip(1), m =>
        {
            Assert.Equal(JsonValueKind.Null, m.GetProperty("valor").ValueKind);
            Assert.Equal("o mês ainda não aconteceu", m.GetProperty("motivo").GetString());
        });
        Assert.Equal(Mes(mesAtual), saida.GetProperty("mesEmAndamento").GetString());
        // O banco só foi consultado até o mês corrente.
        Assert.Equal(mesAtual.AddMonths(1), Assert.Single(servico.Chamadas).Fim);
    }

    [Fact]
    public async Task Periodo_inteiro_sem_lancamentos_volta_semDados()
    {
        var (ferramenta, _) = Criar(new());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync,
            """{"empresa":"aepl participacoes","anoMesInicial":"2014-01","anoMes":"2014-12"}""");

        Assert.True(saida.GetProperty("semDados").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(saida.GetProperty("motivo").GetString()));
        Assert.False(saida.TryGetProperty("indicadores", out _));
    }

    [Theory]
    [InlineData("""{"empresa":"aepl participacoes","anoMesInicial":"2025-01","anoMes":"2026-03"}""", "No máximo 12 meses")]
    [InlineData("""{"empresa":"empresa que nao existe","anoMesInicial":"2026-01","anoMes":"2026-03"}""", "Não encontrei")]
    [InlineData("""{"empresa":"AEPL","anoMesInicial":"2026-01","anoMes":"2026-03"}""", "Mais de uma empresa")]
    [InlineData("""{"empresa":"aepl participacoes","anoMesInicial":"2026-01","anoMes":"2026-03","indicadores":["liquidez"]}""", "Não identifiquei")]
    [InlineData("""{"empresa":"aepl participacoes","anoMesInicial":"2026-01","anoMes":"2026-03","indicadores":5}""", "Parâmetro inválido: indicadores")]
    [InlineData("""{"anoMesInicial":"2026-01","anoMes":"2026-03"}""", "Empresa não informada")]
    [InlineData("""{"empresa":"aepl participacoes","anoMesInicial":"2026-04","anoMes":"2026-03"}""", "mês inicial")]
    public async Task Parametro_invalido_volta_erro_sem_chamar_o_service(string entrada, string trecho)
    {
        var (ferramenta, servico) = Criar(PrimeiroTrimestre());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, entrada);

        Assert.Contains(trecho, Ferramenta.Erro(saida));
        Assert.Empty(servico.Chamadas);
    }
}
