using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Exceptions;
using PainelWebSwissPark.Api.Options;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Regras da área administrativa.
///
/// Duas famílias de trava aqui. A primeira separa os módulos: o administrador da
/// conciliação não enxerga nem toca em quem é só do mkt, e vice-versa. A segunda
/// impede que um administrador se tranque para fora — situação que só se resolve
/// com acesso direto ao banco.
/// </summary>
public class UsuariosService(
    PainelDbContext db,
    IPasswordHasher<BiUser> hasher,
    IMemoryCache cache,
    IOptions<AuthOptions> authOptions,
    ILogger<UsuariosService> logger) : IUsuariosService
{
    private readonly int _tamanhoMinimoDeSenha = authOptions.Value.MinimumPasswordLength;

    public async Task<IReadOnlyList<UsuarioViewModel>> ListarAsync(
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var consulta = db.BiUsers.AsNoTracking().Include(u => u.Modules).AsQueryable();

        if (!autor.IsSuperAdmin)
        {
            var modulos = autor.ModulosQueAdministra;

            consulta = consulta.Where(u =>
                !u.IsSuperAdmin
                && u.Modules.Any(m => modulos.Contains(m.ModuleKey)));
        }

        var usuarios = await consulta.OrderBy(u => u.Name).ToListAsync(cancellationToken);
        var nomes = await NomesDeModulosAsync(cancellationToken);

        // Quem não é superadministrador não vê os vínculos da pessoa em módulos
        // alheios: saber que o fulano também é do mkt não é da conta dele.
        var visiveis = autor.IsSuperAdmin ? null : autor.ModulosQueAdministra;

        return [.. usuarios.Select(usuario => Mapear(usuario, nomes, visiveis))];
    }

    public async Task<IReadOnlyList<ModuloViewModel>> ListarModulosGerenciaveisAsync(
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var consulta = db.BiModules.AsNoTracking().Where(m => m.IsActive);

        if (!autor.IsSuperAdmin)
        {
            var modulos = autor.ModulosQueAdministra;
            consulta = consulta.Where(m => modulos.Contains(m.Key));
        }

        return await consulta
            .OrderBy(m => m.SortOrder)
            .Select(m => new ModuloViewModel { Key = m.Key, Name = m.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<UsuarioViewModel> CriarAsync(
        CriarUsuarioRequest request,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var nome = (request.Name ?? string.Empty).Trim();
        var email = NormalizarEmail(request.Email);

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraDeNegocioException("Informe o nome do usuário.");
        }

        if (!EmailEhValido(email))
        {
            throw new RegraDeNegocioException("Informe um e-mail corporativo válido.");
        }

        LoginService.ValidarSenha(request.SenhaInicial, _tamanhoMinimoDeSenha);

        var vinculos = await ValidarVinculosAsync(request.Modulos, autor, cancellationToken);

        if (vinculos.Count == 0)
        {
            throw new RegraDeNegocioException("Escolha ao menos um módulo para o usuário.");
        }

        if (await db.BiUsers.AnyAsync(u => u.Email.ToLower() == email, cancellationToken))
        {
            throw new RegraDeNegocioException("Já existe um usuário cadastrado com esse e-mail.");
        }

        var agora = DateTimeOffset.UtcNow;
        var usuario = new BiUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = nome,
            IsActive = true,
            MustChangePassword = true,
            IsSuperAdmin = false,
            CreatedAt = agora,
            CreatedBy = autor.Id,
            UpdatedAt = agora,
            UpdatedBy = autor.Id,
        };

        usuario.PasswordHash = hasher.HashPassword(usuario, request.SenhaInicial);

        usuario.Modules =
        [
            .. vinculos.Select(vinculo => new BiUserModule
            {
                UserId = usuario.Id,
                ModuleKey = vinculo.Key,
                Role = vinculo.Value,
                CreatedAt = agora,
                CreatedBy = autor.Id,
            })
        ];

        db.BiUsers.Add(usuario);

        foreach (var vinculo in vinculos)
        {
            RegistrarAuditoria(
                usuario.Id,
                AcoesAuditoria.Criado,
                $"{email} como {vinculo.Value}",
                autor.Id,
                vinculo.Key);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Usuário {Email} criado por {AutorId} nos módulos {Modulos}.",
            email,
            autor.Id,
            string.Join(", ", vinculos.Keys));

        return Mapear(usuario, await NomesDeModulosAsync(cancellationToken));
    }

    public async Task<UsuarioViewModel?> AtualizarAsync(
        Guid id,
        AtualizarUsuarioRequest request,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var usuario = await CarregarGerenciavelAsync(id, autor, cancellationToken);
        if (usuario is null) return null;

        var nome = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraDeNegocioException("Informe o nome do usuário.");
        }

        var desejados = await ValidarVinculosAsync(request.Modulos, autor, cancellationToken);
        var gerenciaveis = await ModulosGerenciaveisAsync(autor, cancellationToken);
        var mudancas = new List<string>();

        if (usuario.Name != nome)
        {
            mudancas.Add($"nome: {usuario.Name} -> {nome}");
            usuario.Name = nome;
        }

        // Só os vínculos dos módulos que o autor administra entram na conta. Os
        // outros ficam intocados: o admin do mkt não remove ninguém da
        // conciliação nem por acidente.
        foreach (var moduloKey in gerenciaveis)
        {
            var atual = usuario.Modules.FirstOrDefault(m => m.ModuleKey == moduloKey);
            desejados.TryGetValue(moduloKey, out var perfilDesejado);

            if (atual is null && perfilDesejado is not null)
            {
                usuario.Modules.Add(new BiUserModule
                {
                    UserId = usuario.Id,
                    ModuleKey = moduloKey,
                    Role = perfilDesejado,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = autor.Id,
                });

                mudancas.Add($"{moduloKey}: entrou como {perfilDesejado}");
                RegistrarAuditoria(usuario.Id, AcoesAuditoria.ModuloConcedido, perfilDesejado, autor.Id, moduloKey);
                continue;
            }

            if (atual is not null && perfilDesejado is null)
            {
                await GarantirQueNaoEhOUltimoAdministradorAsync(usuario, moduloKey, autor, cancellationToken);

                db.BiUserModules.Remove(atual);
                usuario.Modules.Remove(atual);

                mudancas.Add($"{moduloKey}: saiu");
                RegistrarAuditoria(usuario.Id, AcoesAuditoria.ModuloRevogado, null, autor.Id, moduloKey);
                continue;
            }

            if (atual is not null && perfilDesejado is not null && atual.Role != perfilDesejado)
            {
                if (atual.Role == PerfisUsuario.Administrador)
                {
                    await GarantirQueNaoEhOUltimoAdministradorAsync(usuario, moduloKey, autor, cancellationToken);
                }

                mudancas.Add($"{moduloKey}: {atual.Role} -> {perfilDesejado}");
                RegistrarAuditoria(
                    usuario.Id,
                    AcoesAuditoria.Atualizado,
                    $"perfil: {atual.Role} -> {perfilDesejado}",
                    autor.Id,
                    moduloKey);

                atual.Role = perfilDesejado;
            }
        }

        if (usuario.Modules.Count == 0)
        {
            throw new RegraDeNegocioException(
                "O usuário precisa continuar em pelo menos um módulo. Para tirar o acesso, desative-o.");
        }

        if (mudancas.Count == 0)
        {
            return Mapear(usuario, await NomesDeModulosAsync(cancellationToken));
        }

        usuario.UpdatedAt = DateTimeOffset.UtcNow;
        usuario.UpdatedBy = autor.Id;

        await db.SaveChangesAsync(cancellationToken);
        InvalidarCache(usuario.Id);

        logger.LogInformation(
            "Usuário {UserId} atualizado por {AutorId}: {Mudancas}.",
            usuario.Id,
            autor.Id,
            string.Join("; ", mudancas));

        return Mapear(usuario, await NomesDeModulosAsync(cancellationToken));
    }

    public async Task<UsuarioViewModel?> AlterarStatusAsync(
        Guid id,
        bool ativo,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var usuario = await CarregarGerenciavelAsync(id, autor, cancellationToken);
        if (usuario is null) return null;

        if (!ativo)
        {
            if (usuario.Id == autor.Id)
            {
                throw new RegraDeNegocioException("Você não pode desativar o seu próprio acesso.");
            }

            if (usuario.IsActive)
            {
                // Desativar tira a pessoa de todos os módulos de uma vez: é preciso
                // sobrar administrador em cada um onde ela era administradora.
                foreach (var vinculo in usuario.Modules.Where(m => m.Role == PerfisUsuario.Administrador))
                {
                    await GarantirQueNaoEhOUltimoAdministradorAsync(
                        usuario,
                        vinculo.ModuleKey,
                        autor,
                        cancellationToken);
                }
            }
        }

        var nomes = await NomesDeModulosAsync(cancellationToken);

        if (usuario.IsActive == ativo) return Mapear(usuario, nomes);

        usuario.IsActive = ativo;
        usuario.UpdatedAt = DateTimeOffset.UtcNow;
        usuario.UpdatedBy = autor.Id;

        RegistrarAuditoria(
            usuario.Id,
            ativo ? AcoesAuditoria.Ativado : AcoesAuditoria.Desativado,
            usuario.Email,
            autor.Id,
            null);

        await db.SaveChangesAsync(cancellationToken);
        InvalidarCache(usuario.Id);

        logger.LogInformation(
            "Usuário {UserId} {Status} por {AutorId}.",
            usuario.Id,
            ativo ? "ativado" : "desativado",
            autor.Id);

        return Mapear(usuario, nomes);
    }

    public async Task<UsuarioViewModel?> RedefinirSenhaAsync(
        Guid id,
        string novaSenha,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var usuario = await CarregarGerenciavelAsync(id, autor, cancellationToken);
        if (usuario is null) return null;

        LoginService.ValidarSenha(novaSenha, _tamanhoMinimoDeSenha);

        usuario.PasswordHash = hasher.HashPassword(usuario, novaSenha);

        // Provisória: quem receber é obrigado a trocar antes de usar o painel.
        // Sem isso, o administrador continuaria conhecendo a senha de outra pessoa.
        usuario.MustChangePassword = true;
        usuario.UpdatedAt = DateTimeOffset.UtcNow;
        usuario.UpdatedBy = autor.Id;

        RegistrarAuditoria(
            usuario.Id,
            AcoesAuditoria.SenhaRedefinida,
            "senha provisória definida por administrador",
            autor.Id,
            null);

        await db.SaveChangesAsync(cancellationToken);
        InvalidarCache(usuario.Id);

        logger.LogInformation("Senha de {UserId} redefinida por {AutorId}.", usuario.Id, autor.Id);

        return Mapear(usuario, await NomesDeModulosAsync(cancellationToken));
    }

    // ------------------------------------------------------------------------
    // Alcance do autor
    // ------------------------------------------------------------------------

    private async Task<IReadOnlyList<string>> ModulosGerenciaveisAsync(
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        if (!autor.IsSuperAdmin) return autor.ModulosQueAdministra;

        return await db.BiModules
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => m.Key)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Carrega o usuário só se ele estiver ao alcance do autor. Devolver null em
    /// vez de 403 é deliberado: o administrador do mkt não deve nem descobrir que
    /// existe alguém na conciliação com aquele id.
    /// </summary>
    private async Task<BiUser?> CarregarGerenciavelAsync(
        Guid id,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var usuario = await db.BiUsers
            .Include(u => u.Modules)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario is null) return null;
        if (autor.IsSuperAdmin) return usuario;

        // Um administrador de módulo não mexe no superadministrador.
        if (usuario.IsSuperAdmin) return null;

        var gerenciaveis = autor.ModulosQueAdministra;

        return usuario.Modules.Any(m => gerenciaveis.Contains(m.ModuleKey)) ? usuario : null;
    }

    /// <summary>
    /// Valida os vínculos pedidos: módulo existe, está ativo, o perfil é válido, e
    /// o autor tem alçada para concedê-lo.
    /// </summary>
    private async Task<Dictionary<string, string>> ValidarVinculosAsync(
        List<VinculoDeModuloRequest>? pedidos,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var resultado = new Dictionary<string, string>(StringComparer.Ordinal);
        if (pedidos is null || pedidos.Count == 0) return resultado;

        var gerenciaveis = await ModulosGerenciaveisAsync(autor, cancellationToken);

        foreach (var pedido in pedidos)
        {
            var moduloKey = (pedido.ModuleKey ?? string.Empty).Trim();
            var perfil = (pedido.Role ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(moduloKey)) continue;

            if (!gerenciaveis.Contains(moduloKey))
            {
                throw new RegraDeNegocioException($"Você não administra o módulo '{moduloKey}'.");
            }

            if (!PerfisUsuario.EhValido(perfil))
            {
                throw new RegraDeNegocioException(
                    $"Perfil inválido em '{moduloKey}'. Use um destes: {string.Join(", ", PerfisUsuario.Todos)}.");
            }

            resultado[moduloKey] = perfil;
        }

        return resultado;
    }

    private async Task GarantirQueNaoEhOUltimoAdministradorAsync(
        BiUser usuario,
        string moduloKey,
        UsuarioAutorizado autor,
        CancellationToken cancellationToken)
    {
        var eraAdministrador = usuario.Modules
            .Any(m => m.ModuleKey == moduloKey && m.Role == PerfisUsuario.Administrador);

        if (!eraAdministrador) return;

        if (usuario.Id == autor.Id)
        {
            throw new RegraDeNegocioException(
                "Você não pode remover o seu próprio acesso de administrador. Peça a outro administrador.");
        }

        var outros = await db.BiUserModules.CountAsync(
            m => m.ModuleKey == moduloKey
                 && m.Role == PerfisUsuario.Administrador
                 && m.UserId != usuario.Id
                 && m.User!.IsActive,
            cancellationToken);

        if (outros == 0)
        {
            var nome = await db.BiModules
                .Where(m => m.Key == moduloKey)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? moduloKey;

            throw new RegraDeNegocioException(
                $"Este é o último administrador ativo de {nome}. Promova outra pessoa antes de seguir.");
        }
    }

    // ------------------------------------------------------------------------
    // Apoio
    // ------------------------------------------------------------------------

    private async Task<Dictionary<string, string>> NomesDeModulosAsync(CancellationToken cancellationToken) =>
        await db.BiModules.AsNoTracking().ToDictionaryAsync(m => m.Key, m => m.Name, cancellationToken);

    private void RegistrarAuditoria(Guid usuarioId, string acao, string? detalhes, Guid autorId, string? moduloKey)
    {
        db.BiUserAudits.Add(new BiUserAudit
        {
            TargetUserId = usuarioId,
            Action = acao,
            Details = detalhes,
            ModuleKey = moduloKey,
            PerformedBy = autorId,
            PerformedAt = DateTimeOffset.UtcNow,
        });
    }

    /// <summary>
    /// Só faz diferença quando PainelDb:CacheSeconds está ligado — sem isso o próximo
    /// request já consulta o banco. Com cache ligado, é o que mantém a revogação imediata.
    /// </summary>
    private void InvalidarCache(Guid usuarioId) => cache.Remove($"bi_users:{usuarioId}");

    private static string NormalizarEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool EmailEhValido(string email) =>
        !string.IsNullOrWhiteSpace(email)
        && MailAddress.TryCreate(email, out var endereco)
        && endereco.Address == email;

    public static UsuarioViewModel Mapear(
        BiUser usuario,
        IReadOnlyDictionary<string, string>? nomesDeModulos = null,
        IReadOnlyList<string>? modulosVisiveis = null) => new()
    {
        Id = usuario.Id.ToString(),
        Name = usuario.Name,
        Email = usuario.Email,
        IsActive = usuario.IsActive,
        IsSuperAdmin = usuario.IsSuperAdmin,
        MustChangePassword = usuario.MustChangePassword,
        LastLoginAt = usuario.LastLoginAt,
        AvatarInitials = MontarIniciais(usuario.Name),
        Modulos =
        [
            .. usuario.Modules
                .Where(m => modulosVisiveis is null || modulosVisiveis.Contains(m.ModuleKey))
                .Select(m => new ModuloDoUsuarioViewModel
                {
                    ModuleKey = m.ModuleKey,
                    ModuleName = nomesDeModulos is not null && nomesDeModulos.TryGetValue(m.ModuleKey, out var nome)
                        ? nome
                        : m.ModuleKey,
                    Role = m.Role,
                })
                .OrderBy(m => m.ModuleName)
        ],
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
