using Microsoft.Extensions.Caching.Memory;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class ContasContabeisTotvsService(
    IContaContabilTotvsRepository contaContabilRepository,
    IContaContabilConfiguracaoRepository contaContabilConfiguracaoRepository,
    IMemoryCache cache) : IContasContabeisTotvsService
{
    // Plano de contas do TOTVS muda raríssimo, mas o UNION cruza os 4 bancos a
    // cada chamada. As telas de Fechamentos e de Contas Contábeis pedem essa
    // mesma lista — sem cache, abrir as duas seguidas dispara a consulta de
    // novo. O status ativo/inativo do painel NÃO entra nesse cache: vem sempre
    // fresco do Postgres, então ativar/desativar reflete na hora.
    private static readonly TimeSpan DuracaoCache = TimeSpan.FromHours(6);
    private const string ChaveCache = "contascontabeis:totvs";

    public async Task<List<ContaContabilViewModel>> ListarAsync(CancellationToken cancellationToken)
    {
        var contas = await ObterContasAsync(cancellationToken);
        var status = await contaContabilConfiguracaoRepository.ListarStatusAsync(cancellationToken);

        return contas.Select(c => ParaViewModel(c, status)).ToList();
    }

    public async Task<ContaContabilViewModel?> AtualizarStatusAsync(
        short codColigada, string codConta, bool ativo, Guid autorId, CancellationToken cancellationToken)
    {
        var contas = await ObterContasAsync(cancellationToken);
        var conta = contas.FirstOrDefault(c => c.CodColigada == codColigada && c.CodConta == codConta);
        if (conta is null) return null;

        await contaContabilConfiguracaoRepository.DefinirStatusAsync(codColigada, codConta, ativo, autorId, cancellationToken);

        return ParaViewModel(conta, new Dictionary<(short, string), bool> { [(codColigada, codConta)] = ativo });
    }

    private async Task<List<CcontaUniao>> ObterContasAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(ChaveCache, out List<CcontaUniao>? emCache) && emCache is not null)
        {
            return emCache;
        }

        var contas = await contaContabilRepository.ListarTodasColigadasAsync(cancellationToken);

        var deduplicadas = contas
            // CODCOLIGADA+CODCONTA é a chave de negócio real, mas REDUZIDO/DESCRICAO
            // têm pequenas diferenças de formatação entre as 4 bases — o DISTINCT do
            // SQL não colapsa isso sozinho, então dedupe aqui por coligada+conta.
            .GroupBy(c => (c.CodColigada, c.CodConta))
            .Select(g => g.First())
            .ToList();

        cache.Set(ChaveCache, deduplicadas, DuracaoCache);
        return deduplicadas;
    }

    // Ausência na configuração do painel significa ativa (default) — só ganha
    // linha em bi_conta_contabil_configuracao quando alguém mexe explicitamente.
    private static ContaContabilViewModel ParaViewModel(
        CcontaUniao c, IReadOnlyDictionary<(short CodColigada, string CodConta), bool> status) =>
        new()
        {
            Id = $"{c.CodColigada}-{c.CodConta}",
            CodColigada = c.CodColigada,
            CodConta = c.CodConta,
            Reduzido = c.Reduzido ?? string.Empty,
            Descricao = c.Descricao ?? string.Empty,
            Ativo = !status.TryGetValue((c.CodColigada, c.CodConta), out var ativo) || ativo,
        };
}
