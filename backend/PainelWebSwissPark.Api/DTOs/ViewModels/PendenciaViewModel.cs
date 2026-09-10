namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class PendenciaViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
}
