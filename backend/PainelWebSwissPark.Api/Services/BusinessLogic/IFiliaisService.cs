using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IFiliaisService
{
    Task<List<FilialRelacionadaViewModel>> ObterFiliaisComMesmoCodigoAsync(CancellationToken cancellationToken);
}
