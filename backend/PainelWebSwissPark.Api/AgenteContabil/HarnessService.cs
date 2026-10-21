using System.ClientModel;
using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Exceptions;
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
    ILogger<HarnessService> logger)
{
    // Cada pergunta legítima resolve em 1-2 idas à ferramenta; isso é só um
    // limite de segurança contra o modelo entrar num loop de tool_use.
    private const int MaxIteracoes = 4;
    private const int MaxMensagensDeHistorico = 12;

    // Segurança contra o modelo (ou a rede) travar a conversa por tempo
    // indefinido — sem isso, uma resposta lenta prende a requisição HTTP até o
    // cliente desistir sozinho.
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(60);

    private static readonly Lazy<string> Instrucoes = new(CarregarInstrucoes);

    // Os .md de AgenteContabil/Conhecimento, lidos uma vez como as instruções.
    private static readonly Lazy<BaseDeConhecimento> Conhecimento = new(CarregarConhecimento);

    private sealed record BaseDeConhecimento(string Texto, int Arquivos, int Caracteres, string? Aviso);

    /// <summary>
    /// Lê as instruções e a base de conhecimento na subida da API e registra no log o que foi
    /// carregado. Uma falha aqui não derruba a API: só a Tati deixa de responder, como antes.
    /// </summary>
    public static void PreCarregar(ILogger logger)
    {
        try
        {
            logger.LogInformation("CFO Tati: instruções carregadas ({Caracteres} caracteres)", Instrucoes.Value.Length);
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
    public Task<string> ExecutarComFiltrosAsync(PerguntarCfoTatiComFiltrosRequest pedido, CancellationToken cancellationToken)
    {
        var anoMesInicial = string.IsNullOrWhiteSpace(pedido.AnoMesInicial) ? pedido.AnoMes : pedido.AnoMesInicial;
        logger.LogInformation(
            "CFO Tati: pergunta recebida (período={Inicio} a {Fim}, empresa={Empresa}, filial={Filial}, conta={Conta}, " +
            "status={Status}, tamanhoHistorico={TamanhoHistorico})",
            anoMesInicial, pedido.AnoMes, pedido.Empresa ?? "todas", pedido.CodFilial,
            pedido.CodConta ?? "todas", pedido.Status ?? "todos", pedido.Historico.Count);

        // Só a consultar_indicadores recebe um período; as outras consultam um mês por vez, e o
        // mês final é o padrão delas.
        var periodo = anoMesInicial == pedido.AnoMes
            ? $"período {pedido.AnoMes}"
            : $"período de {anoMesInicial} a {pedido.AnoMes} (consultar_indicadores recebe o período inteiro; as outras " +
              $"ferramentas consultam um mês por vez: use {pedido.AnoMes}, o mês final, a menos que o usuário peça outro " +
              "mês ou a evolução do período)";
        var empresa = string.IsNullOrWhiteSpace(pedido.Empresa)
            ? "todas"
            : pedido.CodColigada is { } coligada ? $"{pedido.Empresa} (coligada {coligada})" : pedido.Empresa;
        var filial = EmpresasConsolidadas.Consolida(pedido.Empresa)
            ? "todas (a empresa fecha com as filiais consolidadas)"
            : pedido.CodFilial?.ToString() ?? "todas";
        var conta = string.IsNullOrWhiteSpace(pedido.CodConta) ? "todas" : pedido.CodConta;
        var status = string.IsNullOrWhiteSpace(pedido.Status) ? "todos" : pedido.Status;

        var contexto =
            $"Contexto atual da tela: {periodo}, empresa: {empresa}, filial: {filial}, conta: {conta}, " +
            $"status do fechamento: {status}. " +
            "Use esses valores ao chamar as ferramentas, a menos que o usuário peça outro período/empresa/conta.";

        return ResponderAsync(pedido.Pergunta, contexto, pedido.Historico, cancellationToken);
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
        var systemPrompt = baseDeConhecimento.Length == 0
            ? $"{Instrucoes.Value}\n\n## Contexto da conversa\n\n{contexto}"
            : $"{Instrucoes.Value}\n\n{baseDeConhecimento}\n\n## Contexto da conversa\n\n{contexto}";

        List<ChatTool> ferramentas =
        [
            ConsultarSaldo.Definicao,
            ConsultarBacklogStatus.Definicao,
            ConsultarBacklogPorEmpresa.Definicao,
            ConsultarProdutividadeTime.Definicao,
            ConsultarFechamento.Definicao,
            ConsultarIndicadores.Definicao,
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

                    var texto = resposta.Content.Count > 0 ? resposta.Content[0].Text : null;
                    if (string.IsNullOrWhiteSpace(texto))
                    {
                        return "Não consegui gerar uma resposta agora. Tente novamente.";
                    }

                    return await GarantirOpcoesAsync(texto, mensagens, tokenComTimeout);
                }

                mensagens.Add(new AssistantChatMessage(resposta));

                foreach (var chamada in resposta.ToolCalls)
                {
                    logger.LogInformation("CFO Tati: chamando ferramenta {Ferramenta}", chamada.FunctionName);

                    using var argumentos = JsonDocument.Parse(
                        string.IsNullOrWhiteSpace(chamada.FunctionArguments.ToString()) ? "{}" : chamada.FunctionArguments.ToString());

                    var resultado = await ExecutarFerramentaAsync(chamada.FunctionName, argumentos.RootElement, tokenComTimeout);
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
            var bloco = resposta.Content.Count > 0 ? resposta.Content[0].Text : null;

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

    private Task<string> ExecutarFerramentaAsync(string nome, JsonElement input, CancellationToken cancellationToken) =>
        nome switch
        {
            ConsultarSaldo.NomeFerramenta => consultarSaldo.ExecutarAsync(input, cancellationToken),
            ConsultarBacklogStatus.NomeFerramenta => consultarBacklogStatus.ExecutarAsync(input, cancellationToken),
            ConsultarBacklogPorEmpresa.NomeFerramenta => consultarBacklogPorEmpresa.ExecutarAsync(input, cancellationToken),
            ConsultarProdutividadeTime.NomeFerramenta => consultarProdutividadeTime.ExecutarAsync(input, cancellationToken),
            ConsultarFechamento.NomeFerramenta => consultarFechamento.ExecutarAsync(input, cancellationToken),
            ConsultarIndicadores.NomeFerramenta => consultarIndicadores.ExecutarAsync(input, cancellationToken),
            _ => Task.FromResult($"Ferramenta desconhecida: {nome}"),
        };

    private static string CarregarInstrucoes()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "AgenteContabil", "Instrucoes", "instrucoes-v1.md");

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
