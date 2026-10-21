using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// O histórico dos indicadores, mês a mês: as comparações e os 12 meses da Visão do
/// contador. Separado de <see cref="IndicadoresController"/>, que continua sem mudança.
/// </summary>
[ApiController]
[Route("api/indicadores/mensal")]
[Authorize(Policy = PoliticasAutorizacao.UsuarioAtivo)]
public class IndicadoresMensaisController(IIndicadoresMensaisService indicadoresMensaisService) : ControllerBase
{
    /// <summary>Os 12 meses do minigráfico e mais 12 para trás, para as comparações.</summary>
    private const int MaximoDeMeses = 24;

    /// <summary>
    /// GET /api/indicadores/mensal?inicio=2024-04&amp;fim=2026-03&amp;codColigada=1 — a base dos
    /// indicadores de cada mês de <paramref name="inicio"/> a <paramref name="fim"/> (AAAA-MM, inclusive).
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
        if (dataFinal > dataInicial.AddMonths(MaximoDeMeses))
            return BadRequest($"O histórico pode ter no máximo {MaximoDeMeses} meses.");

        return Ok(await indicadoresMensaisService.CalcularAsync(
            dataInicial, dataFinal, codColigada, codFilial, cancellationToken));
    }

    private static bool PeriodoValido(string? valor) =>
        !string.IsNullOrWhiteSpace(valor) && Regex.IsMatch(valor, @"^\d{4}-(0[1-9]|1[0-2])$");
}
