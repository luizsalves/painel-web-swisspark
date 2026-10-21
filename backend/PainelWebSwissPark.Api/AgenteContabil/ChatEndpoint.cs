using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.AgenteContabil;

/// <summary>
/// Endpoint de chat da CFO Tati (tela IA Planning). Restrito ao superadministrador
/// por enquanto — diferente do resto do painel, que libera por módulo/perfil.
/// </summary>
[ApiController]
[Route("api/agente-contabil")]
[ExigeSuperAdmin]
public class ChatEndpoint(HarnessService harnessService) : ControllerBase
{
    /// <summary>POST /api/agente-contabil/chat</summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] PerguntarCfoTatiRequest pedido, CancellationToken cancellationToken)
    {
        var resposta = await harnessService.ExecutarAsync(
            pedido.Pergunta, pedido.AnoMes, pedido.Empresa, pedido.Historico, cancellationToken);

        return Ok(new RespostaCfoTatiViewModel { Resposta = resposta });
    }

    /// <summary>
    /// POST /api/agente-contabil/chat/filtros — a mesma conversa, com todos os filtros da
    /// tela (período, filial, conta e status) no contexto da Tati.
    /// </summary>
    [HttpPost("chat/filtros")]
    public async Task<IActionResult> ChatComFiltros(
        [FromBody] PerguntarCfoTatiComFiltrosRequest pedido, CancellationToken cancellationToken)
    {
        var resposta = await harnessService.ExecutarComFiltrosAsync(pedido, cancellationToken);

        return Ok(new RespostaCfoTatiViewModel { Resposta = resposta });
    }
}
