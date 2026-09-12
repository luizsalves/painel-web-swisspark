using System.Security.Claims;

namespace PainelWebSwissPark.Api.Services.Auth;

/// <summary>
/// Retrato imutável do usuário autorizado. É um snapshot destacado do EF de propósito:
/// pode ser cacheado entre requisições sem arrastar entidade rastreada junto.
/// </summary>
public sealed record UsuarioAutorizado(
    Guid Id,
    Guid? EntraObjectId,
    string Email,
    string Name,
    string Role,
    bool IsActive);

public interface IUsuarioAtualService
{
    /// <summary>
    /// Traduz o token do Entra ID para o registro correspondente na bi_users.
    /// Casa por oid; no primeiro login casa por e-mail e grava o oid.
    /// Retorna null quando a pessoa não está cadastrada — quem decide o 403 é a autorização.
    /// O resultado ainda pode estar inativo: cabe ao chamador checar <c>IsActive</c>.
    /// </summary>
    Task<UsuarioAutorizado?> ResolverAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
