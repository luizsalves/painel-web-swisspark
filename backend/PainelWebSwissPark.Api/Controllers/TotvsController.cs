using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

/// <summary>
/// Leitura das tabelas do TOTVS RM — só GET, nunca grava. Uma rota por tabela.
/// Em todas: codColigada obrigatório (as tabelas são grandes e indexadas por
/// ele), limite opcional (padrão 100, máx. 1000) e base opcional
/// (corpore, bsb, mao, neu — sem ela, lê as 4). Cada linha traz todas as
/// colunas da tabela, com o nome original do TOTVS, mais "BASE".
/// Datas "até" são inclusivas (o dia inteiro).
/// </summary>
[ApiController]
[Route("api/totvs")]
[ExigeModulo(Modulos.Conciliacao)]
public class TotvsController(ITotvsConsultaService service) : ControllerBase
{
    /// <summary>GET /api/totvs/cexercicio — exercícios contábeis (períodos abertos/fechados).</summary>
    [HttpGet("cexercicio")]
    public Task<IActionResult> Cexercicio(
        [FromQuery, BindRequired] short codColigada, [FromQuery] int? idExercicio, [FromQuery] string? status,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .Igual("CODCOLIGADA", codColigada)
            .Igual("IDEXERCICIO", idExercicio)
            .Igual("STATUS", status);

        return Listar(TabelaTotvs.CEXERCICIO, filtros, "[CODCOLIGADA], [IDEXERCICIO] DESC", limite, @base, cancellationToken);
    }

    /// <summary>GET /api/totvs/csaldoexercicio — saldos contábeis por exercício.</summary>
    [HttpGet("csaldoexercicio")]
    public Task<IActionResult> CsaldoExercicio(
        [FromQuery, BindRequired] short codColigada, [FromQuery] int? idExercicio, [FromQuery] string? codConta,
        [FromQuery] short? codFilial, [FromQuery] string? codCCusto,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .Igual("CODCOLIGADA", codColigada)
            .Igual("IDEXERCICIO", idExercicio)
            .Igual("CODCONTA", codConta)
            .Igual("CODFILIAL", codFilial)
            .Igual("CODCCUSTO", codCCusto);

        return Listar(TabelaTotvs.CSALDOEXERCICIO, filtros, "[CODCOLIGADA], [IDSALDOEXERCICIO] DESC", limite, @base, cancellationToken);
    }

    /// <summary>GET /api/totvs/crateiolc — rateios dos lançamentos contábeis.</summary>
    [HttpGet("crateiolc")]
    public Task<IActionResult> CrateioLc(
        [FromQuery, BindRequired] short codColigada, [FromQuery] int? idPartida, [FromQuery] string? codConta,
        [FromQuery] DateTime? dataDe, [FromQuery] DateTime? dataAte,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .Igual("CODCOLIGADA", codColigada)
            .Igual("IDPARTIDA", idPartida)
            .Igual("CODCONTA", codConta)
            .Periodo("DATA", dataDe, dataAte);

        return Listar(TabelaTotvs.CRATEIOLC, filtros, "[CODCOLIGADA], [IDRATEIO] DESC", limite, @base, cancellationToken);
    }

    /// <summary>
    /// GET /api/totvs/fcfo — clientes/fornecedores. Traz também o cadastro
    /// global (CODCOLIGADA 0), que vale pra todas as coligadas.
    /// </summary>
    [HttpGet("fcfo")]
    public Task<IActionResult> Fcfo(
        [FromQuery, BindRequired] short codColigada, [FromQuery] string? codCfo, [FromQuery] string? cnpj,
        [FromQuery] string? nome, [FromQuery] short? pagRec, [FromQuery] short? ativo,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .IgualOuGlobal("CODCOLIGADA", codColigada)
            .Igual("CODCFO", codCfo)
            .Contem("CGCCFO", cnpj)
            .Contem("NOME", nome)
            .Igual("PAGREC", pagRec)
            .Igual("ATIVO", ativo);

        return Listar(TabelaTotvs.FCFO, filtros, "[CODCOLIGADA], [CODCFO]", limite, @base, cancellationToken);
    }

    /// <summary>GET /api/totvs/flan — lançamentos financeiros (contas a pagar e a receber).</summary>
    [HttpGet("flan")]
    public Task<IActionResult> Flan(
        [FromQuery, BindRequired] short codColigada, [FromQuery] short? codFilial, [FromQuery] int? idLan,
        [FromQuery] string? codCfo, [FromQuery] short? pagRec, [FromQuery] short? statusLan, [FromQuery] int? idMov,
        [FromQuery] string? numeroDocumento, [FromQuery] string? codTdo,
        [FromQuery] DateTime? vencimentoDe, [FromQuery] DateTime? vencimentoAte,
        [FromQuery] DateTime? emissaoDe, [FromQuery] DateTime? emissaoAte,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .Igual("CODCOLIGADA", codColigada)
            .Igual("CODFILIAL", codFilial)
            .Igual("IDLAN", idLan)
            .Igual("CODCFO", codCfo)
            .Igual("PAGREC", pagRec)
            .Igual("STATUSLAN", statusLan)
            .Igual("IDMOV", idMov)
            .Igual("NUMERODOCUMENTO", numeroDocumento)
            .Igual("CODTDO", codTdo)
            .Periodo("DATAVENCIMENTO", vencimentoDe, vencimentoAte)
            .Periodo("DATAEMISSAO", emissaoDe, emissaoAte);

        return Listar(TabelaTotvs.FLAN, filtros, "[CODCOLIGADA], [IDLAN] DESC", limite, @base, cancellationToken);
    }

    /// <summary>GET /api/totvs/flanbaixa — baixas (pagamentos/recebimentos) dos lançamentos financeiros.</summary>
    [HttpGet("flanbaixa")]
    public Task<IActionResult> FlanBaixa(
        [FromQuery, BindRequired] short codColigada, [FromQuery] int? idLan, [FromQuery] short? codFilial,
        [FromQuery] DateTime? dataBaixaDe, [FromQuery] DateTime? dataBaixaAte,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .Igual("CODCOLIGADA", codColigada)
            .Igual("IDLAN", idLan)
            .Igual("CODFILIAL", codFilial)
            .Periodo("DATABAIXA", dataBaixaDe, dataBaixaAte);

        return Listar(TabelaTotvs.FLANBAIXA, filtros, "[CODCOLIGADA], [IDLAN] DESC, [IDBAIXA] DESC", limite, @base, cancellationToken);
    }

    /// <summary>GET /api/totvs/frellan — relacionamentos entre lançamentos financeiros.</summary>
    [HttpGet("frellan")]
    public Task<IActionResult> FrelLan(
        [FromQuery, BindRequired] short codColigada, [FromQuery] int? idLan, [FromQuery] int? idLanRel,
        [FromQuery] short? tipoRel,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .Igual("CODCOLIGADA", codColigada)
            .Igual("IDLAN", idLan)
            .Igual("IDLANREL", idLanRel)
            .Igual("TIPOREL", tipoRel);

        return Listar(TabelaTotvs.FRELLAN, filtros, "[CODCOLIGADA], [IDLAN] DESC, [IDLANREL] DESC", limite, @base, cancellationToken);
    }

    /// <summary>GET /api/totvs/ftdo — tipos de documento (NF, boleto, duplicata...). Inclui o cadastro global.</summary>
    [HttpGet("ftdo")]
    public Task<IActionResult> Ftdo(
        [FromQuery, BindRequired] short codColigada, [FromQuery] string? codTdo, [FromQuery] string? descricao,
        [FromQuery] short? pagRec,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .IgualOuGlobal("CODCOLIGADA", codColigada)
            .Igual("CODTDO", codTdo)
            .Contem("DESCRICAO", descricao)
            .Igual("PAGREC", pagRec);

        return Listar(TabelaTotvs.FTDO, filtros, "[CODCOLIGADA], [CODTDO]", limite, @base, cancellationToken);
    }

    /// <summary>GET /api/totvs/gccusto — centros de custo. Inclui o cadastro global.</summary>
    [HttpGet("gccusto")]
    public Task<IActionResult> GcCusto(
        [FromQuery, BindRequired] short codColigada, [FromQuery] string? codCCusto, [FromQuery] string? nome,
        [FromQuery] string? ativo,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .IgualOuGlobal("CODCOLIGADA", codColigada)
            .Igual("CODCCUSTO", codCCusto)
            .Contem("NOME", nome)
            .Igual("ATIVO", ativo);

        return Listar(TabelaTotvs.GCCUSTO, filtros, "[CODCOLIGADA], [CODCCUSTO]", limite, @base, cancellationToken);
    }

    /// <summary>GET /api/totvs/tmov — movimentos (pedidos, NFs, entradas/saídas de estoque).</summary>
    [HttpGet("tmov")]
    public Task<IActionResult> Tmov(
        [FromQuery, BindRequired] short codColigada, [FromQuery] short? codFilial, [FromQuery] int? idMov,
        [FromQuery] string? codCfo, [FromQuery] string? codTmv, [FromQuery] string? numeroMov,
        [FromQuery] string? status, [FromQuery] DateTime? emissaoDe, [FromQuery] DateTime? emissaoAte,
        [FromQuery] int? limite, [FromQuery] string? @base, CancellationToken cancellationToken)
    {
        var filtros = new Filtros()
            .Igual("CODCOLIGADA", codColigada)
            .Igual("CODFILIAL", codFilial)
            .Igual("IDMOV", idMov)
            .Igual("CODCFO", codCfo)
            .Igual("CODTMV", codTmv)
            .Igual("NUMEROMOV", numeroMov)
            .Igual("STATUS", status)
            .Periodo("DATAEMISSAO", emissaoDe, emissaoAte);

        return Listar(TabelaTotvs.TMOV, filtros, "[CODCOLIGADA], [IDMOV] DESC", limite, @base, cancellationToken);
    }

    private async Task<IActionResult> Listar(
        TabelaTotvs tabela, Filtros filtros, string ordenacao, int? limite, string? baseDados,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListarAsync(tabela, filtros.Lista, ordenacao, limite, baseDados, cancellationToken));
    }

    /// <summary>Monta a lista ignorando os filtros não informados na requisição.</summary>
    private sealed class Filtros
    {
        public List<FiltroTotvs> Lista { get; } = [];

        public Filtros Igual(string coluna, object? valor)
        {
            if (valor is not null && valor is not string { Length: 0 }) Lista.Add(new(coluna, TipoFiltroTotvs.Igual, valor));
            return this;
        }

        public Filtros IgualOuGlobal(string coluna, object valor)
        {
            Lista.Add(new(coluna, TipoFiltroTotvs.IgualOuGlobal, valor));
            return this;
        }

        public Filtros Contem(string coluna, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor)) Lista.Add(new(coluna, TipoFiltroTotvs.Contem, valor.Trim()));
            return this;
        }

        public Filtros Periodo(string coluna, DateTime? de, DateTime? ate)
        {
            if (de is not null) Lista.Add(new(coluna, TipoFiltroTotvs.MaiorOuIgual, de.Value.Date));
            if (ate is not null) Lista.Add(new(coluna, TipoFiltroTotvs.Menor, ate.Value.Date.AddDays(1)));
            return this;
        }
    }
}
