using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Services.Auth;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// Área administrativa: quem entra no painel, em quais módulos e com qual perfil.
///
/// O recorte por módulo é feito no serviço: o administrador da conciliação só
/// lista, edita e desativa quem tem vínculo com a conciliação. Um id de outro
/// módulo responde 404, não 403 — ele não deve nem descobrir que aquela pessoa
/// existe.
///
/// Usuário não é removido, é desativado — a trilha de auditoria depende do
/// registro continuar existindo.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PoliticasAutorizacao.AdministradorDeModulo)]
public class UsuariosController(
    IUsuariosService usuariosService,
    IUsuarioAtualService usuarioAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        return Ok(await usuariosService.ListarAsync(autor, cancellationToken));
    }

    /// <summary>Os módulos que quem está logado pode conceder. Alimenta o formulário.</summary>
    [HttpGet("modulos")]
    public async Task<IActionResult> GetModulos(CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        return Ok(await usuariosService.ListarModulosGerenciaveisAsync(autor, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] CriarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        var usuario = await usuariosService.CriarAsync(request, autor, cancellationToken);

        return CreatedAtAction(nameof(Get), usuario);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(
        Guid id,
        [FromBody] AtualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        var usuario = await usuariosService.AtualizarAsync(id, request, autor, cancellationToken);

        return usuario is null ? NaoEncontrado() : Ok(usuario);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> PatchStatus(
        Guid id,
        [FromBody] AlterarStatusUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        var usuario = await usuariosService.AlterarStatusAsync(id, request.IsActive, autor, cancellationToken);

        return usuario is null ? NaoEncontrado() : Ok(usuario);
    }

    /// <summary>Define uma senha provisória para outra pessoa.</summary>
    [HttpPost("{id:guid}/senha")]
    public async Task<IActionResult> RedefinirSenha(
        Guid id,
        [FromBody] RedefinirSenhaRequest request,
        CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        var usuario = await usuariosService.RedefinirSenhaAsync(id, request.NovaSenha, autor, cancellationToken);

        return usuario is null ? NaoEncontrado() : Ok(usuario);
    }

    private async Task<UsuarioAutorizado> ObterAutorAsync(CancellationToken cancellationToken) =>
        await usuarioAtual.ResolverAsync(User, cancellationToken)
        ?? throw new InvalidOperationException("Endpoint autorizado sem usuário resolvido.");

    private NotFoundObjectResult NaoEncontrado() =>
        NotFound(new { error = "Usuário não encontrado." });
}
