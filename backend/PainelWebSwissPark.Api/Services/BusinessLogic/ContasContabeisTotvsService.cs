using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class ContasContabeisTotvsService(IContaContabilTotvsRepository contaContabilRepository) : IContasContabeisTotvsService
{
    public async Task<List<ContaContabilViewModel>> ListarAsync(CancellationToken cancellationToken)
    {
        var contas = await contaContabilRepository.ListarTodasColigadasAsync(cancellationToken);

        return contas
            // CODCOLIGADA+CODCONTA é a chave de negócio real, mas REDUZIDO/DESCRICAO
            // têm pequenas diferenças de formatação entre as 4 bases — o DISTINCT do
            // SQL não colapsa isso sozinho, então dedupe aqui por coligada+conta.
            .GroupBy(c => (c.CodColigada, c.CodConta))
            .Select(g => g.First())
            .Select(c => new ContaContabilViewModel
            {
                Id = $"{c.CodColigada}-{c.CodConta}",
                CodColigada = c.CodColigada,
                CodConta = c.CodConta,
                Reduzido = c.Reduzido ?? string.Empty,
                Descricao = c.Descricao ?? string.Empty,
            })
            .ToList();
    }
}
