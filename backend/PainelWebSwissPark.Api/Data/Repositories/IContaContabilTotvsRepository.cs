namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IContaContabilTotvsRepository
{
    /// <summary>
    /// União do CCONTA das quatro coligadas (corpore, corpore_bsb, corpore_mao,
    /// corpore_neu) — cada uma é um banco separado no mesmo SQL Server.
    /// </summary>
    Task<List<CcontaUniao>> ListarTodasColigadasAsync(CancellationToken cancellationToken);
}
