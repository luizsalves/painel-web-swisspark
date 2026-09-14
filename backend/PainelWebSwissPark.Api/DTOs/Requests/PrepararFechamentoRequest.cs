namespace PainelWebSwissPark.Api.DTOs.Requests;

public class PrepararFechamentoRequest : ChaveFechamentoRequest
{
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoAtual { get; set; }

    /// <summary>1 (mais alta) a 4. Opcional.</summary>
    public short? Prioridade { get; set; }

    public string? PendenciaTexto { get; set; }
    public Guid? PendenciaResponsavelId { get; set; }
}
