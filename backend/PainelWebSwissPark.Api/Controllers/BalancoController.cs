using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/balanco")]
[Authorize(Policy = PoliticasAutorizacao.UsuarioAtivo)]
public class BalancoController(IBalancoService balancoService) : ControllerBase
{
    /// <summary>Balanço patrimonial no último dia de <paramref name="mes"/> (AAAA-MM).</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string mes, [FromQuery] short codColigada, [FromQuery] short? codFilial,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mes) || !Regex.IsMatch(mes, @"^\d{4}-(0[1-9]|1[0-2])$"))
            return BadRequest("Mês inválido. Use AAAA-MM.");
        if (codColigada <= 0 || codFilial is <= 0)
            return BadRequest("Coligada ou filial inválida.");

        var data = new DateTime(int.Parse(mes[..4]), int.Parse(mes[5..]), 1);
        return Ok(await balancoService.CalcularAsync(data, codColigada, codFilial, cancellationToken));
    }

    /// <summary>Gera a planilha do balanço que está na tela, sem recalcular nada.</summary>
    [HttpPost("exportar-excel")]
    [RequestSizeLimit(5_000_000)]
    public IActionResult ExportarExcel([FromBody] ExportarBalancoRequest request) =>
        File(BalancoExcel.Gerar(request), "application/vnd.ms-excel", $"balanco-{request.Mes}.xls");
}
