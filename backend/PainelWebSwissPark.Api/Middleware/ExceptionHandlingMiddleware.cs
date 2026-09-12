using System.Net;
using System.Text.Json;
using PainelWebSwissPark.Api.Exceptions;

namespace PainelWebSwissPark.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RegraDeNegocioException ex)
        {
            // Erro previsto: a mensagem foi escrita para o usuário final e vai inteira.
            logger.LogInformation("Regra de negócio bloqueou {Path}: {Mensagem}", context.Request.Path, ex.Message);
            await EscreverAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado ao processar {Path}", context.Request.Path);
            await EscreverAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Ocorreu um erro ao processar a solicitação.");
        }
    }

    private static async Task EscreverAsync(HttpContext context, HttpStatusCode status, string mensagem)
    {
        if (context.Response.HasStarted) return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var payload = JsonSerializer.Serialize(new { error = mensagem });
        await context.Response.WriteAsync(payload);
    }
}
