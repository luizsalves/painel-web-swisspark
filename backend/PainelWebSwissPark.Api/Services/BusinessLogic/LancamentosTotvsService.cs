using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class LancamentosTotvsService(ILancamentosTotvsRepository lancamentosRepository) : ILancamentosTotvsService
{
    private static readonly BaseTotvs[] Bases =
    [
        BaseTotvs.Corpore, BaseTotvs.CorporeBsb, BaseTotvs.CorporeMao, BaseTotvs.CorporeNeu,
    ];

    public async Task<List<LancamentoTotvsViewModel>> ListarAsync(
        short codColigada, short codFilial, string anoMes, string codConta,
        CancellationToken cancellationToken)
    {
        // Sequencial, não Task.WhenAll: as 4 bases passam pela mesma conexão
        // (só troca o nome do banco no FROM), e o EF Core não aceita duas
        // operações ao mesmo tempo na mesma instância de DbContext.
        var resultadosPorBase = new List<LancamentoTotvsLinha>();
        foreach (var baseDados in Bases)
        {
            var linhas = await lancamentosRepository.ListarAsync(
                baseDados, codColigada, codFilial, anoMes, codConta, cancellationToken);
            resultadosPorBase.AddRange(linhas);
        }

        return resultadosPorBase
            .Select(l => new LancamentoTotvsViewModel
            {
                AnoMes = l.AnoMes ?? string.Empty,
                Data = l.Data,
                Empresa = l.Empresa ?? string.Empty,
                CodColigada = l.CodColigada,
                Reduzido = l.Reduzido ?? string.Empty,
                CodConta = l.CodConta,
                CodFilial = l.CodFilial,
                NomeFilial = $"{l.CodFilial} - {l.NomeFantasia}",
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
