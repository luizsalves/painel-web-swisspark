using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/indicadores")]
[Authorize(Policy = PoliticasAutorizacao.UsuarioAtivo)]
public class IndicadoresController(IIndicadoresService indicadoresService) : ControllerBase
{
    /// <summary>
    /// Índices financeiros e DFC de <paramref name="inicio"/> a <paramref name="fim"/> (AAAA-MM, inclusive).
    /// O balanço é o do último dia de <paramref name="fim"/>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string inicio, [FromQuery] string fim,
        [FromQuery] short codColigada, [FromQuery] short? codFilial,
        CancellationToken cancellationToken)
    {
        if (!PeriodoValido(inicio) || !PeriodoValido(fim))
            return BadRequest("Período inválido. Use AAAA-MM.");
        if (codColigada <= 0 || codFilial is <= 0)
            return BadRequest("Coligada ou filial inválida.");

        var dataInicial = new DateTime(int.Parse(inicio[..4]), int.Parse(inicio[5..]), 1);
        var dataFinal = new DateTime(int.Parse(fim[..4]), int.Parse(fim[5..]), 1).AddMonths(1);
        if (dataFinal <= dataInicial)
            return BadRequest("O mês final não pode ser anterior ao mês inicial.");

        return Ok(await indicadoresService.CalcularAsync(
            dataInicial, dataFinal, codColigada, codFilial, cancellationToken));
    }

    /// <summary>Gera a planilha da DFC que está na tela, sem recalcular nada.</summary>
    [HttpPost("dfc/exportar-excel")]
    [RequestSizeLimit(5_000_000)]
    public IActionResult ExportarDfcExcel([FromBody] ExportarDfcRequest request) =>
        File(DfcExcel.Gerar(request), "application/vnd.ms-excel", $"dfc-{request.Inicio}-a-{request.Fim}.xls");

    private static bool PeriodoValido(string? valor) =>
        !string.IsNullOrWhiteSpace(valor) && Regex.IsMatch(valor, @"^\d{4}-(0[1-9]|1[0-2])$");
}
