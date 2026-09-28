using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.Auth;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ExigeModulo(Modulos.Conciliacao)]
public class ContasContabeisController(
    IContasContabeisTotvsService contasContabeisService,
    IUsuarioAtualService usuarioAtual) : ControllerBase
{
    /// <summary>
    /// GET /api/contascontabeis — plano de contas das quatro bases TOTVS
    /// (corpore, corpore_bsb, corpore_mao, corpore_neu), só para leitura, com
    /// o flag de ativo/inativo do próprio painel.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var contas = await contasContabeisService.ListarAsync(cancellationToken);
        return Ok(contas);
    }

    /// <summary>
    /// PATCH /api/contascontabeis/{codColigada}/{codConta}/status — ativa ou
    /// desativa uma conta contábil no painel. Não mexe em CCONTA (somente
    /// leitura); grava só no Postgres do painel. Qualquer usuário do módulo de
    /// Conciliação pode alterar.
    /// </summary>
    [HttpPatch("{codColigada}/{codConta}/status")]
    public async Task<IActionResult> PatchStatus(
        short codColigada, string codConta,
        [FromBody] AlterarStatusContaContabilRequest request, CancellationToken cancellationToken)
    {
        var autor = await usuarioAtual.ResolverAsync(User, cancellationToken)
            ?? throw new InvalidOperationException("Endpoint autorizado sem usuário resolvido.");

        var conta = await contasContabeisService.AtualizarStatusAsync(
            codColigada, codConta, request.Ativo, autor.Id, cancellationToken);

        return conta is null
            ? NotFound(new { error = "Conta contábil não encontrada." })
            : Ok(conta);
    }
}
