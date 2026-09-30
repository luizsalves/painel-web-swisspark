using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Exceptions;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IDiferencasService
{
    /// <summary>Contas com diferença (saldo do razão − composição ≠ 0) no fim do mês. empresa (Nome) opcional filtra.</summary>
    Task<DiferencasViewModel> ObterAsync(string anoMes, string? empresa, CancellationToken cancellationToken);
}

public class DiferencasService(
    IDiferencasTotvsRepository repository,
    IEmpresasService empresasService,
    IMemoryCache cache,
    ILogger<DiferencasService> logger) : IDiferencasService
{
    // O cálculo cruza as 4 bases do TOTVS e leva alguns minutos; o resultado do
    // mês fica guardado pra tela, o quadro e a Tati não recalcularem a cada pedido.
    private static readonly TimeSpan DuracaoCache = TimeSpan.FromHours(2);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Travas = new();
    private const decimal Tolerancia = 0.01m;

    public async Task<DiferencasViewModel> ObterAsync(string anoMes, string? empresa, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact($"{anoMes}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var primeiroDia))
        {
            throw new RegraDeNegocioException($"Período inválido: \"{anoMes}\". Use AAAA-MM.");
        }

        var completo = await ObterCompletoAsync(anoMes, primeiroDia, cancellationToken);
        return string.IsNullOrWhiteSpace(empresa) ? completo : FiltrarPorEmpresa(completo, empresa);
    }

    private async Task<DiferencasViewModel> ObterCompletoAsync(string anoMes, DateOnly primeiroDia, CancellationToken cancellationToken)
    {
        var chave = $"diferencas:{anoMes}";
        if (cache.TryGetValue(chave, out DiferencasViewModel? emCache) && emCache is not null) return emCache;

        // Só um cálculo por mês de cada vez — quem chegar junto espera e usa o mesmo resultado.
        var trava = Travas.GetOrAdd(chave, _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(chave, out emCache) && emCache is not null) return emCache;

            var calculado = await CalcularAsync(anoMes, primeiroDia.AddMonths(1).AddDays(-1), cancellationToken);
            cache.Set(chave, calculado, DuracaoCache);
            return calculado;
        }
        finally
        {
            trava.Release();
        }
    }

    private async Task<DiferencasViewModel> CalcularAsync(string anoMes, DateOnly fimDoMes, CancellationToken cancellationToken)
    {
        var inicio = DateTime.UtcNow;
        var composicao = new List<ComposicaoContaLinha>();
        var razao = new List<SaldoRazaoContaLinha>();

        foreach (var baseTotvs in BasesTotvs.Todas)
        {
            razao.AddRange(await repository.ListarSaldoRazaoAsync(baseTotvs, fimDoMes, cancellationToken));
            composicao.AddRange(await repository.ListarComposicaoAsync(baseTotvs, fimDoMes, cancellationToken));
        }

        // A mesma coligada/filial/conta pode ter movimento em mais de uma base: soma tudo.
        var razaoPorConta = razao
            .GroupBy(r => (r.CodColigada, r.CodFilial, r.CodConta))
            .ToDictionary(g => g.Key, g => (Saldo: g.Sum(x => x.SaldoRazao), Descricao: g.First().Descricao));
        var composicaoPorConta = composicao
            .GroupBy(c => (c.CodColigada, c.CodFilial, c.CodConta))
            .ToDictionary(g => g.Key, g => (Valor: g.Sum(x => x.EmAberto), Documentos: g.Sum(x => x.Documentos), Descricao: g.First().Descricao));

        var empresas = await empresasService.ListarAsync(cancellationToken);
        var filialPorChave = empresas
            .GroupBy(e => (e.CodColigada, e.CodFilial))
            .ToDictionary(g => g.Key, g => g.First());

        var contas = razaoPorConta.Keys.Union(composicaoPorConta.Keys)
            .Select(chave =>
            {
                var saldo = razaoPorConta.TryGetValue(chave, out var r) ? r.Saldo : 0m;
                var comp = composicaoPorConta.TryGetValue(chave, out var c) ? c.Valor : 0m;
                filialPorChave.TryGetValue((chave.CodColigada, chave.CodFilial), out var filial);

                return new DiferencaContaViewModel
                {
                    Empresa = filial?.Nome ?? $"Coligada {chave.CodColigada}",
                    CodColigada = chave.CodColigada,
                    CodFilial = chave.CodFilial,
                    NomeFilial = filial?.NomeFilial ?? $"{chave.CodFilial}",
                    CodConta = chave.CodConta,
                    Descricao = r.Descricao ?? c.Descricao ?? string.Empty,
                    SaldoRazao = Math.Round(saldo, 2),
                    Composicao = Math.Round(comp, 2),
                    Diferenca = Math.Round(saldo - comp, 2),
                    Documentos = c.Documentos,
                };
            })
            .Where(c => Math.Abs(c.Diferenca) >= Tolerancia)
            .OrderByDescending(c => Math.Abs(c.Diferenca))
            .ToList();

        logger.LogInformation(
            "Diferenças {AnoMes}: {Contas} contas com diferença, calculado em {DuracaoMs}ms",
            anoMes, contas.Count, (DateTime.UtcNow - inicio).TotalMilliseconds);

        return Montar(anoMes, contas);
    }

    private static DiferencasViewModel FiltrarPorEmpresa(DiferencasViewModel completo, string empresa)
    {
        var contas = completo.Contas.Where(c => string.Equals(c.Empresa, empresa, StringComparison.OrdinalIgnoreCase)).ToList();
        var filtrado = Montar(completo.AnoMes, contas);
        filtrado.CalculadoEm = completo.CalculadoEm;
        return filtrado;
    }

    private static DiferencasViewModel Montar(string anoMes, List<DiferencaContaViewModel> contas) => new()
    {
        AnoMes = anoMes,
        CalculadoEm = DateTimeOffset.UtcNow,
        Contas = contas,
        TotalContas = contas.Count,
        ValorTotal = contas.Sum(c => Math.Abs(c.Diferenca)),
        Empresas = contas
            .GroupBy(c => c.Empresa)
            .Select(g => new DiferencaEmpresaViewModel
            {
                Empresa = g.Key,
                Contas = g.Count(),
                ValorAbsoluto = g.Sum(c => Math.Abs(c.Diferenca)),
                ValorLiquido = g.Sum(c => c.Diferenca),
            })
            .OrderByDescending(e => e.ValorAbsoluto)
            .ToList(),
        TotalEmpresas = contas.Select(c => c.Empresa).Distinct().Count(),
    };
}
