using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.Auth;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// Preparar → Conferir → Realizar Fechamento, com Reverter em qualquer etapa.
/// Cada ação grava uma linha nova em bi_fechamento_validacao — nunca atualiza
/// uma existente, então a trilha de quem fez o quê e quando fica completa.
/// </summary>
[ApiController]
[Route("api/fechamento-validacao")]
[ExigeModulo(Modulos.Conciliacao)]
public class FechamentoValidacaoController(
    IFechamentoValidacaoService service,
    IUsuarioAtualService usuarioAtual) : ControllerBase
{
    /// <summary>GET /api/fechamento-validacao?codColigada=&amp;codFilial=&amp;codConta=&amp;anoMes=</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] short codColigada, [FromQuery] short codFilial, [FromQuery] string codConta,
        [FromQuery] string anoMes, CancellationToken cancellationToken)
    {
        var status = await service.ObterStatusAtualAsync(codColigada, codFilial, codConta, anoMes, cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// GET /api/fechamento-validacao/produtividade?status=Fechado&amp;anoMes=2026-08 —
    /// quantas contas cada usuário preparou/conferiu/fechou, maior primeiro.
    /// status: Preparado, Conferido ou Fechado (Finalizado = Fechado). anoMes opcional.
    /// </summary>
    [HttpGet("produtividade")]
    public async Task<IActionResult> GetProdutividade(
        [FromQuery] string status, [FromQuery] string? anoMes, CancellationToken cancellationToken)
    {
        return Ok(await service.ObterProdutividadeAsync(status, anoMes, cancellationToken));
    }

    /// <summary>Analista, Gerente ou Administrador podem preparar.</summary>
    [HttpPost("preparar")]
    [ExigePerfil(Modulos.Conciliacao, PerfisUsuario.Analista, PerfisUsuario.Gerente, PerfisUsuario.Administrador)]
    public async Task<IActionResult> Preparar(
        [FromBody] PrepararFechamentoRequest request, CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        return Ok(await service.PrepararAsync(request, autor, cancellationToken));
    }

    /// <summary>Só Gerente e Administrador conferem — o Analista não confere o que ele mesmo preparou.</summary>
    [HttpPost("conferir")]
    [ExigePerfil(Modulos.Conciliacao, PerfisUsuario.Gerente, PerfisUsuario.Administrador)]
    public async Task<IActionResult> Conferir(
        [FromBody] ChaveFechamentoRequest request, CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        return Ok(await service.ConferirAsync(request, autor, cancellationToken));
    }

    /// <summary>Analista, Gerente ou Administrador podem realizar o fechamento.</summary>
    [HttpPost("realizar-fechamento")]
    [ExigePerfil(Modulos.Conciliacao, PerfisUsuario.Analista, PerfisUsuario.Gerente, PerfisUsuario.Administrador)]
    public async Task<IActionResult> RealizarFechamento(
        [FromBody] RealizarFechamentoRequest request, CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        return Ok(await service.RealizarFechamentoAsync(request, autor, cancellationToken));
    }

    /// <summary>
    /// Desfaz a etapa atual. Quem pode depende do que está sendo desfeito — o
    /// service decide (Analista desfaz Preparar/Fechar, Gerente desfaz Conferir).
    /// </summary>
    [HttpPost("reverter")]
    public async Task<IActionResult> Reverter(
        [FromBody] ChaveFechamentoRequest request, CancellationToken cancellationToken)
    {
        var autor = await ObterAutorAsync(cancellationToken);
        return Ok(await service.ReverterAsync(request, autor, cancellationToken));
    }

    private async Task<UsuarioAutorizado> ObterAutorAsync(CancellationToken cancellationToken) =>
        await usuarioAtual.ResolverAsync(User, cancellationToken)
        ?? throw new InvalidOperationException("Endpoint autorizado sem usuário resolvido.");
}
