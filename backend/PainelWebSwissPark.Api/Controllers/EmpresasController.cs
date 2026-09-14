using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ExigeModulo(Modulos.Conciliacao)]
public class EmpresasController(IEmpresasService empresasService) : ControllerBase
{
    /// <summary>
    /// GET /api/empresas — coligadas/filiais das quatro bases TOTVS (corpore,
    /// corpore_bsb, corpore_mao, corpore_neu), só para leitura.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var empresas = await empresasService.ListarAsync(cancellationToken);
        return Ok(empresas);
    }
}
