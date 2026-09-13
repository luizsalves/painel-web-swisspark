using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Authorization;

/// <summary>
/// Amarra um endpoint a um módulo. Quem não pertence ao módulo toma 403, mesmo
/// estando autenticado e ativo.
///
/// É aqui que a separação entre os sistemas realmente acontece. Esconder o item
/// do menu no front é cosmético: sem esta checagem, bastaria digitar a URL.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ExigeModuloAttribute : TypeFilterAttribute
{
    public ExigeModuloAttribute(string moduloKey) : base(typeof(ExigeModuloFilter))
    {
        Arguments = [moduloKey];
    }
}

public class ExigeModuloFilter(
    string moduloKey,
    IUsuarioAtualService usuarioAtual,
    ILogger<ExigeModuloFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var usuario = await usuarioAtual.ResolverAsync(
            context.HttpContext.User,
            context.HttpContext.RequestAborted);

        if (usuario is not null && usuario.PodeAcessarModulo(moduloKey)) return;

        logger.LogInformation(
            "Acesso ao módulo {Modulo} negado para {UserId}.",
            moduloKey,
            usuario?.Id);

        context.Result = new ObjectResult(new { error = "Você não tem acesso a este módulo." })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
