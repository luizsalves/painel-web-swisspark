namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IEmpresaRepository
{
    /// <summary>
    /// União do GFILIAL das quatro coligadas (corpore, corpore_bsb, corpore_mao,
    /// corpore_neu) — cada uma é um banco separado no mesmo SQL Server.
    /// </summary>
    Task<List<GfilialUniao>> ListarTodasColigadasAsync(CancellationToken cancellationToken);
}
