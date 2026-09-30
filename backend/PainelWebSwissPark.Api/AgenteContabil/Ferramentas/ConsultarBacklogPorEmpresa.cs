using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Quebra o backlog de conciliação por empresa — é o que a Tati usa quando o
/// pedido é uma comparação ou um gráfico entre empresas, em vez do resumo
/// único de <see cref="ConsultarBacklogStatus"/>.
/// </summary>
public class ConsultarBacklogPorEmpresa(IDashboardService dashboardService)
{
    // Com dezenas de empresas, mandar todas faz a resposta do modelo (texto +
    // bloco do gráfico) ficar longa a ponto de chegar cortada — o gráfico só
    // precisa das maiores; o resto vai somado.
    private const int MaxEmpresas = 15;

    public const string NomeFerramenta = "consultar_backlog_por_empresa";

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Quebra o backlog de conciliação por empresa, para um período: as 15 empresas com mais contas pendentes " +
            "(pendentes, preparados, conferidos, fechados, backlog total) e a soma das demais em 'demais'. " +
            "Use quando o usuário pedir uma comparação ou um gráfico entre empresas.",
        functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                anoMes = new { type = "string", description = "Período no formato AAAA-MM." },
            },
            required = new[] { "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var anoMes = input.TryGetProperty("anoMes", out var anoMesEl) ? anoMesEl.GetString() ?? "" : "";

        var porEmpresa = await dashboardService.ObterResumoPorEmpresaAsync(anoMes, cancellationToken);

        var ordenadas = porEmpresa
            .OrderByDescending(par => par.Value.Pendentes)
            .ThenBy(par => par.Key)
            .ToList();

        var itens = ordenadas
            .Take(MaxEmpresas)
            .Select(par => new
            {
                empresa = par.Key,
                pendentes = par.Value.Pendentes,
                preparados = par.Value.Preparados,
                conferidos = par.Value.Conferidos,
                fechados = par.Value.Fechados,
                backlogTotal = par.Value.BacklogTotal,
            })
            .ToList();

        var restantes = ordenadas.Skip(MaxEmpresas).ToList();
        var demais = restantes.Count == 0
            ? null
            : new
            {
                quantidadeDeEmpresas = restantes.Count,
                pendentes = restantes.Sum(par => par.Value.Pendentes),
                backlogTotal = restantes.Sum(par => par.Value.BacklogTotal),
            };

        return JsonSerializer.Serialize(new { anoMes, totalDeEmpresas = ordenadas.Count, itens, demais });
    }
}
