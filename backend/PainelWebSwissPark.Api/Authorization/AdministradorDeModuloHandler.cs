using Microsoft.AspNetCore.Authorization;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Authorization;

public class AdministradorDeModuloRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Abre a área administrativa para quem administra algum módulo. Não decide
/// QUAIS usuários a pessoa enxerga — esse recorte é por módulo e fica no
/// UsuariosService, onde o admin da conciliação vê só a turma da conciliação.
/// </summary>
public class AdministradorDeModuloHandler(
    IUsuarioAtualService usuarioAtual,
    ILogger<AdministradorDeModuloHandler> logger) : AuthorizationHandler<AdministradorDeModuloRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdministradorDeModuloRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var usuario = await usuarioAtual.ResolverAsync(context.User, CancellationToken.None);

        if (usuario is not { IsActive: true, MustChangePassword: false })
        {
            return;
        }

        if (!usuario.EhAdministradorDeAlgumModulo)
        {
            logger.LogInformation(
                "Acesso à área administrativa negado: {UserId} não administra nenhum módulo.",
                usuario.Id);
            return;
        }

        context.Succeed(requirement);
    }
}
