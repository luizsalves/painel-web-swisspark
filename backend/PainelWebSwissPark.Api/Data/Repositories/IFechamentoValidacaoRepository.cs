using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IFechamentoValidacaoRepository
{
    /// <summary>Histórico completo da chave, mais antigo primeiro.</summary>
    Task<List<FechamentoValidacao>> ListarHistoricoAsync(
        short codColigada, short codFilial, string codConta, string anoMes, CancellationToken cancellationToken);

    Task AdicionarAsync(FechamentoValidacao linha, CancellationToken cancellationToken);

    /// <summary>Nomes dos usuários dados os ids — usado para montar "preparado por", "conferido por" etc.</summary>
    Task<Dictionary<Guid, string>> ObterNomesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
