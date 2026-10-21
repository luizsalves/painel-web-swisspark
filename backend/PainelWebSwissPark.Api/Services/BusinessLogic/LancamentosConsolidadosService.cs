using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface ILancamentosConsolidadosService
{
    /// <summary>
    /// O mesmo retorno de <see cref="ILancamentosTotvsService.ListarAsync"/>, mas com os
    /// lançamentos de todas as filiais da empresa (pelo nome, dentro da coligada) e a
    /// linha-resumo do saldo do razão da empresa inteira.
    /// </summary>
    Task<List<LancamentoTotvsViewModel>> ListarAsync(
        short codColigada, string empresa, string anoMes, string codConta,
        CancellationToken cancellationToken);
}

public class LancamentosConsolidadosService(LancamentosConsolidadosTotvsRepository repository)
    : ILancamentosConsolidadosService
{
    public async Task<List<LancamentoTotvsViewModel>> ListarAsync(
        short codColigada, string empresa, string anoMes, string codConta,
        CancellationToken cancellationToken)
    {
        // Sequencial pelo mesmo motivo do LancamentosTotvsService: as 4 bases passam
        // pela mesma instância de DbContext.
        var linhas = new List<LancamentoTotvsLinha>();
        foreach (var baseDados in BasesTotvs.Todas)
        {
            linhas.AddRange(await repository.ListarAsync(
                baseDados, codColigada, empresa, anoMes, codConta, cancellationToken));
        }

        return linhas
            .Select(l => new LancamentoTotvsViewModel
            {
                AnoMes = l.AnoMes ?? string.Empty,
                Data = l.Data,
                Empresa = l.Empresa ?? string.Empty,
                CodColigada = l.CodColigada,
                Reduzido = l.Reduzido ?? string.Empty,
                CodConta = l.CodConta,
                CodFilial = l.CodFilial,
                // Cada lançamento continua dizendo de que filial veio; a linha-resumo é de todas.
                NomeFilial = l.Data is null ? "Todas as filiais" : $"{l.CodFilial} - {l.NomeFantasia}",
                Complemento = l.Complemento ?? string.Empty,
                Documento = l.Documento,
                IdLancamento = l.IdLancamento,
                IdPartida = l.IdPartida,
                Usuario = l.Usuario,
                Debitos = l.Debitos,
                Creditos = l.Creditos,
                SaldoRazao = l.SaldoRazao,
                SaldoAnterior = l.SaldoAnterior,
            })
            .ToList();
    }
}
