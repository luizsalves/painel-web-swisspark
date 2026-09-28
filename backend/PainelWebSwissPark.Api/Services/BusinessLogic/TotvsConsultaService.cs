using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.Exceptions;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface ITotvsConsultaService
{
    /// <summary>
    /// Lê a tabela nas 4 bases do TOTVS (ou só na base pedida), em sequência,
    /// até juntar <paramref name="limite"/> linhas. Cada linha traz "BASE".
    /// </summary>
    Task<List<Dictionary<string, object?>>> ListarAsync(
        TabelaTotvs tabela, IReadOnlyList<FiltroTotvs> filtros, string ordenacao,
        int? limite, string? baseDados, CancellationToken cancellationToken);
}

public class TotvsConsultaService(ITotvsConsultaRepository repository) : ITotvsConsultaService
{
    private const int LimitePadrao = 100;
    private const int LimiteMaximo = 1000;

    public async Task<List<Dictionary<string, object?>>> ListarAsync(
        TabelaTotvs tabela, IReadOnlyList<FiltroTotvs> filtros, string ordenacao,
        int? limite, string? baseDados, CancellationToken cancellationToken)
    {
        var limiteFinal = Math.Clamp(limite ?? LimitePadrao, 1, LimiteMaximo);

        BaseTotvs[] bases;
        if (string.IsNullOrWhiteSpace(baseDados))
        {
            bases = BasesTotvs.Todas;
        }
        else
        {
            var baseEscolhida = BasesTotvs.Ler(baseDados)
                ?? throw new RegraDeNegocioException(
                    $"Base inválida: \"{baseDados}\". Use corpore, bsb, mao ou neu.");
            bases = [baseEscolhida];
        }

        // Sequencial: as 4 bases passam pela mesma conexão do DbContext, que
        // não aceita duas consultas ao mesmo tempo.
        var resultado = new List<Dictionary<string, object?>>();
        foreach (var baseTotvs in bases)
        {
            var faltam = limiteFinal - resultado.Count;
            if (faltam <= 0) break;

            resultado.AddRange(await repository.ListarAsync(
                baseTotvs, tabela, filtros, ordenacao, faltam, cancellationToken));
        }

        return resultado;
    }
}
