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
public class EmpresasController(
    IEmpresasService empresasService,
    IUsuarioAtualService usuarioAtual) : ControllerBase
{
    /// <summary>
    /// GET /api/empresas — coligadas/filiais das quatro bases TOTVS (corpore,
    /// corpore_bsb, corpore_mao, corpore_neu), só para leitura, com o flag de
    /// ativo/inativo do próprio painel.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var empresas = await empresasService.ListarAsync(cancellationToken);
        return Ok(empresas);
    }

    /// <summary>
    /// PATCH /api/empresas/{codColigada}/{codFilial}/status — ativa ou desativa
    /// uma coligada+filial no painel. Não mexe em GFILIAL (somente leitura);
    /// grava só no Postgres do painel. Qualquer usuário do módulo de
    /// Conciliação pode alterar.
    /// </summary>
    [HttpPatch("{codColigada}/{codFilial}/status")]
    public async Task<IActionResult> PatchStatus(
        short codColigada, short codFilial,
        [FromBody] AlterarStatusEmpresaRequest request, CancellationToken cancellationToken)
    {
        var autor = await usuarioAtual.ResolverAsync(User, cancellationToken)
            ?? throw new InvalidOperationException("Endpoint autorizado sem usuário resolvido.");

        var empresa = await empresasService.AtualizarStatusAsync(
            codColigada, codFilial, request.Ativo, autor.Id, cancellationToken);

        return empresa is null
            ? NotFound(new { error = "Coligada/filial não encontrada." })
            : Ok(empresa);
    }
}
