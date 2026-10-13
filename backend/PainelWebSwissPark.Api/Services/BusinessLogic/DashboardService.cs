using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class DashboardService(
    IEmpresasService empresasService,
    IContasContabeisTotvsService contasContabeisService,
    IFechamentoValidacaoRepository fechamentoValidacaoRepository) : IDashboardService
{
    public async Task<DashboardResumoViewModel> ObterResumoAsync(
        string anoMes, string? empresa, CancellationToken cancellationToken)
    {
        var (empresas, contasPorColigada, statusAtualPorChave) = await CarregarAsync(anoMes, cancellationToken);

        var filiaisAtivas = empresas
            .Where(e => e.Ativo && (string.IsNullOrEmpty(empresa) || e.Nome == empresa))
            .ToList();

        return Somar(filiaisAtivas, contasPorColigada, statusAtualPorChave);
    }

    public async Task<Dictionary<string, DashboardResumoViewModel>> ObterResumoPorEmpresaAsync(
        string anoMes, CancellationToken cancellationToken)
    {
        // Uma leitura só do histórico pra todas as empresas — chamar
        // ObterResumoAsync por empresa relê o mês inteiro do Postgres a cada vez.
        var (empresas, contasPorColigada, statusAtualPorChave) = await CarregarAsync(anoMes, cancellationToken);

        return empresas
            .Where(e => e.Ativo)
            .GroupBy(e => e.Nome)
            .ToDictionary(g => g.Key, g => Somar(g.ToList(), contasPorColigada, statusAtualPorChave));
    }

    public async Task<List<DashboardContaViewModel>> ObterContasDaEmpresaAsync(
        string anoMes, string empresa, CancellationToken cancellationToken)
    {
        var empresas = await empresasService.ListarAsync(cancellationToken);
        var filiaisAtivas = empresas.Where(e => e.Ativo && e.Nome.Trim() == empresa.Trim()).ToList();
        if (filiaisAtivas.Count == 0) return [];

        var coligadas = filiaisAtivas.Select(f => f.CodColigada).ToHashSet();
        var contas = await contasContabeisService.ListarAsync(cancellationToken);
        var historico = await fechamentoValidacaoRepository.ListarPorAnoMesAsync(anoMes, cancellationToken);

        // Mesma regra do resumo: a linha mais recente de cada chave é o status atual.
        var ultimaPorChave = historico
            .Where(f => coligadas.Contains(f.CodColigada))
            .GroupBy(f => (f.CodColigada, f.CodFilial, f.CodConta))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.RealizadoEm).First());
        var nomes = await fechamentoValidacaoRepository.ObterNomesAsync(
            ultimaPorChave.Values.Select(f => f.RealizadoPor).Distinct().ToList(), cancellationToken);

        var contasPorColigada = contas
            .Where(c => c.Ativo && coligadas.Contains(c.CodColigada))
            .GroupBy(c => c.CodColigada)
            .ToDictionary(g => g.Key, g => g.ToList());

        var resultado = new List<DashboardContaViewModel>();
        foreach (var filial in filiaisAtivas)
        {
            if (!contasPorColigada.TryGetValue(filial.CodColigada, out var contasDaColigada)) continue;

            foreach (var conta in contasDaColigada)
            {
                var ultima = ultimaPorChave.GetValueOrDefault((filial.CodColigada, filial.CodFilial, conta.CodConta));
                resultado.Add(new DashboardContaViewModel
                {
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

        return resultado
            .OrderBy(c => c.CodConta, StringComparer.Ordinal)
            .ThenBy(c => c.CodFilial)
            .ToList();
    }

    private async Task<(
        List<EmpresaViewModel> Empresas,
        Dictionary<short, List<string>> ContasPorColigada,
        Dictionary<(short, short, string), string> StatusAtualPorChave)> CarregarAsync(
        string anoMes, CancellationToken cancellationToken)
    {
        var empresas = await empresasService.ListarAsync(cancellationToken);
        var contas = await contasContabeisService.ListarAsync(cancellationToken);
        var historico = await fechamentoValidacaoRepository.ListarPorAnoMesAsync(anoMes, cancellationToken);

        // Plano de contas é por coligada — cada conta ativa vale pra todas as
        // filiais daquela coligada.
        var contasPorColigada = contas
            .Where(c => c.Ativo)
            .GroupBy(c => c.CodColigada)
            .ToDictionary(g => g.Key, g => g.Select(c => c.CodConta).ToList());

        // Preparar/Conferir/Fechar/Reverter sempre inserem, nunca atualizam —
        // a última linha de cada chave é o status "atual" daquela combinação
        // filial+conta no mês.
        var statusAtualPorChave = historico
            .GroupBy(f => (f.CodColigada, f.CodFilial, f.CodConta))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.RealizadoEm).First().Status);

        return (empresas, contasPorColigada, statusAtualPorChave);
    }

    private static DashboardResumoViewModel Somar(
        List<EmpresaViewModel> filiaisAtivas,
        Dictionary<short, List<string>> contasPorColigada,
        Dictionary<(short, short, string), string> statusAtualPorChave)
    {
        var resumo = new DashboardResumoViewModel();

        foreach (var filial in filiaisAtivas)
        {
            if (!contasPorColigada.TryGetValue(filial.CodColigada, out var contasDaColigada)) continue;

            foreach (var codConta in contasDaColigada)
            {
                resumo.BacklogTotal++;

                var chave = (filial.CodColigada, filial.CodFilial, codConta);
                var status = statusAtualPorChave.TryGetValue(chave, out var s) ? s : StatusFechamento.Pendente;

                switch (status)
                {
                    case StatusFechamento.Preparado:
                        resumo.Preparados++;
                        break;
                    case StatusFechamento.Conferido:
                        resumo.Conferidos++;
                        break;
                    case StatusFechamento.Fechado:
                        resumo.Fechados++;
                        break;
                    default:
                        resumo.Pendentes++;
                        break;
                }
            }
        }

        return resumo;
    }

    public Task<string?> ObterUltimoPeriodoComDadosAsync(CancellationToken cancellationToken) =>
        fechamentoValidacaoRepository.ObterUltimoAnoMesAsync(cancellationToken);
}
