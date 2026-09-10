using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IFechamentosService
{
    Task<FechamentoViewModel?> ObterFechamentoAsync(
        int empresaId, int contaContabilId, DateOnly periodo, CancellationToken cancellationToken);
}
