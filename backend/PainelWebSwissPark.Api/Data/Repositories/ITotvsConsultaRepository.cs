namespace PainelWebSwissPark.Api.Data.Repositories;

public interface ITotvsConsultaRepository
{
    /// <summary>
    /// SELECT TOP (limite) * de uma tabela do TOTVS numa base, com os filtros
    /// (todos em AND). Cada linha vem com todas as colunas da tabela + "BASE".
    /// ordenacao é texto fixo do código (ex.: "[CODCOLIGADA], [IDLAN] DESC").
    /// </summary>
    Task<List<Dictionary<string, object?>>> ListarAsync(
        BaseTotvs baseDados, TabelaTotvs tabela, IReadOnlyList<FiltroTotvs> filtros, string ordenacao,
        int limite, CancellationToken cancellationToken);
}
