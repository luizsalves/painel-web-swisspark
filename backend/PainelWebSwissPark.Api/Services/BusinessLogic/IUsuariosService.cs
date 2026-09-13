using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IUsuariosService
{
    /// <summary>Só a turma dos módulos que o autor administra. O superadministrador vê todos.</summary>
    Task<IReadOnlyList<UsuarioViewModel>> ListarAsync(
        UsuarioAutorizado autor,
        CancellationToken cancellationToken);

    /// <summary>Módulos que o autor pode conceder ao cadastrar ou editar alguém.</summary>
    Task<IReadOnlyList<ModuloViewModel>> ListarModulosGerenciaveisAsync(
        UsuarioAutorizado autor,
        CancellationToken cancellationToken);

    Task<UsuarioViewModel> CriarAsync(
        CriarUsuarioRequest request,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken);

    /// <summary>Retorna null quando o usuário não existe ou está fora do alcance do autor.</summary>
    Task<UsuarioViewModel?> AtualizarAsync(
        Guid id,
        AtualizarUsuarioRequest request,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken);

    /// <summary>Retorna null quando o usuário não existe ou está fora do alcance do autor.</summary>
    Task<UsuarioViewModel?> AlterarStatusAsync(
        Guid id,
        bool ativo,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken);

    /// <summary>Retorna null quando o usuário não existe ou está fora do alcance do autor.</summary>
    Task<UsuarioViewModel?> RedefinirSenhaAsync(
        Guid id,
        string novaSenha,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken);
}
