using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// A ferramenta consultar_balancete com um balancete falso da AEPL Participações (coligada 17) e da
/// AGV Campinas (coligada 1). Os valores são de teste, não os do TOTVS.
/// </summary>
public class ConsultarBalanceteTests
{
    private static BalanceteContaViewModel Conta(string codigo, decimal saldoAtual, bool sintetica = true) => new()
    {
        CodColigada = 17,
        Empresa = "AEPL PARTICIPACOES LTDA",
        CodConta = codigo,
        Descricao = $"Conta {codigo}",
        SaldoAnterior = saldoAtual - 10,
        Debito = 30,
        Credito = 20,
        SaldoAtual = saldoAtual,
        Nivel = codigo.Count(c => c == '.'),
        Sintetica = sintetica,
    };

    // 1, 1.1, 1.1.1 e a conta analítica 1.1.1.01 (nível 4); 2 e 2.1 credores.
    private static List<BalanceteContaViewModel> Pequeno() =>
    [
        Conta("1", 1_000),
        Conta("1.1", 1_000),
        Conta("1.1.1", 1_000),
        Conta("1.1.1.01", 1_000, sintetica: false),
        Conta("2", -1_000),
        Conta("2.1", -1_000),
    ];

    private static (ConsultarBalancete Ferramenta, BalanceteFalso Servico) Criar(List<BalanceteContaViewModel> contas)
    {
        var servico = new BalanceteFalso(contas);
        return (new ConsultarBalancete(EmpresasFalsas.DoPainel(), servico, NullLogger<ConsultarBalancete>.Instance), servico);
    }

    [Fact]
    public async Task Parametros_validos_saem_no_formato_combinado()
    {
        var (ferramenta, servico) = Criar(Pequeno());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2026-09"}""");

        Assert.Equal("AEPL PARTICIPACOES LTDA", saida.GetProperty("empresa").GetString());
        Assert.Equal(17, saida.GetProperty("coligada").GetInt32());
        Assert.Equal("2026-09-01", saida.GetProperty("periodo").GetProperty("inicio").GetString());
        Assert.Equal("2026-09-30", saida.GetProperty("periodo").GetProperty("fim").GetString());
        Assert.False(saida.GetProperty("truncado").GetBoolean());
        // Sem conta, só até o nível 3 (1.1.1): a analítica 1.1.1.01 fica de fora.
        Assert.Equal(3, saida.GetProperty("nivelMaximo").GetInt32());
        Assert.Equal(5, saida.GetProperty("totalLinhas").GetInt32());

        var linha = saida.GetProperty("linhas").EnumerateArray().Single(l => l.GetProperty("conta").GetString() == "2.1");
        Assert.Equal(2, linha.GetProperty("nivel").GetInt32());
        Assert.Equal(-1_010m, linha.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(30m, linha.GetProperty("debitos").GetDecimal());
        Assert.Equal(20m, linha.GetProperty("creditos").GetDecimal());
        Assert.Equal(-1_000m, linha.GetProperty("saldoAtual").GetDecimal());
        Assert.Equal(JsonValueKind.Number, linha.GetProperty("saldoAtual").ValueKind);

        Assert.Equal((new DateTime(2026, 9, 1), (short?)17, (short?)null, (string?)null), Assert.Single(servico.Chamadas));
    }

    [Fact]
    public async Task Com_conta_vem_ela_e_tudo_abaixo_dela()
    {
        var (ferramenta, servico) = Criar(Pequeno());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2026-09","conta":"1.1.1"}""");

        Assert.Equal(new[] { "1.1.1", "1.1.1.01" }, saida.GetProperty("linhas").EnumerateArray().Select(l => l.GetProperty("conta").GetString()));
        Assert.Equal(JsonValueKind.Null, saida.GetProperty("nivelMaximo").ValueKind);
        Assert.Equal("1.1.1", Assert.Single(servico.Chamadas).Conta);
    }

    [Fact]
    public async Task Mais_de_50_linhas_volta_truncado_com_as_de_maior_saldo()
    {
        // AGV Campinas tem 52 linhas até o nível 3 em set/2026; aqui, 62.
        var contas = new List<BalanceteContaViewModel> { Conta("1", 500_000), Conta("1.1", 500_000), Conta("2", -900_000) };
        contas.AddRange(Enumerable.Range(1, 59).Select(i => Conta($"1.1.{i}", i * 100)));
        var (ferramenta, _) = Criar(contas);

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"agv campinas","anoMes":"2026-09"}""");

        Assert.True(saida.GetProperty("truncado").GetBoolean());
        Assert.Equal(62, saida.GetProperty("totalLinhas").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(saida.GetProperty("sugestao").GetString()));
        var codigos = saida.GetProperty("linhas").EnumerateArray().Select(l => l.GetProperty("conta").GetString()!).ToList();
        Assert.Equal(50, codigos.Count);
        // Entram as de maior valor absoluto (o passivo credor também); saem as 12 menores.
        Assert.Contains("2", codigos);
        Assert.Contains("1.1.13", codigos);
        Assert.DoesNotContain("1.1.12", codigos);
        Assert.Equal(codigos.Order(StringComparer.Ordinal), codigos);
    }

    [Fact]
    public async Task Sem_contas_no_mes_volta_semDados_com_o_motivo()
    {
        var (ferramenta, _) = Criar([]);

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2015-12"}""");

        Assert.True(saida.GetProperty("semDados").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(saida.GetProperty("motivo").GetString()));
        Assert.Equal(17, saida.GetProperty("coligada").GetInt32());
        Assert.False(saida.TryGetProperty("linhas", out _));
    }

    [Fact]
    public async Task Mes_no_futuro_volta_semDados_sem_chamar_o_service()
    {
        var (ferramenta, servico) = Criar(Pequeno());

        var saida = await Ferramenta.Executar(
            ferramenta.ExecutarAsync, $$"""{"empresa":"aepl participacoes","anoMes":"{{Ferramenta.ProximoMes()}}"}""");

        Assert.True(saida.GetProperty("semDados").GetBoolean());
        Assert.Empty(servico.Chamadas);
    }

    [Theory]
    [InlineData("""{"anoMes":"2026-09"}""", "Empresa não informada")]
    [InlineData("""{"empresa":"aepl participacoes"}""", "Período não informado")]
    [InlineData("""{"empresa":"aepl participacoes","anoMes":"2026-09","conta":"caixa"}""", "Conta inválida")]
    [InlineData("""{"empresa":"aepl participacoes","anoMes":"2026-09","nivel":0}""", "Nível inválido")]
    [InlineData("""{"empresa":"aepl participacoes","anoMes":"2026-09","codFilial":"matriz"}""", "Parâmetro inválido: codFilial")]
    public async Task Parametro_faltando_ou_invalido_volta_erro_sem_chamar_o_service(string entrada, string trecho)
    {
        var (ferramenta, servico) = Criar(Pequeno());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, entrada);

        Assert.Contains(trecho, Ferramenta.Erro(saida));
        Assert.Empty(servico.Chamadas);
    }

    [Fact]
    public async Task Filial_vai_para_o_service()
    {
        var (ferramenta, servico) = Criar(Pequeno());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"scp alfa","anoMes":"2026-09"}""");

        Assert.Equal(7, saida.GetProperty("filial").GetInt32());
        Assert.Equal("7 - AGV CPS - SCP ALFA", saida.GetProperty("abrangencia").GetString());
        var chamada = Assert.Single(servico.Chamadas);
        Assert.Equal((short?)1, chamada.Coligada);
        Assert.Equal((short?)7, chamada.Filial);
    }
}
