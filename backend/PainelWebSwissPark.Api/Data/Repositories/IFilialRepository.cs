using PainelWebSwissPark.Api.Data.Entities.Totvs;

namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IFilialRepository
{
    Task<List<Gfilial>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// GFILIAL com ela mesma, casando pelo CODFILIAL — traz pares de filiais
    /// com o mesmo código em coligadas diferentes.
    /// </summary>
    Task<List<(Gfilial FilialA, Gfilial FilialB)>> ObterComMesmoCodFilialAsync(CancellationToken cancellationToken);
}
