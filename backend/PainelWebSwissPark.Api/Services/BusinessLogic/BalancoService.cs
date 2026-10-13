using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IBalancoService
{
    /// <summary>Balanço no último dia de <paramref name="mes"/>.</summary>
    Task<BalancoViewModel> CalcularAsync(
        DateTime mes, short codColigada, short? codFilial, CancellationToken cancellationToken);
}

/// <summary>
/// Balanço patrimonial a partir dos saldos da CPARTIDA, no layout do balanço da
/// contabilidade. O de-para foi conferido ao centavo contra o balanço de
/// 31/12/2025 da coligada 1, com a mesma exceção da DFC: os R$ 497,00 que a
/// contabilidade soma em Adiantamentos e em Fornecedores.
/// </summary>
public class BalancoService(
    IndicadoresTotvsRepository repository,
    BalanceteTotvsRepository balanceteRepository) : IBalancoService
{
    private enum Especial { Nenhuma, LucrosAcumulados, ResultadoExercicio }

    private sealed record Linha(string Descricao, string[] Prefixos, Especial Especial = Especial.Nenhuma);
    /// <summary>Linhas com subtotal próprio; sem subtotal, elas somam direto no bloco.</summary>
    private sealed record Grupo(string? Subtotal, Linha[] Linhas);
    private sealed record Bloco(string Total, Grupo[] Grupos);
    private sealed record Lado(string Total, Bloco[] Blocos);

    private static Linha L(string descricao, params string[] prefixos) => new(descricao, prefixos);

    private static readonly string[] ContasResultado = ["3", "4", "5", "6", "7", "8"];

    // Cada conta vai para a linha com o prefixo mais longo que a contém. As linhas
    // "Outros..." têm o prefixo do grupo inteiro e recolhem o que não tem linha
    // própria, então os subtotais sempre batem com as contas sintéticas.
    private static readonly Lado Ativo = new("ATIVO",
    [
        new("Ativo Circulante",
        [
            new("Disponível",
            [
                L("Caixa", "1.1.1.01"),
                L("Bancos c/ Movimento", "1.1.1.02"),
                L("Aplicações Financeiras", "1.1.1.03"),
                L("Outras Disponibilidades", "1.1.1"),
            ]),
            new("Realizável a Curto Prazo",
            [
                L("Duplicatas a Receber", "1.1.2.01"),
                L("Coobrigação de Securitização", "1.1.2.02.02", "1.1.2.02.04"),
                L("Fechamento de Securitização", "1.1.2.02.03"),
                L("Adiantamentos", "1.1.2.03", "1.1.2.04"),
                L("Outros Créditos", "1.1"),
                L("Impostos a Compensar", "1.1.2.06"),
                L("Despesas Antecipadas", "1.1.2.07"),
                L("Estoque", "1.1.2.08"),
            ]),
        ]),
        new("Ativo Não Circulante",
        [
            new("Realizável a Longo Prazo",
            [
                L("Duplicatas a Receber", "1.2.1.03.03", "1.2.1.03.04"),
                L("Impostos a Recuperar", "1.2.1.01"),
                L("Aplicação Financeira", "1.2.1.02"),
                L("Impostos Diferidos", "1.2.1.01.02"),
                L("Outros Créditos", "1.2.1"),
            ]),
            new(null,
            [
                L("Investimentos", "1.2.2"),
                L("Imobilizado", "1.2.3"),
                L("Intangíveis", "1.2.4"),
                L("Outros Ativos Não Circulantes", "1.2", "1"),
            ]),
        ]),
    ]);

    private static readonly Lado Passivo = new("PASSIVO",
    [
        new("Passivo Circulante",
        [
            new("Exigível a Curto Prazo",
            [
                // Seguros a Pagar entra em Fornecedores, como no balanço da contabilidade.
                L("Fornecedores", "2.1.1.01", "2.1.1.04.01.0005"),
                L("Salários e Encargos Sociais", "2.1.1.02"),
                L("Impostos a Recolher", "2.1.1.03"),
                L("Provisão p/ IRPJ e CSLL", "2.1.1.05"),
                L("Provisões Trabalhistas", "2.1.1.06"),
                L("Adiantamento de Clientes", "2.1.1.04.01.0002", "2.1.1.09"),
                L("Empréstimos e Financiamentos", "2.1.1.08"),
                L("Cessões de Crédito", "2.1.1.10"),
                L("Outras Contas a Pagar", "2.1"),
            ]),
        ]),
        new("Passivo Não Circulante",
        [
            new("Exigível a Longo Prazo",
            [
                L("Empréstimos e Financiamentos", "2.2.2.02"),
                L("Distribuição de Lucros a Pagar", "2.2.2.03.02"),
                L("Impostos Diferidos", "2.2.2.05"),
                L("Cessões de Crédito", "2.2.2.06"),
                L("Provisão para Contingência", "2.2.2.08"),
                L("Outras Obrigações", "2.2", "2"),
            ]),
        ]),
        new("Patrimônio Líquido",
        [
            new(null,
            [
                L("Capital Social", "2.3.1.01"),
                L("Reservas de Capital", "2.3.1.02"),
                L("Adiantamento p/ Futuro Aumento de Capital", "2.3.1.03"),
                L("Reservas de Lucros", "2.3.2.01"),
                new("Lucros/Prejuízos Acumulados", ["2.3"], Especial.LucrosAcumulados),
                new("Resultado do Exercício", [], Especial.ResultadoExercicio),
            ]),
        ]),
    ]);

    private static IEnumerable<Linha> LinhasDe(Lado lado) =>
        lado.Blocos.SelectMany(b => b.Grupos).SelectMany(g => g.Linhas);

    // Do mais longo para o mais curto: o primeiro que contém a conta é o mais específico.
    private static readonly (string Prefixo, Linha Linha, bool Credora)[] Prefixos =
    [
        .. LinhasDe(Ativo).SelectMany(l => l.Prefixos.Select(p => (p, l, false)))
            .Concat(LinhasDe(Passivo).SelectMany(l => l.Prefixos.Select(p => (p, l, true))))
            .OrderByDescending(p => p.Item1.Length),
    ];

    private static bool Pertence(string codConta, string prefixo) =>
        codConta == prefixo || codConta.StartsWith(prefixo + ".", StringComparison.Ordinal);

    private sealed class Saldo
    {
        public decimal SaldoFinal;
        /// <summary>Movimento do exercício sem as partidas de apuração.</summary>
        public decimal Resultado;
    }

    public async Task<BalancoViewModel> CalcularAsync(
        DateTime mes, short codColigada, short? codFilial, CancellationToken cancellationToken)
    {
        var fim = mes.AddMonths(1);
        // O resultado do exercício vai de janeiro até o mês do balanço.
        var inicioExercicio = new DateTime(mes.Year, 1, 1);
        var saldos = new Dictionary<string, Saldo>(StringComparer.Ordinal);
        var basesComMovimento = new List<BaseTotvs>();

        // O mesmo DbContext lê as bases em sequência; consultas paralelas na mesma
        // conexão provocam erro no EF Core.
        foreach (var baseDados in BasesTotvs.Todas)
        {
            var linhas = await repository.ListarSaldosAsync(
                baseDados, inicioExercicio, fim, codColigada, codFilial, cancellationToken);
            if (linhas.Count == 0) continue;
            basesComMovimento.Add(baseDados);

            foreach (var linha in linhas)
            {
                if (!saldos.TryGetValue(linha.CodConta, out var saldo))
                {
                    saldo = new Saldo();
                    saldos.Add(linha.CodConta, saldo);
                }
                saldo.SaldoFinal += linha.SaldoFinal;
                saldo.Resultado += linha.MovimentoSemApuracao;
            }
        }

        // Por referência: "Duplicatas a Receber" e "Outros Créditos" aparecem no curto e no longo prazo.
        var composicao = new Dictionary<Linha, List<BalancoComposicaoViewModel>>(ReferenceEqualityComparer.Instance);
        foreach (var linha in LinhasDe(Ativo).Concat(LinhasDe(Passivo))) composicao.Add(linha, []);
        var lucrosAcumulados = composicao[LinhasDe(Passivo).Single(l => l.Especial == Especial.LucrosAcumulados)];
        var resultadoExercicio = composicao[LinhasDe(Passivo).Single(l => l.Especial == Especial.ResultadoExercicio)];

        static void Adicionar(List<BalancoComposicaoViewModel> itens, string conta, decimal valor, string descricao = "")
        {
            valor = Math.Round(valor, 2);
            if (valor != 0)
                itens.Add(new() { CodConta = conta, Descricao = descricao, Valor = valor });
        }
        static bool EhResultado(string conta) => ContasResultado.Any(r => Pertence(conta, r));

        foreach (var (conta, saldo) in saldos.OrderBy(s => s.Key, StringComparer.Ordinal))
        {
            if (EhResultado(conta)) continue;
            var destino = Prefixos.FirstOrDefault(p => Pertence(conta, p.Prefixo));
            // Contas fora do ativo e do passivo (compensação) não entram no balanço.
            if (destino.Linha is null) continue;
            Adicionar(composicao[destino.Linha], conta, destino.Credora ? -saldo.SaldoFinal : saldo.SaldoFinal);
        }

        // Antes do encerramento o resultado ainda está nas contas 3 a 8; depois,
        // a apuração o leva para o PL. Nos dois casos o resultado do exercício é a
        // DRE de janeiro até o mês, e Lucros/Prejuízos Acumulados é o resto do PL.
        foreach (var grupo in ContasResultado)
            Adicionar(resultadoExercicio, grupo, -saldos.Where(s => Pertence(s.Key, grupo)).Sum(s => s.Value.Resultado));
        var resultado = resultadoExercicio.Sum(i => i.Valor);
        var naoEncerrado = -saldos.Where(s => EhResultado(s.Key)).Sum(s => s.Value.SaldoFinal);

        // Antes da apuração as duas parcelas se anulam; depois, sobra o resultado
        // que a apuração levou para o PL e que já aparece em linha própria.
        Adicionar(lucrosAcumulados, "", naoEncerrado - resultado,
            "(−) Resultado do exercício já apurado, apresentado em linha própria");

        await DescreverAsync(composicao.Values.SelectMany(i => i).ToList(), basesComMovimento, codColigada, cancellationToken);

        var ativo = Montar(Ativo, composicao);
        var passivo = Montar(Passivo, composicao);
        return new BalancoViewModel
        {
            Ativo = ativo,
            Passivo = passivo,
            TotalAtivo = ativo[^1].Valor,
            TotalPassivo = passivo[^1].Valor,
        };
    }

    private static List<BalancoLinhaViewModel> Montar(Lado lado, Dictionary<Linha, List<BalancoComposicaoViewModel>> composicao)
    {
        var linhas = new List<BalancoLinhaViewModel>();
        var totalLado = 0m;
        foreach (var bloco in lado.Blocos)
        {
            var totalBloco = 0m;
            foreach (var grupo in bloco.Grupos)
            {
                var totalGrupo = 0m;
                foreach (var linha in grupo.Linhas)
                {
                    var itens = composicao[linha];
                    var valor = itens.Sum(i => i.Valor);
                    totalGrupo += valor;
                    // Linha zerada não aparece, como no balanço da contabilidade.
                    if (valor == 0) continue;
                    linhas.Add(new() { Tipo = "linha", Descricao = linha.Descricao, Valor = valor, Composicao = itens });
                }
                if (grupo.Subtotal is not null)
                    linhas.Add(new() { Tipo = "grupo", Descricao = grupo.Subtotal, Valor = totalGrupo });
                totalBloco += totalGrupo;
            }
            linhas.Add(new() { Tipo = "bloco", Descricao = bloco.Total, Valor = totalBloco });
            totalLado += totalBloco;
        }
        linhas.Add(new() { Tipo = "total", Descricao = lado.Total, Valor = totalLado });
        return linhas;
    }

    private async Task DescreverAsync(
        List<BalancoComposicaoViewModel> itens, List<BaseTotvs> bases, short codColigada,
        CancellationToken cancellationToken)
    {
        var semDescricao = itens.Where(i => i.Descricao == "" && i.CodConta != "").ToList();
        var codigos = semDescricao.Select(i => i.CodConta).Distinct().ToList();
        if (codigos.Count == 0) return;

        var descricoes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var baseDados in bases)
        {
            var linhas = await balanceteRepository.ListarDescricoesAsync(baseDados, codigos, codColigada, cancellationToken);
            // A descrição própria da coligada vence a do plano padrão (coligada 0).
            foreach (var linha in linhas.Where(l => !string.IsNullOrWhiteSpace(l.Descricao))
                         .OrderByDescending(l => l.CodColigada))
                descricoes.TryAdd(linha.CodConta, linha.Descricao!.Trim());
        }
        foreach (var item in semDescricao)
            item.Descricao = descricoes.GetValueOrDefault(item.CodConta, string.Empty);
    }
}
