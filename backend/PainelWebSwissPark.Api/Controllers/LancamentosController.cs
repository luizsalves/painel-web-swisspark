using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ExigeModulo(Modulos.Conciliacao)]
public class LancamentosController(ILancamentosTotvsService lancamentosService) : ControllerBase
{
    /// <summary>
    /// GET /api/lancamentos?codColigada=1&amp;codFilial=1&amp;anoMes=2026-01&amp;codConta=1.1.01.01.0001
    /// — consulta as quatro bases TOTVS em paralelo e junta o resultado; só para leitura.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] short codColigada, [FromQuery] short codFilial, [FromQuery] string anoMes,
        [FromQuery] string codConta, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(anoMes) || !Regex.IsMatch(anoMes, @"^\d{4}-\d{2}$"))
        {
            return BadRequest("Período inválido. Use o formato AAAA-MM.");
        }

        var lancamentos = await lancamentosService.ListarAsync(
            codColigada, codFilial, anoMes, codConta, cancellationToken);

        return Ok(lancamentos);
    }
}
