using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Ranking do time: quantas contas cada usuário preparou, conferiu ou fechou.
/// Mesma consulta do GET /api/fechamento-validacao/produtividade.
/// </summary>
public class ConsultarProdutividadeTime(IFechamentoValidacaoService fechamentoValidacaoService)
{
    public const string NomeFerramenta = "consultar_produtividade_time";

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Ranking de usuários por quantidade de contas que cada um preparou, conferiu ou fechou. " +
            "Use para perguntas como 'quem mais preparou/conferiu/fechou', 'produtividade do time', " +
            "'quem fez mais fechamento este mês'.",
        functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                status = new
                {
                    type = "string",
                    @enum = new[] { "Preparado", "Conferido", "Fechado" },
                    description = "Qual ação contar. 'Finalizado' na tela é o mesmo que Fechado.",
                },
                anoMes = new
                {
                    type = "string",
                    description = "Período no formato AAAA-MM. Omita para contar todos os períodos.",
                },
            },
            required = new[] { "status" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var status = input.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        var anoMes = input.TryGetProperty("anoMes", out var anoMesEl) ? anoMesEl.GetString() : null;

        var statusNormalizado = FechamentoValidacaoService.NormalizarStatusDeAcao(status);
        if (statusNormalizado is null)
        {
            return JsonSerializer.Serialize(new
            {
                erro = "Status inválido — informe Preparado, Conferido ou Fechado.",
            });
        }

        var ranking = await fechamentoValidacaoService.ObterProdutividadeAsync(
            statusNormalizado, anoMes, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            status = statusNormalizado,
            anoMes = string.IsNullOrWhiteSpace(anoMes) ? "todos os períodos" : anoMes,
            total = ranking.Sum(r => r.Quantidade),
            itens = ranking.Select(r => new { usuario = r.Usuario, quantidade = r.Quantidade }),
        });
    }
}
