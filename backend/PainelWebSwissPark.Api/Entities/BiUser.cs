namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// Quem pode entrar no painel e a senha com que entra. A senha nunca é guardada
/// em texto: só o hash PBKDF2 produzido pelo PasswordHasher do ASP.NET Core.
///
/// O perfil NÃO fica aqui — ele é por módulo, na <see cref="BiUserModule"/>.
/// </summary>
public class BiUser
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 da senha. Nunca sai da camada de serviço.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Verdadeiro quando a senha atual foi definida por um administrador.
    /// A pessoa é obrigada a trocar antes de usar o painel — assim ninguém
    /// continua acessando com uma senha que outra pessoa conhece.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// Enxerga e administra todos os módulos. É quem cria os administradores de
    /// cada módulo — sem ele, o administrador da conciliação não teria como
    /// criar o administrador do outro sistema, já que um não enxerga o outro.
    ///
    /// Criado uma única vez, pelas variáveis PAINEL_ADMIN_*.
    /// </summary>
    public bool IsSuperAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public List<BiUserModule> Modules { get; set; } = [];
}
