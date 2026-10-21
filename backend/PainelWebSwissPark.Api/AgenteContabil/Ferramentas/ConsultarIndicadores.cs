using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
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
/// pelo nome e monta um JSON enxuto para o agente.
/// </summary>
public class ConsultarIndicadores(
    IEmpresasService empresasService, IIndicadoresService indicadoresService, ILogger<ConsultarIndicadores> logger)
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
            "verificar e a cascata do resultado. Use para 'como está a empresa', saúde financeira, índices, liquidez, " +
            "endividamento, dívida, rentabilidade, ROE, ROA, margens, EBITDA ou 'por que deu prejuízo'. Não confundir " +
            "com consultar_saldo, que traz o saldo em R$ de UMA conta contábil. Diferente das outras ferramentas, " +
            "aceita um período de vários meses (anoMesInicial a anoMes): use o período da tela, se houver.",
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
            },
            required = new[] { "empresa", "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var empresa = input.TryGetProperty("empresa", out var empresaEl) ? empresaEl.GetString() ?? "" : "";
        var anoMes = input.TryGetProperty("anoMes", out var anoMesEl) ? anoMesEl.GetString() ?? "" : "";
        var anoMesInicial = input.TryGetProperty("anoMesInicial", out var inicialEl) && inicialEl.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(inicialEl.GetString())
                ? inicialEl.GetString()!
                : anoMes;
        short? codColigada = input.TryGetProperty("codColigada", out var coligadaEl) && coligadaEl.TryGetInt16(out var col) ? col : null;
        short? codFilial = input.TryGetProperty("codFilial", out var filialEl) && filialEl.TryGetInt16(out var fil) ? fil : null;

        if (LerMes(anoMesInicial) is not { } mesInicial || LerMes(anoMes) is not { } mesFinal)
        {
            return Serializar(new { erro = "Período inválido — use o formato AAAA-MM." });
        }
        if (mesInicial > mesFinal)
        {
            return Serializar(new { erro = "O mês inicial não pode ser depois do mês final." });
        }
        if (mesFinal > new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1))
        {
            return Serializar(new { semDados = true, erro = $"Ainda não há dados para {anoMes}: o período está no futuro." });
        }

        // O nome inteiro primeiro; só sem nenhum igual, um trecho do nome.
        var procurado = EmpresasConsolidadas.Normalizar(empresa);
        if (procurado.Length == 0)
        {
            return Serializar(new { erro = "Informe o nome da empresa." });
        }

        var filiais = await empresasService.ListarAsync(cancellationToken);
        var candidatas = filiais.Where(f => EmpresasConsolidadas.Normalizar(f.Nome) == procurado).ToList();
        if (candidatas.Count == 0)
        {
            candidatas = filiais.Where(f => EmpresasConsolidadas.Normalizar(f.Nome).Contains(procurado, StringComparison.Ordinal)).ToList();
        }
        if (codColigada is { } coligadaPedida)
        {
            candidatas = candidatas.Where(f => f.CodColigada == coligadaPedida).ToList();
        }

        // A mesma empresa aparece uma vez por filial; o nome pode se repetir em outra coligada.
        var empresas = candidatas
            .GroupBy(f => (f.CodColigada, Nome: EmpresasConsolidadas.Normalizar(f.Nome)))
            .Select(g => g.ToList())
            .ToList();

        if (empresas.Count == 0)
        {
            return Serializar(new { erro = $"Não encontrei nenhuma empresa com \"{empresa}\"." });
        }
        if (empresas.Count > 1)
        {
            return Serializar(new
            {
                erro = "Mais de uma empresa combina com esse nome — ofereça as empresas como opções numeradas e chame de " +
                    "novo com o nome completo (e codColigada, se o mesmo nome aparecer em mais de uma coligada).",
                empresas = empresas.Select(g => new { empresa = g[0].Nome, coligada = g[0].CodColigada }),
            });
        }

        var filiaisDaEmpresa = empresas[0];
        var nome = filiaisDaEmpresa[0].Nome;
        var coligada = filiaisDaEmpresa[0].CodColigada;
        var filial = codFilial is { } filialPedida ? filiaisDaEmpresa.FirstOrDefault(f => f.CodFilial == filialPedida) : null;
        if (codFilial is not null && filial is null)
        {
            return Serializar(new
            {
                erro = $"A empresa \"{nome}\" não tem a filial {codFilial}.",
                filiais = filiaisDaEmpresa.Select(f => new { f.CodFilial, f.NomeFilial }),
            });
        }

        var cronometro = Stopwatch.StartNew();
        var resultado = await indicadoresService.CalcularAsync(
            mesInicial, mesFinal.AddMonths(1), coligada, codFilial, cancellationToken);
        logger.LogInformation(
            "CFO Tati: indicadores de {Empresa} (coligada {Coligada}, {Inicio} a {Fim}) calculados em {DuracaoMs} ms",
            nome, coligada, anoMesInicial, anoMes, cronometro.ElapsedMilliseconds);

        return Serializar(MontarResposta(nome, coligada, filial, mesInicial, mesFinal, resultado));
    }

    /// <summary>
    /// O JSON enxuto a partir do resultado do <see cref="IIndicadoresService"/>. Percentuais vão em
    /// pontos percentuais (71,79% = 71.79), como na tela; o resultado vai com o sinal da
    /// contribuição para o lucro.
    /// </summary>
    public static object MontarResposta(
        string nome, short coligada, EmpresaViewModel? filial, DateTime mesInicial, DateTime mesFinal,
        IndicadoresViewModel resultado)
    {
        var meses = (mesFinal.Year - mesInicial.Year) * 12 + mesFinal.Month - mesInicial.Month + 1;
        var periodo = new
        {
            inicio = mesInicial.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            fim = mesFinal.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            meses,
        };
        var b = resultado.BaseCalculo;

        if (SemDados(b))
        {
            return new
            {
                empresa = nome, coligada, filial = filial?.CodFilial, periodo, semDados = true,
                mensagem = "Não há lançamentos dessa empresa até o fim desse período.",
            };
        }

        var avaliacao = RegrasIndicadores.Avaliar(b, resultado.Dfc, meses);
        var lucroBruto = b.ReceitaLiquida - b.Custos;

        return new
        {
            empresa = nome,
            coligada,
            filial = filial?.CodFilial,
            abrangencia = filial is null ? "todas as filiais da coligada" : filial.NomeFilial,
            periodo,
            statusGeral = avaliacao.StatusGeral,
            veredito = avaliacao.Veredito,
            pilares = avaliacao.Pilares.Select(p => new { pilar = p.Nome, valorFormatado = p.Valor, status = p.Status, frase = p.Frase }),
            indicadores = avaliacao.Indicadores.Select(i => new
            {
                codigo = i.Config.Codigo,
                grupo = i.Config.Grupo,
                nome = i.Config.Nome,
                valor = NaUnidade(i.Valor, i.Config),
                valorFormatado = RegrasIndicadores.TextoDoValor(i),
                valorAnualizado = i.Config.StatusAnualizado ? NaUnidade(i.ValorFaixa, i.Config) : null,
                status = i.Status,
                direcao = i.Config.MaiorMelhor ? "maior_melhor" : "menor_melhor",
                faixas = new { risco = NaUnidade(i.Config.Risco, i.Config), bom = NaUnidade(i.Config.Bom, i.Config) },
                formula = i.Config.Formula,
                nota = i.Nota,
                detalhe = i.DetalheValor,
            }),
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

    /// <summary>Na unidade da tela e com as casas dela: percentual em pontos (0,7179 → 71.79).</summary>
    private static double? NaUnidade(double? valor, ConfigIndicador config) => valor is not { } v ? null
        : config.Unidade == UnidadeIndicador.Percentual
            ? Math.Round(v * 100, config.Casas, MidpointRounding.AwayFromZero)
            : Math.Round(v, config.Unidade == UnidadeIndicador.Moeda ? 2 : config.Casas, MidpointRounding.AwayFromZero);

    private static decimal Reais(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    /// <summary>Tudo zerado: a empresa ainda não tinha lançamentos — "sem dados", e não "Caixa líquido R$ 0".</summary>
    private static bool SemDados(BaseCalculoIndicadoresViewModel b) =>
        typeof(BaseCalculoIndicadoresViewModel).GetProperties().All(p => (decimal)p.GetValue(b)! == 0);

    private static DateTime? LerMes(string valor) =>
        Regex.IsMatch(valor, @"^\d{4}-(0[1-9]|1[0-2])$")
            ? DateTime.ParseExact(valor + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;

    private static string Serializar(object valor) => JsonSerializer.Serialize(valor, OpcoesJson);
}
