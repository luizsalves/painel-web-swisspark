namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// Status atual de uma conta/mês, junto de quem fez cada etapa e quando —
/// derivado da linha mais recente (status atual) e das linhas anteriores de
/// cada tipo (quem preparou, quem conferiu, quem fechou).
/// </summary>
public class FechamentoValidacaoViewModel
{
    /// <summary>Ver <see cref="Entities.StatusFechamento"/>. "Pendente" quando não há nenhuma linha ainda.</summary>
    public string Status { get; set; } = string.Empty;

    public short? Prioridade { get; set; }
    public decimal? SaldoAnterior { get; set; }
    public decimal? SaldoAtual { get; set; }

    public Guid? PreparadoPorId { get; set; }
    public string? PreparadoPorNome { get; set; }
    public DateTimeOffset? PreparadoEm { get; set; }

    public Guid? ConferidoPorId { get; set; }
    public string? ConferidoPorNome { get; set; }
    public DateTimeOffset? ConferidoEm { get; set; }

    public Guid? FechadoPorId { get; set; }
    public string? FechadoPorNome { get; set; }
    public DateTimeOffset? FechadoEm { get; set; }

    public string? PendenciaTexto { get; set; }
    public Guid? PendenciaResponsavelId { get; set; }
    public string? PendenciaResponsavelNome { get; set; }
    public DateTimeOffset? PendenciaRegistradaEm { get; set; }
}
