using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Authorization;

/// <summary>
/// O token diz quem a pessoa é; o perfil dela é assunto da bi_users e não viaja
/// dentro do token. Esta transformação traz o perfil do banco para dentro do
/// principal, para que [Authorize(Roles = "Administrador")] funcione como em
/// qualquer app ASP.NET — e para que rebaixar alguém valha na hora.
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
        if (identity.HasClaim(claim => claim.Type == ClaimTypes.Role))
        {
            return principal;
        }

        var usuario = await usuarioAtual.ResolverAsync(principal, CancellationToken.None);

        // Sem cadastro ou inativo não ganha perfil nenhum — o UsuarioAtivoHandler recusa depois.
        if (usuario is not { IsActive: true }) return principal;

        identity.AddClaim(new Claim(ClaimTypes.Role, usuario.Role));

        return principal;
    }
}
