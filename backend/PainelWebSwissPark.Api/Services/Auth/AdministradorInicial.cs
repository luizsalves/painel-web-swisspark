using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Services.Auth;

/// <summary>
/// Cria o superadministrador a partir de variáveis de ambiente.
///
/// Ele existe por um motivo estrutural: o administrador da conciliação não
/// enxerga o mkt, e vice-versa — então nenhum dos dois conseguiria criar o
/// outro. Alguém precisa estar acima dos módulos para isso.
///
/// A tabela nasce vazia, então sem este bootstrap ninguém entra. A alternativa
/// comum — "o primeiro que logar vira admin" — entregaria o sistema a qualquer
/// pessoa que achasse a URL.
///
/// Roda só quando PAINEL_ADMIN_EMAIL e PAINEL_ADMIN_PASSWORD estão definidas E
/// não existe superadministrador ativo. Depois do primeiro acesso as duas
/// variáveis podem sair do .env. Não há tela para promover ninguém a
/// superadministrador, de propósito.
/// </summary>
public static class AdministradorInicial
{
    public static async Task GarantirAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        var email = Environment.GetEnvironmentVariable("PAINEL_ADMIN_EMAIL")?.Trim().ToLowerInvariant();
        var senha = Environment.GetEnvironmentVariable("PAINEL_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            return;
        }

        using var escopo = serviceProvider.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<PainelDbContext>();
        var hasher = escopo.ServiceProvider.GetRequiredService<IPasswordHasher<BiUser>>();

        var jaTemAdministrador = await db.BiUsers.AnyAsync(u => u.IsActive && u.IsSuperAdmin);

        if (jaTemAdministrador)
        {
            logger.LogInformation("Já existe superadministrador ativo — PAINEL_ADMIN_* ignorado.");
            return;
        }

        if (await db.BiUsers.AnyAsync(u => u.Email.ToLower() == email))
        {
            // Promover alguém que já existe seria uma surpresa desagradável: o
            // operador do container não deveria conseguir mudar perfil de ninguém
            // por variável de ambiente.
            logger.LogWarning(
                "PAINEL_ADMIN_EMAIL {Email} já está cadastrado e não é superadministrador. "
                + "Nada foi alterado — promova pelo banco ou use outro e-mail.",
                email);
            return;
        }

        var agora = DateTimeOffset.UtcNow;
        var administrador = new BiUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = Environment.GetEnvironmentVariable("PAINEL_ADMIN_NAME")?.Trim() is { Length: > 0 } nome
                ? nome
                : email,
            IsSuperAdmin = true,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = agora,
            UpdatedAt = agora,
        };

        administrador.PasswordHash = hasher.HashPassword(administrador, senha);

        db.BiUsers.Add(administrador);

        db.BiUserAudits.Add(new BiUserAudit
        {
            TargetUserId = administrador.Id,
            Action = AcoesAuditoria.Criado,
            Details = "superadministrador inicial criado a partir de PAINEL_ADMIN_*",
            PerformedBy = administrador.Id,
            PerformedAt = agora,
        });

        await db.SaveChangesAsync();

        logger.LogWarning(
            "Superadministrador {Email} criado — ele enxerga todos os módulos e cria os "
            + "administradores de cada um. A senha é provisória: troque no primeiro acesso "
            + "e remova PAINEL_ADMIN_PASSWORD do ambiente.",
            email);
    }
}
