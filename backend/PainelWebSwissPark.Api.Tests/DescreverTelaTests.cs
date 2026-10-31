using PainelWebSwissPark.Api.AgenteContabil;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// A tela aberta que vai no fim do contexto da Tati quando a pergunta vem do ícone: vinda do
/// navegador, vai numa linha só e curta.
/// </summary>
public class DescreverTelaTests
{
    [Fact]
    public void Tela_com_aba()
    {
        Assert.Equal("Balancete, aba Balanço", HarnessService.DescreverTela("Balancete", "Balanço"));
    }

    [Fact]
    public void Tela_sem_aba()
    {
        Assert.Equal("Produtividade", HarnessService.DescreverTela(" Produtividade ", "  "));
    }

    [Fact]
    public void Sem_tela_nao_ha_descricao_mesmo_com_aba()
    {
        Assert.Null(HarnessService.DescreverTela(null, "Balanço"));
        Assert.Null(HarnessService.DescreverTela("   ", null));
    }

    [Fact]
    public void Quebras_de_linha_viram_espaco_e_nome_longo_e_cortado()
    {
        Assert.Equal("Balancete, aba Balanço DFC", HarnessService.DescreverTela("Balancete", "Balanço\r\n\tDFC"));
        Assert.Equal(new string('a', 60), HarnessService.DescreverTela(new string('a', 200), null));
    }
}
