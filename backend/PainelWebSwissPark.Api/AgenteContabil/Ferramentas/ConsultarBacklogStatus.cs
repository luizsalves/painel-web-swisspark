using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Ferramenta que a CFO Tati chama quando precisa de números de backlog de
/// conciliação (contas pendentes/preparadas/conferidas/fechadas) — os mesmos
/// KPIs fixos da tela IA Planning. Não confundir com <see cref="ConsultarSaldo"/>,
/// que traz o saldo contábil em R$ de uma conta específica.
/// </summary>
public class ConsultarBacklogStatus(IDashboardService dashboardService)
{
    public const string NomeFerramenta = "consultar_status_backlog";

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Consulta o backlog de conciliação (quantas contas estão pendentes, preparadas, conferidas e " +
            "fechadas) para um período e empresa. Use para perguntas sobre status/pendências, não sobre valor em R$.",
        functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                anoMes = new { type = "string", description = "Período no formato AAAA-MM." },
                empresa = new { type = "string", description = "Nome (razão social) da empresa. Omita para consultar todas." },
            },
            required = new[] { "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var anoMes = input.TryGetProperty("anoMes", out var anoMesEl) ? anoMesEl.GetString() ?? "" : "";
        var empresa = input.TryGetProperty("empresa", out var empresaEl) ? empresaEl.GetString() : null;

        var resumo = await dashboardService.ObterResumoAsync(anoMes, empresa, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            anoMes,
            empresa = string.IsNullOrWhiteSpace(empresa) ? "todas" : empresa,
            backlogTotal = resumo.BacklogTotal,
            preparados = resumo.Preparados,
            conferidos = resumo.Conferidos,
            fechados = resumo.Fechados,
            pendentes = resumo.Pendentes,
        });
    }
}
