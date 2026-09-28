namespace PainelWebSwissPark.Api.Data.Repositories;

public interface IEmpresaConfiguracaoRepository
{
    /// <summary>Status (ativo/inativo) de cada coligada+filial que já foi alterada pelo painel.</summary>
    Task<Dictionary<(short CodColigada, short CodFilial), bool>> ListarStatusAsync(CancellationToken cancellationToken);

    Task DefinirStatusAsync(
        short codColigada, short codFilial, bool ativo, Guid autorId, CancellationToken cancellationToken);
}
