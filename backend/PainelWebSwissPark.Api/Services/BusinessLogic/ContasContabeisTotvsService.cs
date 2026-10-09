using Microsoft.Extensions.Caching.Memory;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class ContasContabeisTotvsService(
    IContaContabilTotvsRepository contaContabilRepository,
    IContaContabilConfiguracaoRepository contaContabilConfiguracaoRepository,
    IMemoryCache cache) : IContasContabeisTotvsService
{
    // Compartilha a leitura da CPARTIDA entre as telas por poucos minutos:
    // novos lançamentos podem introduzir contas na lista. O status do painel
    // é lido separadamente, para ativar/desativar refletir na hora.
    private static readonly TimeSpan DuracaoCache = TimeSpan.FromMinutes(5);
    private const string ChaveCache = "contascontabeis:cpartida";

    public async Task<List<EmpresaContabilViewModel>> ListarEmpresasAsync(CancellationToken cancellationToken)
    {
        const string chave = ChaveCache + ":empresas:nomes:v2";
        if (cache.TryGetValue(chave, out List<EmpresaContabilViewModel>? emCache) && emCache is not null)
            return emCache;

        var linhas = await contaContabilRepository.ListarEmpresasAsync(cancellationToken);
        // O mesmo código pode ter nomes diferentes nas bases e nas SCPs.
        // Remover apenas duplicatas exatas, sem esconder os demais nomes.
        var resultado = linhas.Select(e => new EmpresaContabilViewModel(e.CodColigada, e.Nome))
            .Distinct().OrderBy(e => e.Nome).ThenBy(e => e.CodColigada).ToList();
        cache.Set(chave, resultado, DuracaoCache);
        return resultado;
    }

    public async Task<List<ColigadaFilialContabilViewModel>> ListarColigadasFiliaisAsync(
        CancellationToken cancellationToken, short? codColigada = null)
    {
        var chave = $"{ChaveCache}:coligadasfiliais:{codColigada}";
        if (cache.TryGetValue(chave, out List<ColigadaFilialContabilViewModel>? emCache) && emCache is not null)
            return emCache;

        var linhas = await contaContabilRepository.ListarColigadasFiliaisAsync(cancellationToken, codColigada);
        var resultado = linhas.Select(l => new ColigadaFilialContabilViewModel(l.CodColigada, l.CodFilial)).ToList();
        cache.Set(chave, resultado, DuracaoCache);
        return resultado;
    }

    public async Task<List<ContaContabilViewModel>> ListarAsync(
        CancellationToken cancellationToken, bool somenteBsb = false, short? codColigada = null, short? codFilial = null)
    {
        var contas = await ObterContasAsync(cancellationToken, somenteBsb, codColigada, codFilial);
        var status = await contaContabilConfiguracaoRepository.ListarStatusAsync(cancellationToken);

        return contas.GroupBy(c => (c.CodColigada, c.CodConta))
            .Select(g => ParaViewModel(g, status)).ToList();
    }

    public async Task<ContaContabilViewModel?> AtualizarStatusAsync(
        short codColigada, string codConta, bool ativo, Guid autorId, CancellationToken cancellationToken)
    {
        var contas = await ObterContasAsync(cancellationToken, codColigada: codColigada);
        var linhas = contas.Where(c => c.CodColigada == codColigada && c.CodConta == codConta).ToList();
        if (linhas.Count == 0) return null;

        await contaContabilConfiguracaoRepository.DefinirStatusAsync(codColigada, codConta, ativo, autorId, cancellationToken);

        return ParaViewModel(linhas, new Dictionary<(short, string), bool> { [(codColigada, codConta)] = ativo });
    }

    private async Task<List<ContaContabilMovimentada>> ObterContasAsync(
        CancellationToken cancellationToken, bool somenteBsb = false, short? codColigada = null, short? codFilial = null)
    {
        var chave = $"{ChaveCache}:{somenteBsb}:{codColigada}:{codFilial}";
        if (cache.TryGetValue(chave, out List<ContaContabilMovimentada>? emCache) && emCache is not null)
        {
            return emCache;
        }

        var contas = await contaContabilRepository.ListarTodasColigadasAsync(cancellationToken, somenteBsb, codColigada, codFilial);

        cache.Set(chave, contas, DuracaoCache);
        return contas;
    }

    // Ausência na configuração do painel significa ativa (default) — só ganha
    // linha em bi_conta_contabil_configuracao quando alguém mexe explicitamente.
    private static ContaContabilViewModel ParaViewModel(
        IEnumerable<ContaContabilMovimentada> linhas, IReadOnlyDictionary<(short CodColigada, string CodConta), bool> status)
    {
        var lista = linhas.ToList();
        var c = lista[0];
        return new()
        {
            Id = $"{c.CodColigada}-{c.CodConta}",
            CodColigada = c.CodColigada,
            CodConta = c.CodConta,
            CodFiliais = lista.Select(l => l.CodFilial).Distinct().Order().ToList(),
            Reduzido = lista.Select(l => l.Reduzido).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)) ?? string.Empty,
            Descricao = lista.Select(l => l.Descricao).Where(d => !string.IsNullOrWhiteSpace(d))
                .Order(StringComparer.Ordinal).FirstOrDefault() ?? string.Empty,
            Ativo = !status.TryGetValue((c.CodColigada, c.CodConta), out var ativo) || ativo,
        };
    }
}
