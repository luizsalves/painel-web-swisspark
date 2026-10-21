using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// O Excel do fechamento com o ano até o mês fechado. Separado de
/// <see cref="FechamentosController"/>, que continua sem mudança.
/// </summary>
[ApiController]
[Route("api/fechamentos/ano")]
[ExigeModulo(Modulos.Conciliacao)]
public class FechamentoAnualController : ControllerBase
{
    /// <summary>A aba RAZAO junta todos os meses, e uma aba .xls tem 65.536 linhas; o resto fica para cabeçalhos e resumos.</summary>
    private const int MaximoDeLancamentos = 60_000;

    /// <summary>POST /api/fechamentos/ano/exportar-excel — aba RAZAO e uma aba por mês, em .xls.</summary>
    [HttpPost("exportar-excel")]
    [RequestSizeLimit(30_000_000)]
    public IActionResult ExportarExcel([FromBody] ExportarFechamentoAnualRequest request)
    {
        if (request.Meses.Sum(m => m.Lancamentos.Count) > MaximoDeLancamentos)
            return BadRequest("O ano tem lançamentos demais para uma planilha. Exporte o histórico por partes.");
        // Cada mês vira uma aba com o nome dele, e o Excel não aceita duas abas com o mesmo nome.
        if (request.Meses.Select(m => m.AnoMes).Distinct().Count() != request.Meses.Count)
            return BadRequest("Há meses repetidos no pedido.");

        var fim = request.Meses[^1].AnoMes;
        return File(FechamentoAnualExcel.Gerar(request), "application/vnd.ms-excel", $"fechamento-{fim}.xls");
    }
}
