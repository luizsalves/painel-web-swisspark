using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI.Chat;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Ferramenta que traz o balancete de uma empresa num mês — saldo anterior, débitos, créditos e
/// saldo atual por conta, os mesmos números da tela Balancete. Quem soma é o
/// <see cref="IBalanceteService"/> que já existe; esta classe só acha a empresa pelo nome, corta
/// os níveis e limita as linhas para o JSON caber na conversa.
/// </summary>
public class ConsultarBalancete(
    IEmpresasService empresasService, IBalanceteService balanceteService, ILogger<ConsultarBalancete> logger)
{
    public const string NomeFerramenta = "consultar_balancete";

    /// <summary>Acima disso vão só as contas de maior saldo, com <c>truncado: true</c>.</summary>
    public const int MaximoDeLinhas = 50;

    // Até 1.1.1 (Disponível, Realizável...): o resumo dos grupos, sem descer às contas analíticas.
    private const int NivelPadrao = 3;

    // Sem escapar os acentos: o JSON vai para o modelo, não para uma página.
    private static readonly JsonSerializerOptions OpcoesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Consulta o balancete de uma empresa num mês, com os mesmos números da tela Balancete: por conta " +
            "contábil, saldo anterior, débitos, créditos e saldo atual. Sem conta, devolve o resumo até o nível 3 " +
            "(1.1.1); com conta, ela e as contas abaixo dela. No máximo 50 linhas: se passar, vêm as 50 de maior " +
            "saldo e truncado = true. Plano de contas: 1 Ativo (1.1 circulante — 1.1.1 disponível: caixa 1.1.1.01, " +
            "bancos 1.1.1.02, aplicações 1.1.1.03; 1.1.2 realizável a curto prazo; 1.2 não circulante), 2 Passivo " +
            "(2.1 circulante, 2.2 não circulante, 2.3 patrimônio líquido), 3 em diante resultado. Use para saldo de " +
            "um grupo de contas no balancete ou 'me mostra o balancete'. Para o saldo e o razão de UMA conta " +
            "analítica, use consultar_saldo.",
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
                anoMes = new { type = "string", description = "Mês do balancete, no formato AAAA-MM." },
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
                conta = new
                {
                    type = "string",
                    description = "Código de um grupo de contas para filtrar, ex.: 1.1.1 ou 2.1. Vêm ele e as contas abaixo dele.",
                },
                nivel = new
                {
                    type = "integer",
                    description = "Até que nível mostrar: 1 = 1, 2, 3...; 2 = 1.1, 1.2...; 3 = 1.1.1 (o padrão sem conta). " +
                        "Com conta, o padrão é todos os níveis abaixo dela.",
                },
            },
            required = new[] { "empresa", "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var parametros = new LocalizarEmpresa.Parametros(input);
        var empresa = parametros.Texto("empresa");
        var anoMes = parametros.Texto("anoMes");
        var codColigada = parametros.Inteiro("codColigada");
        var codFilial = parametros.Inteiro("codFilial");
        var conta = parametros.Texto("conta");
        var nivel = parametros.Inteiro("nivel");

        if (parametros.Erro is { } erroDeTipo)
        {
            return Serializar(erroDeTipo);
        }
        if (LocalizarEmpresa.ValidarParametros(
                empresa, anoMes, anoMesInicial: null, codColigada, codFilial, out var mes, out _) is { } erro)
        {
            return Serializar(erro);
        }
        if (conta is not null && !Regex.IsMatch(conta, @"^\d+(\.\d+)*$"))
        {
            return Serializar(new { erro = "Conta inválida — use o código do grupo, ex.: 1.1.1 ou 2.1." });
        }
        if (nivel is < 1 or > 10)
        {
            return Serializar(new { erro = "Nível inválido — use de 1 (só 1, 2, 3...) em diante; o padrão é 3." });
        }

        var (achada, erroEmpresa) = await LocalizarEmpresa.ExecutarAsync(
            empresasService, empresa!, codColigada, codFilial, cancellationToken);
        if (achada is null)
        {
            return Serializar(erroEmpresa!);
        }
        if (LocalizarEmpresa.NoFuturo(mes))
        {
            return Serializar(LocalizarEmpresa.SemDados(achada, LocalizarEmpresa.Periodo(mes, mes), LocalizarEmpresa.MotivoNoFuturo(mes)));
        }

        var cronometro = Stopwatch.StartNew();
        var contas = await balanceteService.ListarAsync(mes, achada.CodColigada, achada.CodFilial, conta, cancellationToken);
        logger.LogInformation(
            "CFO Tati: balancete de {Empresa} (coligada {Coligada}, {Mes:yyyy-MM}, conta {Conta}) com {Contas} contas em {DuracaoMs} ms",
            achada.Nome, achada.CodColigada, mes, conta ?? "todas", contas.Count, cronometro.ElapsedMilliseconds);

        return Serializar(MontarResposta(achada, mes, conta, nivel, contas));
    }

    /// <summary>
    /// O JSON enxuto a partir das contas do <see cref="IBalanceteService"/>: corta no nível pedido e,
    /// acima de <see cref="MaximoDeLinhas"/>, fica com as de maior saldo atual em valor absoluto.
    /// </summary>
    public static object MontarResposta(
        LocalizarEmpresa.Empresa empresa, DateTime mes, string? conta, int? nivel, List<BalanceteContaViewModel> contas)
    {
        var periodo = LocalizarEmpresa.Periodo(mes, mes);
        if (contas.Count == 0)
        {
            return LocalizarEmpresa.SemDados(empresa, periodo, conta is null
                ? "Não há saldo nem movimento dessa empresa nesse mês."
                : $"Não há saldo nem movimento na conta {conta} (nem abaixo dela) nesse mês.");
        }

        // O nível da tela conta os pontos (1.1.1 = 2); aqui é o número de partes do código (1.1.1 = 3).
        // Com conta, a própria conta sempre entra, mesmo que o nível pedido seja menor que o dela.
        int? nivelMaximo = conta is null ? nivel ?? NivelPadrao
            : nivel is { } pedido ? Math.Max(pedido, Partes(conta)) : null;
        var selecionadas = contas.Where(c => nivelMaximo is null || c.Nivel + 1 <= nivelMaximo).ToList();

        var truncado = selecionadas.Count > MaximoDeLinhas;
        var linhas = truncado
            ? selecionadas
                .OrderByDescending(c => Math.Abs(c.SaldoAtual))
                .ThenByDescending(c => Math.Abs(c.Debito) + Math.Abs(c.Credito))
                .Take(MaximoDeLinhas)
                .OrderBy(c => c.CodConta, StringComparer.Ordinal)
                .ToList()
            : selecionadas;

        return new
        {
            empresa = empresa.Nome,
            coligada = empresa.CodColigada,
            filial = empresa.CodFilial,
            abrangencia = empresa.Abrangencia,
            filiaisDaColigada = empresa.FiliaisDaColigada,
            periodo,
            conta,
            nivelMaximo,
            convencaoDeSinais = "como na tela: saldo devedor positivo e credor negativo; saldoAtual = saldoAnterior + debitos − creditos",
            totalLinhas = selecionadas.Count,
            truncado,
            criterio = truncado ? $"as {MaximoDeLinhas} contas de maior saldo atual em valor absoluto, em ordem de código" : null,
            sugestao = truncado ? "Sugira filtrar por um grupo de contas (ex.: 1.1 ou 2.1) ou por um nível menor." : null,
            linhas = linhas.Select(c => new
            {
                conta = c.CodConta,
                descricao = c.Descricao,
                nivel = c.Nivel + 1,
                sintetica = c.Sintetica,
                saldoAnterior = LocalizarEmpresa.Reais(c.SaldoAnterior),
                debitos = LocalizarEmpresa.Reais(c.Debito),
                creditos = LocalizarEmpresa.Reais(c.Credito),
                saldoAtual = LocalizarEmpresa.Reais(c.SaldoAtual),
            }),
        };
    }

    private static int Partes(string conta) => conta.Split('.').Length;

    private static string Serializar(object valor) => JsonSerializer.Serialize(valor, OpcoesJson);
}
