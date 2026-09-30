using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// GET /api/diferencas?anoMes=2026-08&amp;empresa= — contas com diferença entre o
/// saldo do razão e a composição do saldo (títulos/NFs em aberto), por empresa.
/// Só leitura do TOTVS.
/// </summary>
[ApiController]
[Route("api/diferencas")]
[ExigeModulo(Modulos.Conciliacao)]
public class DiferencasController(IDiferencasService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string anoMes, [FromQuery] string? empresa, CancellationToken cancellationToken)
    {
        return Ok(await service.ObterAsync(anoMes, empresa, cancellationToken));
    }
}
