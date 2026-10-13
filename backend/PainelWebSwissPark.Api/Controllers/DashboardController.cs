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
    [HttpGet("empresas")]
    public async Task<IActionResult> GetEmpresas([FromQuery] string anoMes, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(anoMes + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
            return BadRequest("Período inválido. Use AAAA-MM.");
        return Ok(await dashboardService.ObterResumoPorEmpresaAsync(anoMes, cancellationToken));
    }

    /// <summary>
    /// GET /api/dashboard/contas?anoMes=2026-01&amp;empresa=... — as contas da empresa
    /// (Nome) no período, por filial, com o status atual de cada uma.
    /// </summary>
    [HttpGet("contas")]
    public async Task<IActionResult> GetContas(
        [FromQuery] string anoMes, [FromQuery] string empresa, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(anoMes + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
            return BadRequest("Período inválido. Use AAAA-MM.");
        if (string.IsNullOrWhiteSpace(empresa))
            return BadRequest("Informe a empresa.");
        return Ok(await dashboardService.ObterContasDaEmpresaAsync(anoMes, empresa, cancellationToken));
    }

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
