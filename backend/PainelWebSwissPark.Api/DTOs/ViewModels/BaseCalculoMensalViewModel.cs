namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>A base dos indicadores de um mês: o balanço no último dia dele e o resultado do mês.</summary>
public class BaseCalculoMensalViewModel
{
    /// <summary>AAAA-MM.</summary>
    public string AnoMes { get; set; } = string.Empty;
    public BaseCalculoIndicadoresViewModel BaseCalculo { get; set; } = new();
}
