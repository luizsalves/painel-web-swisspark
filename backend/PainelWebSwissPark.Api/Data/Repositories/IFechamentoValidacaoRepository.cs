using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IFechamentoValidacaoRepository
{
    /// <summary>Histórico completo da chave, mais antigo primeiro.</summary>
    Task<List<FechamentoValidacao>> ListarHistoricoAsync(
        short codColigada, short codFilial, string codConta, string anoMes, CancellationToken cancellationToken);

    Task AdicionarAsync(FechamentoValidacao linha, CancellationToken cancellationToken);

    /// <summary>Todas as linhas de um período (todas as chaves) — base do resumo do dashboard.</summary>
    Task<List<FechamentoValidacao>> ListarPorAnoMesAsync(string anoMes, CancellationToken cancellationToken);

    /// <summary>O AnoMes mais recente com pelo menos uma linha — null se a tabela estiver vazia.</summary>
    Task<string?> ObterUltimoAnoMesAsync(CancellationToken cancellationToken);

    /// <summary>Nomes dos usuários dados os ids — usado para montar "preparado por", "conferido por" etc.</summary>
    Task<Dictionary<Guid, string>> ObterNomesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Quantas linhas com esse status cada usuário registrou (realizado_por), maior primeiro.
    /// anoMes null = todos os períodos.
    /// </summary>
    Task<List<ProdutividadeUsuarioViewModel>> ContarPorUsuarioAsync(
        string status, string? anoMes, CancellationToken cancellationToken);
}
