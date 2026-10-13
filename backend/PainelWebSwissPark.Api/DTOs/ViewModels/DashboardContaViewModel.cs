namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// Uma combinação filial ativa x conta contábil ativa de uma empresa no período,
/// com o status atual do fechamento — o detalhe por trás dos números do dashboard.
/// </summary>
public class DashboardContaViewModel
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    /// <summary>"1 - Nome fantasia", como no combobox de Filial.</summary>
    public string Filial { get; set; } = string.Empty;
    public string CodConta { get; set; } = string.Empty;
    public string Reduzido { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Pendente, Preparado, Conferido ou Fechado.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Quem gravou a última ação; nulo enquanto a conta está pendente.</summary>
    public string? UltimaAcaoPor { get; set; }
    public DateTimeOffset? UltimaAcaoEm { get; set; }
}
