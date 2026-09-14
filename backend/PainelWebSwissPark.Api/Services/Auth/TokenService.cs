using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Options;

namespace PainelWebSwissPark.Api.Services.Auth;

public class TokenService(IOptions<AuthOptions> options) : ITokenService
{
    private readonly AuthOptions _auth = options.Value;

    public TokenEmitido Emitir(BiUser usuario)
    {
        var agora = DateTimeOffset.UtcNow;
        var expiraEm = agora.AddMinutes(_auth.TokenLifetimeMinutes);

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_auth.JwtSigningKey));

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _auth.Issuer,
            Audience = _auth.Audience,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256),

            // O token carrega só a identidade. Perfil e situação ficam de fora de
            // propósito: são lidos da bi_users a cada requisição, então rebaixar
            // ou desativar alguém vale no request seguinte, sem esperar expirar.
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimsPainel.Subject, usuario.Id.ToString()),
                new Claim(ClaimsPainel.Email, usuario.Email),
                new Claim(ClaimsPainel.Nome, usuario.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
        };

        var token = new JsonWebTokenHandler().CreateToken(descritor);

        return new TokenEmitido(token, expiraEm);
    }
}
