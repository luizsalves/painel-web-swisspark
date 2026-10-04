using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IEmpresasService
{
    Task<List<EmpresaViewModel>> ListarAsync(CancellationToken cancellationToken, bool somenteBsb = false);

    Task<EmpresaViewModel?> AtualizarStatusAsync(
        short codColigada, short codFilial, bool ativo, Guid autorId, CancellationToken cancellationToken);
}
