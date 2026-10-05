using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IBalanceteService
{
    Task<List<BalanceteContaViewModel>> ListarAsync(
        DateTime inicio, short? codColigada, short? codFilial, string? codConta,
        CancellationToken cancellationToken);
}

public class BalanceteService(BalanceteTotvsRepository repository) : IBalanceteService
{
    private sealed class Acumulado
    {
        public short CodColigada { get; init; }
        public string Empresa { get; set; } = string.Empty;
        public string CodConta { get; init; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal SaldoAnterior { get; set; }
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public bool Sintetica { get; set; }
    }

    private static IEnumerable<string> NiveisDaConta(string codigo)
    {
        var partes = codigo.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var nivel = 1; nivel <= partes.Length; nivel++)
            yield return string.Join('.', partes.Take(nivel));
    }

    public async Task<List<BalanceteContaViewModel>> ListarAsync(
        DateTime inicio, short? codColigada, short? codFilial, string? codConta,
        CancellationToken cancellationToken)
    {
        var acumulados = new Dictionary<(short CodColigada, string CodConta), Acumulado>();

        // O mesmo DbContext lê as quatro bases em sequência; consultas paralelas
        // na mesma conexão provocam erro no EF Core.
        foreach (var baseDados in BasesTotvs.Todas)
        {
            var movimentos = await repository.ListarMovimentosAsync(
                baseDados, inicio, codColigada, codFilial, codConta, cancellationToken);
            if (movimentos.Count == 0) continue;

            var codigos = movimentos.SelectMany(m => NiveisDaConta(m.CodConta))
                .Distinct(StringComparer.Ordinal).ToArray();
            var descricoes = await repository.ListarDescricoesAsync(
                baseDados, codigos, codColigada, cancellationToken);
            var descricoesPorConta = descricoes
                .Where(d => !string.IsNullOrWhiteSpace(d.Descricao))
                .ToDictionary(d => (d.CodColigada, d.CodConta), d => d.Descricao!);

            foreach (var movimento in movimentos)
            {
                if (movimento.SaldoAnterior == 0 && movimento.Debito == 0 && movimento.Credito == 0)
                    continue;

                foreach (var codigo in NiveisDaConta(movimento.CodConta))
                {
                    var chave = (movimento.CodColigada, codigo);
                    if (!acumulados.TryGetValue(chave, out var conta))
                    {
                        conta = new Acumulado
                        {
                            CodColigada = movimento.CodColigada,
                            CodConta = codigo,
                            Empresa = movimento.Empresa,
                        };
                        acumulados.Add(chave, conta);
                    }

                    conta.SaldoAnterior += movimento.SaldoAnterior;
                    conta.Debito += movimento.Debito;
                    conta.Credito += movimento.Credito;
                    conta.Sintetica |= codigo != movimento.CodConta;

                    if (string.IsNullOrWhiteSpace(conta.Descricao))
                    {
                        conta.Descricao = descricoesPorConta.GetValueOrDefault(chave)
                            ?? descricoesPorConta.GetValueOrDefault(((short)0, codigo))
                            ?? string.Empty;
                    }
                }
            }
        }

        return acumulados.Values
            .Where(c => codConta is null || c.CodConta == codConta || c.CodConta.StartsWith(codConta + ".", StringComparison.Ordinal))
            .OrderBy(c => c.Empresa, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(c => c.CodColigada)
            .ThenBy(c => c.CodConta, StringComparer.Ordinal)
            .Select(c => new BalanceteContaViewModel
            {
                CodColigada = c.CodColigada,
                Empresa = c.Empresa,
                CodConta = c.CodConta,
                Descricao = c.Descricao,
                SaldoAnterior = c.SaldoAnterior,
                Debito = c.Debito,
                Credito = c.Credito,
                SaldoAtual = c.SaldoAnterior + c.Debito - c.Credito,
                Nivel = c.CodConta.Count(caractere => caractere == '.'),
                Sintetica = c.Sintetica,
            })
            .ToList();
    }
}
