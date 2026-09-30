using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IContasContabeisTotvsService
{
    Task<List<ContaContabilViewModel>> ListarAsync(CancellationToken cancellationToken);

    Task<ContaContabilViewModel?> AtualizarStatusAsync(
        short codColigada, string codConta, bool ativo, Guid autorId, CancellationToken cancellationToken);
}
