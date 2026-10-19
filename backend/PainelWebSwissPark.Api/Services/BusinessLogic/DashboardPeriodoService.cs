using System.Globalization;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IDashboardPeriodoService
{
    /// <summary>
    /// Resumo de cada empresa ativa (chave = Nome) somado mês a mês, de anoMesInicial a
    /// anoMesFinal (inclusive): cada filial x conta entra uma vez por mês.
    /// </summary>
    Task<Dictionary<string, DashboardResumoViewModel>> ObterResumoPorEmpresaAsync(
        string anoMesInicial, string anoMesFinal, CancellationToken cancellationToken);

    /// <summary>
    /// Contas de uma empresa (Nome) de anoMesInicial a anoMesFinal: uma linha por
    /// mês x filial ativa x conta ativa, com o status daquele mês.
    /// </summary>
    Task<List<DashboardContaMesViewModel>> ObterContasDaEmpresaAsync(
        string anoMesInicial, string anoMesFinal, string empresa, CancellationToken cancellationToken);
}

/// <summary>
/// A visão por período da Produtividade. Usa as mesmas fontes e a mesma regra do
/// <see cref="DashboardService"/> (que continua respondendo por um mês só), lendo o
/// histórico do fechamento um mês por vez com a consulta que já existe.
/// </summary>
public class DashboardPeriodoService(
    IEmpresasService empresasService,
    IContasContabeisTotvsService contasContabeisService,
    IFechamentoValidacaoRepository fechamentoValidacaoRepository) : IDashboardPeriodoService
{
    public async Task<Dictionary<string, DashboardResumoViewModel>> ObterResumoPorEmpresaAsync(
        string anoMesInicial, string anoMesFinal, CancellationToken cancellationToken)
    {
        var empresas = await empresasService.ListarAsync(cancellationToken);
        var contas = await contasContabeisService.ListarAsync(cancellationToken);

        // Plano de contas é por coligada — cada conta ativa vale pra todas as filiais dela.
        var contasPorColigada = contas
            .Where(c => c.Ativo)
            .GroupBy(c => c.CodColigada)
            .ToDictionary(g => g.Key, g => g.Select(c => c.CodConta).ToList());
        var filiaisPorEmpresa = empresas
            .Where(e => e.Ativo)
            .GroupBy(e => e.Nome)
            .ToDictionary(g => g.Key, g => g.ToList());

        var resultado = filiaisPorEmpresa.ToDictionary(par => par.Key, _ => new DashboardResumoViewModel());
        foreach (var anoMes in MesesDoPeriodo(anoMesInicial, anoMesFinal))
        {
            var historico = await fechamentoValidacaoRepository.ListarPorAnoMesAsync(anoMes, cancellationToken);
            // A última linha de cada chave é o status "atual" daquela filial+conta no mês.
            var statusAtualPorChave = historico
                .GroupBy(f => (f.CodColigada, f.CodFilial, f.CodConta))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.RealizadoEm).First().Status);

            foreach (var (nome, filiais) in filiaisPorEmpresa)
            {
                var resumo = resultado[nome];
                foreach (var filial in filiais)
                {
                    if (!contasPorColigada.TryGetValue(filial.CodColigada, out var contasDaColigada)) continue;

                    foreach (var codConta in contasDaColigada)
                    {
                        resumo.BacklogTotal++;
                        var status = statusAtualPorChave.GetValueOrDefault(
                            (filial.CodColigada, filial.CodFilial, codConta), StatusFechamento.Pendente);
                        switch (status)
                        {
                            case StatusFechamento.Preparado: resumo.Preparados++; break;
                            case StatusFechamento.Conferido: resumo.Conferidos++; break;
                            case StatusFechamento.Fechado: resumo.Fechados++; break;
                            default: resumo.Pendentes++; break;
                        }
                    }
                }
            }
        }

        return resultado;
    }

    public async Task<List<DashboardContaMesViewModel>> ObterContasDaEmpresaAsync(
        string anoMesInicial, string anoMesFinal, string empresa, CancellationToken cancellationToken)
    {
        var empresas = await empresasService.ListarAsync(cancellationToken);
        var filiaisAtivas = empresas.Where(e => e.Ativo && e.Nome.Trim() == empresa.Trim()).ToList();
        if (filiaisAtivas.Count == 0) return [];

        var coligadas = filiaisAtivas.Select(f => f.CodColigada).ToHashSet();
        var contas = await contasContabeisService.ListarAsync(cancellationToken);
        var contasPorColigada = contas
            .Where(c => c.Ativo && coligadas.Contains(c.CodColigada))
            .GroupBy(c => c.CodColigada)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Em cada mês, a linha mais recente de cada chave é o status atual.
        var ultimaPorMes = new List<(string AnoMes, Dictionary<(short, short, string), FechamentoValidacao> UltimaPorChave)>();
        foreach (var anoMes in MesesDoPeriodo(anoMesInicial, anoMesFinal))
        {
            var historico = await fechamentoValidacaoRepository.ListarPorAnoMesAsync(anoMes, cancellationToken);
            ultimaPorMes.Add((anoMes, historico
                .Where(f => coligadas.Contains(f.CodColigada))
                .GroupBy(f => (f.CodColigada, f.CodFilial, f.CodConta))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.RealizadoEm).First())));
        }
        var nomes = await fechamentoValidacaoRepository.ObterNomesAsync(
            ultimaPorMes.SelectMany(m => m.UltimaPorChave.Values).Select(f => f.RealizadoPor).Distinct().ToList(),
            cancellationToken);

        var resultado = new List<DashboardContaMesViewModel>();
        foreach (var (anoMes, ultimaPorChave) in ultimaPorMes)
        {
            foreach (var filial in filiaisAtivas)
            {
                if (!contasPorColigada.TryGetValue(filial.CodColigada, out var contasDaColigada)) continue;

                foreach (var conta in contasDaColigada)
                {
                    var ultima = ultimaPorChave.GetValueOrDefault((filial.CodColigada, filial.CodFilial, conta.CodConta));
                    resultado.Add(new DashboardContaMesViewModel
                    {
                        AnoMes = anoMes,
                        CodColigada = filial.CodColigada,
                        CodFilial = filial.CodFilial,
                        Filial = filial.NomeFilial,
                        CodConta = conta.CodConta,
                        Reduzido = conta.Reduzido,
                        Descricao = conta.Descricao,
                        Status = ultima?.Status ?? StatusFechamento.Pendente,
                        UltimaAcaoPor = ultima is null ? null : nomes.GetValueOrDefault(ultima.RealizadoPor),
                        UltimaAcaoEm = ultima?.RealizadoEm,
                    });
                }
            }
        }

        return resultado
            .OrderBy(c => c.CodConta, StringComparer.Ordinal)
            .ThenBy(c => c.CodFilial)
            .ThenBy(c => c.AnoMes, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Os meses AAAA-MM de anoMesInicial a anoMesFinal, inclusive (o controller já validou).</summary>
    private static IEnumerable<string> MesesDoPeriodo(string anoMesInicial, string anoMesFinal)
    {
        var mes = DateOnly.ParseExact(anoMesInicial + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fim = DateOnly.ParseExact(anoMesFinal + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        for (; mes <= fim; mes = mes.AddMonths(1))
            yield return mes.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }
}
