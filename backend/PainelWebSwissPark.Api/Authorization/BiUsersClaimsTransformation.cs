using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Authorization;

/// <summary>
/// O token do Entra diz quem a pessoa é; o perfil dela é assunto da bi_users.
/// Esta transformação traz o perfil para dentro do principal, para que
/// [Authorize(Roles = "Administrador")] funcione como em qualquer app ASP.NET.
/// </summary>
public class BiUsersClaimsTransformation(IUsuarioAtualService usuarioAtual) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        // O ASP.NET pode chamar a transformação mais de uma vez na mesma requisição.
        if (identity.HasClaim(claim => claim.Type == ClaimsPainel.BiUserId))
        {
            return principal;
        }

        var usuario = await usuarioAtual.ResolverAsync(principal, CancellationToken.None);

        // Sem cadastro ou inativo não ganha perfil nenhum — o UsuarioAtivoHandler recusa depois.
        if (usuario is not { IsActive: true }) return principal;

        identity.AddClaim(new Claim(ClaimsPainel.BiUserId, usuario.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, usuario.Role));

        return principal;
    }
}
