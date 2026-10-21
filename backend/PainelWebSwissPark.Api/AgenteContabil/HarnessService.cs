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

        // As ferramentas consultam um mês por vez; num período, o mês final é o padrão.
        var periodo = anoMesInicial == pedido.AnoMes
            ? $"período {pedido.AnoMes}"
            : $"período de {anoMesInicial} a {pedido.AnoMes} (as ferramentas consultam um mês por vez: use {pedido.AnoMes}, " +
              "o mês final, a menos que o usuário peça outro mês ou a evolução do período)";
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

        var systemPrompt = $"{Instrucoes.Value}\n\n## Contexto da conversa\n\n{contexto}";

        List<ChatTool> ferramentas =
        [
            ConsultarSaldo.Definicao,
            ConsultarBacklogStatus.Definicao,
            ConsultarBacklogPorEmpresa.Definicao,
            ConsultarProdutividadeTime.Definicao,
            ConsultarFechamento.Definicao,
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
}
