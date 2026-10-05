using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IContasContabeisTotvsService
{
    Task<List<EmpresaContabilViewModel>> ListarEmpresasAsync(CancellationToken cancellationToken);
    Task<List<ColigadaFilialContabilViewModel>> ListarColigadasFiliaisAsync(CancellationToken cancellationToken, short? codColigada = null);

    Task<List<ContaContabilViewModel>> ListarAsync(CancellationToken cancellationToken, bool somenteBsb = false, short? codColigada = null, short? codFilial = null);

    Task<ContaContabilViewModel?> AtualizarStatusAsync(
        short codColigada, string codConta, bool ativo, Guid autorId, CancellationToken cancellationToken);
}
