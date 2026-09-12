using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Options;

namespace PainelWebSwissPark.Api.Services.Auth;

public class UsuarioAtualService(
    PainelDbContext db,
    IMemoryCache cache,
    IOptions<PainelDbOptions> options,
    ILogger<UsuarioAtualService> logger) : IUsuarioAtualService
{
    // O oid é o único identificador estável da pessoa no tenant. E-mail corporativo muda
    // (casamento, troca de domínio); o oid não. Por isso ele é a chave de busca.
    private static readonly string[] ClaimsDeObjectId =
    [
        "http://schemas.microsoft.com/identity/claims/objectidentifier",
        "oid",
    ];

    private static readonly string[] ClaimsDeEmail =
    [
        "preferred_username",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn",
        "upn",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
        "email",
    ];

    private static readonly string[] ClaimsDeNome =
    [
        "name",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
    ];

    private readonly int _cacheSeconds = options.Value.CacheSeconds;

    // Cache de requisição: o handler de autorização, a transformação de claims e o
    // controller pedem o mesmo usuário — o banco é consultado uma vez só.
    private bool _resolvido;
    private UsuarioAutorizado? _usuario;

    public async Task<UsuarioAutorizado?> ResolverAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (_resolvido) return _usuario;

        _usuario = await BuscarAsync(principal, cancellationToken);
        _resolvido = true;
        return _usuario;
    }

    private async Task<UsuarioAutorizado?> BuscarAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var objectId = LerObjectId(principal);
        var email = LerPrimeiroClaim(principal, ClaimsDeEmail)?.Trim().ToLowerInvariant();

        if (objectId is null && email is null)
        {
            logger.LogWarning("Token sem oid e sem e-mail — não dá para identificar o usuário na bi_users.");
            return null;
        }

        if (objectId is { } oid)
        {
            if (_cacheSeconds > 0 && cache.TryGetValue(ChaveCache(oid), out UsuarioAutorizado? emCache))
            {
                return emCache;
            }

            var porOid = await db.BiUsers
                .AsNoTracking()
                .Where(u => u.EntraObjectId == oid)
                .Select(u => new UsuarioAutorizado(u.Id, u.EntraObjectId, u.Email, u.Name, u.Role, u.IsActive))
                .FirstOrDefaultAsync(cancellationToken);

            if (porOid is not null)
            {
                Cachear(oid, porOid);
                return porOid;
            }
        }

        if (email is null) return null;

        // Primeiro login: o administrador cadastrou por e-mail, ainda sem oid.
        var porEmail = await db.BiUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (porEmail is null) return null;

        if (porEmail.EntraObjectId is not null && porEmail.EntraObjectId != objectId)
        {
            // O e-mail bate, mas a linha já pertence a outro oid. Reaproveitar seria
            // entregar o acesso de uma pessoa a outra que herdou o endereço.
            logger.LogWarning(
                "E-mail {Email} já vinculado a outro oid do Entra. Acesso negado.",
                email);
            return null;
        }

        if (porEmail.EntraObjectId is null && objectId is { } novoOid)
        {
            porEmail.EntraObjectId = novoOid;
            porEmail.UpdatedAt = DateTimeOffset.UtcNow;

            var nomeDoToken = LerPrimeiroClaim(principal, ClaimsDeNome)?.Trim();
            if (!string.IsNullOrWhiteSpace(nomeDoToken))
            {
                porEmail.Name = nomeDoToken;
            }

            db.BiUserAudits.Add(new BiUserAudit
            {
                TargetUserId = porEmail.Id,
                Action = AcoesAuditoria.PrimeiroLogin,
                Details = $"entra_object_id vinculado: {novoOid}",
                PerformedBy = porEmail.Id,
                PerformedAt = DateTimeOffset.UtcNow,
            });

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Primeiro login de {Email}: oid {Oid} vinculado à bi_users.",
                email,
                novoOid);
        }

        var resolvido = new UsuarioAutorizado(
            porEmail.Id,
            porEmail.EntraObjectId,
            porEmail.Email,
            porEmail.Name,
            porEmail.Role,
            porEmail.IsActive);

        if (objectId is { } oidParaCache) Cachear(oidParaCache, resolvido);

        return resolvido;
    }

    private void Cachear(Guid objectId, UsuarioAutorizado usuario)
    {
        if (_cacheSeconds <= 0) return;

        cache.Set(ChaveCache(objectId), usuario, TimeSpan.FromSeconds(_cacheSeconds));
    }

    private static string ChaveCache(Guid objectId) => $"bi_users:oid:{objectId}";

    private static Guid? LerObjectId(ClaimsPrincipal principal)
    {
        var valor = LerPrimeiroClaim(principal, ClaimsDeObjectId);
        return Guid.TryParse(valor, out var oid) ? oid : null;
    }

    private static string? LerPrimeiroClaim(ClaimsPrincipal principal, string[] tipos)
    {
        foreach (var tipo in tipos)
        {
            var valor = principal.FindFirst(tipo)?.Value;
            if (!string.IsNullOrWhiteSpace(valor)) return valor;
        }

        return null;
    }
}
