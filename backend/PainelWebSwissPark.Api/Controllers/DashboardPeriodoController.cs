using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// A Produtividade por período (ano inteiro, semestre ou meses escolhidos). Os endpoints
/// de um mês só continuam em <see cref="DashboardController"/>, sem mudança.
/// </summary>
[ApiController]
[Route("api/dashboard/periodo")]
[ExigeModulo(Modulos.Conciliacao)]
public class DashboardPeriodoController(IDashboardPeriodoService dashboardPeriodoService) : ControllerBase
{
    /// <summary>Maior período aceito: cada mês é uma leitura do histórico do fechamento.</summary>
    private const int MaximoDeMeses = 12;

    /// <summary>GET /api/dashboard/periodo/empresas?inicio=2025-01&amp;fim=2025-12 — resumo por empresa somado mês a mês.</summary>
    [HttpGet("empresas")]
    public async Task<IActionResult> GetEmpresas(
        [FromQuery] string inicio, [FromQuery] string fim, CancellationToken cancellationToken)
    {
        if (ValidarPeriodo(inicio, fim) is { } erro) return BadRequest(erro);
        return Ok(await dashboardPeriodoService.ObterResumoPorEmpresaAsync(inicio, fim, cancellationToken));
    }

    /// <summary>
    /// GET /api/dashboard/periodo/contas?inicio=2025-01&amp;fim=2025-12&amp;empresa=... — as contas da
    /// empresa (Nome) por filial, com o status de cada uma em cada mês do período.
    /// </summary>
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
