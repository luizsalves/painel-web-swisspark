using PainelWebSwissPark.Api.AgenteContabil;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// A limpeza do texto do modelo: some só a sobra da marcação de chamada de ferramenta; o texto, as
/// opções e o gráfico ficam como vieram.
/// </summary>
public class LimpezaRespostaTatiTests
{
    private const string Opcoes = "```opcoes\n[\"Ver o balanço\", \"Ver a DFC\"]\n```";

    [Fact]
    public void Parameter_solto_depois_das_opcoes_sai_e_as_opcoes_ficam()
    {
        var texto = $"A liquidez melhorou.\n\n{Opcoes}\n</parameter>";

        Assert.Equal($"A liquidez melhorou.\n\n{Opcoes}", LimpezaRespostaTati.Limpar(texto));
    }

    [Fact]
    public void Tags_de_chamada_com_atributos_e_tokens_da_DeepSeek_saem()
    {
        var texto = "<invoke name=\"consultar_dfc\">\n<parameter name=\"empresa\">A DFC fechou.</parameter>\n</invoke><｜tool▁calls▁end｜>";

        Assert.Equal("A DFC fechou.", LimpezaRespostaTati.Limpar(texto));
    }

    [Fact]
    public void Tags_com_o_prefixo_DSML_saem_e_as_opcoes_continuam_legiveis()
    {
        var texto = "A DFC fechou.\n\n```opcoes\n[\"Ver o balanço\", \"Ver a DFC\"]</｜｜DSML｜｜ parameter>\n</｜｜DSML｜｜ invoke>\n</｜｜DSML｜｜ calls>";

        Assert.Equal("A DFC fechou.\n\n```opcoes\n[\"Ver o balanço\", \"Ver a DFC\"]", LimpezaRespostaTati.Limpar(texto));
    }

    [Fact]
    public void Tokens_com_barra_comum_saem()
    {
        Assert.Equal("A DFC fechou.", LimpezaRespostaTati.Limpar("A DFC fechou.<|tool_calls_end|>"));
    }

    [Fact]
    public void Texto_sem_marcacao_passa_sem_alteracao()
    {
        var texto = $"  Liquidez de 1,20 (era 1,03): 1 < 2 e 3 > 2; título \"Liquidez — <empresa>\".\n\n{Opcoes}\n";

        Assert.Same(texto, LimpezaRespostaTati.Limpar(texto));
    }

    [Fact]
    public void Palavra_parecida_com_tag_fica()
    {
        var texto = "Os <parameters> e o <invoker> não são marcação de ferramenta.";

        Assert.Same(texto, LimpezaRespostaTati.Limpar(texto));
    }

    [Fact]
    public void Nulo_e_vazio_passam()
    {
        Assert.Null(LimpezaRespostaTati.Limpar(null));
        Assert.Equal(string.Empty, LimpezaRespostaTati.Limpar(string.Empty));
    }

    [Fact]
    public void Chamada_inteira_escrita_no_texto_e_reconhecida()
    {
        Assert.True(LimpezaRespostaTati.TemChamadaNoTexto(
            "<｜｜DSML｜｜function_calls>\n<｜｜DSML｜｜invoke name=\"consultar_balanco\">\n" +
            "<｜｜DSML｜｜parameter name=\"anoMes\">2026-09</｜｜DSML｜｜parameter>"));
        Assert.True(LimpezaRespostaTati.TemChamadaNoTexto("Vou consultar. <invoke name=\"consultar_dfc\">"));
        Assert.True(LimpezaRespostaTati.TemChamadaNoTexto("<｜tool▁calls▁begin｜>"));
    }

    [Fact]
    public void Tag_solta_no_fim_nao_e_chamada()
    {
        Assert.False(LimpezaRespostaTati.TemChamadaNoTexto($"A liquidez melhorou.\n\n{Opcoes}\n</parameter><｜tool▁calls▁end｜>"));
        Assert.False(LimpezaRespostaTati.TemChamadaNoTexto("Os <parameters> e o <invoker> não são marcação de ferramenta."));
        Assert.False(LimpezaRespostaTati.TemChamadaNoTexto(null));
    }
}
