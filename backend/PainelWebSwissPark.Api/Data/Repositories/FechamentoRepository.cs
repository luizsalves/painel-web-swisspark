using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Data.Entities;

namespace PainelWebSwissPark.Api.Data.Repositories;

/// <summary>
/// Isola o FechamentosService do EF Core/DbContext diretamente — só essa classe
/// sabe como o Fechamento é montado a partir das tabelas reais do SQL Server.
/// </summary>
public class FechamentoRepository(AppDbContext context) : IFechamentoRepository
{
    public async Task<Fechamento?> ObterPorFiltroAsync(
        int empresaId, int contaContabilId, DateOnly periodo, CancellationToken cancellationToken)
    {
        return await context.Fechamentos
            .Include(f => f.Empresa)
            .Include(f => f.ContaContabil)
            .Include(f => f.Responsavel)
            .Include(f => f.Lancamentos)
            .Include(f => f.Pendencias)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                f => f.EmpresaId == empresaId
                     && f.ContaContabilId == contaContabilId
                     && f.Periodo == periodo,
                cancellationToken);
    }
}
