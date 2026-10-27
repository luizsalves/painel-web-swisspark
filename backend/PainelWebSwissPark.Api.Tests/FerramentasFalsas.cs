using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// Services falsos para as ferramentas da Tati: devolvem o que o teste mandou e anotam as chamadas,
/// sem banco. As empresas seguem a estrutura real (só nomes e códigos) de out/2026.
/// </summary>
internal sealed class EmpresasFalsas(params EmpresaViewModel[] filiais) : IEmpresasService
{
    public Task<List<EmpresaViewModel>> ListarAsync(CancellationToken cancellationToken, bool somenteBsb = false) =>
        Task.FromResult(filiais.ToList());

    public Task<EmpresaViewModel?> AtualizarStatusAsync(
        short codColigada, short codFilial, bool ativo, Guid autorId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public static EmpresaViewModel Filial(short coligada, short filial, string nome, string nomeFilial) => new()
    {
        Id = $"{coligada}-{filial}",
        CodColigada = coligada,
        CodFilial = filial,
        Nome = nome,
        NomeFilial = $"{filial} - {nomeFilial}",
    };

    /// <summary>
    /// "AEPL" em três coligadas; a AGV Campinas (coligada 1) com as SCPs como filiais de nomes
    /// próprios — e a filial 5, que no TOTVS se chama "SCP BETA" na razão social e "SCP DELTA" no nome.
    /// </summary>
    public static EmpresasFalsas DoPainel() => new(
        Filial(14, 1, "AEPL AGROPECUARIA LTDA", "AEPL AGROPECUARIA LTDA"),
        Filial(14, 2, "AEPL AGROPECUARIA LTDA", "AEPL AGROPECUARIA LTDA - FILIAL PR"),
        Filial(17, 1, "AEPL PARTICIPACOES LTDA", "AEPL PARTICIPACOES LTDA"),
        Filial(18, 1, "AEPL EMPREENDIMENTOS LTDA", "AEPL EMPREENDIMENTOS LTDA"),
        Filial(1, 1, "AGV CAMPINAS EMPREENDIMENTOS LTDA", "AGV CAMPINAS EMPREENDIMENTOS LTDA"),
        Filial(1, 2, "AGV CAMPINAS EMPREENDIMENTOS LTDA", "AGV CPS EMPREENDIMENTOS - FILIAL"),
        Filial(1, 5, "AGV CAMPINAS EMPREENDIMENTOS LTDA - SCP BETA", "AGV CPS - SCP DELTA"),
        Filial(1, 6, "AGV CAMPINAS EMPREENDIMENTOS LTDA - SCP GAMA", "AGV CPS - SCP GAMA"),
        Filial(1, 7, "AGV CAMPINAS EMPREENDIMENTOS LTDA - SCP ALFA", "AGV CPS - SCP ALFA"));
}

internal sealed class BalanceteFalso(List<BalanceteContaViewModel> contas) : IBalanceteService
{
    public ConcurrentQueue<(DateTime Inicio, short? Coligada, short? Filial, string? Conta)> Chamadas { get; } = new();

    public Task<List<BalanceteContaViewModel>> ListarAsync(
        DateTime inicio, short? codColigada, short? codFilial, string? codConta, CancellationToken cancellationToken)
    {
        Chamadas.Enqueue((inicio, codColigada, codFilial, codConta));
        // O mesmo filtro de conta do service: a própria conta e as abaixo dela.
        return Task.FromResult(contas
            .Where(c => codConta is null || c.CodConta == codConta || c.CodConta.StartsWith(codConta + ".", StringComparison.Ordinal))
            .ToList());
    }
}

/// <summary>Um balanço por mês; mês sem balanço volta vazio, como uma empresa sem saldos.</summary>
internal sealed class BalancoFalso(Dictionary<DateTime, BalancoViewModel> porMes) : IBalancoService
{
    public ConcurrentQueue<(DateTime Mes, short Coligada, short? Filial)> Chamadas { get; } = new();

    public Task<BalancoViewModel> CalcularAsync(DateTime mes, short codColigada, short? codFilial, CancellationToken cancellationToken)
    {
        Chamadas.Enqueue((mes, codColigada, codFilial));
        return Task.FromResult(porMes.GetValueOrDefault(mes) ?? new BalancoViewModel());
    }
}

/// <summary>Um resultado por mês inicial; sem resultado, tudo zerado, como uma empresa sem lançamentos.</summary>
internal sealed class IndicadoresFalso(Dictionary<DateTime, IndicadoresViewModel> porInicio) : IIndicadoresService
{
    public ConcurrentQueue<(DateTime Inicio, DateTime Fim, short Coligada, short? Filial)> Chamadas { get; } = new();

    public Task<IndicadoresViewModel> CalcularAsync(
        DateTime inicio, DateTime fim, short codColigada, short? codFilial, CancellationToken cancellationToken)
    {
        Chamadas.Enqueue((inicio, fim, codColigada, codFilial));
        return Task.FromResult(porInicio.GetValueOrDefault(inicio) ?? new IndicadoresViewModel());
    }
}

internal static class Ferramenta
{
    /// <summary>Os escopos de DI que as ferramentas abrem para o período anterior, com o mesmo service falso.</summary>
    public static IServiceScopeFactory Escopos<TServico>(TServico servico) where TServico : class =>
        new ServiceCollection().AddSingleton(servico).BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

    public static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    /// <summary>Roda a ferramenta com a entrada (JSON) e devolve a saída já lida.</summary>
    public static async Task<JsonElement> Executar(Func<JsonElement, CancellationToken, Task<string>> executar, string entrada) =>
        Json(await executar(Json(entrada), CancellationToken.None));

    public static string Erro(JsonElement saida) =>
        saida.TryGetProperty("erro", out var erro) ? erro.GetString()! : throw new Xunit.Sdk.XunitException($"Sem erro na saída: {saida}");

    public static string ProximoMes() => DateTime.Today.AddMonths(1).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
}
