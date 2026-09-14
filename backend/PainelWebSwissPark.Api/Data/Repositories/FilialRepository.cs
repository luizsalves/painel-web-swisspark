using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Data.Entities.Totvs;

namespace PainelWebSwissPark.Api.Data.Repositories;

public class FilialRepository(AppDbContext context) : IFilialRepository
{
    public async Task<List<Gfilial>> ListarAsync(CancellationToken cancellationToken)
    {
        return await context.Gfiliais
            .OrderBy(f => f.CodColigada).ThenBy(f => f.CodFilial)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(Gfilial FilialA, Gfilial FilialB)>> ObterComMesmoCodFilialAsync(
        CancellationToken cancellationToken)
    {
        var pares = await context.Gfiliais
            .Join(
                context.Gfiliais,
                a => a.CodFilial,
                b => b.CodFilial,
                (a, b) => new { A = a, B = b })
            // Sem isso, toda filial casaria com ela mesma (mesma coligada e mesma filial).
            .Where(x => x.A.CodColigada != x.B.CodColigada)
            .OrderBy(x => x.A.CodFilial)
            .ToListAsync(cancellationToken);

        return pares.Select(x => (x.A, x.B)).ToList();
    }
}
