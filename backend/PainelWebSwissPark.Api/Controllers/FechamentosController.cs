using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Fechamentos é tela da conciliação: quem é só do mkt não passa daqui, mesmo
// digitando a URL direto.
[ExigeModulo(Modulos.Conciliacao)]
public class FechamentosController(IFechamentosService fechamentosService) : ControllerBase
{
    // Exporta o retrato carregado no PDF, sem alterar o fechamento.
    [HttpPost("exportar-excel")]
    [RequestSizeLimit(30_000_000)]
    public IActionResult ExportarExcel([FromBody] ExportarFechamentoRequest request) =>
        File(FechamentoExcel.Gerar(request), "application/vnd.ms-excel", $"fechamento-{request.Periodo}.xls");

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
