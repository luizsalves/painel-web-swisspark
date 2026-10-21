using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// A Produtividade por período com as empresas de filiais consolidadas contadas uma vez
/// por conta (filial 0), como o fechamento delas. As demais empresas saem iguais a
/// <see cref="DashboardPeriodoController"/>, que continua sem mudança.
/// </summary>
[ApiController]
[Route("api/dashboard/periodo/consolidado")]
[ExigeModulo(Modulos.Conciliacao)]
public class DashboardPeriodoConsolidadoController(DashboardPeriodoConsolidadoService dashboardPeriodoService) : ControllerBase
{
    /// <summary>Maior período aceito: cada mês é uma leitura do histórico do fechamento.</summary>
    private const int MaximoDeMeses = 12;

    /// <summary>GET /api/dashboard/periodo/consolidado/empresas?inicio=2025-01&amp;fim=2025-12</summary>
    [HttpGet("empresas")]
    public async Task<IActionResult> GetEmpresas(
        [FromQuery] string inicio, [FromQuery] string fim, CancellationToken cancellationToken)
    {
        if (ValidarPeriodo(inicio, fim) is { } erro) return BadRequest(erro);
        return Ok(await dashboardPeriodoService.ObterResumoPorEmpresaAsync(inicio, fim, cancellationToken));
    }

    /// <summary>GET /api/dashboard/periodo/consolidado/contas?inicio=2025-01&amp;fim=2025-12&amp;empresa=...</summary>
    [HttpGet("contas")]
    public async Task<IActionResult> GetContas(
        [FromQuery] string inicio, [FromQuery] string fim, [FromQuery] string empresa,
        CancellationToken cancellationToken)
    {
        if (ValidarPeriodo(inicio, fim) is { } erro) return BadRequest(erro);
        if (string.IsNullOrWhiteSpace(empresa)) return BadRequest("Informe a empresa.");
        return Ok(await dashboardPeriodoService.ObterContasDaEmpresaAsync(inicio, fim, empresa, cancellationToken));
    }

    /// <summary>Mensagem do problema com o período, ou nulo quando ele é válido.</summary>
    private static string? ValidarPeriodo(string? inicio, string? fim)
    {
        static DateOnly? Ler(string? valor) => DateOnly.TryParseExact(valor + "-01", "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ? data : null;

        var mesInicial = Ler(inicio);
        var mesFinal = Ler(fim);
        if (mesInicial is null || mesFinal is null) return "Período inválido. Use AAAA-MM.";
        if (mesFinal < mesInicial) return "O mês final não pode ser anterior ao inicial.";
        if (mesFinal >= mesInicial.Value.AddMonths(MaximoDeMeses)) return $"O período pode ter no máximo {MaximoDeMeses} meses.";
        return null;
    }
}
