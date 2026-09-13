using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.Options;

namespace PainelWebSwissPark.Api.Services.Auth;

public class UsuarioAtualService(
    PainelDbContext db,
    IMemoryCache cache,
    IOptions<PainelDbOptions> options,
    ILogger<UsuarioAtualService> logger) : IUsuarioAtualService
{
    private readonly int _cacheSeconds = options.Value.CacheSeconds;

    // Cache de requisição: o handler de autorização, os filtros de módulo e o
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
        var bruto = principal.FindFirst(ClaimsPainel.Subject)?.Value;

        if (!Guid.TryParse(bruto, out var id))
        {
            logger.LogWarning("Token sem o claim 'sub' — não dá para identificar o usuário na bi_users.");
            return null;
        }

        if (_cacheSeconds > 0 && cache.TryGetValue(ChaveCache(id), out UsuarioAutorizado? emCache))
        {
            return emCache;
        }

        var usuario = await db.BiUsers
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Name,
                u.IsActive,
                u.MustChangePassword,
                u.IsSuperAdmin,
                Modulos = u.Modules
                    .Select(m => new ModuloDoUsuario(m.ModuleKey, m.Role))
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (usuario is null) return null;

        var autorizado = new UsuarioAutorizado(
            usuario.Id,
            usuario.Email,
            usuario.Name,
            usuario.IsActive,
            usuario.MustChangePassword,
            usuario.IsSuperAdmin,
            usuario.Modulos);

        if (_cacheSeconds > 0)
        {
            cache.Set(ChaveCache(id), autorizado, TimeSpan.FromSeconds(_cacheSeconds));
        }

        return autorizado;
    }

    private static string ChaveCache(Guid id) => $"bi_users:{id}";
}
