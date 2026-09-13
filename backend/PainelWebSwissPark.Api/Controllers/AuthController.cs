using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.Auth;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    ILoginService loginService,
    IUsuarioAtualService usuarioAtual,
    PainelDbContext db) : ControllerBase
{
    /// <summary>
    /// Troca e-mail e senha por um token. A senha é conferida contra o hash
    /// PBKDF2 guardado na bi_users — em nenhum momento existe senha em texto no banco.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await loginService.AutenticarAsync(request.Email, request.Password, cancellationToken);

        if (!resultado.Sucesso)
        {
            return Unauthorized(new { error = resultado.Mensagem });
        }

        return Ok(new
        {
            accessToken = resultado.AccessToken,
            expiraEm = resultado.ExpiraEm,
            usuario = resultado.Usuario,
        });
    }

    /// <summary>
    /// Quem está falando com a API, com os módulos a que pertence. É o primeiro
    /// request do front depois do login, e o que monta o menu.
    /// </summary>
    /// <remarks>
    /// Exige apenas autenticação, e não a política padrão da API, de propósito: assim
    /// responde também para quem está com senha provisória — que precisa saber disso
    /// para ser mandado à tela de troca em vez de levar um 403 sem explicação.
    /// </remarks>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var usuario = await usuarioAtual.ResolverAsync(User, cancellationToken);

        if (usuario is null)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = "Seu usuário não existe mais. Procure um administrador." });
        }

        if (!usuario.IsActive)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = "Seu acesso ao painel foi desativado. Procure um administrador." });
        }

        var nomes = await db.BiModules
            .AsNoTracking()
            .ToDictionaryAsync(m => m.Key, m => m.Name, cancellationToken);

        // O superadministrador enxerga todos os módulos ativos, mesmo sem vínculo
        // em bi_user_modules — o acesso dele não vem de vínculo, vem do papel.
        var modulos = usuario.IsSuperAdmin
            ? await db.BiModules
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .Select(m => new ModuloDoUsuarioViewModel
                {
                    ModuleKey = m.Key,
                    ModuleName = m.Name,
                    Role = PainelWebSwissPark.Api.Entities.PerfisUsuario.Administrador,
                })
                .ToListAsync(cancellationToken)
            : [.. usuario.Modulos.Select(modulo => new ModuloDoUsuarioViewModel
                {
                    ModuleKey = modulo.Key,
                    ModuleName = nomes.TryGetValue(modulo.Key, out var nome) ? nome : modulo.Key,
                    Role = modulo.Role,
                })];

        return Ok(new UsuarioViewModel
        {
            Id = usuario.Id.ToString(),
            Name = usuario.Name,
            Email = usuario.Email,
            IsActive = usuario.IsActive,
            IsSuperAdmin = usuario.IsSuperAdmin,
            MustChangePassword = usuario.MustChangePassword,
            AvatarInitials = UsuariosService.MontarIniciais(usuario.Name),
            Modulos = modulos,
        });
    }

    /// <summary>
    /// Troca da própria senha. Também é o caminho de quem entrou com senha
    /// provisória — por isso exige só [Authorize], sem a política padrão que
    /// justamente bloqueia esse caso.
    /// </summary>
    [Authorize]
    [HttpPost("trocar-senha")]
    public async Task<IActionResult> TrocarSenha(
        [FromBody] TrocarSenhaRequest request,
        CancellationToken cancellationToken)
    {
        await loginService.TrocarSenhaAsync(
            User.ObterBiUserId(),
            request.SenhaAtual,
            request.NovaSenha,
            cancellationToken);

        return NoContent();
    }
}
