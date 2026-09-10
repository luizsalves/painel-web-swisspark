using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Extensions;

/// <summary>
/// Mantém o Program.cs enxuto: aqui fica o registro de acesso ao SQL Server
/// (EF Core) e dos Services/Repositories que dependem dele.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServer")));

        services.AddScoped<IFechamentoRepository, FechamentoRepository>();
        services.AddScoped<IFechamentosService, FechamentosService>();

        return services;
    }
}
