namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class UsuarioViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>
    /// A senha atual foi definida por um administrador e ainda não foi trocada.
    /// A tela mostra isso como "senha provisória".
    /// </summary>
    public bool MustChangePassword { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public string AvatarInitials { get; set; } = string.Empty;
}
