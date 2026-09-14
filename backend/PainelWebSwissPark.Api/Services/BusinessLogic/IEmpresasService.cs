using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IEmpresasService
{
    Task<List<EmpresaViewModel>> ListarAsync(CancellationToken cancellationToken);
}
