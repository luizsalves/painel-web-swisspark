using PainelWebSwissPark.Api.AgenteContabil;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using static PainelWebSwissPark.Api.AgenteContabil.ValidadorGraficoTati;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// O validador do bloco ```grafico: só passa gráfico cujos números saíram das ferramentas da
/// resposta. Os JSONs de ferramenta aqui são trechos de teste no formato das ferramentas reais.
/// </summary>
public class ValidadorGraficoTatiTests
{
    private const string Historico = """
        {"empresa":"EMPRESA TESTE LTDA","coligada":17,"periodo":{"inicio":"2026-07-01","fim":"2026-09-30","meses":3},
         "indicadores":[{"codigo":"LIQ_CORRENTE","nome":"Liquidez corrente","unidade":"numero",
           "meses":[{"mes":"2026-07","valor":1.15,"status":"atencao"},{"mes":"2026-08","valor":null,"status":null,"motivo":"sem lançamentos"},
                    {"mes":"2026-09","valor":1.35,"status":"atencao"}]}]}
        """;

    private const string BacklogPorEmpresa = """
        {"anoMes":"2026-09","totalDeEmpresas":3,"itens":[{"empresa":"A","pendentes":12,"backlogTotal":20},{"empresa":"B","pendentes":7,"backlogTotal":9}],
         "demais":{"quantidadeDeEmpresas":1,"pendentes":4,"backlogTotal":5}}
        """;

    private const string Balancete = """
        {"empresa":"EMPRESA TESTE LTDA","linhas":[{"conta":"2.1","saldoAtual":-800000.25},{"conta":"1.1","saldoAtual":1500000.5}]}
        """;

    private const string Dfc = """
        {"empresa":"EMPRESA TESTE LTDA","atividades":[{"atividade":"Atividades Operacionais","total":-1234.56},{"atividade":"Atividades de Investimentos","total":-2000.0}]}
        """;

    private static ResultadoDeFerramenta Chamada(string ferramenta, string json) => new(ferramenta, json);

    private static string Resposta(string grafico) =>
        $"A liquidez melhorou no trimestre.\n\n```grafico\n{grafico}\n```\n\n```opcoes\n[\"Ver o balanço\"]\n```";

    private const string LinhaValida = """
        {"tipo":"linha","titulo":"Liquidez corrente","unidade":"indice",
         "fonte":{"ferramenta":"consultar_historico_indicadores","empresa":"EMPRESA TESTE LTDA","periodo":"2026-07 a 2026-09"},
         "eixoX":["2026-07","2026-08","2026-09"],"series":[{"nome":"Liquidez corrente","valores":[1.15,null,1.35]}]}
        """;

    [Fact]
    public void Grafico_valido_passa_sem_alteracao()
    {
        var texto = Resposta(LinhaValida);

        var resultado = Validar(texto, [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.Null(resultado.Motivo);
        Assert.Equal(texto, resultado.Texto);
    }

    [Fact]
    public void Resposta_sem_grafico_passa_sem_alteracao()
    {
        var resultado = Validar("Só texto.\n\n```opcoes\n[\"a\"]\n```", []);

        Assert.Null(resultado.Motivo);
        Assert.Equal("Só texto.\n\n```opcoes\n[\"a\"]\n```", resultado.Texto);
    }

    [Fact]
    public void Numero_sem_origem_remove_o_bloco_e_mantem_o_resto()
    {
        var inventado = LinhaValida.Replace("1.35", "1.25");

        var resultado = Validar(Resposta(inventado), [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.Contains("sem origem", resultado.Motivo);
        Assert.DoesNotContain("```grafico", resultado.Texto);
        Assert.StartsWith("A liquidez melhorou no trimestre.", resultado.Texto);
        Assert.Contains("```opcoes", resultado.Texto);
        Assert.DoesNotContain("\n\n\n", resultado.Texto);
    }

    [Fact]
    public void Fonte_que_nao_foi_chamada_remove_o_bloco()
    {
        var resultado = Validar(Resposta(LinhaValida), [Chamada(ConsultarIndicadores.NomeFerramenta, Historico)]);

        Assert.Contains("não foi chamada", resultado.Motivo);
        Assert.DoesNotContain("```grafico", resultado.Texto);
    }

    [Fact]
    public void Nenhuma_ferramenta_chamada_remove_ate_o_formato_antigo()
    {
        var resultado = Validar(Resposta("""{"titulo":"Backlog","itens":[{"label":"A","valor":12}]}"""), []);

        Assert.NotNull(resultado.Motivo);
        Assert.DoesNotContain("```grafico", resultado.Texto);
    }

    [Theory]
    [InlineData("""{"tipo":"linha","eixoX":["a"],""")]
    [InlineData("""[1, 2, 3]""")]
    [InlineData("""{"tipo":"pizza","unidade":"%","fonte":{"ferramenta":"consultar_historico_indicadores"},"eixoX":["a"],"series":[{"nome":"x","valores":[1.15]}]}""")]
    [InlineData("""{"tipo":"linha","unidade":"metros","fonte":{"ferramenta":"consultar_historico_indicadores"},"eixoX":["a"],"series":[{"nome":"x","valores":[1.15]}]}""")]
    [InlineData("""{"tipo":"linha","unidade":"indice","eixoX":["a"],"series":[{"nome":"x","valores":[1.15]}]}""")]
    [InlineData("""{"tipo":"linha","unidade":"indice","fonte":{"ferramenta":"consultar_historico_indicadores"},"eixoX":["a","b"],"series":[{"nome":"x","valores":[1.15]}]}""")]
    [InlineData("""{"tipo":"linha","unidade":"indice","fonte":{"ferramenta":"consultar_historico_indicadores"},"eixoX":["a"],"series":[{"nome":"x","valores":["1.15"]}]}""")]
    public void Json_quebrado_ou_fora_do_contrato_remove_o_bloco(string grafico)
    {
        var resultado = Validar(Resposta(grafico), [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.NotNull(resultado.Motivo);
        Assert.DoesNotContain("```grafico", resultado.Texto);
    }

    [Fact]
    public void Acima_dos_limites_remove_o_bloco()
    {
        var treze = string.Join(",", Enumerable.Range(1, 13).Select(i => $"\"m{i}\""));
        var valores = string.Join(",", Enumerable.Repeat("1.15", 13));
        var linhaLonga = $$"""{"tipo":"linha","unidade":"indice","fonte":{"ferramenta":"consultar_historico_indicadores"},"eixoX":[{{treze}}],"series":[{"nome":"x","valores":[{{valores}}]}]}""";
        var quatroSeries = """{"tipo":"colunas","unidade":"indice","fonte":{"ferramenta":"consultar_historico_indicadores"},"eixoX":["a"],"series":[{"nome":"1","valores":[1.15]},{"nome":"2","valores":[1.15]},{"nome":"3","valores":[1.15]},{"nome":"4","valores":[1.15]}]}""";
        var chamada = Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico);

        Assert.Contains("12 categorias", Validar(Resposta(linhaLonga), [chamada]).Motivo);
        Assert.Contains("3 séries", Validar(Resposta(quatroSeries), [chamada]).Motivo);
        Assert.Contains("mais de um gráfico", Validar(Resposta(LinhaValida) + Resposta(LinhaValida), [chamada]).Motivo);
    }

    [Fact]
    public void Formato_antigo_com_Demais_somado_pela_ferramenta_passa()
    {
        var grafico = """{"titulo":"Backlog por empresa","itens":[{"label":"A","valor":12},{"label":"B","valor":7},{"label":"Demais empresas","valor":4}]}""";

        var resultado = Validar(Resposta(grafico), [Chamada("consultar_backlog_por_empresa", BacklogPorEmpresa)]);

        Assert.Null(resultado.Motivo);
    }

    [Fact]
    public void Quinze_barras_mais_Demais_passa_e_dezesseis_sem_Demais_nao()
    {
        var numeros = string.Join(",", Enumerable.Range(1, 16).Select(i => i.ToString()));
        var ferramenta = Chamada("consultar_backlog_por_empresa", $$"""{"itens":[{{numeros}}]}""");
        static string Barras(int quantas, bool demais)
        {
            var itens = Enumerable.Range(1, quantas).Select(i =>
                "{\"label\":\"" + (demais && i == quantas ? "Demais empresas" : "E" + i) + "\",\"valor\":" + i + "}");
            return "{\"titulo\":\"Backlog\",\"itens\":[" + string.Join(",", itens) + "]}";
        }

        Assert.Null(Validar(Resposta(Barras(16, demais: true)), [ferramenta]).Motivo);
        Assert.Contains("15 barras", Validar(Resposta(Barras(16, demais: false)), [ferramenta]).Motivo);
    }

    [Fact]
    public void Sinal_trocado_so_vale_para_o_balancete()
    {
        // Passivo circulante credor (negativo no balancete) mostrado positivo: passa.
        var doBalancete = """{"tipo":"barras","unidade":"R$","fonte":{"ferramenta":"consultar_balancete"},"eixoX":["Ativo circulante","Passivo circulante"],"series":[{"nome":"Saldo","valores":[1500000.5,800000.25]}]}""";
        Assert.Null(Validar(Resposta(doBalancete), [Chamada(ConsultarBalancete.NomeFerramenta, Balancete)]).Motivo);

        // Caixa operacional negativo na DFC mostrado positivo: não passa.
        var daDfc = """{"tipo":"colunas","unidade":"R$","fonte":{"ferramenta":"consultar_dfc"},"eixoX":["Operacionais","Investimentos"],"series":[{"nome":"Total","valores":[1234.56,-2000]}]}""";
        Assert.Contains("sem origem", Validar(Resposta(daDfc), [Chamada(ConsultarDfc.NomeFerramenta, Dfc)]).Motivo);

        // Com o sinal da DFC: passa.
        var daDfcComSinal = daDfc.Replace("1234.56", "-1234.56");
        Assert.Null(Validar(Resposta(daDfcComSinal), [Chamada(ConsultarDfc.NomeFerramenta, Dfc)]).Motivo);
    }

    [Theory]
    // Os 3 casos que o árbitro achou: números num formato que o validador não lia e a tela desenharia.
    [InlineData("""{"tipo":"linha","unidade":"indice","fonte":{"ferramenta":"consultar_historico_indicadores"},"eixoX":["2026-07","2026-08","2026-09"],"series":[{"nome":"Liquidez","valores":[1.15,null,1.35]}],"itens":[{"label":"X","valor":999}]}""")]
    [InlineData("""{"tipo":"barras","unidade":"indice","fonte":{"ferramenta":"consultar_historico_indicadores"},"itens":[{"label":"A","valor":1.15}],"eixoX":["a"],"series":[{"nome":"x","valores":[999]}]}""")]
    [InlineData("""{"titulo":"Liquidez","itens":[{"label":"A","valor":1.15}],"series":[{"nome":"x","valores":[999]}]}""")]
    public void Bloco_que_mistura_os_dois_formatos_e_removido(string grafico)
    {
        var resultado = Validar(Resposta(grafico), [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.Contains("mistura", resultado.Motivo);
        Assert.DoesNotContain("```grafico", resultado.Texto);
    }

    [Fact]
    public void Numero_em_qualquer_campo_do_bloco_tambem_e_conferido()
    {
        var comCampoExtra = LinhaValida.Replace("\"titulo\":\"Liquidez corrente\"", "\"titulo\":\"Liquidez corrente\",\"meta\":{\"alvo\":999}");

        var resultado = Validar(Resposta(comCampoExtra), [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.Contains("sem origem", resultado.Motivo);
    }

    [Fact]
    public void Coligada_e_quantidade_de_meses_nao_servem_de_origem()
    {
        // O histórico de teste tem "coligada": 17 e "meses": 3 — números que identificam a consulta, não dados.
        var comIdentificadores = LinhaValida.Replace("1.15,null,1.35", "17,null,3");

        var resultado = Validar(Resposta(comIdentificadores), [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.Contains("2 número(s)", resultado.Motivo);
    }

    [Fact]
    public void Barras_com_eixoX_aceitam_15_mais_Demais_como_o_formato_antigo()
    {
        var ferramenta = Chamada("consultar_backlog_por_empresa", $$"""{"itens":[{{string.Join(",", Enumerable.Range(1, 16))}}]}""");
        static string Barras(int quantas, bool demais) =>
            "{\"tipo\":\"barras\",\"unidade\":\"contas\",\"fonte\":{\"ferramenta\":\"consultar_backlog_por_empresa\"},\"eixoX\":["
            + string.Join(",", Enumerable.Range(1, quantas).Select(i => "\"" + (demais && i == quantas ? "Demais empresas" : "E" + i) + "\""))
            + "],\"series\":[{\"nome\":\"Pendentes\",\"valores\":[" + string.Join(",", Enumerable.Range(1, quantas)) + "]}]}";

        Assert.Null(Validar(Resposta(Barras(16, demais: true)), [ferramenta]).Motivo);
        Assert.Contains("15 categorias", Validar(Resposta(Barras(16, demais: false)), [ferramenta]).Motivo);
    }

    [Fact]
    public void Remocao_nao_deixa_buraco_de_linhas_mesmo_com_CRLF()
    {
        var texto = "Antes.\r\n\r\n```grafico\r\n{\"titulo\":\"x\",\"itens\":[{\"label\":\"A\",\"valor\":999}]}\r\n```\r\n\r\nDepois.";

        var resultado = Validar(texto, [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.NotNull(resultado.Motivo);
        Assert.Equal("Antes.\n\nDepois.", resultado.Texto);
    }

    [Theory]
    // Resposta cortada no meio do gráfico, com as opções que o GarantirOpcoesAsync acrescenta depois.
    [InlineData("Texto.\n\n```grafico\n{\"titulo\":\"t\",\"itens\":[{\"label\":\"A\",\"va\n\n```opcoes\n[\"Ver o balanço\",\"Ver a DFC\"]\n```")]
    // Bloco sem a cerca de fechamento, JSON válido e número inventado, seguido das opções.
    [InlineData("Texto.\n\n```grafico\n{\"titulo\":\"t\",\"itens\":[{\"label\":\"A\",\"valor\":999}]}\n\n```opcoes\n[\"Ver o balanço\",\"Ver a DFC\"]\n```")]
    public void Remover_bloco_sem_fechamento_mantem_as_opcoes_intactas(string texto)
    {
        var resultado = Validar(texto, [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.NotNull(resultado.Motivo);
        Assert.Equal("Texto.\n\n```opcoes\n[\"Ver o balanço\",\"Ver a DFC\"]\n```", resultado.Texto);
    }

    [Theory]
    // P1 do árbitro: o primeiro bloco cortado engolia as crases do segundo, que passava sem conferência.
    [InlineData("Texto.\n\n```grafico\n{\"titulo\":\"t\",\"itens\":[{\"label\":\"A\",\"va\n\n```grafico\n{\"titulo\":\"t\",\"itens\":[{\"label\":\"X\",\"valor\":999}]}\n```\n\n```opcoes\n[\"a\"]\n```")]
    // P2: o primeiro, sem fechamento, com 999; o segundo com 888.
    [InlineData("Texto.\n\n```grafico\n{\"titulo\":\"t\",\"itens\":[{\"label\":\"A\",\"valor\":999}]}\n\n```grafico\n{\"titulo\":\"t\",\"itens\":[{\"label\":\"X\",\"valor\":888}]}\n```\n\n```opcoes\n[\"a\"]\n```")]
    public void Bloco_colado_depois_de_um_bloco_cortado_tambem_e_conferido(string texto)
    {
        var resultado = Validar(texto, [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.NotNull(resultado.Motivo);
        Assert.DoesNotContain("```grafico", resultado.Texto);
        Assert.Equal("Texto.\n\n```opcoes\n[\"a\"]\n```", resultado.Texto);
    }

    [Fact]
    public void Bloco_cortado_seguido_de_bloco_valido_deixa_so_o_valido()
    {
        var texto = "Texto.\n\n```grafico\n{\"titulo\":\"t\",\"itens\":[{\"label\":\"A\",\"va\n\n```grafico\n"
            + LinhaValida + "\n```\n\n```opcoes\n[\"a\"]\n```";

        var resultado = Validar(texto, [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.Contains("JSON válido", resultado.Motivo);
        Assert.Equal("Texto.\n\n```grafico\n" + LinhaValida + "\n```\n\n```opcoes\n[\"a\"]\n```", resultado.Texto);
    }

    [Fact]
    public void Bloco_colado_em_texto_nao_deixa_crases_soltas()
    {
        var texto = "Antes.\n\n```grafico\n{\"titulo\":\"t\",\"itens\":[{\"label\":\"A\",\"valor\":999}]}\n```Depois.";

        var resultado = Validar(texto, [Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico)]);

        Assert.Equal("Antes.\n\nDepois.", resultado.Texto);
    }

    [Fact]
    public void Fonte_no_formato_antigo_tambem_precisa_ter_sido_chamada()
    {
        var grafico = """{"titulo":"Backlog","fonte":{"ferramenta":"consultar_dfc"},"itens":[{"label":"A","valor":12}]}""";

        Assert.Contains("não foi chamada", Validar(Resposta(grafico), [Chamada("consultar_backlog_por_empresa", BacklogPorEmpresa)]).Motivo);
        Assert.Null(Validar(Resposta(grafico.Replace("consultar_dfc", "consultar_backlog_por_empresa")), [Chamada("consultar_backlog_por_empresa", BacklogPorEmpresa)]).Motivo);
    }

    [Fact]
    public void Numero_de_outra_ferramenta_da_mesma_resposta_tambem_vale()
    {
        // O livro exige que cada número exista nos resultados das ferramentas desta resposta, não só na da fonte.
        var grafico = LinhaValida.Replace("1.15,null,1.35", "1.15,null,12");

        var resultado = Validar(Resposta(grafico), [
            Chamada(ConsultarHistoricoIndicadores.NomeFerramenta, Historico),
            Chamada("consultar_backlog_por_empresa", BacklogPorEmpresa)]);

        Assert.Null(resultado.Motivo);
    }
}
