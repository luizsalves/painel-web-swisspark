using System.Security.Claims;

namespace PainelWebSwissPark.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Id do usuário na bi_users, colocado no principal pela <see cref="BiUsersClaimsTransformation"/>.
    /// Só existe em endpoint autorizado — por isso o lançamento em vez de retorno nulo.
    /// </summary>
    public static Guid ObterBiUserId(this ClaimsPrincipal principal)
    {
        var valor = principal.FindFirst(ClaimsPainel.BiUserId)?.Value;

        return Guid.TryParse(valor, out var id)
            ? id
            : throw new InvalidOperationException(
                "Claim do usuário do painel ausente. O endpoint precisa exigir autorização.");
    }
}
