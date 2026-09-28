using Microsoft.Extensions.Caching.Memory;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class EmpresasService(
    IEmpresaRepository empresaRepository,
    IEmpresaConfiguracaoRepository empresaConfiguracaoRepository,
    IMemoryCache cache) : IEmpresasService
{
    // GFILIAL quase não muda (uma coligada/filial nova a cada alguns meses),
    // mas o UNION cruza os 4 bancos do TOTVS a cada chamada — sem cache, essa
    // consulta roda de novo toda vez que a tela de Fechamentos ou de Empresas
    // abre. O status ativo/inativo do painel NÃO entra nesse cache: ele vem
    // sempre fresco do Postgres, então ativar/desativar reflete na hora, sem
    // precisar invalidar nada aqui.
    private static readonly TimeSpan DuracaoCacheFiliais = TimeSpan.FromHours(6);
    private const string ChaveCacheFiliais = "empresas:totvs-filiais";

    public async Task<List<EmpresaViewModel>> ListarAsync(CancellationToken cancellationToken)
    {
        var filiais = await ObterFiliaisAsync(cancellationToken);
        var status = await empresaConfiguracaoRepository.ListarStatusAsync(cancellationToken);

        return filiais
            .Select(f => ParaViewModel(f, status))
            .ToList();
    }

    public async Task<EmpresaViewModel?> AtualizarStatusAsync(
        short codColigada, short codFilial, bool ativo, Guid autorId, CancellationToken cancellationToken)
    {
        var filiais = await ObterFiliaisAsync(cancellationToken);
        var filial = filiais.FirstOrDefault(f => f.CodColigada == codColigada && f.CodFilial == codFilial);
        if (filial is null) return null;

        await empresaConfiguracaoRepository.DefinirStatusAsync(codColigada, codFilial, ativo, autorId, cancellationToken);

        return ParaViewModel(filial, new Dictionary<(short, short), bool> { [(codColigada, codFilial)] = ativo });
    }

    private async Task<List<GfilialUniao>> ObterFiliaisAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(ChaveCacheFiliais, out List<GfilialUniao>? emCache) && emCache is not null)
        {
            return emCache;
        }

        var filiais = await empresaRepository.ListarTodasColigadasAsync(cancellationToken);
        cache.Set(ChaveCacheFiliais, filiais, DuracaoCacheFiliais);
        return filiais;
    }

    // Ausência na configuração do painel significa ativa (default) — só ganha
    // linha em bi_empresa_configuracao quando alguém mexe explicitamente.
    private static EmpresaViewModel ParaViewModel(
        GfilialUniao f, IReadOnlyDictionary<(short CodColigada, short CodFilial), bool> status) =>
        new()
        {
            Id = $"{f.CodColigada}-{f.CodFilial}",
            CodColigada = f.CodColigada,
            CodFilial = f.CodFilial,
            Nome = f.Nome ?? string.Empty,
            NomeFantasia = f.NomeFantasia ?? string.Empty,
            NomeFilial = $"{f.CodFilial} - {f.NomeFantasia}",
            Cgc = f.Cgc ?? string.Empty,
            Cidade = f.Cidade ?? string.Empty,
            Estado = f.Estado ?? string.Empty,
            Ativo = !status.TryGetValue((f.CodColigada, f.CodFilial), out var ativo) || ativo,
        };
}
