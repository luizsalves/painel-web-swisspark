using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Exceptions;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Regras da área administrativa. As travas daqui existem para impedir que um
/// administrador se tranque para fora do próprio sistema — situação que só se
/// resolve com acesso direto ao banco.
/// </summary>
public class UsuariosService(
    PainelDbContext db,
    IMemoryCache cache,
    ILogger<UsuariosService> logger) : IUsuariosService
{
    public async Task<IReadOnlyList<UsuarioViewModel>> ListarAsync(CancellationToken cancellationToken)
    {
        var usuarios = await db.BiUsers
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);

        return [.. usuarios.Select(Mapear)];
    }

    public async Task<UsuarioViewModel> CriarAsync(
        CriarUsuarioRequest request,
        Guid autorId,
        CancellationToken cancellationToken)
    {
        var nome = (request.Name ?? string.Empty).Trim();
        var email = NormalizarEmail(request.Email);
        var perfil = (request.Role ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraDeNegocioException("Informe o nome do usuário.");
        }

        if (!EmailEhValido(email))
        {
            throw new RegraDeNegocioException("Informe um e-mail corporativo válido.");
        }

        if (!PerfisUsuario.EhValido(perfil))
        {
            throw new RegraDeNegocioException($"Perfil inválido. Use um destes: {string.Join(", ", PerfisUsuario.Todos)}.");
        }

        var jaExiste = await db.BiUsers.AnyAsync(u => u.Email.ToLower() == email, cancellationToken);
        if (jaExiste)
        {
            throw new RegraDeNegocioException("Já existe um usuário cadastrado com esse e-mail.");
        }

        var agora = DateTimeOffset.UtcNow;
        var usuario = new BiUser
        {
            Id = Guid.NewGuid(),
            EntraObjectId = null,
            Email = email,
            Name = nome,
            Role = perfil,
            IsActive = true,
            CreatedAt = agora,
            CreatedBy = autorId,
            UpdatedAt = agora,
            UpdatedBy = autorId,
        };

        db.BiUsers.Add(usuario);
        RegistrarAuditoria(usuario.Id, AcoesAuditoria.Criado, $"{email} como {perfil}", autorId);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Usuário {Email} criado como {Perfil} por {AutorId}.", email, perfil, autorId);

        return Mapear(usuario);
    }

    public async Task<UsuarioViewModel?> AtualizarAsync(
        Guid id,
        AtualizarUsuarioRequest request,
        Guid autorId,
        CancellationToken cancellationToken)
    {
        var usuario = await db.BiUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (usuario is null) return null;

        var nome = (request.Name ?? string.Empty).Trim();
        var perfil = (request.Role ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraDeNegocioException("Informe o nome do usuário.");
        }

        if (!PerfisUsuario.EhValido(perfil))
        {
            throw new RegraDeNegocioException($"Perfil inválido. Use um destes: {string.Join(", ", PerfisUsuario.Todos)}.");
        }

        var perdeuAdministrador = usuario.Role == PerfisUsuario.Administrador
            && perfil != PerfisUsuario.Administrador;

        if (perdeuAdministrador)
        {
            if (usuario.Id == autorId)
            {
                throw new RegraDeNegocioException(
                    "Você não pode rebaixar o seu próprio perfil. Peça a outro administrador.");
            }

            await GarantirQueSobraAdministradorAsync(usuario.Id, cancellationToken);
        }

        var mudancas = new List<string>();
        if (usuario.Name != nome) mudancas.Add($"nome: {usuario.Name} -> {nome}");
        if (usuario.Role != perfil) mudancas.Add($"perfil: {usuario.Role} -> {perfil}");

        if (mudancas.Count == 0) return Mapear(usuario);

        usuario.Name = nome;
        usuario.Role = perfil;
        usuario.UpdatedAt = DateTimeOffset.UtcNow;
        usuario.UpdatedBy = autorId;

        RegistrarAuditoria(usuario.Id, AcoesAuditoria.Atualizado, string.Join("; ", mudancas), autorId);

        await db.SaveChangesAsync(cancellationToken);
        InvalidarCache(usuario);

        logger.LogInformation("Usuário {UserId} atualizado por {AutorId}: {Mudancas}.", usuario.Id, autorId, string.Join("; ", mudancas));

        return Mapear(usuario);
    }

    public async Task<UsuarioViewModel?> AlterarStatusAsync(
        Guid id,
        bool ativo,
        Guid autorId,
        CancellationToken cancellationToken)
    {
        var usuario = await db.BiUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (usuario is null) return null;

        if (!ativo)
        {
            if (usuario.Id == autorId)
            {
                throw new RegraDeNegocioException(
                    "Você não pode desativar o seu próprio acesso.");
            }

            if (usuario.Role == PerfisUsuario.Administrador && usuario.IsActive)
            {
                await GarantirQueSobraAdministradorAsync(usuario.Id, cancellationToken);
            }
        }

        if (usuario.IsActive == ativo) return Mapear(usuario);

        usuario.IsActive = ativo;
        usuario.UpdatedAt = DateTimeOffset.UtcNow;
        usuario.UpdatedBy = autorId;

        RegistrarAuditoria(
            usuario.Id,
            ativo ? AcoesAuditoria.Ativado : AcoesAuditoria.Desativado,
            usuario.Email,
            autorId);

        await db.SaveChangesAsync(cancellationToken);
        InvalidarCache(usuario);

        logger.LogInformation(
            "Usuário {UserId} {Status} por {AutorId}.",
            usuario.Id,
            ativo ? "ativado" : "desativado",
            autorId);

        return Mapear(usuario);
    }

    private async Task GarantirQueSobraAdministradorAsync(Guid idIgnorado, CancellationToken cancellationToken)
    {
        var outrosAdministradores = await db.BiUsers.CountAsync(
            u => u.Id != idIgnorado && u.IsActive && u.Role == PerfisUsuario.Administrador,
            cancellationToken);

        if (outrosAdministradores == 0)
        {
            throw new RegraDeNegocioException(
                "Este é o último administrador ativo. Promova outra pessoa antes de seguir.");
        }
    }

    private void RegistrarAuditoria(Guid usuarioId, string acao, string? detalhes, Guid autorId)
    {
        db.BiUserAudits.Add(new BiUserAudit
        {
            TargetUserId = usuarioId,
            Action = acao,
            Details = detalhes,
            PerformedBy = autorId,
            PerformedAt = DateTimeOffset.UtcNow,
        });
    }

    /// <summary>
    /// Só faz diferença quando PainelDb:CacheSeconds está ligado — sem isso o próximo
    /// request já consulta o banco. Com cache ligado, é o que mantém a revogação imediata.
    /// </summary>
    private void InvalidarCache(BiUser usuario)
    {
        if (usuario.EntraObjectId is { } oid)
        {
            cache.Remove($"bi_users:oid:{oid}");
        }
    }

    private static string NormalizarEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool EmailEhValido(string email) =>
        !string.IsNullOrWhiteSpace(email)
        && MailAddress.TryCreate(email, out var endereco)
        && endereco.Address == email;

    private static UsuarioViewModel Mapear(BiUser usuario) => new()
    {
        Id = usuario.Id.ToString(),
        Name = usuario.Name,
        Email = usuario.Email,
        Role = usuario.Role,
        IsActive = usuario.IsActive,
        PrimeiroAcessoPendente = usuario.EntraObjectId is null,
        AvatarInitials = MontarIniciais(usuario.Name),
    };

    public static string MontarIniciais(string nome)
    {
        var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => string.Concat(partes[0][..1], partes[^1][..1]).ToUpperInvariant(),
        };
    }
}
