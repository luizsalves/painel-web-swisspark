namespace PainelWebSwissPark.Api.DTOs.Requests;

/// <summary>Identifica a conta/mês que está sendo preparada, conferida, fechada ou revertida.</summary>
public class ChaveFechamentoRequest
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string CodConta { get; set; } = string.Empty;

    /// <summary>Formato AAAA-MM.</summary>
    public string AnoMes { get; set; } = string.Empty;
}
