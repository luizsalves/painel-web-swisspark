using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Data.Repositories;

public class FechamentoValidacaoRepository(PainelDbContext context) : IFechamentoValidacaoRepository
{
    public async Task<List<FechamentoValidacao>> ListarHistoricoAsync(
        short codColigada, short codFilial, string codConta, string anoMes, CancellationToken cancellationToken)
    {
        return await context.FechamentoValidacoes
            .AsNoTracking()
            .Where(f =>
                f.CodColigada == codColigada
                && f.CodFilial == codFilial
                && f.CodConta == codConta
                && f.AnoMes == anoMes)
            .OrderBy(f => f.RealizadoEm)
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(FechamentoValidacao linha, CancellationToken cancellationToken)
    {
        context.FechamentoValidacoes.Add(linha);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, string>> ObterNomesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return [];

        return await context.BiUsers
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);
    }
}
