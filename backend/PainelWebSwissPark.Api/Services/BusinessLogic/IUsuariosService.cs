using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IUsuariosService
{
    Task<IReadOnlyList<UsuarioViewModel>> ListarAsync(CancellationToken cancellationToken);

    Task<UsuarioViewModel> CriarAsync(
        CriarUsuarioRequest request,
        Guid autorId,
        CancellationToken cancellationToken);

    /// <summary>Retorna null quando o usuário não existe.</summary>
    Task<UsuarioViewModel?> AtualizarAsync(
        Guid id,
        AtualizarUsuarioRequest request,
        Guid autorId,
        CancellationToken cancellationToken);

    /// <summary>Retorna null quando o usuário não existe.</summary>
    Task<UsuarioViewModel?> AlterarStatusAsync(
        Guid id,
        bool ativo,
        Guid autorId,
        CancellationToken cancellationToken);
}
