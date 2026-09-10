using PainelWebSwissPark.Api.Data.Entities;

namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IFechamentoRepository
{
    Task<Fechamento?> ObterPorFiltroAsync(
        int empresaId, int contaContabilId, DateOnly periodo, CancellationToken cancellationToken);
}
