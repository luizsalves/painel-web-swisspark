using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic.Indicadores;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// Os mesmos casos e os mesmos valores esperados de frontend/src/paginas/Indicadores/regras.test.ts:
/// se a tela e a Tati divergirem, um dos dois lados quebra.
/// </summary>
public class RegrasIndicadoresTests
{
    // AEPL Participações (coligada 17), mar/2026 — o caso de teste combinado com a contabilidade.
    internal static BaseCalculoIndicadoresViewModel BaseAepl() => new()
    {
        AtivoTotal = 34_087_932.98m,
        AtivoCirculante = 24_960_794.97m,
        Disponivel = 984_563.27m,
        Estoques = 20_392_081.09m,
        RealizavelLongoPrazo = 4_787_803.87m,
        PassivoCirculante = 24_355_366.24m,
        PassivoNaoCirculante = 116_549.27m,
        PatrimonioLiquido = 9_616_017.47m,
        DividaBruta = 4_541_150.68m,
        DividaLiquida = 4_541_150.68m - 984_563.27m,
        ReceitaLiquida = 187_530.96m,
        Custos = 0,
        LucroBruto = 187_530.96m,
        ResultadoFinanceiro = -14_880.2m,
        ImpostoRendaContribuicao = 77_292.19m,
        LucroLiquido = -75_670.44m,
        Ebit = 16_501.95m,
        DepreciacaoAmortizacao = 67_633.78m,
        Ebitda = 16_501.95m + 67_633.78m,
    };

    internal static DfcViewModel DfcAepl() => new()
    {
        Grupos =
        [
            new() { Nome = "Atividades Operacionais", Total = 1_036_235.8m },
            new() { Nome = "Atividades de Financiamento", Total = 457_259.39m },
            new() { Nome = "Atividades de Investimentos", Total = -308_800m },
            new() { Nome = "Variações não classificadas", Total = -359_713.43m },
        ],
        SaldoInicialCaixa = 159_581.51m,
        SaldoFinalCaixa = 984_563.27m,
        VariacaoCaixa = 984_563.27m - 159_581.51m,
    };

    private static ResultadoIndicador Indicador(IReadOnlyList<ResultadoIndicador> indicadores, string codigo) =>
        indicadores.Single(i => i.Config.Codigo == codigo);

    private static ConfigIndicador Config(string codigo) => RegrasIndicadores.Configuracao.Single(c => c.Codigo == codigo);

    [Theory]
    [InlineData("LIQ_CORRENTE", "1,02", "atencao")]
    [InlineData("LIQ_SECA", "0,19", "risco")]
    [InlineData("LIQ_IMEDIATA", "0,04", "risco")]
    [InlineData("LIQ_GERAL", "1,22", "bom")]
    [InlineData("END_GERAL", "71,79%", "atencao")]
    [InlineData("END_CURTO_PRAZO", "99,52%", "risco")]
    [InlineData("DIV_LIQ_EBITDA_12M", "3,52x", "atencao")]
    [InlineData("DIV_BRUTA_PL", "0,47x", "bom")]
    [InlineData("ROE", "-0,79%", "risco")]
    [InlineData("ROA", "-0,22%", "risco")]
    [InlineData("MARGEM_LIQUIDA", "-40,35%", "risco")]
    [InlineData("MARGEM_BRUTA", "100,00%", "verificar")]
    [InlineData("EBITDA", "84,1 mil", "bom")]
    public void Caso_AEPL_marco_2026(string codigo, string valor, string status)
    {
        var indicador = Indicador(RegrasIndicadores.Calcular(BaseAepl(), 1), codigo);
        Assert.Equal(valor, RegrasIndicadores.FormatarValor(indicador.Valor, indicador.Config));
        Assert.Equal(status, indicador.Status);
    }

    [Fact]
    public void Sao_13_indicadores_na_ordem_da_tabela_e_sem_PCT()
    {
        var codigos = RegrasIndicadores.Calcular(BaseAepl(), 1).Select(i => i.Config.Codigo).ToList();
        Assert.Equal(
            ["LIQ_CORRENTE", "LIQ_SECA", "LIQ_IMEDIATA", "LIQ_GERAL", "END_GERAL", "END_CURTO_PRAZO", "DIV_LIQ_EBITDA_12M",
             "DIV_BRUTA_PL", "ROE", "ROA", "MARGEM_LIQUIDA", "MARGEM_BRUTA", "EBITDA"],
            codigos);
    }

    [Fact]
    public void Anualiza_o_EBITDA_no_multiplo_da_divida()
    {
        var indicador = Indicador(RegrasIndicadores.Calcular(BaseAepl(), 1), "DIV_LIQ_EBITDA_12M");
        Assert.Equal(3_556_587.41 / (84_135.73 * 12), indicador.Valor!.Value, 6);
    }

    [Fact]
    public void Periodo_de_3_meses_anualiza_com_12_dividido_por_3()
    {
        var indicador = Indicador(RegrasIndicadores.Calcular(BaseAepl(), 3), "DIV_LIQ_EBITDA_12M");
        Assert.Equal(3_556_587.41 / (84_135.73 * 4), indicador.Valor!.Value, 6);
    }

    [Fact]
    public void ROE_mostra_o_periodo_e_o_status_usa_o_anualizado()
    {
        var roe = Indicador(RegrasIndicadores.Calcular(BaseAepl(), 1), "ROE");
        Assert.Equal("≈ -9,4% ao ano", roe.DetalheValor);
        Assert.Equal(roe.Valor!.Value * 12, roe.ValorFaixa!.Value, 10);
    }

    [Fact]
    public void Notas_da_liquidez_seca_da_margem_bruta_e_da_margem_liquida()
    {
        var indicadores = RegrasIndicadores.Calcular(BaseAepl(), 1);
        Assert.Equal("Estoques são 82% do ativo circulante", Indicador(indicadores, "LIQ_SECA").Nota);
        Assert.Equal("Custos zerados no mês: conferir o de-para", Indicador(indicadores, "MARGEM_BRUTA").Nota);
        Assert.Equal("IR e CS acima do resultado antes dos impostos", Indicador(indicadores, "MARGEM_LIQUIDA").Nota);
    }

    [Fact]
    public void Divida_liquida_menor_ou_igual_a_zero_vira_caixa_liquido_bom()
    {
        var b = BaseAepl();
        b.DividaBruta = 500_000m;
        b.DividaLiquida = 500_000m - 984_563.27m;
        var indicador = Indicador(RegrasIndicadores.Calcular(b, 1), "DIV_LIQ_EBITDA_12M");
        Assert.Null(indicador.Valor);
        Assert.Equal("bom", indicador.Status);
        Assert.Equal("Caixa líquido", indicador.TextoValor);
        Assert.Equal("R$ 484,6 mil", indicador.DetalheValor);
    }

    [Fact]
    public void EBITDA_negativo_com_divida_liquida_vira_EBITDA_negativo_risco()
    {
        var b = BaseAepl();
        b.Ebit = -80_000m;
        b.Ebitda = -12_366.22m;
        var indicador = Indicador(RegrasIndicadores.Calcular(b, 1), "DIV_LIQ_EBITDA_12M");
        Assert.Equal("risco", indicador.Status);
        Assert.Equal("EBITDA negativo", indicador.TextoValor);
    }

    [Fact]
    public void Divida_bruta_zero_vira_sem_divida_bom()
    {
        var b = BaseAepl();
        b.DividaBruta = 0;
        b.DividaLiquida = -984_563.27m;
        var indicador = Indicador(RegrasIndicadores.Calcular(b, 1), "DIV_BRUTA_PL");
        Assert.Equal("bom", indicador.Status);
        Assert.Equal("Sem dívida", indicador.TextoValor);
    }

    [Fact]
    public void PL_negativo_com_divida_vira_PL_negativo_risco()
    {
        var b = BaseAepl();
        b.PatrimonioLiquido = -1_000m;
        var indicador = Indicador(RegrasIndicadores.Calcular(b, 1), "DIV_BRUTA_PL");
        Assert.Null(indicador.Valor);
        Assert.Equal("risco", indicador.Status);
        Assert.Equal("PL negativo", indicador.TextoValor);
    }

    [Fact]
    public void Sem_receita_as_margens_ficam_Verificar()
    {
        var b = BaseAepl();
        b.ReceitaLiquida = 0;
        b.LucroBruto = 0;
        b.Estoques = 0;
        var indicadores = RegrasIndicadores.Calcular(b, 1);
        Assert.Equal("verificar", Indicador(indicadores, "MARGEM_LIQUIDA").Status);
        Assert.Equal("verificar", Indicador(indicadores, "MARGEM_BRUTA").Status);
    }

    [Theory]
    [InlineData(1.5, "bom")]
    [InlineData(1.0, "atencao")]
    [InlineData(0.99, "risco")]
    public void Maior_e_melhor_o_limite_de_bom_ja_e_bom(double valor, string status) =>
        Assert.Equal(status, RegrasIndicadores.CalcularStatus(valor, Config("LIQ_CORRENTE")));

    [Theory]
    [InlineData(0.6, "bom")]
    [InlineData(0.8, "atencao")]
    [InlineData(0.81, "risco")]
    public void Menor_e_melhor_acima_do_risco_e_risco(double valor, string status) =>
        Assert.Equal(status, RegrasIndicadores.CalcularStatus(valor, Config("END_GERAL")));

    [Fact]
    public void Sem_valor_e_Verificar() =>
        Assert.Equal("verificar", RegrasIndicadores.CalcularStatus(null, Config("LIQ_CORRENTE")));

    [Theory]
    [InlineData(984_563.27, true, "R$ 984,6 mil")]
    [InlineData(3_556_587.41, true, "R$ 3,56 mi")]
    [InlineData(-75_670.44, false, "-75,7 mil")]
    public void Abrevia_reais_em_mil_e_mi(double valor, bool simbolo, string esperado) =>
        Assert.Equal(esperado, RegrasIndicadores.FormatarMoedaAbreviada(valor, simbolo));

    [Fact]
    public void Pilares_do_caso_AEPL()
    {
        var b = BaseAepl();
        var indicadores = RegrasIndicadores.Calcular(b, 1);
        var pilares = RegrasIndicadores.MontarPilares(b, indicadores, RegrasIndicadores.TotaisDaDfc(DfcAepl()), 1);

        Assert.Equal(
            [("resultado", "risco"), ("caixa", "bom"), ("divida", "atencao"), ("curtoPrazo", "atencao")],
            pilares.Select(p => (p.Codigo, p.Status)).ToList());
        // A tela mostra os valores sem o "R$"; para a Tati, ele vai junto.
        Assert.Equal(["-R$ 75,7 mil", "R$ 984,6 mil", "3,5x o EBITDA", "1,02"], pilares.Select(p => p.Valor).ToList());
        Assert.Equal("Prejuízo veio do IR e CS", pilares[0].Frase);
        Assert.Equal("Subiu R$ 825,0 mil no mês", pilares[1].Frase);
        Assert.Equal("99,5% do passivo vence em até 12 meses", pilares[3].Frase);
    }

    [Fact]
    public void Veredito_do_caso_AEPL_comeca_pelo_que_esta_bem()
    {
        var avaliacao = RegrasIndicadores.Avaliar(BaseAepl(), DfcAepl(), 1);
        Assert.Equal("risco", avaliacao.StatusGeral);
        Assert.Equal(
            "A operação gera caixa, mas a dívida pede atenção, a folga para as contas de curto prazo é pequena e o mês fechou com prejuízo.",
            avaliacao.Veredito);
    }

    [Fact]
    public void Veredito_sem_pilar_bom_nao_usa_mas()
    {
        var b = BaseAepl();
        var pilares = RegrasIndicadores.MontarPilares(b, RegrasIndicadores.Calcular(b, 3), RegrasIndicadores.TotaisDaDfc(DfcAepl()), 3)
            .Select(p => p with { Status = "risco" }).ToList();
        Assert.Equal(
            "O período fechou com prejuízo, a operação consumiu caixa, a dívida está alta para a geração de caixa e o ativo de curto prazo não cobre as contas do próximo ano.",
            RegrasIndicadores.GerarVeredito(pilares, 3).Frase);
    }

    [Fact]
    public void Alertas_do_caso_AEPL_do_mais_grave_ao_menos_grave()
    {
        var avaliacao = RegrasIndicadores.Avaliar(BaseAepl(), DfcAepl(), 1);
        Assert.Equal(
            [
                new AlertaIndicadores("risco", "R$ 24,36 mi em obrigações vencem em até 12 meses, e o caixa de R$ 984,6 mil cobre 4% disso."),
                new AlertaIndicadores("atencao", "O mês fechou com prejuízo de R$ 75,7 mil porque o IR e CS de R$ 77,3 mil superou o resultado antes dos impostos, de R$ 1,6 mil."),
                new AlertaIndicadores("info", "R$ 359,7 mil da variação de caixa ainda estão sem classificação na contabilidade."),
            ],
            avaliacao.Alertas);
    }

    [Fact]
    public void Pontos_a_verificar_do_caso_AEPL()
    {
        var avaliacao = RegrasIndicadores.Avaliar(BaseAepl(), DfcAepl(), 1);
        Assert.Equal(["Custos zerados com R$ 20,39 mi em estoque", "R$ 359,7 mil na DFC sem classificação"], avaliacao.PontosAVerificar);
    }
}
