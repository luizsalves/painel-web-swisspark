using Microsoft.AspNetCore.Authorization;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Authorization;

public class UsuarioAtivoHandler(
    IUsuarioAtualService usuarioAtual,
    ILogger<UsuarioAtivoHandler> logger) : AuthorizationHandler<UsuarioAtivoRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UsuarioAtivoRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var usuario = await usuarioAtual.ResolverAsync(context.User, CancellationToken.None);

        if (usuario is null)
        {
            logger.LogInformation("Token válido, mas o usuário não está mais na bi_users.");
            return;
        }

        if (!usuario.IsActive)
        {
            logger.LogInformation("Acesso negado: usuário {UserId} está inativo na bi_users.", usuario.Id);
            return;
        }

        if (usuario.MustChangePassword)
        {
            // Senha definida por um administrador: até trocar, o painel fica fechado.
            // /api/auth/me e /api/auth/trocar-senha seguem acessíveis — eles exigem
            // só [Authorize], sem esta política.
            logger.LogInformation("Acesso bloqueado: usuário {UserId} precisa trocar a senha.", usuario.Id);
            return;
        }

        context.Succeed(requirement);
    }
}
