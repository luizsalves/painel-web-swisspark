using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using PainelWebSwissPark.Api.Options;

namespace PainelWebSwissPark.Api.AgenteContabil;

/// <summary>
/// Fala com o modelo — o HarnessService decide o quê perguntar, este só envia.
/// A DeepSeek expõe uma API compatível com a da OpenAI, então reaproveita o SDK
/// oficial da OpenAI apontado pro endpoint da DeepSeek, em vez de montar HTTP cru.
/// </summary>
public class ModelClient
{
    private readonly ChatClient chatClient;

    public ModelClient(IOptions<DeepSeekOptions> opcoesDeepSeek)
    {
        var opcoes = opcoesDeepSeek.Value;

        var client = new OpenAIClient(
            new ApiKeyCredential(opcoes.ApiKey ?? ""),
            new OpenAIClientOptions { Endpoint = new Uri(opcoes.Endpoint) });

        chatClient = client.GetChatClient(opcoes.Model);
    }

    public async Task<ChatCompletion> CriarMensagemAsync(
        IEnumerable<ChatMessage> mensagens, IEnumerable<ChatTool> ferramentas, CancellationToken cancellationToken)
    {
        // Sem isso vale o padrão da DeepSeek, curto o bastante pra cortar uma
        // resposta com gráfico no meio do JSON.
        var opcoes = new ChatCompletionOptions { MaxOutputTokenCount = 8000 };

        foreach (var ferramenta in ferramentas)
        {
            opcoes.Tools.Add(ferramenta);
        }

        var resposta = await chatClient.CompleteChatAsync(mensagens, opcoes, cancellationToken);
        return resposta.Value;
    }
}
