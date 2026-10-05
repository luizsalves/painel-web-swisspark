namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IContaContabilTotvsRepository
{
    Task<List<EmpresaContabil>> ListarEmpresasAsync(CancellationToken cancellationToken);
    Task<List<ColigadaFilialMovimentada>> ListarColigadasFiliaisAsync(CancellationToken cancellationToken, short? codColigada = null);

    /// <summary>
    /// União dos créditos e débitos da CPARTIDA nas quatro bases, sem coligada zero.
    /// </summary>
    Task<List<ContaContabilMovimentada>> ListarTodasColigadasAsync(CancellationToken cancellationToken, bool somenteBsb = false, short? codColigada = null, short? codFilial = null);
}
