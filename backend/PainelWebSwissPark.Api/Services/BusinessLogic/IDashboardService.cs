using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IDashboardService
{
    /// <summary>anoMes no formato AAAA-MM. empresa (opcional) filtra por Nome (razão social).</summary>
    Task<DashboardResumoViewModel> ObterResumoAsync(
        string anoMes, string? empresa, CancellationToken cancellationToken);

    /// <summary>Resumo de cada empresa ativa (chave = Nome), calculado numa leitura só.</summary>
    Task<Dictionary<string, DashboardResumoViewModel>> ObterResumoPorEmpresaAsync(
        string anoMes, CancellationToken cancellationToken);

    /// <summary>
    /// Contas de uma empresa (Nome) no período, uma por filial ativa x conta ativa,
    /// com o status atual — o mesmo universo que o resumo conta.
    /// </summary>
    Task<List<DashboardContaViewModel>> ObterContasDaEmpresaAsync(
        string anoMes, string empresa, CancellationToken cancellationToken);

    /// <summary>
    /// Período mais recente com algum fechamento registrado — usado pelo front pra abrir o
    /// dashboard já num mês com dado, em vez do mês calendário atual (que pode estar vazio).
    /// </summary>
    Task<string?> ObterUltimoPeriodoComDadosAsync(CancellationToken cancellationToken);
}
