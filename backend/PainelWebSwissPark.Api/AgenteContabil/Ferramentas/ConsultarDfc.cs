using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Ferramenta que traz a DFC (método indireto) de uma empresa num período, com os mesmos números da
/// aba DFC: atividades operacionais, de financiamento e de investimentos, variações não
/// classificadas e o caixa inicial e final. A DFC não tem service próprio: sai do
/// <see cref="IIndicadoresService"/>, a mesma chamada da tela. Esta classe só acha a empresa pelo
/// nome e monta um JSON enxuto para o agente.
/// </summary>
public class ConsultarDfc(
    IEmpresasService empresasService, IIndicadoresService indicadoresService, ILogger<ConsultarDfc> logger)
{
    public const string NomeFerramenta = "consultar_dfc";

    // As contas fora do de-para que vão na resposta; a tela mostra todas na composição.
    private const int MaximoDeContasNaoClassificadas = 10;

    // Sem escapar os acentos: o JSON vai para o modelo, não para uma página.
    private static readonly JsonSerializerOptions OpcoesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Consulta a DFC (demonstração do fluxo de caixa, método indireto) de uma empresa num período, com os " +
            "mesmos números da aba DFC: o total e as linhas de cada atividade (operacionais, financiamento, " +
            "investimentos e variações não classificadas), o caixa inicial, a variação e o caixa final. Use para " +
            "fluxo de caixa, caixa operacional, quanto a empresa gerou ou consumiu de caixa, investimentos ou " +
            "financiamentos. Aceita um período de vários meses (anoMesInicial a anoMes): use o período da tela, se houver.",
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
                        "Sem ele, entram todas as filiais da coligada.",
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

        if (parametros.Erro is { } erroDeTipo)
        {
            return Serializar(erroDeTipo);
        }
        if (LocalizarEmpresa.ValidarParametros(
                empresa, anoMes, anoMesInicial, codColigada, codFilial, out var mesInicial, out var mesFinal) is { } erro)
        {
            return Serializar(erro);
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

        var cronometro = Stopwatch.StartNew();
        var resultado = await indicadoresService.CalcularAsync(
            mesInicial, mesFinal.AddMonths(1), achada.CodColigada, achada.CodFilial, cancellationToken);
        logger.LogInformation(
            "CFO Tati: DFC de {Empresa} (coligada {Coligada}, {Inicio:yyyy-MM} a {Fim:yyyy-MM}) calculada em {DuracaoMs} ms",
            achada.Nome, achada.CodColigada, mesInicial, mesFinal, cronometro.ElapsedMilliseconds);

        return Serializar(MontarResposta(achada, mesInicial, mesFinal, resultado));
    }

    /// <summary>O JSON enxuto a partir da DFC do <see cref="IIndicadoresService"/>, sem as linhas zeradas.</summary>
    public static object MontarResposta(
        LocalizarEmpresa.Empresa empresa, DateTime mesInicial, DateTime mesFinal, IndicadoresViewModel resultado)
    {
        var periodo = LocalizarEmpresa.Periodo(mesInicial, mesFinal);
        if (ConsultarIndicadores.SemDados(resultado.BaseCalculo))
        {
            return LocalizarEmpresa.SemDados(empresa, periodo, "Não há lançamentos dessa empresa até o fim desse período.");
        }

        var dfc = resultado.Dfc;
        var naoClassificadas = dfc.Grupos
            .SelectMany(g => g.Linhas)
            .Where(l => l.NaoClassificada)
            .SelectMany(l => l.Composicao)
            .ToList();

        return new
        {
            empresa = empresa.Nome,
            coligada = empresa.CodColigada,
            filial = empresa.CodFilial,
            abrangencia = empresa.Abrangencia,
            filiaisDaColigada = empresa.FiliaisDaColigada,
            periodo,
            metodo = "indireto",
            convencaoDeSinais = "como na tela: valor positivo aumenta o caixa, negativo reduz",
            atividades = dfc.Grupos.Select(g => new
            {
                atividade = g.Nome,
                total = LocalizarEmpresa.Reais(g.Total),
                linhas = g.Linhas
                    .Where(l => l.Valor != 0)
                    .Select(l => new { linha = l.Descricao, valor = LocalizarEmpresa.Reais(l.Valor) }),
            }),
            caixaInicial = LocalizarEmpresa.Reais(dfc.SaldoInicialCaixa),
            variacaoCaixa = LocalizarEmpresa.Reais(dfc.VariacaoCaixa),
            caixaFinal = LocalizarEmpresa.Reais(dfc.SaldoFinalCaixa),
            // Contas fora do de-para: a DFC fecha, mas o contador precisa revisar a classificação.
            contasNaoClassificadas = naoClassificadas.Count == 0 ? null : new
            {
                quantidade = naoClassificadas.Count,
                maiores = naoClassificadas
                    .OrderByDescending(c => Math.Abs(c.Valor))
                    .Take(MaximoDeContasNaoClassificadas)
                    .Select(c => new { conta = c.CodConta, descricao = c.Descricao, valor = LocalizarEmpresa.Reais(c.Valor) }),
            },
        };
    }

    private static string Serializar(object valor) => JsonSerializer.Serialize(valor, OpcoesJson);
}
