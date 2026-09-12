namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class UsuarioViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>
    /// True enquanto o entra_object_id estiver nulo: cadastrado, mas ainda não entrou
    /// nenhuma vez. Serve para a tela mostrar "aguardando primeiro acesso".
    /// </summary>
    public bool PrimeiroAcessoPendente { get; set; }

    public string AvatarInitials { get; set; } = string.Empty;
}
