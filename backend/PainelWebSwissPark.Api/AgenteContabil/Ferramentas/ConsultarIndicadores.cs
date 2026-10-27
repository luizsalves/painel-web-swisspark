using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic;
using PainelWebSwissPark.Api.Services.BusinessLogic.Indicadores;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Ferramenta que traz a saúde financeira de uma empresa num período, com os mesmos números e
/// status da tela de Indicadores: quem calcula é o <see cref="IIndicadoresService"/> que já
/// existe, e as faixas, os casos especiais, os pilares e o veredito saem da
/// <see cref="RegrasIndicadores"/>, a cópia das regras da tela. Esta classe só acha a empresa
/// pelo nome, pede também o período anterior (a comparação da tela) e monta um JSON enxuto para
/// o agente.
/// </summary>
public class ConsultarIndicadores(
    IEmpresasService empresasService, IIndicadoresService indicadoresService, IServiceScopeFactory escopos,
    ILogger<ConsultarIndicadores> logger)
{
    public const string NomeFerramenta = "consultar_indicadores";

    // Sem escapar os acentos: o JSON vai para o modelo, não para uma página.
    private static readonly JsonSerializerOptions OpcoesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Consulta a saúde financeira de uma empresa num período, com os mesmos números e status da tela " +
            "Indicadores: liquidez, endividamento, rentabilidade e resultado (13 indicadores com status Bom, Atenção, " +
            "Risco ou Verificar e as faixas), os pilares e o veredito da visão da diretoria, os alertas, os pontos a " +
            "verificar e a cascata do resultado. Cada indicador vem também com o valor do período anterior (os mesmos " +
            "meses, logo antes, como na tela) e a variação. Use para 'como está a empresa', saúde financeira, índices, " +
            "liquidez, endividamento, dívida, dívida líquida/EBITDA, rentabilidade, ROE, ROA, margens, EBITDA ou 'por " +
            "que deu prejuízo'. Não confundir com consultar_saldo, que traz o saldo em R$ de UMA conta contábil. " +
            "Aceita um período de vários meses (anoMesInicial a anoMes): use o período da tela, se houver.",
        functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                empresa = new
                {
                    type = "string",
                    description = "Nome da empresa ou parte dele; maiúsculas e acentos não importam.",
                },
                anoMes = new { type = "string", description = "Último mês do período, no formato AAAA-MM." },
                anoMesInicial = new
                {
                    type = "string",
                    description = "Primeiro mês do período, no formato AAAA-MM. Omita para um mês só.",
                },
                codColigada = new
                {
                    type = "integer",
                    description = "Código da coligada — só quando a ferramenta devolver mais de uma empresa com o mesmo nome.",
                },
                codFilial = new
                {
                    type = "integer",
                    description = "Código da filial — a que o usuário pedir ou, se ele não disser, a do contexto da tela. " +
                        "Sem ele, entram todas as filiais da coligada, como na tela com a filial \"Todas\".",
                },
                indicador = new
                {
                    type = "string",
                    description = "Só um indicador, pelo código (" +
                        string.Join(", ", RegrasIndicadores.Configuracao.Select(c => c.Codigo)) +
                        ") ou pelo nome. Omita para todos, com pilares, veredito e alertas.",
                },
            },
            required = new[] { "empresa", "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var parametros = new LocalizarEmpresa.Parametros(input);
        var empresa = parametros.Texto("empresa");
        var anoMes = parametros.Texto("anoMes");
        var anoMesInicial = parametros.Texto("anoMesInicial");
        var codColigada = parametros.Inteiro("codColigada");
        var codFilial = parametros.Inteiro("codFilial");
        var indicador = parametros.Texto("indicador");

        if (parametros.Erro is { } erroDeTipo)
        {
            return Serializar(erroDeTipo);
        }
        if (LocalizarEmpresa.ValidarParametros(
                empresa, anoMes, anoMesInicial, codColigada, codFilial, out var mesInicial, out var mesFinal) is { } erro)
        {
            return Serializar(erro);
        }

        string? codigo = null;
        if (indicador is not null && (codigo = AcharIndicador(indicador)) is null)
        {
            return Serializar(new
            {
                erro = $"Não identifiquei um indicador só com \"{indicador}\" — chame de novo com um destes códigos.",
                indicadores = RegrasIndicadores.Configuracao.Select(c => new { codigo = c.Codigo, nome = c.Nome }),
            });
        }

        var (achada, erroEmpresa) = await LocalizarEmpresa.ExecutarAsync(
            empresasService, empresa!, codColigada, codFilial, cancellationToken);
        if (achada is null)
        {
            return Serializar(erroEmpresa!);
        }
        if (LocalizarEmpresa.NoFuturo(mesFinal))
        {
            return Serializar(LocalizarEmpresa.SemDados(
                achada, LocalizarEmpresa.Periodo(mesInicial, mesFinal), LocalizarEmpresa.MotivoNoFuturo(mesFinal)));
        }

        // O período anterior da tela: os mesmos meses, logo antes do período pedido.
        var meses = LocalizarEmpresa.Meses(mesInicial, mesFinal);
        // Os dois períodos ao mesmo tempo, como as colunas da tela: o anterior num escopo próprio,
        // porque o DbContext do escopo atual não aceita duas consultas juntas.
        var cronometro = Stopwatch.StartNew();
        using var escopoAnterior = escopos.CreateScope();
        var tarefaAtual = indicadoresService.CalcularAsync(
            mesInicial, mesFinal.AddMonths(1), achada.CodColigada, achada.CodFilial, cancellationToken);
        var tarefaAnterior = escopoAnterior.ServiceProvider.GetRequiredService<IIndicadoresService>().CalcularAsync(
            mesInicial.AddMonths(-meses), mesInicial, achada.CodColigada, achada.CodFilial, cancellationToken);
        await Task.WhenAll(tarefaAtual, tarefaAnterior);
        var resultado = await tarefaAtual;
        var anterior = await tarefaAnterior;
        logger.LogInformation(
            "CFO Tati: indicadores de {Empresa} (coligada {Coligada}, {Inicio:yyyy-MM} a {Fim:yyyy-MM}, com o período anterior) " +
            "calculados em {DuracaoMs} ms",
            achada.Nome, achada.CodColigada, mesInicial, mesFinal, cronometro.ElapsedMilliseconds);

        return Serializar(MontarResposta(achada, mesInicial, mesFinal, resultado, anterior, codigo));
    }

    /// <summary>
    /// O JSON enxuto a partir do resultado do <see cref="IIndicadoresService"/>. Percentuais vão em
    /// pontos percentuais (71,79% = 71.79), como na tela; o resultado vai com o sinal da
    /// contribuição para o lucro. Com <paramref name="anterior"/>, cada indicador traz o valor do
    /// período anterior e a variação; com <paramref name="codigoIndicador"/>, só aquele indicador.
    /// </summary>
    public static object MontarResposta(
        LocalizarEmpresa.Empresa empresa, DateTime mesInicial, DateTime mesFinal,
        IndicadoresViewModel resultado, IndicadoresViewModel? anterior = null, string? codigoIndicador = null)
    {
        var meses = LocalizarEmpresa.Meses(mesInicial, mesFinal);
        var periodo = LocalizarEmpresa.Periodo(mesInicial, mesFinal);
        var b = resultado.BaseCalculo;

        if (SemDados(b))
        {
            return LocalizarEmpresa.SemDados(empresa, periodo, "Não há lançamentos dessa empresa até o fim desse período.");
        }

        var avaliacao = RegrasIndicadores.Avaliar(b, resultado.Dfc, meses);
        var lucroBruto = b.ReceitaLiquida - b.Custos;

        // Sem lançamentos no período anterior, a comparação fica nula — não vira "variação" contra zero.
        var avaliacaoAnterior = anterior is null || SemDados(anterior.BaseCalculo)
            ? null
            : RegrasIndicadores.Avaliar(anterior.BaseCalculo, anterior.Dfc, meses);
        var anteriores = avaliacaoAnterior?.Indicadores.ToDictionary(i => i.Config.Codigo, StringComparer.Ordinal);
        var periodoAnterior = anterior is null ? null : new
        {
            inicio = mesInicial.AddMonths(-meses).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            fim = mesInicial.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            meses,
            semDados = avaliacaoAnterior is null,
        };

        var indicadores = avaliacao.Indicadores
            .Where(i => codigoIndicador is null || i.Config.Codigo == codigoIndicador)
            .Select(i =>
            {
                var antes = anteriores?.GetValueOrDefault(i.Config.Codigo);
                var (variacao, sentido) = Variar(i.Valor, antes?.Valor, i.Config);
                return new
                {
                    codigo = i.Config.Codigo,
                    grupo = i.Config.Grupo,
                    nome = i.Config.Nome,
                    unidade = Unidade(i.Config),
                    valor = NaUnidade(i.Valor, i.Config),
                    valorFormatado = RegrasIndicadores.TextoDoValor(i),
                    valorAnualizado = i.Config.StatusAnualizado ? NaUnidade(i.ValorFaixa, i.Config) : null,
                    status = i.Status,
                    direcao = i.Config.MaiorMelhor ? "maior_melhor" : "menor_melhor",
                    faixas = new { risco = NaUnidade(i.Config.Risco, i.Config), bom = NaUnidade(i.Config.Bom, i.Config) },
                    formula = i.Config.Formula,
                    nota = i.Nota,
                    detalhe = i.DetalheValor,
                    valorAnterior = NaUnidade(antes?.Valor, i.Config),
                    valorFormatadoAnterior = antes is null ? null : RegrasIndicadores.TextoDoValor(antes),
                    statusAnterior = antes?.Status,
                    variacao,
                    sentido,
                };
            })
            .ToList();

        const string ConvencaoDeUnidades =
            "percentual em pontos percentuais (12.5 = 12,5%; a variação também em p.p.); multiplo em vezes; moeda em R$";

        if (codigoIndicador is not null)
        {
            return new
            {
                empresa = empresa.Nome,
                coligada = empresa.CodColigada,
                filial = empresa.CodFilial,
                abrangencia = empresa.Abrangencia,
                filiaisDaColigada = empresa.FiliaisDaColigada,
                periodo,
                periodoAnterior,
                convencaoDeUnidades = ConvencaoDeUnidades,
                indicadores,
            };
        }

        return new
        {
            empresa = empresa.Nome,
            coligada = empresa.CodColigada,
            filial = empresa.CodFilial,
            abrangencia = empresa.Abrangencia,
            filiaisDaColigada = empresa.FiliaisDaColigada,
            periodo,
            periodoAnterior,
            convencaoDeUnidades = ConvencaoDeUnidades,
            statusGeral = avaliacao.StatusGeral,
            veredito = avaliacao.Veredito,
            pilares = avaliacao.Pilares.Select(p => new { pilar = p.Nome, valorFormatado = p.Valor, status = p.Status, frase = p.Frase }),
            indicadores,
            alertas = avaliacao.Alertas.Select(a => new { severidade = a.Severidade, texto = a.Texto }),
            pontosAVerificar = avaliacao.PontosAVerificar,
            // A cascata da tela: receita, custos, despesas, EBITDA, depreciação, financeiro, outros, IR e CS, lucro.
            resultado = new
            {
                convencaoDeSinais = "contribuição para o lucro: receitas positivas; custos, despesas, depreciação e IR/CS negativos",
                receitaLiquida = Reais(b.ReceitaLiquida),
                custos = Reais(-b.Custos),
                lucroBruto = Reais(lucroBruto),
                despesasOperacionais = Reais(b.Ebitda - lucroBruto),
                ebitda = Reais(b.Ebitda),
                depreciacaoAmortizacao = Reais(b.Ebit - b.Ebitda),
                ebit = Reais(b.Ebit),
                resultadoFinanceiro = Reais(b.ResultadoFinanceiro),
                equivalenciaEOutros = Reais(b.LucroLiquido + b.ImpostoRendaContribuicao - (b.Ebit + b.ResultadoFinanceiro)),
                resultadoAntesIR = Reais(b.LucroLiquido + b.ImpostoRendaContribuicao),
                irCs = Reais(-b.ImpostoRendaContribuicao),
                lucroLiquido = Reais(b.LucroLiquido),
            },
            caixaFinal = Reais(resultado.Dfc.SaldoFinalCaixa),
        };
    }

    /// <summary>
    /// A variação como o formatarVariacao da tela: na unidade do indicador (pontos percentuais,
    /// vezes, R$), e "estavel" quando ela arredonda para zero na casa que a tela mostra.
    /// </summary>
    private static (double? Variacao, string? Sentido) Variar(double? atual, double? anterior, ConfigIndicador config)
    {
        if (atual is not { } a || anterior is not { } b || !double.IsFinite(a) || !double.IsFinite(b))
        {
            return (null, null);
        }

        var escala = config.Unidade switch { UnidadeIndicador.Percentual => 100, UnidadeIndicador.Moeda => 0.001, _ => 1 };
        // Math.Floor(x + 0,5) é o Math.round do JavaScript.
        if (Math.Floor((a - b) * escala * Math.Pow(10, config.Casas) + 0.5) == 0)
        {
            return (0, "estavel");
        }

        var diferenca = a - b;
        return (NaUnidade(diferenca, config), config.MaiorMelhor == diferenca > 0 ? "melhorou" : "piorou");
    }

    // Os códigos da tela (frontend/src/paginas/Indicadores/config.ts) e os nomes por extenso que a
    // abreviação do nome esconde ("Dív. líquida"), já sem acento, espaço e pontuação.
    private static readonly Dictionary<string, string> Sinonimos = new(StringComparer.Ordinal)
    {
        ["COMPOSICAOENDIVIDAMENTO"] = "END_CURTO_PRAZO",
        ["DIVIDALIQUIDAEBITDA"] = "DIV_LIQ_EBITDA_12M",
        ["DIVIDALIQUIDA"] = "DIV_LIQ_EBITDA_12M",
        ["DIVIDABRUTAPL"] = "DIV_BRUTA_PL",
        ["DIVIDABRUTA"] = "DIV_BRUTA_PL",
    };

    /// <summary>O código do indicador pelo código ou pelo nome; nulo se não achar ou se achar mais de um.</summary>
    private static string? AcharIndicador(string texto)
    {
        var procurado = SoLetrasENumeros(texto);
        if (Sinonimos.TryGetValue(procurado, out var sinonimo))
        {
            return sinonimo;
        }

        var exato = RegrasIndicadores.Configuracao.FirstOrDefault(c =>
            SoLetrasENumeros(c.Codigo) == procurado || SoLetrasENumeros(c.Nome) == procurado);
        if (exato is not null)
        {
            return exato.Codigo;
        }

        var parecidos = RegrasIndicadores.Configuracao
            .Where(c => procurado.Length > 0 && SoLetrasENumeros(c.Nome).Contains(procurado, StringComparison.Ordinal))
            .ToList();
        return parecidos.Count == 1 ? parecidos[0].Codigo : null;
    }

    private static string SoLetrasENumeros(string texto) =>
        string.Concat(EmpresasConsolidadas.Normalizar(texto).Where(char.IsLetterOrDigit));

    private static string Unidade(ConfigIndicador config) => config.Unidade switch
    {
        UnidadeIndicador.Percentual => "percentual",
        UnidadeIndicador.Multiplo => "multiplo",
        UnidadeIndicador.Moeda => "moeda",
        _ => "numero",
    };

    /// <summary>Na unidade da tela e com as casas dela: percentual em pontos (0,7179 → 71.79).</summary>
    private static double? NaUnidade(double? valor, ConfigIndicador config) => valor is not { } v ? null
        : config.Unidade == UnidadeIndicador.Percentual
            ? Math.Round(v * 100, config.Casas, MidpointRounding.AwayFromZero)
            : Math.Round(v, config.Unidade == UnidadeIndicador.Moeda ? 2 : config.Casas, MidpointRounding.AwayFromZero);

    private static decimal Reais(decimal valor) => LocalizarEmpresa.Reais(valor);

    /// <summary>Tudo zerado: a empresa ainda não tinha lançamentos — "sem dados", e não "Caixa líquido R$ 0".</summary>
    public static bool SemDados(BaseCalculoIndicadoresViewModel b) =>
        typeof(BaseCalculoIndicadoresViewModel).GetProperties().All(p => (decimal)p.GetValue(b)! == 0);

    private static string Serializar(object valor) => JsonSerializer.Serialize(valor, OpcoesJson);
}
