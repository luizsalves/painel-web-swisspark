namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IContaContabilConfiguracaoRepository
{
    /// <summary>Status (ativo/inativo) de cada coligada+conta que já foi alterada pelo painel.</summary>
    Task<Dictionary<(short CodColigada, string CodConta), bool>> ListarStatusAsync(CancellationToken cancellationToken);

    Task DefinirStatusAsync(
        short codColigada, string codConta, bool ativo, Guid autorId, CancellationToken cancellationToken);
}
