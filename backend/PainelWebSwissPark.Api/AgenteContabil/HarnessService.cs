using System.ClientModel;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Exceptions;
using PainelWebSwissPark.Api.Options;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil;

/// <summary>
/// Roda o loop do agente: manda a pergunta pro modelo, e enquanto ele pedir pra
/// usar uma ferramenta, executa e devolve o resultado, até ele responder com
/// texto final (ou estourar o limite de idas e vindas).
/// </summary>
public class HarnessService(
    ModelClient modelClient,
    ConsultarSaldo consultarSaldo,
    ConsultarBacklogStatus consultarBacklogStatus,
    ConsultarBacklogPorEmpresa consultarBacklogPorEmpresa,
    ConsultarProdutividadeTime consultarProdutividadeTime,
    ConsultarFechamento consultarFechamento,
    ConsultarIndicadores consultarIndicadores,
    ConsultarBalancete consultarBalancete,
    ConsultarBalanco consultarBalanco,
    ConsultarDfc consultarDfc,
    ConsultarHistoricoIndicadores consultarHistoricoIndicadores,
    IOptions<AgenteContabilOptions> opcoes,
    ILogger<HarnessService> logger)
{
    // Limite de segurança contra o modelo entrar num laço de chamadas de ferramenta: as perguntas
    // que cruzam módulos (balanço + DFC + indicadores) gastam mais idas que as de antes.
    private int MaxIteracoes => Math.Max(1, opcoes.Value.MaxIteracoes);
    private const int MaxMensagensDeHistorico = 12;

    // Segurança contra o modelo (ou a rede) travar a conversa por tempo
    // indefinido — sem isso, uma resposta lenta prende a requisição HTTP até o
    // cliente desistir sozinho.
    private TimeSpan TempoLimite => TimeSpan.FromSeconds(Math.Max(1, opcoes.Value.TempoLimiteSegundos));

    // No log, os argumentos vão cortados: são só filtros (empresa, período, conta), nunca resultado.
    private const int MaxCaracteresDosArgumentosNoLog = 300;

    // Sem escapar os acentos: o JSON vai para o modelo, não para uma página.
    private static readonly JsonSerializerOptions OpcoesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // Uma por versão (AgenteContabil:VersaoInstrucoes), lidas uma vez como a base de conhecimento.
    private static readonly ConcurrentDictionary<string, Lazy<string>> Instrucoes = new(StringComparer.OrdinalIgnoreCase);

    private string VersaoInstrucoes => opcoes.Value.VersaoInstrucoes?.Trim() ?? string.Empty;

    // Os .md de AgenteContabil/Conhecimento, lidos uma vez como as instruções.
    private static readonly Lazy<BaseDeConhecimento> Conhecimento = new(CarregarConhecimento);

    private sealed record BaseDeConhecimento(string Texto, int Arquivos, int Caracteres, string? Aviso);

    /// <summary>
    /// Lê as instruções e a base de conhecimento na subida da API e registra no log o que foi
    /// carregado. Uma falha aqui não derruba a API: só a Tati deixa de responder, como antes.
    /// </summary>
    public static void PreCarregar(ILogger logger, string versaoInstrucoes)
    {
        // Com o mesmo ajuste da requisição, para a subida carregar a versão que a Tati vai usar.
        versaoInstrucoes = versaoInstrucoes?.Trim() ?? string.Empty;
        try
        {
            logger.LogInformation(
                "CFO Tati: instruções {Versao} carregadas ({Caracteres} caracteres)",
                versaoInstrucoes, InstrucoesDa(versaoInstrucoes).Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CFO Tati: não foi possível carregar as instruções");
        }

        var conhecimento = Conhecimento.Value;
        if (conhecimento.Aviso is not null)
        {
            logger.LogWarning("CFO Tati: seguindo sem base de conhecimento — {Aviso}", conhecimento.Aviso);
            return;
        }

        logger.LogInformation(
            "CFO Tati: base de conhecimento carregada ({Arquivos} arquivos, {Caracteres} caracteres)",
            conhecimento.Arquivos, conhecimento.Caracteres);
    }

    public Task<string> ExecutarAsync(
        string pergunta, string anoMes, string? empresa, List<MensagemChatDto> historico, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "CFO Tati: pergunta recebida (anoMes={AnoMes}, empresa={Empresa}, tamanhoHistorico={TamanhoHistorico})",
            anoMes, empresa ?? "todas", historico.Count);

        var contexto =
            $"Contexto atual da tela: período {anoMes}, empresa: " +
            $"{(string.IsNullOrWhiteSpace(empresa) ? "todas" : empresa)}. " +
            "Use esses valores ao chamar as ferramentas, a menos que o usuário peça outro período/empresa/conta.";

        return ResponderAsync(pergunta, contexto, historico, cancellationToken);
    }

    /// <summary>A mesma conversa, com todos os filtros da tela (período, filial, conta e status) no contexto.</summary>
    public Task<string> ExecutarComFiltrosAsync(PerguntarCfoTatiComFiltrosRequest pedido, CancellationToken cancellationToken) =>
        ResponderAsync(pedido.Pergunta, ContextoDosFiltros(pedido), pedido.Historico, cancellationToken);

    /// <summary>
    /// A conversa do ícone da Tati nas outras telas: os filtros da tela e, no fim do contexto, a
    /// tela (e a aba) aberta, para ela saber de que assunto é uma pergunta que não diz o assunto.
    /// </summary>
    public Task<string> ExecutarNaTelaAsync(PerguntarCfoTatiNaTelaRequest pedido, CancellationToken cancellationToken)
    {
        var contexto = ContextoDosFiltros(pedido);
        var tela = DescreverTela(pedido.Tela, pedido.Aba);
        logger.LogInformation("CFO Tati: pergunta feita pelo ícone, na tela {Tela}", tela ?? "(não informada)");
        if (tela is not null) contexto += $" Tela aberta: {tela}.";

        return ResponderAsync(pedido.Pergunta, contexto, pedido.Historico, cancellationToken);
    }

    /// <summary>
    /// "Balancete, aba Balanço"; null sem tela. Os nomes vêm do navegador e vão para o modelo:
    /// numa linha só e curtos.
    /// </summary>
    public static string? DescreverTela(string? tela, string? aba)
    {
        var nomeDaTela = Limpo(tela);
        if (nomeDaTela is null) return null;
        var nomeDaAba = Limpo(aba);
        return nomeDaAba is null ? nomeDaTela : $"{nomeDaTela}, aba {nomeDaAba}";

        static string? Limpo(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return null;
            var umaLinha = string.Join(' ', nome.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return umaLinha.Length <= MaxCaracteresDoNomeDaTela ? umaLinha : umaLinha[..MaxCaracteresDoNomeDaTela];
        }
    }

    private const int MaxCaracteresDoNomeDaTela = 60;

    // O contexto da pergunta com os filtros da tela; a pergunta vai para o log com eles.
    private string ContextoDosFiltros(PerguntarCfoTatiComFiltrosRequest pedido)
    {
        var anoMesInicial = string.IsNullOrWhiteSpace(pedido.AnoMesInicial) ? pedido.AnoMes : pedido.AnoMesInicial;
        logger.LogInformation(
            "CFO Tati: pergunta recebida (período={Inicio} a {Fim}, empresa={Empresa}, filial={Filial}, conta={Conta}, " +
            "status={Status}, tamanhoHistorico={TamanhoHistorico})",
            anoMesInicial, pedido.AnoMes, pedido.Empresa ?? "todas", pedido.CodFilial,
            pedido.CodConta ?? "todas", pedido.Status ?? "todos", pedido.Historico.Count);

        // Indicadores e DFC recebem um período; as outras consultam um mês por vez, e o mês final é o
        // padrão delas (o balanço da tela também é a posição no fim do período).
        var periodo = anoMesInicial == pedido.AnoMes
            ? $"período {pedido.AnoMes}"
            : $"período de {anoMesInicial} a {pedido.AnoMes} (consultar_indicadores, consultar_dfc e " +
              $"consultar_historico_indicadores recebem o período inteiro; as outras ferramentas consultam um mês por vez: " +
              $"use {pedido.AnoMes}, o mês final, a menos que o " +
              "usuário peça outro mês ou a evolução do período)";
        var empresa = string.IsNullOrWhiteSpace(pedido.Empresa)
            ? "todas"
            : pedido.CodColigada is { } coligada ? $"{pedido.Empresa} (coligada {coligada})" : pedido.Empresa;
        var filial = EmpresasConsolidadas.Consolida(pedido.Empresa)
            ? "todas (a empresa fecha com as filiais consolidadas)"
            : pedido.CodFilial?.ToString() ?? "todas";
        var conta = string.IsNullOrWhiteSpace(pedido.CodConta) ? "todas" : pedido.CodConta;
        var status = string.IsNullOrWhiteSpace(pedido.Status) ? "todos" : pedido.Status;

        return
            $"Contexto atual da tela: {periodo}, empresa: {empresa}, filial: {filial}, conta: {conta}, " +
            $"status do fechamento: {status}. " +
            "Use esses valores ao chamar as ferramentas, a menos que o usuário peça outro período/empresa/conta.";
    }

    private async Task<string> ResponderAsync(
        string pergunta, string contexto, List<MensagemChatDto> historico, CancellationToken cancellationToken)
    {
        using var origemComTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        origemComTimeout.CancelAfter(TempoLimite);
        var tokenComTimeout = origemComTimeout.Token;

        var inicio = DateTime.UtcNow;

        // Instruções e base de conhecimento são iguais em toda pergunta e vêm primeiro, para o
        // DeepSeek reaproveitar esse começo em cache; o contexto, que muda, fica no fim.
        var baseDeConhecimento = Conhecimento.Value.Texto;
        var instrucoes = InstrucoesDa(VersaoInstrucoes);
        var systemPrompt = baseDeConhecimento.Length == 0
            ? $"{instrucoes}\n\n## Contexto da conversa\n\n{contexto}"
            : $"{instrucoes}\n\n{baseDeConhecimento}\n\n## Contexto da conversa\n\n{contexto}";

        List<ChatTool> ferramentas =
        [
            ConsultarSaldo.Definicao,
            ConsultarBacklogStatus.Definicao,
            ConsultarBacklogPorEmpresa.Definicao,
            ConsultarProdutividadeTime.Definicao,
            ConsultarFechamento.Definicao,
            ConsultarIndicadores.Definicao,
            ConsultarBalancete.Definicao,
            ConsultarBalanco.Definicao,
            ConsultarDfc.Definicao,
            ConsultarHistoricoIndicadores.Definicao,
        ];

        List<ChatMessage> mensagens = [new SystemChatMessage(systemPrompt)];

        foreach (var item in historico.TakeLast(MaxMensagensDeHistorico))
        {
            ChatMessage mensagem = item.Autor == "tati"
                ? new AssistantChatMessage(item.Texto)
                : new UserChatMessage(item.Texto);
            mensagens.Add(mensagem);
        }

        mensagens.Add(new UserChatMessage(pergunta));

        // O que as ferramentas devolveram nesta resposta: é contra isso que o gráfico é conferido.
        var resultadosDaResposta = new List<ValidadorGraficoTati.ResultadoDeFerramenta>();

        try
        {
            for (var iteracao = 0; iteracao < MaxIteracoes; iteracao++)
            {
                ChatCompletion resposta;
                try
                {
                    resposta = await modelClient.CriarMensagemAsync(mensagens, ferramentas, tokenComTimeout);
                }
                catch (ClientResultException ex) when (ex.Status == 401)
                {
                    throw new RegraDeNegocioException(
                        "IA indisponível: a chave da DeepSeek não está configurada ou é inválida (DeepSeek:ApiKey no keys.json).");
                }
                catch (ClientResultException ex) when (ex.Status == 429)
                {
                    throw new RegraDeNegocioException("A CFO Tati está sobrecarregada no momento. Tente novamente em instantes.");
                }
                catch (ClientResultException ex)
                {
                    throw new RegraDeNegocioException($"Erro ao falar com a IA: {ex.Message}");
                }

                if (resposta.ToolCalls.Count == 0)
                {
                    logger.LogInformation(
                        "CFO Tati: respondeu em {Iteracoes} chamada(s) ao modelo, {DuracaoMs}ms",
                        iteracao + 1, (DateTime.UtcNow - inicio).TotalMilliseconds);

                    var texto = LimpezaRespostaTati.Limpar(resposta.Content.Count > 0 ? resposta.Content[0].Text : null);
                    if (string.IsNullOrWhiteSpace(texto))
                    {
                        return "Não consegui gerar uma resposta agora. Tente novamente.";
                    }

                    return Validado(await GarantirOpcoesAsync(texto, mensagens, tokenComTimeout), resultadosDaResposta);
                }

                mensagens.Add(new AssistantChatMessage(resposta));

                foreach (var chamada in resposta.ToolCalls)
                {
                    var resultado = await ExecutarComLogAsync(chamada, tokenComTimeout);
                    resultadosDaResposta.Add(new(chamada.FunctionName, resultado));
                    mensagens.Add(new ToolChatMessage(chamada.Id, resultado));
                }
            }
        }
        catch (OperationCanceledException) when (origemComTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("CFO Tati: estourou o tempo limite de {TempoLimite}", TempoLimite);
            throw new RegraDeNegocioException("A CFO Tati demorou demais pra responder. Tente novamente.");
        }

        logger.LogWarning("CFO Tati: estourou o limite de {MaxIteracoes} idas ao modelo sem resposta final", MaxIteracoes);
        return "Precisei consultar dados demais pra essa pergunta e não cheguei a uma resposta. Tenta reformular de um jeito mais específico?";
    }

    /// <summary>
    /// O texto final, sem o bloco de gráfico se algum número dele não saiu das ferramentas desta
    /// resposta. O motivo vai para o log, sem os valores; o modelo não é chamado de novo.
    /// </summary>
    private string Validado(string texto, IReadOnlyList<ValidadorGraficoTati.ResultadoDeFerramenta> resultados)
    {
        var validacao = ValidadorGraficoTati.Validar(texto, resultados);
        if (validacao.Motivo is not null)
        {
            logger.LogWarning("CFO Tati: gráfico removido da resposta — {Motivo}", validacao.Motivo);
        }
        return validacao.Texto;
    }

    // As opções numeradas são obrigatórias (a tela vira botões 1, 2, 3...). Se o
    // modelo esquecer o bloco, pede só ele numa chamada extra, sem ferramentas.
    private async Task<string> GarantirOpcoesAsync(
        string texto, List<ChatMessage> mensagens, CancellationToken cancellationToken)
    {
        if (texto.Contains("```opcoes", StringComparison.Ordinal)) return texto;

        logger.LogInformation("CFO Tati: resposta sem bloco de opções — pedindo as opções");

        try
        {
            List<ChatMessage> pedido =
            [
                .. mensagens,
                new AssistantChatMessage(texto),
                new UserChatMessage(
                    "Gere agora APENAS o bloco ```opcoes com 2 a 4 próximos passos para a sua última resposta, " +
                    "no formato da instrução (lista JSON de frases). Nenhum outro texto."),
            ];

            var resposta = await modelClient.CriarMensagemAsync(pedido, [], cancellationToken);
            var bloco = LimpezaRespostaTati.Limpar(resposta.Content.Count > 0 ? resposta.Content[0].Text : null);

            return bloco is not null && bloco.Contains("```opcoes", StringComparison.Ordinal)
                ? $"{texto.TrimEnd()}\n\n{bloco.Trim()}"
                : texto;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Sem opções é melhor do que sem resposta.
            logger.LogWarning(ex, "CFO Tati: não consegui gerar as opções");
            return texto;
        }
    }

    /// <summary>
    /// Executa uma ferramenta e registra no log o nome, os argumentos, o tempo e se deu certo. Uma
    /// falha da ferramenta (banco fora do ar, tempo de consulta esgotado) não derruba a conversa:
    /// volta para o modelo como erro, e as instruções mandam dizer que não conseguiu, sem estimar.
    /// </summary>
    private async Task<string> ExecutarComLogAsync(ChatToolCall chamada, CancellationToken cancellationToken)
    {
        var argumentos = chamada.FunctionArguments.ToString();
        var cronometro = Stopwatch.StartNew();
        string resultado;
        string situacao;

        JsonDocument documento;
        try
        {
            documento = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentos) ? "{}" : argumentos);
        }
        catch (JsonException)
        {
            logger.LogInformation(
                "CFO Tati: ferramenta {Ferramenta} {Argumentos} → erro (argumentos não são JSON)", chamada.FunctionName, Cortar(argumentos));
            return JsonSerializer.Serialize(
                new { erro = "Argumentos inválidos: mande um objeto JSON com os parâmetros da ferramenta." }, OpcoesJson);
        }

        try
        {
            resultado = await ExecutarFerramentaAsync(chamada.FunctionName, documento.RootElement, cancellationToken);
            situacao = Situacao(resultado);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "CFO Tati: ferramenta {Ferramenta} cancelada depois de {DuracaoMs} ms", chamada.FunctionName, cronometro.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CFO Tati: ferramenta {Ferramenta} falhou", chamada.FunctionName);
            resultado = JsonSerializer.Serialize(new
            {
                erro = "Não consegui consultar esse dado agora (falha ao acessar os dados). Diga ao usuário que não foi " +
                    "possível e sugira tentar de novo; não estime o valor.",
            }, OpcoesJson);
            situacao = "falha";
        }
        finally
        {
            documento.Dispose();
        }

        logger.LogInformation(
            "CFO Tati: ferramenta {Ferramenta} {Argumentos} → {Situacao} em {DuracaoMs} ms",
            chamada.FunctionName, Cortar(argumentos), situacao, cronometro.ElapsedMilliseconds);
        return resultado;
    }

    /// <summary>ok, erro (parâmetro, empresa ou período) ou semDados, pelo JSON que a ferramenta devolveu.</summary>
    private static string Situacao(string resultado)
    {
        try
        {
            using var documento = JsonDocument.Parse(resultado);
            var raiz = documento.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object) return "ok";
            if (raiz.TryGetProperty("erro", out _)) return "erro";
            return (raiz.TryGetProperty("semDados", out var semDados) || raiz.TryGetProperty("semLancamentos", out semDados))
                && semDados.ValueKind == JsonValueKind.True
                    ? "semDados"
                    : "ok";
        }
        catch (JsonException)
        {
            return "ok";
        }
    }

    private static string Cortar(string argumentos)
    {
        var umaLinha = argumentos.ReplaceLineEndings(" ");
        return umaLinha.Length <= MaxCaracteresDosArgumentosNoLog ? umaLinha : umaLinha[..MaxCaracteresDosArgumentosNoLog] + "…";
    }

    private Task<string> ExecutarFerramentaAsync(string nome, JsonElement input, CancellationToken cancellationToken) =>
        nome switch
        {
            ConsultarSaldo.NomeFerramenta => consultarSaldo.ExecutarAsync(input, cancellationToken),
            ConsultarBacklogStatus.NomeFerramenta => consultarBacklogStatus.ExecutarAsync(input, cancellationToken),
            ConsultarBacklogPorEmpresa.NomeFerramenta => consultarBacklogPorEmpresa.ExecutarAsync(input, cancellationToken),
            ConsultarProdutividadeTime.NomeFerramenta => consultarProdutividadeTime.ExecutarAsync(input, cancellationToken),
            ConsultarFechamento.NomeFerramenta => consultarFechamento.ExecutarAsync(input, cancellationToken),
            ConsultarIndicadores.NomeFerramenta => consultarIndicadores.ExecutarAsync(input, cancellationToken),
            ConsultarBalancete.NomeFerramenta => consultarBalancete.ExecutarAsync(input, cancellationToken),
            ConsultarBalanco.NomeFerramenta => consultarBalanco.ExecutarAsync(input, cancellationToken),
            ConsultarDfc.NomeFerramenta => consultarDfc.ExecutarAsync(input, cancellationToken),
            ConsultarHistoricoIndicadores.NomeFerramenta => consultarHistoricoIndicadores.ExecutarAsync(input, cancellationToken),
            _ => Task.FromResult(JsonSerializer.Serialize(new { erro = $"Ferramenta desconhecida: {nome}" }, OpcoesJson)),
        };

    private static string InstrucoesDa(string versao) =>
        Instrucoes.GetOrAdd(versao, v => new Lazy<string>(() => CarregarInstrucoes(v))).Value;

    private static string CarregarInstrucoes(string versao)
    {
        // A versão vem da configuração e vira nome de arquivo: só "v" e número.
        if (!Regex.IsMatch(versao, @"^v\d+$"))
        {
            throw new InvalidOperationException(
                $"Versão das instruções da CFO Tati inválida em AgenteContabil:VersaoInstrucoes: \"{versao}\" (use v1, v2, v3...).");
        }

        // As versões antigas continuam no build, para dar para voltar trocando só a configuração.
        var caminho = Path.Combine(AppContext.BaseDirectory, "AgenteContabil", "Instrucoes", $"instrucoes-{versao}.md");

        if (!File.Exists(caminho))
        {
            throw new InvalidOperationException($"Arquivo de instruções da CFO Tati não encontrado: {caminho}");
        }

        return File.ReadAllText(caminho);
    }

    /// <summary>
    /// Os .md da pasta Conhecimento em ordem alfabética (o prefixo 01-, 02-… decide), separados
    /// por "---" sob o título "# Base de conhecimento". Sem a pasta, a Tati segue sem a base.
    /// </summary>
    private static BaseDeConhecimento CarregarConhecimento()
    {
        var pasta = Path.Combine(AppContext.BaseDirectory, "AgenteContabil", "Conhecimento");
        if (!Directory.Exists(pasta))
        {
            return new BaseDeConhecimento(string.Empty, 0, 0, $"pasta não encontrada: {pasta}");
        }

        try
        {
            var conteudos = Directory.GetFiles(pasta, "*.md")
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .Select(File.ReadAllText)
                .ToList();
            if (conteudos.Count == 0)
            {
                return new BaseDeConhecimento(string.Empty, 0, 0, $"nenhum arquivo .md em {pasta}");
            }

            // O material é de referência: não muda regras nem ferramentas, e a ferramenta vence.
            var texto = "# Base de conhecimento\n\n" +
                "Material de referência sobre o painel. Ele não muda as suas regras nem as ferramentas; " +
                "se divergir do que uma ferramenta devolveu, vale a ferramenta.\n\n" +
                string.Join("\n\n---\n\n", conteudos.Select(conteudo => conteudo.Trim()));

            return new BaseDeConhecimento(texto, conteudos.Count, conteudos.Sum(conteudo => conteudo.Length), null);
        }
        // Erro de leitura ou de permissão não derruba a subida: a Tati só segue sem a base.
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new BaseDeConhecimento(string.Empty, 0, 0, $"erro ao ler {pasta}: {ex.Message}");
        }
    }
}
