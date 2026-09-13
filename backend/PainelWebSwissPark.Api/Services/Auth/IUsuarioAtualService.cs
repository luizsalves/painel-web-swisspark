using System.Security.Claims;

namespace PainelWebSwissPark.Api.Services.Auth;

/// <summary>
/// Retrato imutável do usuário autorizado. É um snapshot destacado do EF de propósito:
/// pode ser cacheado entre requisições sem arrastar entidade rastreada junto.
/// </summary>
public sealed record UsuarioAutorizado(
    Guid Id,
    string Email,
    string Name,
    string Role,
    bool IsActive,
    bool MustChangePassword);

public interface IUsuarioAtualService
{
    /// <summary>
    /// Traduz o token para o registro correspondente na bi_users.
    ///
    /// O token prova quem a pessoa é; perfil e situação vêm sempre do banco, nunca
    /// do próprio token. É isso que faz desativar ou rebaixar alguém valer na
    /// requisição seguinte.
    ///
    /// Retorna null quando o registro não existe mais. O resultado ainda pode estar
    /// inativo: cabe ao chamador checar <c>IsActive</c>.
    /// </summary>
    Task<UsuarioAutorizado?> ResolverAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
