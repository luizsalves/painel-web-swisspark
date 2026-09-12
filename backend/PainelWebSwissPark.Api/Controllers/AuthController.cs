using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.Auth;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IUsuarioAtualService usuarioAtual) : ControllerBase
{
    /// <summary>
    /// Quem está falando com a API, segundo o token do Entra cruzado com a bi_users.
    /// É o primeiro request do front depois do login.
    /// </summary>
    /// <remarks>
    /// Exige apenas autenticação (e não a política padrão da API) de propósito: assim o
    /// 403 consegue dizer se a pessoa não está cadastrada ou se foi desativada, em vez de
    /// devolver uma resposta vazia que o front não sabe explicar.
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
                new { error = "Seu acesso ao painel ainda não foi liberado. Procure um administrador." });
        }

        if (!usuario.IsActive)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = "Seu acesso ao painel foi desativado. Procure um administrador." });
        }

        return Ok(new UsuarioViewModel
        {
            Id = usuario.Id.ToString(),
            Name = usuario.Name,
            Email = usuario.Email,
            Role = usuario.Role,
            IsActive = usuario.IsActive,
            PrimeiroAcessoPendente = usuario.EntraObjectId is null,
            AvatarInitials = UsuariosService.MontarIniciais(usuario.Name),
        });
    }
}
