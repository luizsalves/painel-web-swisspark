using System.Security.Claims;

namespace PainelWebSwissPark.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Id do usuário na bi_users, vindo do claim "sub" do token.
    /// Só existe em endpoint autorizado — por isso o lançamento em vez de retorno nulo.
    /// </summary>
    public static Guid ObterBiUserId(this ClaimsPrincipal principal)
    {
        var valor = principal.FindFirst(ClaimsPainel.Subject)?.Value;

        return Guid.TryParse(valor, out var id)
            ? id
            : throw new InvalidOperationException(
                "Claim 'sub' ausente. O endpoint precisa exigir autorização.");
    }
}
