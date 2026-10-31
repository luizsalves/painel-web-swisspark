using PainelWebSwissPark.Api.AgenteContabil;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using static PainelWebSwissPark.Api.AgenteContabil.ValidadorGraficoTati;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// Os links dos números para os cards — [R$ 1,2 mi](card:resultado/receitaLiquida). No resumo do mês
/// (estrito), só fica a linha cujos números saíram das ferramentas da resposta, cada link com um número
/// do próprio card; fora dele, o link que não confere vira texto. Os JSONs são trechos de teste no
/// formato das ferramentas reais.
/// </summary>
public class ValidadorReferenciasTatiTests
{
    private const string Indicadores = """
        {"empresa":"EMPRESA TESTE LTDA","coligada":17,"filial":null,"periodo":{"inicio":"2026-09-01","fim":"2026-09-30","meses":1},
         "convencaoDeUnidades":"percentual em pontos percentuais (12.5 = 12,5%; a variação também em p.p.); multiplo em vezes; moeda em R$",
         "statusGeral":"Atenção","veredito":"Resultado e caixa pedem atenção.",
         "pilares":[{"pilar":"Resultado","valorFormatado":"-R$ 1,23 mi","status":"Risco","frase":"Prejuízo no mês"},
                    {"pilar":"Caixa","valorFormatado":"R$ 3,56 mi","status":"Atenção","frase":"Caiu R$ 120,5 mil no mês"}],
         "indicadores":[
           {"codigo":"LIQ_CORRENTE","nome":"Liquidez corrente","unidade":"numero","valor":1.42,"valorFormatado":"1,42",
            "faixas":{"risco":1.0,"bom":1.5},"valorAnterior":1.38,"valorFormatadoAnterior":"1,38","variacao":0.04,"sentido":"melhorou"},
           {"codigo":"MARGEM_BRUTA","nome":"Margem bruta","unidade":"percentual","valor":35.2,"valorFormatado":"35,2%",
            "valorAnterior":33.1,"valorFormatadoAnterior":"33,1%","variacao":2.1,"sentido":"melhorou"}],
         "resultado":{"receitaLiquida":2500000.0,"custos":-1620000.0,"ebitda":480000.55,"lucroLiquido":-1230000.0},
         "caixaFinal":3560000.0}
        """;

    private const string Dfc = """
        {"empresa":"EMPRESA TESTE LTDA","coligada":17,"periodo":{"inicio":"2026-09-01","fim":"2026-09-30","meses":1},
         "atividades":[{"atividade":"Atividades Operacionais","total":-1234.56,"linhas":[{"linha":"Fornecedores","valor":-500.0}]},
                       {"atividade":"Atividades de Investimentos","total":-2000.0,"linhas":[]}],
         "caixaInicial":3680500.0,"variacaoCaixa":-120500.0,"caixaFinal":3560000.0}
        """;

    private const string Balanco = """
        {"empresa":"EMPRESA TESTE LTDA","coligada":17,
         "totais":{"ativo":{"valor":10500000.0,"valorAnterior":10000000.0,"variacao":500000.0},
                   "patrimonioLiquido":{"valor":4200000.0,"valorAnterior":4100000.0,"variacao":100000.0}},
         "blocos":[{"lado":"ativo","bloco":"Ativo Circulante","valor":6000000.0,"valorAnterior":5800000.0,"variacao":200000.0}]}
        """;

    private const string Conciliacao = """
        {"anoMes":"2026-09","empresa":"EMPRESA TESTE LTDA","backlogTotal":120,"preparados":30,"conferidos":40,"fechados":20,"pendentes":30}
        """;

    private static readonly ResultadoDeFerramenta[] DoResumo =
    [
        new(ConsultarIndicadores.NomeFerramenta, Indicadores),
        new(ConsultarDfc.NomeFerramenta, Dfc),
        new(ConsultarBalanco.NomeFerramenta, Balanco),
        new(ConsultarBacklogStatus.NomeFerramenta, Conciliacao),
    ];

    private const string Opcoes = "\n\n```opcoes\n[\"Ver o balanço\", \"Comparar com 99,9% do ano passado\"]\n```";

    [Fact]
    public void Resumo_com_links_que_conferem_passa_sem_alteracao()
    {
        var texto =
            "### Resultado\n" +
            "- Receita de [R$ 2,5 mi](card:resultado/receitaLiquida) e EBITDA de [R$ 480 mil](card:resultado/ebitda).\n" +
            "- Prejuízo de [R$ 1,23 mi](card:resultado/lucroLiquido); a saúde geral está em [Atenção](card:veredito).\n\n" +
            "### Indicadores\n" +
            "- Liquidez corrente em [1,42](card:indicador/LIQ_CORRENTE), contra [1,4](card:indicador/LIQ_CORRENTE) arredondado.\n" +
            "- Margem bruta de [35,2%](card:indicador/MARGEM_BRUTA), [+2,1 p.p.](card:indicador/MARGEM_BRUTA) no mês.\n\n" +
            "### Caixa e balanço\n" +
            "- A operação consumiu [R$ 1,2 mil](card:dfc/operacional) e o caixa fechou em [R$ 3,56 mi](card:dfc/caixaFinal).\n" +
            "- O caixa caiu [R$ 120,5 mil](card:pilar/caixa); ativo de [R$ 10,5 mi](card:balanco/ativo), circulante de [R$ 6 mi](card:balanco/ativoCirculante).\n" +
            "- Faltam [30](card:conciliacao/pendentes) contas de [120](card:conciliacao/backlogTotal)." +
            Opcoes;

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Null(resultado.Motivo);
        Assert.Equal(texto, resultado.Texto);
    }

    [Fact]
    public void Numero_de_outro_card_tira_a_linha_do_resumo()
    {
        var texto = "- Liquidez corrente em [35,2%](card:indicador/LIQ_CORRENTE).\n- Margem bruta de [35,2%](card:indicador/MARGEM_BRUTA).";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Equal("- Margem bruta de [35,2%](card:indicador/MARGEM_BRUTA).", resultado.Texto);
        Assert.Contains("indicador/LIQ_CORRENTE: número sem origem no card", resultado.Motivo);
    }

    [Fact]
    public void Numero_fora_da_tolerancia_da_casa_tira_a_linha()
    {
        // 1,42 com duas casas não aceita 1,44; "R$ 2,6 mi" não é a receita de R$ 2,5 mi.
        var texto = "- Liquidez em [1,44](card:indicador/LIQ_CORRENTE).\n- Receita de [R$ 2,6 mi](card:resultado/receitaLiquida).\n- Fica.";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Equal("- Fica.", resultado.Texto);
    }

    [Fact]
    public void Card_desconhecido_ou_sem_a_ferramenta_tira_a_linha_do_resumo()
    {
        var texto =
            "- Um indicador que não existe: [1,42](card:indicador/NAO_EXISTE).\n" +
            "- Um alvo inventado: [1,42](card:tela/qualquer).\n" +
            "- Fica.";
        var semConciliacao = DoResumo.Where(r => r.Ferramenta != ConsultarBacklogStatus.NomeFerramenta).ToList();

        var resultado = ValidadorReferenciasTati.Validar(texto + "\n- Faltam [30](card:conciliacao/pendentes) contas.", semConciliacao, estrito: true);

        Assert.Equal("- Fica.", resultado.Texto);
        Assert.Contains("indicador/NAO_EXISTE: card desconhecido", resultado.Motivo);
        Assert.Contains("conciliacao/pendentes: card desconhecido", resultado.Motivo);
    }

    [Fact]
    public void Valor_sem_link_e_sem_origem_tira_a_linha_do_resumo()
    {
        // O 12,5% da convenção de unidades não é dado da empresa.
        var texto = "- A receita subiu 12,5% no mês.\n- O caixa caiu R$ 120,5 mil, como na tela.";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Equal("- O caixa caiu R$ 120,5 mil, como na tela.", resultado.Texto);
        Assert.Contains("valor sem link e sem origem nas ferramentas", resultado.Motivo);
    }

    [Fact]
    public void Datas_e_contagens_nao_sao_valores()
    {
        var texto = "Resumo de set/2026 (2026-09, até 30/09/2026): 3 de 4 pilares em atenção nos últimos 12 meses.";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Null(resultado.Motivo);
        Assert.Equal(texto, resultado.Texto);
    }

    [Fact]
    public void Titulo_que_fica_sem_nada_sai_junto()
    {
        var texto =
            "### Caixa\n- Caixa de [R$ 9,99 mi](card:dfc/caixaFinal).\n\n" +
            "### Balanço\n- Ativo de [R$ 10,5 mi](card:balanco/ativo)." + Opcoes;

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Equal("### Balanço\n- Ativo de [R$ 10,5 mi](card:balanco/ativo)." + Opcoes, resultado.Texto);
    }

    [Fact]
    public void Blocos_de_opcoes_e_grafico_nao_sao_conferidos()
    {
        var texto = "Só texto." + Opcoes;

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Null(resultado.Motivo);
        Assert.Equal(texto, resultado.Texto);
    }

    [Fact]
    public void Alvo_fora_do_padrao_vira_a_forma_que_a_tela_le()
    {
        var texto =
            "- Liquidez em [1,42] (card:indicador/LIQ_CORRENTE) e em [1,38](indicador/LIQ_CORRENTE) antes.\n" +
            "- Receita de [R$ 2,5 mi](resultado:receitaLiquida), saúde em [Atenção](Veredito).";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Null(resultado.Motivo);
        Assert.Equal(
            "- Liquidez em [1,42](card:indicador/LIQ_CORRENTE) e em [1,38](card:indicador/LIQ_CORRENTE) antes.\n" +
            "- Receita de [R$ 2,5 mi](card:resultado/receitaLiquida), saúde em [Atenção](card:veredito).",
            resultado.Texto);
    }

    [Fact]
    public void A_cascata_e_a_DFC_sao_um_card_so()
    {
        // Na tela, a receita e o EBITDA estão no mesmo card; o valor sem classificação está no card da DFC.
        var texto = "- [R$ 2,5 mi](card:resultado/ebitda) e [R$ 500](card:dfc/variacaoCaixa).";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Null(resultado.Motivo);
    }

    [Fact]
    public void Alvo_no_lugar_do_texto_do_link_vira_ver()
    {
        var texto = "- Caixa em Atenção ([card:pilar/caixa](card:pilar/caixa)); saúde geral em **Atenção** ([veredito](card:veredito)).";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Equal("- Caixa em Atenção ([ver](card:pilar/caixa)); saúde geral em **Atenção** ([ver](card:veredito)).", resultado.Texto);
    }

    [Fact]
    public void A_saude_geral_mostra_os_pilares()
    {
        var texto = "- Saúde geral em [Atenção](card:veredito), com caixa de [R$ 3,56 mi](card:veredito).\n- Liquidez de [1,42](card:veredito).";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Equal("- Saúde geral em [Atenção](card:veredito), com caixa de [R$ 3,56 mi](card:veredito).", resultado.Texto);
    }

    [Fact]
    public void O_balanco_confere_pelo_lado()
    {
        var texto =
            "- Circulante de [R$ 6 mi](card:balanco/ativo) e PL de [R$ 4,2 mi](card:balanco/passivo).\n" +
            "- PL de [R$ 4,2 mi](card:balanco/ativoCirculante).";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: true);

        Assert.Equal("- Circulante de [R$ 6 mi](card:balanco/ativo) e PL de [R$ 4,2 mi](card:balanco/passivo).", resultado.Texto);
        Assert.Contains("balanco/ativoCirculante: número sem origem no card", resultado.Motivo);
    }

    [Fact]
    public void Contagem_de_empresas_da_conciliacao_e_dado_mas_a_coligada_nao()
    {
        const string PorEmpresa = """
            {"anoMes":"2026-09","totalDeEmpresas":3,"itens":[{"empresa":"A","pendentes":12,"backlogTotal":20}],
             "demais":{"quantidadeDeEmpresas":2,"pendentes":4,"backlogTotal":5}}
            """;
        var texto = "- São [3 empresas](card:conciliacao/empresas), a maior com [12](card:conciliacao/empresas) pendentes.\n" +
            "- Coligada [17](card:indicador/LIQ_CORRENTE).";

        var resultado = ValidadorReferenciasTati.Validar(
            texto, [.. DoResumo, new(ConsultarBacklogPorEmpresa.NomeFerramenta, PorEmpresa)], estrito: true);

        Assert.Equal("- São [3 empresas](card:conciliacao/empresas), a maior com [12](card:conciliacao/empresas) pendentes.", resultado.Texto);
    }

    [Fact]
    public void Fora_do_resumo_o_link_que_nao_confere_vira_texto()
    {
        var texto = "A liquidez está em [9,99](card:indicador/LIQ_CORRENTE) e a margem em [35,2%](card:indicador/MARGEM_BRUTA).";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: false);

        Assert.Equal("A liquidez está em 9,99 e a margem em [35,2%](card:indicador/MARGEM_BRUTA).", resultado.Texto);
        Assert.Contains("link desfeito (indicador/LIQ_CORRENTE: número sem origem no card)", resultado.Motivo);
    }

    [Fact]
    public void Fora_do_resumo_resposta_sem_link_nao_muda()
    {
        var texto = "A receita subiu 12,5% no mês, segundo a conta que eu fiz.";

        var resultado = ValidadorReferenciasTati.Validar(texto, DoResumo, estrito: false);

        Assert.Null(resultado.Motivo);
        Assert.Equal(texto, resultado.Texto);
    }
}
