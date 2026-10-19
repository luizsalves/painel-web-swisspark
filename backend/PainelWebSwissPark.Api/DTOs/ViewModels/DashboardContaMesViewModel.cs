namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// A mesma combinação filial x conta de <see cref="DashboardContaViewModel"/>, num mês
/// de um período — a resposta de /api/dashboard/periodo/contas tem uma linha por mês.
/// </summary>
public class DashboardContaMesViewModel : DashboardContaViewModel
{
    /// <summary>Mês (AAAA-MM) a que o status se refere.</summary>
    public string AnoMes { get; set; } = string.Empty;
}
