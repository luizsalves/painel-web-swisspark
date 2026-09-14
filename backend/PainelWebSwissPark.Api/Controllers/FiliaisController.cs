using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ExigeModulo(Modulos.Conciliacao)]
public class FiliaisController(IFiliaisService filiaisService) : ControllerBase
{
    /// <summary>
    /// GET /api/filiais/mesmo-codigo — GFILIAL com ela mesma, casada por
    /// CODFILIAL, só para leitura (nunca grava nada no TOTVS).
    /// </summary>
    [HttpGet("mesmo-codigo")]
    public async Task<IActionResult> ObterComMesmoCodigo(CancellationToken cancellationToken)
    {
        var resultado = await filiaisService.ObterFiliaisComMesmoCodigoAsync(cancellationToken);
        return Ok(resultado);
    }
}
