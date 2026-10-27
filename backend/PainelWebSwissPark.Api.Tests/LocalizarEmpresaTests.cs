using System.Text.Json;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// A busca de empresa e a leitura de parâmetros das ferramentas das demonstrações, com as empresas
/// de <see cref="EmpresasFalsas.DoPainel"/> (AEPL em três coligadas e a AGV Campinas com as SCPs).
/// </summary>
public class LocalizarEmpresaTests
{
    private static Task<(LocalizarEmpresa.Empresa? Empresa, object? Erro)> Localizar(
        string empresa, short? codColigada = null, short? codFilial = null) =>
        LocalizarEmpresa.ExecutarAsync(EmpresasFalsas.DoPainel(), empresa, codColigada, codFilial, CancellationToken.None);

    private static JsonElement Lido(object erro) => Ferramenta.Json(JsonSerializer.Serialize(erro));

    [Fact]
    public async Task Nome_da_empresa_traz_a_coligada_inteira()
    {
        var (empresa, erro) = await Localizar("aepl participações");

        Assert.Null(erro);
        Assert.Equal("AEPL PARTICIPACOES LTDA", empresa!.Nome);
        Assert.Equal(17, empresa.CodColigada);
        Assert.Null(empresa.CodFilial);
        Assert.Equal("todas as filiais da coligada", empresa.Abrangencia);
    }

    [Fact]
    public async Task Nome_em_mais_de_uma_coligada_devolve_a_lista_para_escolher()
    {
        var (empresa, erro) = await Localizar("AEPL");

        Assert.Null(empresa);
        var lista = Lido(erro!).GetProperty("empresas").EnumerateArray().Select(e => e.GetProperty("coligada").GetInt32());
        Assert.Equal(new[] { 14, 17, 18 }, lista.Order());
    }

    [Fact]
    public async Task Com_a_coligada_escolhida_o_mesmo_texto_traz_a_empresa()
    {
        var (empresa, erro) = await Localizar("AEPL", codColigada: 14);

        Assert.Null(erro);
        Assert.Equal(14, empresa!.CodColigada);
        Assert.Null(empresa.CodFilial);
        // Com mais de uma filial e a coligada inteira, a lista vai para a Tati oferecer uma só.
        Assert.Equal(2, empresa.FiliaisDaColigada!.Count());
    }

    [Fact]
    public async Task Filiais_com_nomes_diferentes_na_mesma_coligada_nao_sao_ambiguas()
    {
        var (empresa, erro) = await Localizar("agv campinas");

        Assert.Null(erro);
        Assert.Equal("AGV CAMPINAS EMPREENDIMENTOS LTDA", empresa!.Nome);
        Assert.Equal(1, empresa.CodColigada);
        Assert.Null(empresa.CodFilial);
        Assert.Equal(5, empresa.FiliaisDaColigada!.Count());
    }

    [Fact]
    public async Task Nome_de_uma_SCP_traz_so_a_filial_dela()
    {
        var (empresa, erro) = await Localizar("scp alfa");

        Assert.Null(erro);
        Assert.Equal(1, empresa!.CodColigada);
        Assert.Equal((short)7, empresa.CodFilial);
        Assert.Equal("7 - AGV CPS - SCP ALFA", empresa.Abrangencia);
        Assert.Null(empresa.FiliaisDaColigada);
    }

    [Fact]
    public async Task Nome_que_combina_com_varias_SCPs_pede_para_escolher_a_filial()
    {
        var (empresa, erro) = await Localizar("scp");

        Assert.Null(empresa);
        var filiais = Lido(erro!).GetProperty("filiais").EnumerateArray().Select(f => f.GetProperty("CodFilial").GetInt32());
        Assert.Equal(new[] { 5, 6, 7 }, filiais);
    }

    [Fact]
    public async Task Nome_que_so_aparece_na_filial_tambem_e_achado()
    {
        // "SCP DELTA" é o nome da filial 5; a razão social cadastrada é "SCP BETA".
        var (empresa, erro) = await Localizar("scp delta");

        Assert.Null(erro);
        Assert.Equal((short)5, empresa!.CodFilial);
        Assert.Equal("5 - AGV CPS - SCP DELTA", empresa.Abrangencia);
    }

    [Fact]
    public async Task CodFilial_vale_para_qualquer_filial_da_coligada()
    {
        var (empresa, erro) = await Localizar("AGV CAMPINAS EMPREENDIMENTOS LTDA", codFilial: 7);

        Assert.Null(erro);
        Assert.Equal((short)7, empresa!.CodFilial);
    }

    [Fact]
    public async Task Filial_que_nao_existe_devolve_erro_com_as_filiais()
    {
        var (empresa, erro) = await Localizar("aepl participacoes", codFilial: 9);

        Assert.Null(empresa);
        var lido = Lido(erro!);
        Assert.Contains("não tem a filial 9", lido.GetProperty("erro").GetString());
        Assert.Single(lido.GetProperty("filiais").EnumerateArray());
    }

    [Fact]
    public async Task Empresa_desconhecida_devolve_erro()
    {
        var (empresa, erro) = await Localizar("empresa que nao existe");

        Assert.Null(empresa);
        Assert.Contains("Não encontrei", Lido(erro!).GetProperty("erro").GetString());
    }

    [Theory]
    [InlineData(null, "2026-09", null, "Empresa não informada")]
    [InlineData("AEPL", null, null, "Período não informado")]
    [InlineData("AEPL", "2026-13", null, "Período inválido")]
    [InlineData("AEPL", "09/2026", null, "Período inválido")]
    [InlineData("AEPL", "2026-03", "2026-05", "mês inicial não pode ser depois")]
    public void Parametro_faltando_ou_invalido_vira_erro_claro(string? empresa, string? anoMes, string? anoMesInicial, string trecho)
    {
        var erro = LocalizarEmpresa.ValidarParametros(empresa, anoMes, anoMesInicial, null, null, out _, out _);

        Assert.Contains(trecho, Lido(erro!).GetProperty("erro").GetString());
    }

    [Fact]
    public void Coligada_ou_filial_zero_vira_erro_como_nos_controllers()
    {
        var erro = LocalizarEmpresa.ValidarParametros("AEPL", "2026-09", null, codColigada: 0, codFilial: null, out _, out _);

        Assert.Equal("Coligada ou filial inválida.", Lido(erro!).GetProperty("erro").GetString());
    }

    [Fact]
    public void Tipo_errado_nao_e_ignorado_em_silencio()
    {
        var parametros = new LocalizarEmpresa.Parametros(Ferramenta.Json("""{"codFilial":"abc","detalhado":"sim","empresa":17}"""));

        Assert.Null(parametros.Inteiro("codFilial"));
        Assert.False(parametros.Booleano("detalhado"));
        Assert.Null(parametros.Texto("empresa"));
        Assert.Contains("codFilial, detalhado, empresa", Lido(parametros.Erro!).GetProperty("erro").GetString());
    }

    [Fact]
    public void Numero_entre_aspas_e_vazio_sao_aceitos()
    {
        var parametros = new LocalizarEmpresa.Parametros(
            Ferramenta.Json("""{"codFilial":"7","nivel":3,"detalhado":"true","conta":"  ","anoMesInicial":null}"""));

        Assert.Equal((short)7, parametros.Inteiro("codFilial"));
        Assert.Equal((short)3, parametros.Inteiro("nivel"));
        Assert.True(parametros.Booleano("detalhado"));
        Assert.Null(parametros.Texto("conta"));
        Assert.Null(parametros.Texto("anoMesInicial"));
        Assert.Null(parametros.Erro);
    }
}
