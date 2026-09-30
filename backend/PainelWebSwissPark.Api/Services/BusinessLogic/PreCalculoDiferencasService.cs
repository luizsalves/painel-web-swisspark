namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Ao subir o backend, já calcula as diferenças do último período com dados —
/// o cálculo cruza as 4 bases do TOTVS e leva minutos, então a tela, o quadro
/// e a Tati encontram o resultado pronto no cache.
/// </summary>
public class PreCalculoDiferencasService(
    IServiceScopeFactory scopeFactory,
    ILogger<PreCalculoDiferencasService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var scope = scopeFactory.CreateScope();
        var periodo = DateTime.Today.AddMonths(-1).ToString("yyyy-MM");

        try
        {
            var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
            periodo = await dashboard.ObterUltimoPeriodoComDadosAsync(stoppingToken) ?? periodo;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Pré-cálculo de diferenças: não consegui ler o último período, usando {Periodo}", periodo);
        }

        try
        {
            logger.LogInformation("Pré-cálculo de diferenças: calculando {Periodo}", periodo);
            await scope.ServiceProvider.GetRequiredService<IDiferencasService>().ObterAsync(periodo, null, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Pré-cálculo de diferenças de {Periodo} falhou — será calculado no primeiro acesso", periodo);
        }
    }
}
