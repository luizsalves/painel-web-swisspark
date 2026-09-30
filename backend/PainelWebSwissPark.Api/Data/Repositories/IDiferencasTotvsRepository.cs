namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IDiferencasTotvsRepository
{
    /// <summary>Composição do saldo (em aberto) por coligada/filial/conta, numa base, até o fim do mês.</summary>
    Task<List<ComposicaoContaLinha>> ListarComposicaoAsync(
        BaseTotvs baseDados, DateOnly fimDoMes, CancellationToken cancellationToken);

    /// <summary>Saldo do razão, no fim do mês, das contas que têm composição (fornecedores e adiantamentos).</summary>
    Task<List<SaldoRazaoContaLinha>> ListarSaldoRazaoAsync(
        BaseTotvs baseDados, DateOnly fimDoMes, CancellationToken cancellationToken);
}
