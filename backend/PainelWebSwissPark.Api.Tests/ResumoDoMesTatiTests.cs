using PainelWebSwissPark.Api.AgenteContabil;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.Requests;
using Chamada = PainelWebSwissPark.Api.AgenteContabil.ResumoDoMesTati.Chamada;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// As ferramentas do resumo do mês, pelos filtros da tela: com empresa, indicadores, DFC e balanço;
/// num mês só, também a conciliação; sem empresa, só a conciliação de todas.
/// </summary>
public class ResumoDoMesTatiTests
{
    private static PerguntarCfoTatiComFiltrosRequest Pedido(string? empresa, string anoMes, string? anoMesInicial = null) => new()
    {
        Pergunta = ResumoDoMesTati.PerguntaDoMes,
        Empresa = empresa,
        CodColigada = empresa is null ? null : (short)17,
        CodFilial = empresa is null ? null : (short)3,
        AnoMes = anoMes,
        AnoMesInicial = anoMesInicial,
    };

    private const string DaEmpresa = """{"empresa":"EMPRESA TESTE LTDA","anoMes":"2026-09","codColigada":17,"codFilial":3}""";
    private const string BalancoDetalhado =
        """{"empresa":"EMPRESA TESTE LTDA","anoMes":"2026-09","codColigada":17,"codFilial":3,"detalhado":true}""";

    [Fact]
    public void Com_empresa_num_mes_so_chama_indicadores_dfc_balanco_e_conciliacao()
    {
        var chamadas = ResumoDoMesTati.Chamadas(Pedido("EMPRESA TESTE LTDA", "2026-09"));

        Assert.Equal(new Chamada[]
        {
            new(ConsultarIndicadores.NomeFerramenta, DaEmpresa),
            new(ConsultarDfc.NomeFerramenta, DaEmpresa),
            new(ConsultarBalanco.NomeFerramenta, BalancoDetalhado),
            new(ConsultarBacklogStatus.NomeFerramenta, """{"anoMes":"2026-09","empresa":"EMPRESA TESTE LTDA"}"""),
        }, chamadas);
    }

    [Fact]
    public void Com_varios_meses_o_balanco_fica_no_fim_e_a_conciliacao_fica_de_fora()
    {
        var chamadas = ResumoDoMesTati.Chamadas(Pedido("EMPRESA TESTE LTDA", "2026-09", "2026-07"));

        const string DoPeriodo =
            """{"empresa":"EMPRESA TESTE LTDA","anoMes":"2026-09","anoMesInicial":"2026-07","codColigada":17,"codFilial":3}""";
        Assert.Equal(new Chamada[]
        {
            new(ConsultarIndicadores.NomeFerramenta, DoPeriodo),
            new(ConsultarDfc.NomeFerramenta, DoPeriodo),
            new(ConsultarBalanco.NomeFerramenta, BalancoDetalhado),
        }, chamadas);
    }

    [Fact]
    public void Inicio_igual_ao_fim_e_um_mes_so()
    {
        var chamadas = ResumoDoMesTati.Chamadas(Pedido("EMPRESA TESTE LTDA", "2026-09", "2026-09"));

        Assert.Equal(4, chamadas.Count);
        Assert.DoesNotContain(chamadas, chamada => chamada.Argumentos.Contains("anoMesInicial"));
    }

    [Fact]
    public void Sem_empresa_resume_so_a_conciliacao_de_todas()
    {
        var chamadas = ResumoDoMesTati.Chamadas(Pedido(null, "2026-09"));

        Assert.Equal(new Chamada[]
        {
            new(ConsultarBacklogStatus.NomeFerramenta, """{"anoMes":"2026-09"}"""),
            new(ConsultarBacklogPorEmpresa.NomeFerramenta, """{"anoMes":"2026-09"}"""),
        }, chamadas);
    }

    [Fact]
    public void Sem_empresa_e_com_varios_meses_nao_ha_o_que_resumir()
    {
        Assert.Empty(ResumoDoMesTati.Chamadas(Pedido(null, "2026-09", "2026-07")));
    }

    [Fact]
    public void Com_varios_meses_a_pergunta_e_o_resumo_do_periodo()
    {
        Assert.Equal("Resumo do mês", ResumoDoMesTati.Pergunta(Pedido("EMPRESA TESTE LTDA", "2026-09")));
        Assert.Equal("Resumo do mês", ResumoDoMesTati.Pergunta(Pedido("EMPRESA TESTE LTDA", "2026-09", "2026-09")));
        Assert.Equal("Resumo do período", ResumoDoMesTati.Pergunta(Pedido("EMPRESA TESTE LTDA", "2026-09", "2026-07")));
    }
}
