using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.DTOs.ViewModels;
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

    public async Task<List<FechamentoValidacao>> ListarPorAnoMesAsync(
        string anoMes, CancellationToken cancellationToken)
    {
        return await context.FechamentoValidacoes
            .AsNoTracking()
            .Where(f => f.AnoMes == anoMes)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> ObterUltimoAnoMesAsync(CancellationToken cancellationToken)
    {
        return await context.FechamentoValidacoes
            .AsNoTracking()
            .OrderByDescending(f => f.AnoMes)
            .Select(f => f.AnoMes)
            .FirstOrDefaultAsync(cancellationToken);
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

    public async Task<List<ProdutividadeUsuarioViewModel>> ContarPorUsuarioAsync(
        string status, string? anoMes, CancellationToken cancellationToken)
    {
        var linhas = context.FechamentoValidacoes
            .AsNoTracking()
            .Where(f => f.Status == status);

        if (!string.IsNullOrWhiteSpace(anoMes))
        {
            linhas = linhas.Where(f => f.AnoMes == anoMes);
        }

        return await linhas
            .Join(context.BiUsers, f => f.RealizadoPor, u => u.Id, (f, u) => new { u.Id, u.Name, u.Email })
            .GroupBy(x => new { x.Id, x.Name, x.Email })
            .Select(g => new ProdutividadeUsuarioViewModel
            {
                UsuarioId = g.Key.Id,
                Usuario = g.Key.Name,
                Email = g.Key.Email,
                Quantidade = g.Count(),
            })
            .OrderByDescending(p => p.Quantidade)
            .ThenBy(p => p.Usuario)
            .ToListAsync(cancellationToken);
    }
}
