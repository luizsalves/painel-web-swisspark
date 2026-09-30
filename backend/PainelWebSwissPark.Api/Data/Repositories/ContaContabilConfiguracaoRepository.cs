using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Data.Repositories;

public class ContaContabilConfiguracaoRepository(PainelDbContext context) : IContaContabilConfiguracaoRepository
{
    public async Task<Dictionary<(short CodColigada, string CodConta), bool>> ListarStatusAsync(
        CancellationToken cancellationToken)
    {
        var linhas = await context.ContaContabilConfiguracoes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return linhas.ToDictionary(l => (l.CodColigada, l.CodConta), l => l.Ativo);
    }

    public async Task DefinirStatusAsync(
        short codColigada, string codConta, bool ativo, Guid autorId, CancellationToken cancellationToken)
    {
        var existente = await context.ContaContabilConfiguracoes
            .FirstOrDefaultAsync(
                e => e.CodColigada == codColigada && e.CodConta == codConta, cancellationToken);

        if (existente is null)
        {
            context.ContaContabilConfiguracoes.Add(new ContaContabilConfiguracao
            {
                CodColigada = codColigada,
                CodConta = codConta,
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
