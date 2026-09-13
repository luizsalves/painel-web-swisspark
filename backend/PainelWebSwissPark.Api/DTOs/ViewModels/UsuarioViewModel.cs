namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class ModuloDoUsuarioViewModel
{
    public string ModuleKey { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UsuarioViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>Enxerga e administra todos os módulos.</summary>
    public bool IsSuperAdmin { get; set; }

    /// <summary>
    /// A senha atual foi definida por um administrador e ainda não foi trocada.
    /// A tela mostra isso como "senha provisória".
    /// </summary>
    public bool MustChangePassword { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public string AvatarInitials { get; set; } = string.Empty;

    /// <summary>Os módulos da pessoa, com o perfil em cada um.</summary>
    public List<ModuloDoUsuarioViewModel> Modulos { get; set; } = [];
}

public class ModuloViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
