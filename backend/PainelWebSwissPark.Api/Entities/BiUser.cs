namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// Quem pode entrar no painel. O Entra ID autentica (prova que a pessoa é quem diz ser);
/// esta tabela autoriza (diz se ela pode entrar e com qual perfil). Não guarda senha.
/// </summary>
public class BiUser
{
    public Guid Id { get; set; }

    /// <summary>
    /// oid do Entra ID — identificador imutável da pessoa no tenant.
    /// Nulo até o primeiro login: o administrador cadastra por e-mail, e o backend
    /// grava o oid quando a pessoa entra pela primeira vez.
    /// </summary>
    public Guid? EntraObjectId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = PerfisUsuario.Analista;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}
