using PainelWebSwissPark.Api.Data.Repositories;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// O <see cref="DashboardPeriodoService"/> como ele é, só que lendo a lista de filiais com
/// as empresas de <see cref="EmpresasConsolidadas"/> já juntas na filial 0 — assim cada
/// conta delas conta uma vez por mês, com o status do fechamento consolidado.
/// </summary>
public class DashboardPeriodoConsolidadoService(
    IEmpresasService empresasService,
    IContasContabeisTotvsService contasContabeisService,
    IFechamentoValidacaoRepository fechamentoValidacaoRepository)
    : DashboardPeriodoService(
        new EmpresasComFiliaisConsolidadas(empresasService), contasContabeisService, fechamentoValidacaoRepository);
