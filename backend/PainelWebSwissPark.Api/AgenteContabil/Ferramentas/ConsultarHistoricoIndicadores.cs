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
/// Ferramenta que traz os indicadores de uma empresa mês a mês, com os mesmos valores e status da
/// matriz dos 12 meses da tela de Indicadores: a base de cada mês vem do
/// <see cref="IIndicadoresMensaisService"/> (a mesma fonte do /api/indicadores/mensal) e cada mês
/// é calculado sozinho pela <see cref="RegrasIndicadores"/>, a cópia das regras da tela. Esta
/// classe só acha a empresa pelo nome, corta o período e monta um JSON enxuto para o agente.
/// </summary>
public class ConsultarHistoricoIndicadores(
    IEmpresasService empresasService, IIndicadoresMensaisService indicadoresMensaisService,
    ILogger<ConsultarHistoricoIndicadores> logger)
{
    public const string NomeFerramenta = "consultar_historico_indicadores";

    /// <summary>O mesmo tamanho da matriz da tela.</summary>
    public const int MaximoDeMeses = 12;

    // Sem escapar os acentos: o JSON vai para o modelo, não para uma página.
    private static readonly JsonSerializerOptions OpcoesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Consulta a evolução mês a mês de um ou mais indicadores financeiros de uma empresa (até 12 meses), com " +
            "os mesmos valores e status da matriz dos 12 meses da tela Indicadores: cada mês calculado sozinho. Use para " +
            "'como evoluiu', tendência, os últimos meses de um indicador ou para montar um gráfico de linha. Para a " +
            "situação de um período (com pilares, veredito e comparação com o período anterior), use consultar_indicadores.",
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
                anoMesInicial = new { type = "string", description = "Primeiro mês, no formato AAAA-MM." },
                anoMes = new { type = "string", description = "Último mês, no formato AAAA-MM. No máximo 12 meses no total." },
                indicadores = new
                {
                    type = "array",
                    items = new { type = "string" },
                    description = "Os indicadores, pelo código (" +
                        string.Join(", ", RegrasIndicadores.Configuracao.Select(c => c.Codigo)) +
                        ") ou pelo nome. Omita para todos.",
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
            required = new[] { "empresa", "anoMesInicial", "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var parametros = new LocalizarEmpresa.Parametros(input);
        var empresa = parametros.Texto("empresa");
        var anoMesInicial = parametros.Texto("anoMesInicial");
        var anoMes = parametros.Texto("anoMes");
        var pedidos = parametros.Lista("indicadores");
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
        if (LocalizarEmpresa.Meses(mesInicial, mesFinal) > MaximoDeMeses)
        {
            return Serializar(new
            {
                erro = $"No máximo {MaximoDeMeses} meses por consulta — peça um período menor " +
                    $"(por exemplo, de {mesFinal.AddMonths(1 - MaximoDeMeses):yyyy-MM} a {mesFinal:yyyy-MM}).",
            });
        }

        // Os indicadores pedidos, na ordem da tabela da tela; sem lista, todos.
        var codigos = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pedido in pedidos ?? [])
        {
            if (ConsultarIndicadores.AcharIndicador(pedido) is not { } codigo)
            {
                return Serializar(new
                {
                    erro = $"Não identifiquei um indicador só com \"{pedido}\" — chame de novo com um destes códigos.",
                    indicadores = RegrasIndicadores.Configuracao.Select(c => new { codigo = c.Codigo, nome = c.Nome }),
                });
            }
            codigos.Add(codigo);
        }

        var (achada, erroEmpresa) = await LocalizarEmpresa.ExecutarAsync(
            empresasService, empresa!, codColigada, codFilial, cancellationToken);
        if (achada is null)
        {
            return Serializar(erroEmpresa!);
        }
        if (LocalizarEmpresa.NoFuturo(mesInicial))
        {
            return Serializar(LocalizarEmpresa.SemDados(
                achada, LocalizarEmpresa.Periodo(mesInicial, mesFinal), LocalizarEmpresa.MotivoNoFuturo(mesInicial)));
        }

        // Os meses que ainda não aconteceram nem vão ao banco: o service repetiria o último saldo
        // neles, com resultado zero, e a conta sairia como se fosse dado.
        var mesAtual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var ultimoConsultado = mesFinal > mesAtual ? mesAtual : mesFinal;
        var cronometro = Stopwatch.StartNew();
        var bases = await indicadoresMensaisService.CalcularAsync(
            mesInicial, ultimoConsultado.AddMonths(1), achada.CodColigada, achada.CodFilial, cancellationToken);
        logger.LogInformation(
            "CFO Tati: histórico de {Empresa} (coligada {Coligada}, {Inicio:yyyy-MM} a {Fim:yyyy-MM}) em {DuracaoMs} ms",
            achada.Nome, achada.CodColigada, mesInicial, mesFinal, cronometro.ElapsedMilliseconds);

        return Serializar(MontarResposta(achada, mesInicial, mesFinal, mesAtual, bases, codigos));
    }

    /// <summary>
    /// O JSON enxuto: por indicador, os meses com valor e status na unidade da tela. Mês sem
    /// lançamentos ou no futuro sai com valor e status nulos e o motivo, nunca zero.
    /// </summary>
    public static object MontarResposta(
        LocalizarEmpresa.Empresa empresa, DateTime mesInicial, DateTime mesFinal, DateTime mesAtual,
        IReadOnlyList<BaseCalculoMensalViewModel> bases, IReadOnlySet<string> codigos)
    {
        var periodo = LocalizarEmpresa.Periodo(mesInicial, mesFinal);
        var porMes = bases.ToDictionary(b => b.AnoMes, b => b.BaseCalculo, StringComparer.Ordinal);

        var meses = new List<(string AnoMes, IReadOnlyList<ResultadoIndicador>? Resultados, string? Motivo)>();
        for (var mes = mesInicial; mes <= mesFinal; mes = mes.AddMonths(1))
        {
            var anoMes = mes.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            if (mes > mesAtual)
            {
                meses.Add((anoMes, null, "o mês ainda não aconteceu"));
            }
            else if (!porMes.TryGetValue(anoMes, out var b) || ConsultarIndicadores.SemDados(b))
            {
                meses.Add((anoMes, null, "não há lançamentos dessa empresa até o fim desse mês"));
            }
            else
            {
                // Cada mês sozinho, como a matriz da tela: calcularIndicadores(base do mês, 1).
                meses.Add((anoMes, RegrasIndicadores.Calcular(b, 1), null));
            }
        }

        if (meses.All(m => m.Resultados is null))
        {
            return LocalizarEmpresa.SemDados(empresa, periodo, "Não há lançamentos dessa empresa em nenhum mês desse período.");
        }

        var configs = RegrasIndicadores.Configuracao.Where(c => codigos.Count == 0 || codigos.Contains(c.Codigo));
        return new
        {
            empresa = empresa.Nome,
            coligada = empresa.CodColigada,
            filial = empresa.CodFilial,
            abrangencia = empresa.Abrangencia,
            filiaisDaColigada = empresa.FiliaisDaColigada,
            periodo,
            convencaoDeUnidades = "percentual em pontos percentuais (12.5 = 12,5%); multiplo em vezes; moeda em R$; numero é índice",
            indicadores = configs.Select(config => new
            {
                codigo = config.Codigo,
                nome = config.Nome,
                unidade = ConsultarIndicadores.Unidade(config),
                observacao = Observacao(config),
                meses = meses.Select(m => Mes(m.AnoMes, m.Resultados?.Single(r => r.Config.Codigo == config.Codigo), m.Motivo)),
            }),
            // O mês corrente ainda recebe lançamentos: num gráfico de evolução, ele não é comparável aos outros.
            mesEmAndamento = mesFinal >= mesAtual && mesInicial <= mesAtual
                ? mesAtual.ToString("yyyy-MM", CultureInfo.InvariantCulture)
                : null,
        };
    }

    /// <summary>O que muda ao ler um indicador mês a mês, como a matriz da tela o calcula.</summary>
    private static string? Observacao(ConfigIndicador config) =>
        config.StatusAnualizado ? "valor do mês; o status usa o valor anualizado (× 12), como na tela"
        : config.Codigo == "DIV_LIQ_EBITDA_12M"
            ? "em cada mês, o EBITDA de 12 meses é o EBITDA do próprio mês × 12, como na tela — não é o EBITDA real " +
              "dos últimos 12 meses, por isso pode variar muito de um mês para o outro"
            : null;

    /// <summary>
    /// Um mês de um indicador. Nos casos especiais da tela ("Caixa líquido", "Sem dívida", "PL
    /// negativo"…) o valor é nulo mas há status: vai o texto, para não parecer mês sem dados.
    /// </summary>
    private static object Mes(string anoMes, ResultadoIndicador? resultado, string? motivo)
    {
        if (resultado is null)
        {
            return new { mes = anoMes, valor = (double?)null, status = (string?)null, motivo };
        }

        var valor = ConsultarIndicadores.NaUnidade(resultado.Valor, resultado.Config);
        return valor is null
            ? new { mes = anoMes, valor, status = (string?)resultado.Status, texto = RegrasIndicadores.TextoDoValor(resultado) }
            : (object)new { mes = anoMes, valor, status = (string?)resultado.Status };
    }

    private static string Serializar(object valor) => JsonSerializer.Serialize(valor, OpcoesJson);
}
