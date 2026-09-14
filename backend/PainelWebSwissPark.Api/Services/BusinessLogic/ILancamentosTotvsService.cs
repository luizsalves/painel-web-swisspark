using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface ILancamentosTotvsService
{
    /// <summary>
    /// Consulta as quatro bases TOTVS em paralelo com o mesmo filtro e junta o
    /// resultado — os lançamentos de verdade só existem em uma delas, mas o
    /// front não precisa saber em qual.
    /// </summary>
    Task<List<LancamentoTotvsViewModel>> ListarAsync(
        short codColigada, short codFilial, string anoMes, string codConta,
        CancellationToken cancellationToken);
}
