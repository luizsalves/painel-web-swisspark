using System.Globalization;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic.Indicadores;

public enum UnidadeIndicador { Numero, Percentual, Multiplo, Moeda }

/// <summary>Um indicador da visão do contador: textos, unidade e faixa.</summary>
public sealed record ConfigIndicador(
    string Codigo, string Grupo, string Nome, string Formula, UnidadeIndicador Unidade, int Casas,
    bool MaiorMelhor, double Risco, double Bom, bool StatusAnualizado = false);

/// <summary>O indicador calculado. Valor nulo quando não dá para calcular ou quando vale o texto.</summary>
public sealed record ResultadoIndicador(
    ConfigIndicador Config, double? Valor, double? ValorFaixa, string Status,
    string? TextoValor = null, string? DetalheValor = null, string? Nota = null);

public sealed record PilarIndicadores(
    string Codigo, string Nome, string Status, string Valor, string Frase, bool SemDividaLiquida = false);

public sealed record AlertaIndicadores(string Severidade, string Texto);

public sealed record TotaisDfc(double Operacionais, double NaoClassificadas, double SaldoInicial, double SaldoFinal);

public sealed record AvaliacaoIndicadores(
    IReadOnlyList<ResultadoIndicador> Indicadores, IReadOnlyList<PilarIndicadores> Pilares, string StatusGeral,
    string Veredito, IReadOnlyList<AlertaIndicadores> Alertas, IReadOnlyList<string> PontosAVerificar);

/// <summary>
/// As regras da tela de Indicadores no backend, para a CFO Tati dizer o mesmo que a tela: é uma
/// cópia fiel de frontend/src/paginas/Indicadores/config.ts e regras.ts (faixas, direção, status,
/// casos especiais, pilares, veredito, alertas e pontos a verificar). As duas pontas ainda não leem
/// da mesma fonte; mudar uma regra aqui exige mudar lá também. Sem banco: só recebe a base do
/// <see cref="IndicadoresService"/>. Calcula em double, como o JavaScript, para os status baterem.
/// </summary>
public static class RegrasIndicadores
{
    public const string Bom = "bom", Atencao = "atencao", Risco = "risco", Verificar = "verificar";

    private const string Liquidez = "Liquidez", Endividamento = "Endividamento", Rentabilidade = "Rentabilidade e resultado";

    /// <summary>Os 13 indicadores, na ordem da tabela do contador. Percentuais em fração (0,72 = 72%).</summary>
    public static IReadOnlyList<ConfigIndicador> Configuracao { get; } =
    [
        new("LIQ_CORRENTE", Liquidez, "Liquidez corrente", "Ativo circulante ÷ Passivo circulante",
            UnidadeIndicador.Numero, 2, MaiorMelhor: true, Risco: 1.0, Bom: 1.5),
        new("LIQ_SECA", Liquidez, "Liquidez seca", "(Ativo circulante − Estoques) ÷ Passivo circulante",
            UnidadeIndicador.Numero, 2, MaiorMelhor: true, Risco: 0.5, Bom: 1.0),
        new("LIQ_IMEDIATA", Liquidez, "Liquidez imediata", "Disponível ÷ Passivo circulante",
            UnidadeIndicador.Numero, 2, MaiorMelhor: true, Risco: 0.1, Bom: 0.3),
        new("LIQ_GERAL", Liquidez, "Liquidez geral",
            "(Ativo circulante + Realizável a longo prazo) ÷ (Passivo circulante + Passivo não circulante)",
            UnidadeIndicador.Numero, 2, MaiorMelhor: true, Risco: 1.0, Bom: 1.2),
        new("END_GERAL", Endividamento, "Endividamento geral", "Passivo total ÷ Ativo total",
            UnidadeIndicador.Percentual, 2, MaiorMelhor: false, Risco: 0.8, Bom: 0.6),
        new("END_CURTO_PRAZO", Endividamento, "Dívida no curto prazo",
            "Passivo circulante ÷ Passivo total (composição do endividamento)",
            UnidadeIndicador.Percentual, 2, MaiorMelhor: false, Risco: 0.7, Bom: 0.4),
        new("DIV_LIQ_EBITDA_12M", Endividamento, "Dív. líquida / EBITDA 12m",
            "(Dívida bruta − Caixa) ÷ EBITDA de 12 meses (EBITDA do período × 12 ÷ meses do período)",
            UnidadeIndicador.Multiplo, 2, MaiorMelhor: false, Risco: 4.0, Bom: 2.5),
        new("DIV_BRUTA_PL", Endividamento, "Dívida bruta / PL", "Dívida bruta ÷ Patrimônio líquido",
            UnidadeIndicador.Multiplo, 2, MaiorMelhor: false, Risco: 2.0, Bom: 1.0),
        new("ROE", Rentabilidade, "ROE", "Lucro líquido ÷ Patrimônio líquido",
            UnidadeIndicador.Percentual, 2, MaiorMelhor: true, Risco: 0, Bom: 0.12, StatusAnualizado: true),
        new("ROA", Rentabilidade, "ROA", "Lucro líquido ÷ Ativo total",
            UnidadeIndicador.Percentual, 2, MaiorMelhor: true, Risco: 0, Bom: 0.04, StatusAnualizado: true),
        new("MARGEM_LIQUIDA", Rentabilidade, "Margem líquida", "Lucro líquido ÷ Receita líquida",
            UnidadeIndicador.Percentual, 2, MaiorMelhor: true, Risco: 0, Bom: 0.1),
        new("MARGEM_BRUTA", Rentabilidade, "Margem bruta", "Lucro bruto ÷ Receita líquida",
            UnidadeIndicador.Percentual, 2, MaiorMelhor: true, Risco: 0.2, Bom: 0.4),
        new("EBITDA", Rentabilidade, "EBITDA", "EBIT + Depreciação + Amortização",
            UnidadeIndicador.Moeda, 1, MaiorMelhor: true, Risco: 0, Bom: 0),
    ];

    private static readonly Dictionary<string, ConfigIndicador> PorCodigo =
        Configuracao.ToDictionary(config => config.Codigo, StringComparer.Ordinal);

    /// <summary>Do melhor para o pior: o status geral é o pior entre os pilares.</summary>
    private static readonly Dictionary<string, int> Gravidade = new(StringComparer.Ordinal)
    {
        [Bom] = 0, [Verificar] = 1, [Atencao] = 2, [Risco] = 3,
    };

    private static readonly Dictionary<string, int> OrdemSeveridade = new(StringComparer.Ordinal)
    {
        ["risco"] = 0, ["atencao"] = 1, ["info"] = 2,
    };

    private const int MaximoDeAlertas = 5;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    // ── Formatação, igual à do front ───────────────────────────────────────

    public static string Numero(double valor, int casas) => valor.ToString($"N{casas}", PtBr);

    /// <summary>Sinal de menos comum ("-"), como no resto do painel.</summary>
    private static string ComSinal(double valor, string texto, bool mostrarMais = false) =>
        valor < 0 ? $"-{texto}" : mostrarMais && valor > 0 ? $"+{texto}" : texto;

    /// <summary>R$ em mil com uma casa: "1.036,2 mil".</summary>
    public static string FormatarMil(double valor) => ComSinal(valor, $"{Numero(Math.Abs(valor) / 1000, 1)} mil");

    /// <summary>R$ abreviado: "R$ 984,6 mil", "R$ 3,56 mi". Cada valor escolhe a sua escala.</summary>
    public static string FormatarMoedaAbreviada(double valor, bool simbolo = true)
    {
        var absoluto = Math.Abs(valor);
        var corpo = absoluto >= 1_000_000 ? $"{Numero(absoluto / 1_000_000, 2)} mi"
            : absoluto >= 1_000 ? $"{Numero(absoluto / 1_000, 1)} mil"
            : Numero(absoluto, 2);
        return ComSinal(valor, simbolo ? $"R$ {corpo}" : corpo);
    }

    /// <summary>Valor na unidade do indicador: "1,02", "71,79%", "3,52x", "84,1 mil".</summary>
    public static string FormatarValor(double? valor, ConfigIndicador config)
    {
        if (valor is not { } v || !double.IsFinite(v)) return "—";
        return config.Unidade switch
        {
            UnidadeIndicador.Percentual => $"{Numero(v * 100, config.Casas)}%",
            UnidadeIndicador.Multiplo => $"{Numero(v, config.Casas)}x",
            UnidadeIndicador.Moeda => FormatarMil(v),
            _ => Numero(v, config.Casas),
        };
    }

    /// <summary>O que a tela mostra no lugar do número: o texto ("Caixa líquido") ou o valor formatado.</summary>
    public static string TextoDoValor(ResultadoIndicador indicador) =>
        indicador.TextoValor ?? FormatarValor(indicador.Valor, indicador.Config);

    private static string PalavraPeriodo(int meses) => meses == 1 ? "mês" : "período";

    // ── Status e cálculo ───────────────────────────────────────────────────

    public static string CalcularStatus(double? valor, ConfigIndicador config)
    {
        if (valor is not { } v || !double.IsFinite(v)) return Verificar;
        if (config.MaiorMelhor)
        {
            if (v < config.Risco) return Risco;
            return v >= config.Bom ? Bom : Atencao;
        }
        if (v > config.Risco) return Risco;
        return v <= config.Bom ? Bom : Atencao;
    }

    private static double? Dividir(double numerador, double denominador) =>
        denominador == 0 ? null : numerador / denominador;

    /// <summary>Resultado antes do IR e CS (inclui equivalência e o que mais não entra no EBIT).</summary>
    public static double ResultadoAntesDoIr(BaseCalculoIndicadoresViewModel b) =>
        (double)b.LucroLiquido + (double)b.ImpostoRendaContribuicao;

    /// <summary>Custo zerado com estoque: a margem bruta sairia 100% sem ser verdade.</summary>
    public static bool CustosZeradosComEstoque(BaseCalculoIndicadoresViewModel b) => b.Custos == 0 && b.Estoques > 0;

    /// <summary>Os 13 indicadores do período, com os casos especiais da tela.</summary>
    public static IReadOnlyList<ResultadoIndicador> Calcular(BaseCalculoIndicadoresViewModel b, int meses)
    {
        var ativoTotal = (double)b.AtivoTotal;
        var ativoCirculante = (double)b.AtivoCirculante;
        var disponivel = (double)b.Disponivel;
        var estoques = (double)b.Estoques;
        var realizavelLongoPrazo = (double)b.RealizavelLongoPrazo;
        var passivoCirculante = (double)b.PassivoCirculante;
        var passivoTotal = passivoCirculante + (double)b.PassivoNaoCirculante;
        var patrimonioLiquido = (double)b.PatrimonioLiquido;
        var dividaBruta = (double)b.DividaBruta;
        var dividaLiquida = (double)b.DividaLiquida;
        var receitaLiquida = (double)b.ReceitaLiquida;
        var lucroBruto = (double)b.LucroBruto;
        var lucroLiquido = (double)b.LucroLiquido;
        var ebitda = (double)b.Ebitda;
        var periodo = PalavraPeriodo(meses);
        double? Anualizar(double? valor) => valor is { } v ? v * 12 / meses : null;

        ResultadoIndicador Resultado(
            string codigo, double? valor, string? status = null, bool faixaInformada = false, double? valorFaixa = null,
            string? textoValor = null, string? detalheValor = null, string? nota = null)
        {
            var config = PorCodigo[codigo];
            var faixa = faixaInformada ? valorFaixa : config.StatusAnualizado ? Anualizar(valor) : valor;
            return new ResultadoIndicador(config, valor, faixa, status ?? CalcularStatus(faixa, config), textoValor, detalheValor, nota);
        }

        var ebitda12m = ebitda * 12 / meses;
        var dividaLiquidaEbitda = dividaLiquida <= 0
            ? Resultado("DIV_LIQ_EBITDA_12M", null, Bom, faixaInformada: true, valorFaixa: 0,
                textoValor: "Caixa líquido", detalheValor: FormatarMoedaAbreviada(-dividaLiquida))
            : ebitda12m <= 0
                ? Resultado("DIV_LIQ_EBITDA_12M", null, Risco, faixaInformada: true, textoValor: "EBITDA negativo")
                : Resultado("DIV_LIQ_EBITDA_12M", dividaLiquida / ebitda12m);

        var dividaBrutaPl = dividaBruta <= 0
            ? Resultado("DIV_BRUTA_PL", null, Bom, faixaInformada: true, valorFaixa: 0, textoValor: "Sem dívida")
            : patrimonioLiquido <= 0
                ? Resultado("DIV_BRUTA_PL", null, Risco, faixaInformada: true, textoValor: "PL negativo")
                : Resultado("DIV_BRUTA_PL", dividaBruta / patrimonioLiquido);

        string? AnualTexto(double? valor) => meses == 12 || valor is not { } v
            ? null
            : $"≈ {Numero(v * 12 / meses * 100, 1)}% ao ano";
        var roe = Dividir(lucroLiquido, patrimonioLiquido);
        var roa = Dividir(lucroLiquido, ativoTotal);

        var fatiaEstoques = Dividir(estoques, ativoCirculante);
        var lair = ResultadoAntesDoIr(b);
        var margemBruta = Dividir(lucroBruto, receitaLiquida);

        return
        [
            Resultado("LIQ_CORRENTE", Dividir(ativoCirculante, passivoCirculante)),
            Resultado("LIQ_SECA", Dividir(ativoCirculante - estoques, passivoCirculante),
                nota: fatiaEstoques is >= 0.5 ? $"Estoques são {Numero(fatiaEstoques.Value * 100, 0)}% do ativo circulante" : null),
            Resultado("LIQ_IMEDIATA", Dividir(disponivel, passivoCirculante)),
            Resultado("LIQ_GERAL", Dividir(ativoCirculante + realizavelLongoPrazo, passivoTotal)),
            Resultado("END_GERAL", Dividir(passivoTotal, ativoTotal)),
            Resultado("END_CURTO_PRAZO", Dividir(passivoCirculante, passivoTotal)),
            dividaLiquidaEbitda,
            dividaBrutaPl,
            Resultado("ROE", roe, detalheValor: AnualTexto(roe)),
            Resultado("ROA", roa, detalheValor: AnualTexto(roa)),
            Resultado("MARGEM_LIQUIDA", Dividir(lucroLiquido, receitaLiquida),
                nota: lucroLiquido < 0 && lair > 0 ? "IR e CS acima do resultado antes dos impostos" : null),
            CustosZeradosComEstoque(b)
                ? Resultado("MARGEM_BRUTA", margemBruta, Verificar, faixaInformada: true,
                    nota: $"Custos zerados no {periodo}: conferir o de-para")
                : Resultado("MARGEM_BRUTA", margemBruta),
            Resultado("EBITDA", ebitda),
        ];
    }

    // ── DFC, pontos a verificar, alertas, pilares e veredito ───────────────

    public static TotaisDfc TotaisDaDfc(DfcViewModel dfc)
    {
        double Total(string grupo) => (double)dfc.Grupos.Where(g => g.Nome == grupo).Sum(g => g.Total);
        return new TotaisDfc(
            Total("Atividades Operacionais"), Total("Variações não classificadas"),
            (double)dfc.SaldoInicialCaixa, (double)dfc.SaldoFinalCaixa);
    }

    /// <summary>O que o contador deve conferir antes de fechar: custos zerados e DFC sem classificação.</summary>
    public static List<string> PontosAVerificar(BaseCalculoIndicadoresViewModel b, TotaisDfc dfc)
    {
        var pontos = new List<string>();
        if (CustosZeradosComEstoque(b))
            pontos.Add($"Custos zerados com {FormatarMoedaAbreviada((double)b.Estoques)} em estoque");
        if (dfc.NaoClassificadas != 0)
            pontos.Add($"{FormatarMoedaAbreviada(Math.Abs(dfc.NaoClassificadas))} na DFC sem classificação");
        return pontos;
    }

    private static ResultadoIndicador Indicador(IReadOnlyList<ResultadoIndicador> indicadores, string codigo) =>
        indicadores.First(indicador => indicador.Config.Codigo == codigo);

    /// <summary>Até 5 alertas, do mais grave ao menos grave, com os valores na frase.</summary>
    public static List<AlertaIndicadores> GerarAlertas(
        BaseCalculoIndicadoresViewModel b, IReadOnlyList<ResultadoIndicador> indicadores, TotaisDfc dfc, int meses)
    {
        var alertas = new List<AlertaIndicadores>();
        var periodo = PalavraPeriodo(meses);
        string Reais(double valor) => FormatarMoedaAbreviada(valor);
        var ce = Indicador(indicadores, "END_CURTO_PRAZO").Valor;
        var lc = Indicador(indicadores, "LIQ_CORRENTE").Valor;
        var lair = ResultadoAntesDoIr(b);
        var lucroLiquido = (double)b.LucroLiquido;
        var passivoCirculante = (double)b.PassivoCirculante;
        var disponivel = (double)b.Disponivel;

        if (ce is > 0.7 && lc is < 1.5 && passivoCirculante > 0)
        {
            var cobertura = Numero(disponivel / passivoCirculante * 100, 0);
            alertas.Add(new(Risco,
                $"{Reais(passivoCirculante)} em obrigações vencem em até 12 meses, e o caixa de {Reais(disponivel)} cobre {cobertura}% disso."));
        }
        if (lucroLiquido < 0 && lair > 0)
        {
            alertas.Add(new(Atencao,
                $"O {periodo} fechou com prejuízo de {Reais(-lucroLiquido)} porque o IR e CS de {Reais((double)b.ImpostoRendaContribuicao)} superou o resultado antes dos impostos, de {Reais(lair)}."));
        }
        else if (lucroLiquido < 0)
        {
            alertas.Add(new(Risco, $"O {periodo} fechou com prejuízo de {Reais(-lucroLiquido)}."));
        }
        if (lc is < 1)
        {
            alertas.Add(new(Risco, "O ativo de curto prazo não cobre o passivo de curto prazo."));
        }
        var multiplo = Indicador(indicadores, "DIV_LIQ_EBITDA_12M").Valor;
        if (multiplo is > 4)
        {
            alertas.Add(new(Risco, $"A dívida líquida equivale a {Numero(multiplo.Value, 1)} anos de geração de caixa da operação."));
        }
        if (dfc.NaoClassificadas != 0)
        {
            alertas.Add(new("info",
                $"{Reais(Math.Abs(dfc.NaoClassificadas))} da variação de caixa ainda estão sem classificação na contabilidade."));
        }

        // OrderBy é estável: dentro da mesma severidade fica a ordem das regras.
        return alertas.OrderBy(alerta => OrdemSeveridade[alerta.Severidade]).Take(MaximoDeAlertas).ToList();
    }

    /// <summary>O motivo principal do resultado, seguindo a DRE de cima para baixo.</summary>
    private static string MotivoDoResultado(BaseCalculoIndicadoresViewModel b)
    {
        var lucroLiquido = (double)b.LucroLiquido;
        var receitaLiquida = (double)b.ReceitaLiquida;
        if (lucroLiquido >= 0)
        {
            return receitaLiquida > 0
                ? $"Lucro de {Numero(lucroLiquido / receitaLiquida * 100, 1)}% da receita"
                : "Resultado sem receita no período";
        }
        if ((double)b.Ebitda < 0) return "A operação ficou negativa";
        if ((double)b.Ebit < 0) return "A depreciação levou ao prejuízo";
        if ((double)b.Ebit + (double)b.ResultadoFinanceiro < 0) return "Juros e resultado financeiro levaram ao prejuízo";
        if (ResultadoAntesDoIr(b) > 0) return "Prejuízo veio do IR e CS";
        return "Prejuízo antes dos impostos";
    }

    public static List<PilarIndicadores> MontarPilares(
        BaseCalculoIndicadoresViewModel b, IReadOnlyList<ResultadoIndicador> indicadores, TotaisDfc dfc, int meses)
    {
        var periodo = PalavraPeriodo(meses);
        var lucroLiquido = (double)b.LucroLiquido;
        var margem = Indicador(indicadores, "MARGEM_LIQUIDA");
        var statusResultado = margem.Valor is not null
            ? margem.Status
            : lucroLiquido > 0 ? Bom : lucroLiquido < 0 ? Risco : Atencao;

        var variacaoCaixa = dfc.SaldoFinal - dfc.SaldoInicial;
        var statusCaixa = dfc.Operacionais > 0 ? Bom : dfc.Operacionais < 0 ? Risco : Atencao;

        var multiplo = Indicador(indicadores, "DIV_LIQ_EBITDA_12M");
        var dividaLiquida = (double)b.DividaLiquida;
        var semDividaLiquida = dividaLiquida <= 0;
        var valorDivida = semDividaLiquida
            ? "Sem dívida líquida"
            : multiplo.Valor is { } m ? $"{Numero(m, 1)}x o EBITDA" : "EBITDA negativo";
        var fraseDivida = semDividaLiquida
            ? $"Caixa supera a dívida em {FormatarMoedaAbreviada(-dividaLiquida)}"
            : $"Dívida líquida de {FormatarMoedaAbreviada(dividaLiquida)}";

        var ce = Indicador(indicadores, "END_CURTO_PRAZO").Valor;
        var liquidez = Indicador(indicadores, "LIQ_CORRENTE");

        return
        [
            new("resultado", "Resultado", statusResultado, FormatarMoedaAbreviada(lucroLiquido), MotivoDoResultado(b)),
            new("caixa", "Caixa", statusCaixa, FormatarMoedaAbreviada(dfc.SaldoFinal),
                variacaoCaixa == 0
                    ? $"Ficou estável no {periodo}"
                    : $"{(variacaoCaixa > 0 ? "Subiu" : "Caiu")} {FormatarMoedaAbreviada(Math.Abs(variacaoCaixa))} no {periodo}"),
            new("divida", "Dívida", multiplo.Status, valorDivida, fraseDivida, semDividaLiquida),
            new("curtoPrazo", "Contas de curto prazo", liquidez.Status, FormatarValor(liquidez.Valor, liquidez.Config),
                ce is { } c ? $"{Numero(c * 100, 1)}% do passivo vence em até 12 meses" : "Sem passivo registrado"),
        ];
    }

    private static string FraseDoPilar(PilarIndicadores pilar, string periodo) => (pilar.Codigo, pilar.Status) switch
    {
        ("resultado", Bom) => $"o {periodo} deu lucro",
        ("resultado", Atencao) => "o resultado ficou perto de zero",
        ("resultado", Risco) => $"o {periodo} fechou com prejuízo",
        ("resultado", _) => "o resultado precisa ser conferido",
        ("caixa", Bom) => "a operação gera caixa",
        ("caixa", Atencao) => "a operação não gerou caixa",
        ("caixa", Risco) => "a operação consumiu caixa",
        ("caixa", _) => "o caixa precisa ser conferido",
        ("divida", Bom) => pilar.SemDividaLiquida ? "a empresa não tem dívida líquida" : "a dívida está sob controle",
        ("divida", Atencao) => "a dívida pede atenção",
        ("divida", Risco) => "a dívida está alta para a geração de caixa",
        ("divida", _) => "a dívida precisa ser conferida",
        (_, Bom) => "as contas de curto prazo estão cobertas",
        (_, Atencao) => "a folga para as contas de curto prazo é pequena",
        (_, Risco) => "o ativo de curto prazo não cobre as contas do próximo ano",
        _ => "as contas de curto prazo precisam ser conferidas",
    };

    private static string Juntar(IReadOnlyList<string> partes) => partes.Count <= 1
        ? partes.FirstOrDefault() ?? string.Empty
        : $"{string.Join(", ", partes.Take(partes.Count - 1))} e {partes[^1]}";

    /// <summary>
    /// Status geral (o pior entre os pilares) e a frase: primeiro o que está bem, depois "mas"
    /// e o que preocupa, terminando no mais grave.
    /// </summary>
    public static (string Status, string Frase) GerarVeredito(IReadOnlyList<PilarIndicadores> pilares, int meses)
    {
        var periodo = PalavraPeriodo(meses);
        var bons = pilares.Where(pilar => pilar.Status == Bom).ToList();
        var ruins = pilares.Where(pilar => pilar.Status != Bom)
            .Select((pilar, ordem) => (pilar, ordem))
            .OrderBy(item => Gravidade[item.pilar.Status]).ThenBy(item => item.ordem)
            .Select(item => item.pilar)
            .ToList();

        string Texto(List<PilarIndicadores> lista) => Juntar(lista.Select(pilar => FraseDoPilar(pilar, periodo)).ToList());
        var frase = bons.Count > 0 && ruins.Count > 0
            ? $"{Texto(bons)}, mas {Texto(ruins)}."
            : $"{Texto(bons.Count > 0 ? bons : ruins)}.";

        var status = pilares.Select(pilar => pilar.Status)
            .Aggregate(Bom, (pior, atual) => Gravidade[atual] > Gravidade[pior] ? atual : pior);
        return (status, char.ToUpper(frase[0], PtBr) + frase[1..]);
    }

    /// <summary>Tudo o que a tela mostra para a base e a DFC do período, num lugar só.</summary>
    public static AvaliacaoIndicadores Avaliar(BaseCalculoIndicadoresViewModel b, DfcViewModel dfc, int meses)
    {
        var indicadores = Calcular(b, meses);
        var totais = TotaisDaDfc(dfc);
        var pilares = MontarPilares(b, indicadores, totais, meses);
        var (status, veredito) = GerarVeredito(pilares, meses);
        return new AvaliacaoIndicadores(
            indicadores, pilares, status, veredito, GerarAlertas(b, indicadores, totais, meses), PontosAVerificar(b, totais));
    }
}
