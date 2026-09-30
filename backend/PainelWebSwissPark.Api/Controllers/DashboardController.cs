using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ExigeModulo(Modulos.Conciliacao)]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// GET /api/dashboard/resumo?anoMes=2026-01&amp;empresa=... — backlog de
    /// conciliação do período: das combinações de filial ativa x conta
    /// contábil ativa esperadas, quantas estão em cada etapa do fechamento.
    /// </summary>
    [HttpGet("resumo")]
    public async Task<IActionResult> GetResumo(
        [FromQuery] string anoMes, [FromQuery] string? empresa, CancellationToken cancellationToken)
    {
        var resumo = await dashboardService.ObterResumoAsync(anoMes, empresa, cancellationToken);
        return Ok(resumo);
    }

    /// <summary>GET /api/dashboard/ultimo-periodo — mês mais recente com algum fechamento registrado.</summary>
    [HttpGet("ultimo-periodo")]
    public async Task<IActionResult> GetUltimoPeriodo(CancellationToken cancellationToken)
    {
        var anoMes = await dashboardService.ObterUltimoPeriodoComDadosAsync(cancellationToken);
        return Ok(new { anoMes });
    }
}
