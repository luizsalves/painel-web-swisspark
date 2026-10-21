using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// Os lançamentos do fechamento das empresas de filiais consolidadas. Separado de
/// <see cref="LancamentosController"/>, que continua sem mudança.
/// </summary>
[ApiController]
[Route("api/lancamentos/consolidado")]
[ExigeModulo(Modulos.Conciliacao)]
public class LancamentosConsolidadosController(ILancamentosConsolidadosService lancamentosService) : ControllerBase
{
    /// <summary>
    /// GET /api/lancamentos/consolidado?codColigada=14&amp;empresa=AEPL AGROPECUARIA LTDA&amp;anoMes=2026-01&amp;codConta=1.1.01.01.0001
    /// — o mesmo retorno de GET /api/lancamentos, com todas as filiais da empresa juntas; só para leitura.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] short codColigada, [FromQuery] string empresa, [FromQuery] string anoMes,
        [FromQuery] string codConta, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(anoMes) || !Regex.IsMatch(anoMes, @"^\d{4}-\d{2}$"))
        {
            return BadRequest("Período inválido. Use o formato AAAA-MM.");
        }

        if (!EmpresasConsolidadas.Consolida(empresa))
        {
            return BadRequest("Essa empresa fecha por filial: use /api/lancamentos.");
        }

        var lancamentos = await lancamentosService.ListarAsync(
            codColigada, empresa.Trim(), anoMes, codConta, cancellationToken);

        return Ok(lancamentos);
    }
}
