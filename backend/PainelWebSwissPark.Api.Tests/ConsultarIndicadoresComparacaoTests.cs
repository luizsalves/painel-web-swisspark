using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// A consultar_indicadores pela ExecutarAsync, com o service falso: o período anterior, a variação, o
/// filtro de um indicador e os erros. O mês é o caso AEPL Participações (coligada 17) de mar/2026; o
/// mês anterior é de teste — a mesma base com o passivo circulante dobrado.
/// </summary>
public class ConsultarIndicadoresComparacaoTests
{
    private static readonly DateTime Fevereiro = new(2026, 2, 1), Marco = new(2026, 3, 1);

    private static IndicadoresViewModel Marco2026() => new()
    {
        BaseCalculo = RegrasIndicadoresTests.BaseAepl(),
        Dfc = RegrasIndicadoresTests.DfcAepl(),
    };

    private static IndicadoresViewModel ComPassivoCirculanteDobrado()
    {
        var resultado = Marco2026();
        resultado.BaseCalculo.PassivoCirculante *= 2;
        return resultado;
    }

    private static (ConsultarIndicadores Ferramenta, IndicadoresFalso Servico) Criar(Dictionary<DateTime, IndicadoresViewModel> porInicio)
    {
        var servico = new IndicadoresFalso(porInicio);
        var ferramenta = new ConsultarIndicadores(
            EmpresasFalsas.DoPainel(), servico, Ferramenta.Escopos<IIndicadoresService>(servico), NullLogger<ConsultarIndicadores>.Instance);
        return (ferramenta, servico);
    }

    private static JsonElement Indicador(JsonElement saida, string codigo) =>
        saida.GetProperty("indicadores").EnumerateArray().Single(i => i.GetProperty("codigo").GetString() == codigo);

    [Fact]
    public async Task Cada_indicador_traz_o_periodo_anterior_e_a_variacao()
    {
        var (ferramenta, servico) = Criar(new() { [Marco] = Marco2026(), [Fevereiro] = ComPassivoCirculanteDobrado() });

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2026-03"}""");

        var anterior = saida.GetProperty("periodoAnterior");
        Assert.Equal("2026-02-01", anterior.GetProperty("inicio").GetString());
        Assert.Equal("2026-02-28", anterior.GetProperty("fim").GetString());
        Assert.False(anterior.GetProperty("semDados").GetBoolean());

        // Liquidez corrente: 1,02 em março; com o passivo circulante dobrado, 0,51 em fevereiro.
        var liquidez = Indicador(saida, "LIQ_CORRENTE");
        Assert.Equal("numero", liquidez.GetProperty("unidade").GetString());
        Assert.Equal(1.02, liquidez.GetProperty("valor").GetDouble());
        Assert.Equal(0.51, liquidez.GetProperty("valorAnterior").GetDouble());
        Assert.Equal(0.51, liquidez.GetProperty("variacao").GetDouble());
        Assert.Equal("melhorou", liquidez.GetProperty("sentido").GetString());
        Assert.Equal("atencao", liquidez.GetProperty("status").GetString());
        Assert.Equal("risco", liquidez.GetProperty("statusAnterior").GetString());

        // O que não depende do passivo circulante fica igual: variação zero e "estavel".
        var ebitda = Indicador(saida, "EBITDA");
        Assert.Equal(0, ebitda.GetProperty("variacao").GetDouble());
        Assert.Equal("estavel", ebitda.GetProperty("sentido").GetString());

        // Os dois períodos foram pedidos ao service, com o fim exclusivo.
        Assert.Equal(
            new[] { (Fevereiro, Marco), (Marco, new DateTime(2026, 4, 1)) },
            servico.Chamadas.Select(c => (c.Inicio, c.Fim)).Order());
    }

    [Fact]
    public async Task Periodo_de_tres_meses_compara_com_os_tres_meses_anteriores()
    {
        var (ferramenta, servico) = Criar(new() { [new DateTime(2026, 1, 1)] = Marco2026() });

        var saida = await Ferramenta.Executar(
            ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMesInicial":"2026-01","anoMes":"2026-03"}""");

        Assert.Equal("2025-10-01", saida.GetProperty("periodoAnterior").GetProperty("inicio").GetString());
        Assert.Equal("2025-12-31", saida.GetProperty("periodoAnterior").GetProperty("fim").GetString());
        Assert.Contains(servico.Chamadas, c => c.Inicio == new DateTime(2025, 10, 1) && c.Fim == new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task Periodo_anterior_sem_lancamentos_deixa_a_comparacao_nula()
    {
        var (ferramenta, _) = Criar(new() { [Marco] = Marco2026() });

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2026-03"}""");

        Assert.True(saida.GetProperty("periodoAnterior").GetProperty("semDados").GetBoolean());
        var liquidez = Indicador(saida, "LIQ_CORRENTE");
        Assert.Equal(JsonValueKind.Null, liquidez.GetProperty("valorAnterior").ValueKind);
        Assert.Equal(JsonValueKind.Null, liquidez.GetProperty("variacao").ValueKind);
    }

    [Theory]
    [InlineData("LIQ_CORRENTE", "LIQ_CORRENTE")]
    [InlineData("liquidez corrente", "LIQ_CORRENTE")]
    [InlineData("dívida líquida", "DIV_LIQ_EBITDA_12M")]
    [InlineData("divida-bruta-pl", "DIV_BRUTA_PL")]
    [InlineData("composicao-endividamento", "END_CURTO_PRAZO")]
    [InlineData("roe", "ROE")]
    public async Task Filtro_de_indicador_traz_so_ele(string pedido, string codigo)
    {
        var (ferramenta, _) = Criar(new() { [Marco] = Marco2026(), [Fevereiro] = Marco2026() });

        var saida = await Ferramenta.Executar(
            ferramenta.ExecutarAsync, $$"""{"empresa":"aepl participacoes","anoMes":"2026-03","indicador":"{{pedido}}"}""");

        Assert.Equal(codigo, Assert.Single(saida.GetProperty("indicadores").EnumerateArray()).GetProperty("codigo").GetString());
        Assert.False(saida.TryGetProperty("pilares", out _));
        Assert.Equal(17, saida.GetProperty("coligada").GetInt32());
    }

    [Theory]
    [InlineData("liquidez")]
    [InlineData("faturamento")]
    public async Task Indicador_ambiguo_ou_desconhecido_volta_erro_com_a_lista(string pedido)
    {
        var (ferramenta, servico) = Criar(new() { [Marco] = Marco2026() });

        var saida = await Ferramenta.Executar(
            ferramenta.ExecutarAsync, $$"""{"empresa":"aepl participacoes","anoMes":"2026-03","indicador":"{{pedido}}"}""");

        Assert.Contains("Não identifiquei", Ferramenta.Erro(saida));
        Assert.Equal(13, saida.GetProperty("indicadores").GetArrayLength());
        Assert.Empty(servico.Chamadas);
    }

    [Fact]
    public async Task Sem_lancamentos_volta_semDados()
    {
        var (ferramenta, _) = Criar(new());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2015-12"}""");

        Assert.True(saida.GetProperty("semDados").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(saida.GetProperty("motivo").GetString()));
        Assert.False(saida.TryGetProperty("indicadores", out _));
    }

    [Theory]
    [InlineData("""{"anoMes":"2026-03"}""", "Empresa não informada")]
    [InlineData("""{"empresa":"aepl participacoes"}""", "Período não informado")]
    [InlineData("""{"empresa":"AEPL","anoMes":"2026-03"}""", "Mais de uma empresa")]
    [InlineData("""{"empresa":"aepl participacoes","anoMes":"2026-03","codFilial":"todas"}""", "Parâmetro inválido: codFilial")]
    public async Task Parametro_faltando_ou_invalido_volta_erro_sem_chamar_o_service(string entrada, string trecho)
    {
        var (ferramenta, servico) = Criar(new() { [Marco] = Marco2026() });

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, entrada);

        Assert.Contains(trecho, Ferramenta.Erro(saida));
        Assert.Empty(servico.Chamadas);
    }
}
