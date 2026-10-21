using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/empresas-consolidadas")]
[Authorize(Policy = PoliticasAutorizacao.UsuarioAtivo)]
public class EmpresasConsolidadasController(IContasContabeisTotvsService contasService) : ControllerBase
{
    /// <summary>
    /// GET /api/empresas-consolidadas — as empresas (coligada + nome, como vêm em
    /// GET /api/balancete/empresas) que fecham com todas as filiais juntas, na filial 0.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var empresas = await contasService.ListarEmpresasAsync(cancellationToken);
        return Ok(empresas.Where(e => EmpresasConsolidadas.Consolida(e.Nome)));
    }
}
