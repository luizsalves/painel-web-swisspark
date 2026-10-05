using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/balancete")]
[Authorize(Policy = PoliticasAutorizacao.UsuarioAtivo)]
public class BalanceteController(
    IBalanceteService balanceteService,
    IContasContabeisTotvsService contasService) : ControllerBase
{
    [HttpGet("empresas")]
    public async Task<IActionResult> GetEmpresas(CancellationToken cancellationToken) =>
        Ok(await contasService.ListarEmpresasAsync(cancellationToken));

    [HttpGet("filiais")]
    public async Task<IActionResult> GetFiliais([FromQuery] short? codColigada, CancellationToken cancellationToken)
    {
        if (codColigada is <= 0) return BadRequest("Coligada inválida.");
        return Ok(await contasService.ListarColigadasFiliaisAsync(cancellationToken, codColigada));
    }

    [HttpGet("contas")]
    public async Task<IActionResult> GetContas(
        [FromQuery] short? codColigada, [FromQuery] short? codFilial, CancellationToken cancellationToken)
    {
        if (codColigada is <= 0 || codFilial is <= 0) return BadRequest("Coligada ou filial inválida.");
        return Ok(await contasService.ListarAsync(cancellationToken, codColigada: codColigada, codFilial: codFilial));
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string anoMes, [FromQuery] short? codColigada,
        [FromQuery] short? codFilial, [FromQuery] string? codConta,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(anoMes) || !Regex.IsMatch(anoMes, @"^\d{4}-(0[1-9]|1[0-2])$"))
            return BadRequest("Período inválido. Use AAAA-MM.");
        if (codColigada is <= 0 || codFilial is <= 0)
            return BadRequest("Coligada ou filial inválida.");

        var inicio = new DateTime(int.Parse(anoMes[..4]), int.Parse(anoMes[5..]), 1);
        var conta = string.IsNullOrWhiteSpace(codConta) ? null : codConta.Trim();
        return Ok(await balanceteService.ListarAsync(
            inicio, codColigada, codFilial, conta, cancellationToken));
    }
}
