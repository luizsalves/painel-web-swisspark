using System.Text.Encodings.Web;
using System.Text.Json;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using Xunit.Abstractions;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>O JSON que a ferramenta devolve à Tati, montado com a base do caso AEPL de mar/2026.</summary>
public class ConsultarIndicadoresTests(ITestOutputHelper saida)
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    private static JsonElement RespostaAepl(out string json)
    {
        var resultado = new IndicadoresViewModel
        {
            BaseCalculo = RegrasIndicadoresTests.BaseAepl(),
            Dfc = RegrasIndicadoresTests.DfcAepl(),
        };
        var resposta = ConsultarIndicadores.MontarResposta(
            "AEPL PARTICIPACOES LTDA", 17, filial: null, new DateTime(2026, 3, 1), new DateTime(2026, 3, 1), resultado);
        json = JsonSerializer.Serialize(resposta, Opcoes);
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement Indicador(JsonElement raiz, string codigo) =>
        raiz.GetProperty("indicadores").EnumerateArray().Single(i => i.GetProperty("codigo").GetString() == codigo);

    [Fact]
    public void Caso_AEPL_marco_2026_sai_igual_a_tela()
    {
        var raiz = RespostaAepl(out var json);
        saida.WriteLine(json);

        Assert.Equal("AEPL PARTICIPACOES LTDA", raiz.GetProperty("empresa").GetString());
        Assert.Equal(17, raiz.GetProperty("coligada").GetInt32());
        Assert.Equal(JsonValueKind.Null, raiz.GetProperty("filial").ValueKind);
        Assert.Equal("todas as filiais da coligada", raiz.GetProperty("abrangencia").GetString());
        Assert.Equal("2026-03-01", raiz.GetProperty("periodo").GetProperty("inicio").GetString());
        Assert.Equal("2026-03-31", raiz.GetProperty("periodo").GetProperty("fim").GetString());
        Assert.Equal(1, raiz.GetProperty("periodo").GetProperty("meses").GetInt32());
        Assert.Equal("risco", raiz.GetProperty("statusGeral").GetString());
        Assert.Equal(13, raiz.GetProperty("indicadores").GetArrayLength());

        // A tabela 2.4 do prompt da Tati.
        (string Codigo, double? Valor, string Texto, string Status)[] esperados =
        [
            ("LIQ_CORRENTE", 1.02, "1,02", "atencao"),
            ("LIQ_SECA", 0.19, "0,19", "risco"),
            ("LIQ_IMEDIATA", 0.04, "0,04", "risco"),
            ("LIQ_GERAL", 1.22, "1,22", "bom"),
            ("END_GERAL", 71.79, "71,79%", "atencao"),
            ("END_CURTO_PRAZO", 99.52, "99,52%", "risco"),
            ("DIV_LIQ_EBITDA_12M", 3.52, "3,52x", "atencao"),
            ("DIV_BRUTA_PL", 0.47, "0,47x", "bom"),
            ("ROE", -0.79, "-0,79%", "risco"),
            ("ROA", -0.22, "-0,22%", "risco"),
            ("MARGEM_LIQUIDA", -40.35, "-40,35%", "risco"),
            ("MARGEM_BRUTA", 100, "100,00%", "verificar"),
            ("EBITDA", 84_135.73, "84,1 mil", "bom"),
        ];
        foreach (var (codigo, valor, texto, status) in esperados)
        {
            var indicador = Indicador(raiz, codigo);
            Assert.Equal(valor, indicador.GetProperty("valor").GetDouble());
            Assert.Equal(texto, indicador.GetProperty("valorFormatado").GetString());
            Assert.Equal(status, indicador.GetProperty("status").GetString());
        }

        var endividamento = Indicador(raiz, "END_GERAL");
        Assert.Equal("menor_melhor", endividamento.GetProperty("direcao").GetString());
        Assert.Equal(80, endividamento.GetProperty("faixas").GetProperty("risco").GetDouble());
        Assert.Equal(60, endividamento.GetProperty("faixas").GetProperty("bom").GetDouble());
        Assert.Equal(-9.44, Indicador(raiz, "ROE").GetProperty("valorAnualizado").GetDouble());

        var resultado = raiz.GetProperty("resultado");
        Assert.Equal(-75_670.44m, resultado.GetProperty("lucroLiquido").GetDecimal());
        Assert.Equal(1_621.75m, resultado.GetProperty("resultadoAntesIR").GetDecimal());
        Assert.Equal(-77_292.19m, resultado.GetProperty("irCs").GetDecimal());
        Assert.Equal(-103_395.23m, resultado.GetProperty("despesasOperacionais").GetDecimal());
        Assert.Equal(-67_633.78m, resultado.GetProperty("depreciacaoAmortizacao").GetDecimal());
        Assert.Equal(0m, resultado.GetProperty("equivalenciaEOutros").GetDecimal());
        Assert.Equal(984_563.27m, raiz.GetProperty("caixaFinal").GetDecimal());

        Assert.Equal("-R$ 75,7 mil", raiz.GetProperty("pilares")[0].GetProperty("valorFormatado").GetString());
        Assert.Equal(3, raiz.GetProperty("alertas").GetArrayLength());
        Assert.Equal(2, raiz.GetProperty("pontosAVerificar").GetArrayLength());
    }

    [Fact]
    public void Sem_nenhum_lancamento_diz_que_nao_ha_dados()
    {
        var resposta = ConsultarIndicadores.MontarResposta(
            "EMPRESA SEM LANCAMENTOS", 99, filial: null, new DateTime(2026, 3, 1), new DateTime(2026, 3, 1), new IndicadoresViewModel());
        var raiz = JsonDocument.Parse(JsonSerializer.Serialize(resposta, Opcoes)).RootElement;

        Assert.True(raiz.GetProperty("semDados").GetBoolean());
        Assert.False(raiz.TryGetProperty("indicadores", out _));
    }
}
