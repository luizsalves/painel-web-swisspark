using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.Auth;

public sealed record ResultadoLogin(
    bool Sucesso,
    string? Mensagem,
    string? AccessToken,
    DateTimeOffset? ExpiraEm,
    UsuarioViewModel? Usuario);

public interface ILoginService
{
    Task<ResultadoLogin> AutenticarAsync(string email, string senha, CancellationToken cancellationToken);

    /// <summary>Troca da própria senha. Exige a senha atual.</summary>
    Task TrocarSenhaAsync(Guid usuarioId, string senhaAtual, string novaSenha, CancellationToken cancellationToken);
}
