namespace PainelWebSwissPark.Api.Data.Repositories;

public enum BaseTotvs
{
    Corpore,
    CorporeBsb,
    CorporeMao,
    CorporeNeu,
}

public interface ILancamentosTotvsRepository
{
    /// <summary>
    /// Lançamentos de CPARTIDA de uma única base TOTVS, filtrados por
    /// coligada+filial+período+conta, com a linha extra de SALDO_RAZAO
    /// (saldo anterior ao período + débitos + créditos do período).
    /// </summary>
    Task<List<LancamentoTotvsLinha>> ListarAsync(
        BaseTotvs baseDados, short codColigada, short codFilial, string anoMes, string codConta,
        CancellationToken cancellationToken);
}
