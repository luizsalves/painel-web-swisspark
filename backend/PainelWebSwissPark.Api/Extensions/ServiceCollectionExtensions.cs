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
        // "Totvs_01" é o nome usado no keys.example.json da raiz, junto das outras
        // conexões. "SqlServer" foi a grafia inicial e continua aceita.
        var conexao = configuration.GetConnectionString("Totvs_01")
            ?? configuration.GetConnectionString("SqlServer");

        if (string.IsNullOrWhiteSpace(conexao))
        {
            // Não derruba a aplicação: a autenticação e o resto do painel não
            // dependem do SQL Server. Quem chamar /api/fechamentos é que vai
            // esbarrar na falta do registro — e o aviso abaixo explica por quê.
            Console.Error.WriteLine(
                "[painel] ConnectionStrings:Totvs_01 não configurado no keys.json. "
                + "Os endpoints de Fechamentos vão falhar até isso ser preenchido.");

            return services;
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(conexao));

        services.AddScoped<IFechamentoRepository, FechamentoRepository>();
        services.AddScoped<IFechamentosService, FechamentosService>();

        services.AddScoped<IFilialRepository, FilialRepository>();
        services.AddScoped<IFiliaisService, FiliaisService>();

        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IEmpresasService, EmpresasService>();

        services.AddScoped<IContaContabilTotvsRepository, ContaContabilTotvsRepository>();
        services.AddScoped<IContasContabeisTotvsService, ContasContabeisTotvsService>();

        services.AddScoped<ILancamentosTotvsRepository, LancamentosTotvsRepository>();
        services.AddScoped<ILancamentosTotvsService, LancamentosTotvsService>();
        services.AddScoped<LancamentosConsolidadosTotvsRepository>();
        services.AddScoped<ILancamentosConsolidadosService, LancamentosConsolidadosService>();

        services.AddScoped<BalanceteTotvsRepository>();
        services.AddScoped<IBalanceteService, BalanceteService>();

        services.AddScoped<IndicadoresTotvsRepository>();
        services.AddScoped<IIndicadoresService, IndicadoresService>();
        services.AddScoped<IndicadoresMensaisTotvsRepository>();
        services.AddScoped<IIndicadoresMensaisService, IndicadoresMensaisService>();
        services.AddScoped<IBalancoService, BalancoService>();

        services.AddScoped<ITotvsConsultaRepository, TotvsConsultaRepository>();
        services.AddScoped<ITotvsConsultaService, TotvsConsultaService>();

        services.AddScoped<IDiferencasTotvsRepository, DiferencasTotvsRepository>();
        services.AddScoped<IDiferencasService, DiferencasService>();

        return services;
    }
}
