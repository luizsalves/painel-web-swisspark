using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Exceptions;
using PainelWebSwissPark.Api.Options;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Services.Auth;

public class LoginService(
    PainelDbContext db,
    IPasswordHasher<BiUser> hasher,
    ITokenService tokenService,
    IOptions<AuthOptions> options,
    ILogger<LoginService> logger) : ILoginService
{
    private const string CredenciaisInvalidas = "E-mail ou senha incorretos.";

    private readonly AuthOptions _auth = options.Value;

    // Hash descartável de uma senha qualquer. Quando o e-mail não existe, a
    // verificação roda contra ele assim mesmo: sem isso, o tempo de resposta
    // denunciaria quais e-mails estão cadastrados.
    private static readonly string HashDeDescarte =
        new PasswordHasher<BiUser>().HashPassword(new BiUser(), "senha-que-nao-existe");

    public async Task<ResultadoLogin> AutenticarAsync(
        string email,
        string senha,
        CancellationToken cancellationToken)
    {
        var normalizado = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizado) || string.IsNullOrEmpty(senha))
        {
            return Recusar();
        }

        var usuario = await db.BiUsers.FirstOrDefaultAsync(
            u => u.Email.ToLower() == normalizado,
            cancellationToken);

        var hashArmazenado = usuario?.PasswordHash ?? HashDeDescarte;
        var verificacao = hasher.VerifyHashedPassword(usuario ?? new BiUser(), hashArmazenado, senha);

        if (usuario is null || verificacao == PasswordVerificationResult.Failed)
        {
            logger.LogInformation("Tentativa de login recusada para {Email}.", normalizado);
            return Recusar();
        }

        // Só quem provou saber a senha fica sabendo que a conta existe e está
        // desativada. Para quem errou a senha, a resposta continua sendo genérica.
        if (!usuario.IsActive)
        {
            return new ResultadoLogin(
                false,
                "Seu acesso ao painel está desativado. Procure um administrador.",
                null,
                null,
                null);
        }

        // O PasswordHasher evolui os parâmetros do PBKDF2 entre versões; quando
        // isso acontece, a senha é regravada com o formato novo no próximo login.
        if (verificacao == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = hasher.HashPassword(usuario, senha);
        }

        usuario.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var token = tokenService.Emitir(usuario);

        logger.LogInformation("Login de {Email} ({UserId}).", usuario.Email, usuario.Id);

        return new ResultadoLogin(
            true,
            null,
            token.AccessToken,
            token.ExpiraEm,
            UsuariosService.Mapear(usuario));
    }

    public async Task TrocarSenhaAsync(
        Guid usuarioId,
        string senhaAtual,
        string novaSenha,
        CancellationToken cancellationToken)
    {
        var usuario = await db.BiUsers.FirstOrDefaultAsync(u => u.Id == usuarioId, cancellationToken)
            ?? throw new RegraDeNegocioException("Usuário não encontrado.");

        var verificacao = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, senhaAtual ?? string.Empty);

        if (verificacao == PasswordVerificationResult.Failed)
        {
            throw new RegraDeNegocioException("A senha atual está incorreta.");
        }

        ValidarSenha(novaSenha, _auth.MinimumPasswordLength);

        if (hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, novaSenha) != PasswordVerificationResult.Failed)
        {
            throw new RegraDeNegocioException("A nova senha precisa ser diferente da atual.");
        }

        usuario.PasswordHash = hasher.HashPassword(usuario, novaSenha);
        usuario.MustChangePassword = false;
        usuario.UpdatedAt = DateTimeOffset.UtcNow;
        usuario.UpdatedBy = usuario.Id;

        db.BiUserAudits.Add(new BiUserAudit
        {
            TargetUserId = usuario.Id,
            Action = AcoesAuditoria.SenhaTrocada,
            Details = "trocada pelo próprio usuário",
            PerformedBy = usuario.Id,
            PerformedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Senha trocada por {UserId}.", usuario.Id);
    }

    /// <summary>Regra de senha usada tanto na troca quanto no cadastro pelo administrador.</summary>
    public static void ValidarSenha(string? senha, int tamanhoMinimo)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < tamanhoMinimo)
        {
            throw new RegraDeNegocioException($"A senha precisa ter pelo menos {tamanhoMinimo} caracteres.");
        }

        if (senha.Trim().Length != senha.Length)
        {
            throw new RegraDeNegocioException("A senha não pode começar nem terminar com espaço.");
        }
    }

    private static ResultadoLogin Recusar() =>
        new(false, CredenciaisInvalidas, null, null, null);
}
