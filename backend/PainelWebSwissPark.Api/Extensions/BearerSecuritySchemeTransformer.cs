using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PainelWebSwissPark.Api.Extensions;

/// <summary>
/// Adiciona o esquema "Bearer" ao documento OpenAPI, para que a página de
/// documentação (Scalar) mostre o botão de autenticação e anexe o token JWT
/// automaticamente nas chamadas de teste.
/// </summary>
public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var esquemas = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (!esquemas.Any(esquema => esquema.Name == "Bearer"))
        {
            return;
        }

        var esquemaSeguranca = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token JWT obtido em POST /api/auth/login. Cole aqui só o token, sem o prefixo \"Bearer \".",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = esquemaSeguranca;

        var requisito = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        };

        foreach (var operacao in document.Paths.Values.SelectMany(path => path.Operations!.Values))
        {
            operacao.Security ??= [];
            operacao.Security.Add(requisito);
        }
    }
}
