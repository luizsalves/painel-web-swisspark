using System.Globalization;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>Os saldos de uma conta num período, como o <see cref="IndicadoresService"/> os soma.</summary>
internal sealed record SaldoDaConta(
    string CodConta, decimal SaldoInicial, decimal SaldoFinal, decimal Debitos, decimal Creditos, decimal MovimentoSemApuracao);

public interface IIndicadoresMensaisService
{
    /// <summary>
    /// A base dos indicadores de cada mês de <paramref name="inicio"/> a <paramref name="fim"/>
    /// (o primeiro dia de cada um; <paramref name="fim"/> fora): o balanço no último dia do mês
    /// e o resultado do mês. É a mesma conta de <see cref="IIndicadoresService.CalcularAsync"/>
    /// para um mês só, com uma leitura da CPARTIDA no lugar de uma por mês.
    /// </summary>
    Task<List<BaseCalculoMensalViewModel>> CalcularAsync(
        DateTime inicio, DateTime fim, short codColigada, short? codFilial, CancellationToken cancellationToken);
}

public class IndicadoresMensaisService(IndicadoresMensaisTotvsRepository repository) : IIndicadoresMensaisService
{
    public async Task<List<BaseCalculoMensalViewModel>> CalcularAsync(
        DateTime inicio, DateTime fim, short codColigada, short? codFilial, CancellationToken cancellationToken)
    {
        // O mesmo DbContext lê as quatro bases em sequência, como no IndicadoresService.
        var linhas = new List<IndicadoresMovimentoMensalLinha>();
        foreach (var baseDados in BasesTotvs.Todas)
            linhas.AddRange(await repository.ListarAsync(baseDados, inicio, fim, codColigada, codFilial, cancellationToken));

        return Montar(linhas, inicio, fim);
    }

    /// <summary>A base de cada mês a partir das linhas da consulta, com as quatro bases juntas.</summary>
    internal static List<BaseCalculoMensalViewModel> Montar(
        IEnumerable<IndicadoresMovimentoMensalLinha> linhas, DateTime inicio, DateTime fim)
    {
        // O que veio antes do primeiro mês só entra no saldo; o resto fica separado por mês.
        var saldos = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var movimentosPorMes = new Dictionary<string, Dictionary<string, Movimento>>(StringComparer.Ordinal);
        foreach (var linha in linhas)
        {
            if (linha.AnoMes is null)
            {
                saldos[linha.CodConta] = saldos.GetValueOrDefault(linha.CodConta) + linha.Debitos - linha.Creditos;
                continue;
            }

            if (!movimentosPorMes.TryGetValue(linha.AnoMes, out var doMes))
            {
                doMes = new(StringComparer.Ordinal);
                movimentosPorMes.Add(linha.AnoMes, doMes);
            }
            var atual = doMes.GetValueOrDefault(linha.CodConta);
            doMes[linha.CodConta] = new Movimento(
                atual.Debitos + linha.Debitos,
                atual.Creditos + linha.Creditos,
                atual.SemApuracao + linha.MovimentoSemApuracao);
        }

        var resultado = new List<BaseCalculoMensalViewModel>();
        for (var mes = inicio; mes < fim; mes = mes.AddMonths(1))
        {
            var anoMes = mes.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            var doMes = movimentosPorMes.GetValueOrDefault(anoMes) ?? [];

            var saldoInicial = new Dictionary<string, decimal>(saldos, StringComparer.Ordinal);
            foreach (var (conta, movimento) in doMes)
                saldos[conta] = saldos.GetValueOrDefault(conta) + movimento.Debitos - movimento.Creditos;

            var contas = saldos.Select(par =>
            {
                var movimento = doMes.GetValueOrDefault(par.Key);
                return new SaldoDaConta(
                    par.Key, saldoInicial.GetValueOrDefault(par.Key), par.Value,
                    movimento.Debitos, movimento.Creditos, movimento.SemApuracao);
            });

            resultado.Add(new BaseCalculoMensalViewModel
            {
                AnoMes = anoMes,
                BaseCalculo = IndicadoresService.MontarBaseCalculo(contas),
            });
        }

        return resultado;
    }

    private readonly record struct Movimento(decimal Debitos, decimal Creditos, decimal SemApuracao);
}
