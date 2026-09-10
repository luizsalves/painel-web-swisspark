using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FechamentosController(IFechamentosService fechamentosService) : ControllerBase
{
    /// <summary>
    /// GET /api/fechamentos?empresaId=1&amp;contaContabilId=1&amp;periodo=2026-01
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int empresaId,
        [FromQuery] int contaContabilId,
        [FromQuery] string periodo,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(
                $"{periodo}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var periodoInicioMes))
        {
            return BadRequest("Período inválido. Use o formato AAAA-MM.");
        }

        var fechamento = await fechamentosService.ObterFechamentoAsync(
            empresaId, contaContabilId, periodoInicioMes, cancellationToken);

        if (fechamento is null)
        {
            return NotFound();
        }

        return Ok(fechamento);
    }
}
