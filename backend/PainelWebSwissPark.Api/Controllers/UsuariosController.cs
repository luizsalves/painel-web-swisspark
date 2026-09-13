using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// Área administrativa: quem entra no painel e com qual perfil.
/// Usuário não é removido, é desativado — a trilha de auditoria depende do registro existir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PoliticasAutorizacao.Administrador)]
public class UsuariosController(IUsuariosService usuariosService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var usuarios = await usuariosService.ListarAsync(cancellationToken);
        return Ok(usuarios);
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] CriarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await usuariosService.CriarAsync(request, User.ObterBiUserId(), cancellationToken);
        return CreatedAtAction(nameof(Get), usuario);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(
        Guid id,
        [FromBody] AtualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await usuariosService.AtualizarAsync(id, request, User.ObterBiUserId(), cancellationToken);
        return usuario is null ? NotFound(new { error = "Usuário não encontrado." }) : Ok(usuario);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> PatchStatus(
        Guid id,
        [FromBody] AlterarStatusUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await usuariosService.AlterarStatusAsync(
            id,
            request.IsActive,
            User.ObterBiUserId(),
            cancellationToken);

        return usuario is null ? NotFound(new { error = "Usuário não encontrado." }) : Ok(usuario);
    }

    /// <summary>Define uma senha provisória para outra pessoa.</summary>
    [HttpPost("{id:guid}/senha")]
    public async Task<IActionResult> RedefinirSenha(
        Guid id,
        [FromBody] RedefinirSenhaRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await usuariosService.RedefinirSenhaAsync(
            id,
            request.NovaSenha,
            User.ObterBiUserId(),
            cancellationToken);

        return usuario is null ? NotFound(new { error = "Usuário não encontrado." }) : Ok(usuario);
    }
}
