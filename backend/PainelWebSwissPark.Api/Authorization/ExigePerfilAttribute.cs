using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Authorization;

/// <summary>
/// Amarra um endpoint a um (ou mais) perfil específico dentro de um módulo —
/// não basta pertencer ao módulo (isso já é o <see cref="ExigeModuloAttribute"/>),
/// precisa ter um desses perfis nele. Ex.: só Gerente e Administrador da
/// conciliação podem conferir um fechamento; o Analista dela toma 403.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ExigePerfilAttribute : TypeFilterAttribute
{
    public ExigePerfilAttribute(string moduloKey, params string[] perfis) : base(typeof(ExigePerfilFilter))
    {
        Arguments = [moduloKey, perfis];
    }
}

public class ExigePerfilFilter(
    string moduloKey,
    string[] perfis,
    IUsuarioAtualService usuarioAtual,
    ILogger<ExigePerfilFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var usuario = await usuarioAtual.ResolverAsync(
            context.HttpContext.User,
            context.HttpContext.RequestAborted);

        if (usuario is not null && usuario.TemAlgumPerfilNoModulo(moduloKey, perfis)) return;

        logger.LogInformation(
            "Acesso negado: {UserId} não tem nenhum dos perfis {Perfis} no módulo {Modulo}.",
            usuario?.Id, string.Join(", ", perfis), moduloKey);

        context.Result = new ObjectResult(new { error = $"Só quem tem o perfil {string.Join(" ou ", perfis)} pode fazer isso." })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
