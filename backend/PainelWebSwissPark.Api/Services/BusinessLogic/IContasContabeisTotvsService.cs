using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IContasContabeisTotvsService
{
    Task<List<ContaContabilViewModel>> ListarAsync(CancellationToken cancellationToken);
}
