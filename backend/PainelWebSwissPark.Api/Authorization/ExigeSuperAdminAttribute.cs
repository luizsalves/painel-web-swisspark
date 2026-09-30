using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Authorization;

/// <summary>
/// Amarra um endpoint ao superadministrador — ninguém mais passa, nem quem
/// administra ou gerencia algum módulo. Usado em telas que ainda devem ficar
/// restritas ao dono do sistema (ex.: CFO Tati), diferente do
/// [ExigeModulo]/[AdministradorDeModulo] usados no resto do painel.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ExigeSuperAdminAttribute : TypeFilterAttribute
{
    public ExigeSuperAdminAttribute() : base(typeof(ExigeSuperAdminFilter))
    {
    }
}

public class ExigeSuperAdminFilter(
    IUsuarioAtualService usuarioAtual,
    ILogger<ExigeSuperAdminFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var usuario = await usuarioAtual.ResolverAsync(
            context.HttpContext.User,
            context.HttpContext.RequestAborted);

        if (usuario is not null && usuario.IsSuperAdmin) return;

        logger.LogInformation("Acesso negado: {UserId} não é superadministrador.", usuario?.Id);

        context.Result = new ObjectResult(new { error = "Só o administrador do sistema pode acessar isso." })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
