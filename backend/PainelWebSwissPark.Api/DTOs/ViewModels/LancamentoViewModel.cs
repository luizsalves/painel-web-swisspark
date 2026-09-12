namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class LancamentoViewModel
{
    public string Data { get; set; } = string.Empty;
    public string Historico { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Tipo { get; set; } = string.Empty;
}
