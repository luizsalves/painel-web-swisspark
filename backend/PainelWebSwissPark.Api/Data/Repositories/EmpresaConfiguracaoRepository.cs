using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Data.Repositories;

public class EmpresaConfiguracaoRepository(PainelDbContext context) : IEmpresaConfiguracaoRepository
{
    public async Task<Dictionary<(short CodColigada, short CodFilial), bool>> ListarStatusAsync(
        CancellationToken cancellationToken)
    {
        var linhas = await context.EmpresaConfiguracoes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return linhas.ToDictionary(l => (l.CodColigada, l.CodFilial), l => l.Ativo);
    }

    public async Task DefinirStatusAsync(
        short codColigada, short codFilial, bool ativo, Guid autorId, CancellationToken cancellationToken)
    {
        var existente = await context.EmpresaConfiguracoes
            .FirstOrDefaultAsync(
                e => e.CodColigada == codColigada && e.CodFilial == codFilial, cancellationToken);

        if (existente is null)
        {
            context.EmpresaConfiguracoes.Add(new EmpresaConfiguracao
            {
                CodColigada = codColigada,
                CodFilial = codFilial,
                Ativo = ativo,
                AtualizadoPor = autorId,
                AtualizadoEm = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existente.Ativo = ativo;
            existente.AtualizadoPor = autorId;
            existente.AtualizadoEm = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
