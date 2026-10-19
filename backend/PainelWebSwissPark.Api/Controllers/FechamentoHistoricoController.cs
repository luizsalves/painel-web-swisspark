using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// A aba Histórico do fechamento. Separado de <see cref="FechamentosController"/>,
/// que continua sem mudança.
/// </summary>
[ApiController]
[Route("api/fechamentos/historico")]
[ExigeModulo(Modulos.Conciliacao)]
public class FechamentoHistoricoController : ControllerBase
{
    /// <summary>Uma planilha .xls tem 65.536 linhas; o resto fica para cabeçalhos e resumos.</summary>
    private const int MaximoDeLancamentos = 60_000;

    /// <summary>POST /api/fechamentos/historico/exportar-excel — o histórico que está na tela, em .xls.</summary>
    [HttpPost("exportar-excel")]
    [RequestSizeLimit(30_000_000)]
    public IActionResult ExportarExcel([FromBody] ExportarHistoricoFechamentoRequest request)
    {
        if (request.Meses.Sum(m => m.Lancamentos.Count) > MaximoDeLancamentos)
            return BadRequest("O histórico tem lançamentos demais para uma planilha. Escolha um período menor.");

        var inicio = request.Meses[0].AnoMes;
        var fim = request.Meses[^1].AnoMes;
        return File(FechamentoHistoricoExcel.Gerar(request), "application/vnd.ms-excel",
            $"historico-fechamento-{inicio}-a-{fim}.xls");
    }
}
