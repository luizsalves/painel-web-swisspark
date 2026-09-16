using System.Security.Claims;
using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Services.Auth;

public sealed record ModuloDoUsuario(string Key, string Role);

/// <summary>
/// Retrato imutável do usuário autorizado. É um snapshot destacado do EF de propósito:
/// pode ser cacheado entre requisições sem arrastar entidade rastreada junto.
/// </summary>
public sealed record UsuarioAutorizado(
    Guid Id,
    string Email,
    string Name,
    bool IsActive,
    bool MustChangePassword,
    bool IsSuperAdmin,
    IReadOnlyList<ModuloDoUsuario> Modulos)
{
    /// <summary>Pertence ao módulo — ou é superadministrador, que enxerga todos.</summary>
    public bool PodeAcessarModulo(string moduloKey) =>
        IsSuperAdmin || Modulos.Any(modulo => modulo.Key == moduloKey);

    /// <summary>Tem exatamente este perfil neste módulo — ou é superadministrador, que tem todos.</summary>
    public bool TemPerfilNoModulo(string moduloKey, string perfil) =>
        IsSuperAdmin || Modulos.Any(modulo => modulo.Key == moduloKey && modulo.Role == perfil);

    /// <summary>Tem qualquer um destes perfis neste módulo — ou é superadministrador, que tem todos.</summary>
    public bool TemAlgumPerfilNoModulo(string moduloKey, params string[] perfis) =>
        IsSuperAdmin || Modulos.Any(modulo => modulo.Key == moduloKey && perfis.Contains(modulo.Role));

    public bool EhAdministradorDe(string moduloKey) =>
        IsSuperAdmin
        || Modulos.Any(modulo => modulo.Key == moduloKey && modulo.Role == PerfisUsuario.Administrador);

    /// <summary>Perfis que enxergam e mexem na área administrativa, além do Administrador.</summary>
    private static readonly string[] PerfisComAcessoAdministrativo =
        [PerfisUsuario.Administrador, PerfisUsuario.Gerente];

    /// <summary>Administra (ou gerencia) pelo menos um módulo. É o que libera a tela de Usuários.</summary>
    public bool EhAdministradorDeAlgumModulo =>
        IsSuperAdmin || Modulos.Any(modulo => PerfisComAcessoAdministrativo.Contains(modulo.Role));

    /// <summary>
    /// Módulos cuja turma esta pessoa pode gerenciar. Para o superadministrador a
    /// lista vem vazia e o chamador trata como "todos" — ele não depende de
    /// vínculo em bi_user_modules.
    /// </summary>
    public IReadOnlyList<string> ModulosQueAdministra =>
    [
        .. Modulos
            .Where(modulo => PerfisComAcessoAdministrativo.Contains(modulo.Role))
            .Select(modulo => modulo.Key)
    ];
}

public interface IUsuarioAtualService
{
    /// <summary>
    /// Traduz o token para o registro correspondente na bi_users, junto dos módulos.
    ///
    /// O token prova quem a pessoa é; módulos, perfis e situação vêm sempre do banco,
    /// nunca do próprio token. É isso que faz tirar alguém de um módulo valer na
    /// requisição seguinte.
    /// </summary>
    Task<UsuarioAutorizado?> ResolverAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
