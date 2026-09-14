using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ExigeModulo(Modulos.Conciliacao)]
public class ContasContabeisController(IContasContabeisTotvsService contasContabeisService) : ControllerBase
{
    /// <summary>
    /// GET /api/contascontabeis — plano de contas das quatro bases TOTVS
    /// (corpore, corpore_bsb, corpore_mao, corpore_neu), só para leitura.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var contas = await contasContabeisService.ListarAsync(cancellationToken);
        return Ok(contas);
    }
}
