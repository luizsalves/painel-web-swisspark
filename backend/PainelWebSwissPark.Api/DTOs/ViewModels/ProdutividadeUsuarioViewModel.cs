namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>Quantas ações (Preparado, Conferido ou Fechado) um usuário registrou.</summary>
public class ProdutividadeUsuarioViewModel
{
    public Guid UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}
