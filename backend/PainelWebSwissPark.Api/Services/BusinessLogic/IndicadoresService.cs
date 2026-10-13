using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IIndicadoresService
{
    Task<IndicadoresViewModel> CalcularAsync(
        DateTime inicio, DateTime fim, short codColigada, short? codFilial, CancellationToken cancellationToken);
}

/// <summary>
/// Índices financeiros e DFC pelo método indireto, calculados a partir dos saldos
/// da CPARTIDA. O plano de contas é único (coligada 0), então o de-para vale para
/// todas as empresas — foi conferido ao centavo contra a DFC de 2025 da coligada 1.
/// </summary>
public class IndicadoresService(
    IndicadoresTotvsRepository repository,
    BalanceteTotvsRepository balanceteRepository) : IIndicadoresService
{
    private enum Medida { Variacao, Debitos, Creditos, Resultado, Apuracao }

    private sealed record Parcela(string Conta, Medida Medida, int Fator);

    /// <summary>Uma linha da DFC; o rótulo muda conforme o sinal do valor.</summary>
    private sealed record LinhaDfc(
        string Grupo, string SePositivo, string SeNegativo, Parcela[] Parcelas, bool OcultarQuandoZero = false);

    private sealed class Saldo
    {
        public decimal SaldoInicial;
        public decimal SaldoFinal;
        public decimal Debitos;
        public decimal Creditos;
        public decimal MovimentoSemApuracao;

        public decimal Variacao => SaldoFinal - SaldoInicial;

        public decimal Valor(Medida medida) => medida switch
        {
            Medida.Variacao => Variacao,
            Medida.Debitos => Debitos,
            Medida.Creditos => Creditos,
            Medida.Resultado => MovimentoSemApuracao,
            Medida.Apuracao => Variacao - MovimentoSemApuracao,
            _ => throw new ArgumentOutOfRangeException(nameof(medida)),
        };
    }

    private const string Operacionais = "Atividades Operacionais";
    private const string Financiamento = "Atividades de Financiamento";
    private const string Investimentos = "Atividades de Investimentos";
    private const string NaoClassificadas = "Variações não classificadas";
    private const string Caixa = "1.1.1";
    private static readonly string[] ContasResultado = ["3", "4", "5", "6", "7", "8"];

    // Saldos são devedores positivos, então o efeito de uma conta patrimonial no
    // caixa é -1 x variação — daí o fator padrão.
    private static Parcela Var(string conta, int fator = -1) => new(conta, Medida.Variacao, fator);
    private static Parcela Deb(string conta, int fator = -1) => new(conta, Medida.Debitos, fator);
    private static Parcela Cred(string conta, int fator = 1) => new(conta, Medida.Creditos, fator);

    private static LinhaDfc Fixa(string grupo, string nome, Parcela[] parcelas, bool ocultarQuandoZero = false) =>
        new(grupo, nome, nome, parcelas, ocultarQuandoZero);
    private static LinhaDfc Ativo(string grupo, string nome, Parcela[] parcelas) =>
        new(grupo, $"Diminuição de {nome}", $"Aumento de {nome}", parcelas);
    private static LinhaDfc Passivo(string grupo, string nome, Parcela[] parcelas) =>
        new(grupo, $"Aumento de {nome}", $"Diminuição de {nome}", parcelas);

    // Cada conta analítica tem de ser coberta exatamente uma vez (soma dos fatores
    // aplicados = -1 x variação). O que sobrar vai para "Variações não classificadas".
    private static readonly LinhaDfc[] DeParaDfc =
    [
        Fixa(Operacionais, "Lucro do Exercício",
            [.. ContasResultado.Select(c => new Parcela(c, Medida.Resultado, -1))]),
        Fixa(Operacionais, "(+) Depreciação e Amortização", [Var("1.2.3.05"), Var("1.2.4.02")]),
        Fixa(Operacionais, "(+) Perda c/ Venda de Imobilizado", [Cred("1.2.3.01")]),
        Fixa(Operacionais, "(+) Provisões", [Var("2.2.2.08"), Var("2.1.1.06")]),
        Fixa(Operacionais, "(+) Impostos a Compensar", [Var("1.1.2.06")]),
        Fixa(Operacionais, "(+) Impostos Diferidos", [Var("2.2.2.05"), Var("1.2.1.01.02")]),
        Fixa(Operacionais, "(+) Despesas Antecipadas", [Var("1.1.2.07")]),
        // O AVP vai com o sinal da variação, como na DFC da contabilidade; o efeito
        // no caixa volta descontado nas linhas de Duplicatas e de Cessão de Crédito.
        Fixa(Operacionais, "(+) AVP Ativo", [Var("1.1.2.01.04", 1), Var("1.2.1.03.04", 1), Var("1.1.2.02.04", 1)]),
        Fixa(Operacionais, "(-) AVP Passivo", [Var("2.1.1.10.02", 1), Var("2.2.2.06.06", 1)]),
        Ativo(Operacionais, "Duplicatas a Receber",
            [Var("1.1.2.01"), Var("1.2.1.03"), Var("1.1.2.01.04"), Var("1.2.1.03.04"), Var("1.1.2.02.04")]),
        Ativo(Operacionais, "Estoques", [Var("1.1.2.08")]),
        Ativo(Operacionais, "Adiantamentos a Fornecedores", [Var("1.1.2.04")]),
        Passivo(Operacionais, "Adiantamentos de Clientes", [Var("2.1.1.04.01.0002")]),
        Ativo(Operacionais, "Impostos a Recuperar", [Var("1.2.1.01.01")]),
        Passivo(Operacionais, "Fornecedores", [Var("2.1.1.01"), Var("1.1.2.05.01.0002"), Var("2.1.1.04.01.0005")]),
        Passivo(Operacionais, "Salários e Encargos Sociais", [Var("2.1.1.02")]),
        Passivo(Operacionais, "Impostos e Contribuições a Pagar", [Var("2.1.1.03")]),
        Passivo(Operacionais, "Provisão para IR e CS", [Var("2.1.1.05")]),

        Fixa(Financiamento, "Juros Sobre Empréstimos", [Var("2.1.1.08")]),
        Fixa(Financiamento, "Pagamento de Dividendos", [Deb("2.2.2.03.02")]),
        Fixa(Financiamento, "Cessão de Crédito-Securitização",
        [
            Var("2.1.1.10"), Var("2.2.2.06"), Var("1.1.2.02.01"),
            Var("1.1.2.05.01.0009"), Var("1.1.2.05.01.0010"), Var("1.1.2.05.01.0011"),
            Var("2.1.1.10.02"), Var("2.2.2.06.06"),
        ]),
        new(Financiamento, "Recebimento de Empréstimos", "Pagamento de Empréstimos", [Var("2.2.2.02")]),
        new(Financiamento, "Integralização de Capital", "Redução de Capital Social", [Var("2.3.1")]),
        Fixa(Financiamento, "Coobrigação de Securitização", [Var("1.1.2.02.02"), Var("1.1.2.02.04")]),
        Fixa(Financiamento, "Fechamento de Securitização", [Var("1.1.2.02.03")]),
        // Fecha a conta entre o lucro da DRE e o PL: apuração, destinação de
        // dividendos e ajustes diretos no PL. Normalmente dá zero.
        Fixa(Financiamento, "Outras movimentações do Patrimônio Líquido",
        [
            .. ContasResultado.Select(c => new Parcela(c, Medida.Apuracao, -1)),
            Var("2.3.2"), Cred("2.2.2.03.02"),
        ], ocultarQuandoZero: true),

        Fixa(Investimentos, "Pagamento de Aquisição de Imobilizado/Intangível", [Deb("1.2.3.01"), Var("1.2.4.01")]),
        new(Investimentos, "Resgate de Aplicações Financeiras", "Aplicação de Liquidez Imediata", [Var("1.2.1.02")]),
    ];

    private static readonly string[] OrdemGrupos = [Operacionais, Financiamento, Investimentos, NaoClassificadas];

    private static bool Pertence(string codConta, string prefixo) =>
        codConta == prefixo || codConta.StartsWith(prefixo + ".", StringComparison.Ordinal);

    private static string NomeDaMedida(Medida medida) => medida switch
    {
        Medida.Variacao => "Variação do saldo",
        Medida.Debitos => "Débitos do período",
        Medida.Creditos => "Créditos do período",
        Medida.Resultado => "Resultado do período (DRE)",
        Medida.Apuracao => "Apuração do resultado",
        _ => string.Empty,
    };

    public async Task<IndicadoresViewModel> CalcularAsync(
        DateTime inicio, DateTime fim, short codColigada, short? codFilial, CancellationToken cancellationToken)
    {
        var saldos = new Dictionary<string, Saldo>(StringComparer.Ordinal);
        var basesComMovimento = new List<BaseTotvs>();

        // O mesmo DbContext lê as quatro bases em sequência; consultas paralelas
        // na mesma conexão provocam erro no EF Core.
        foreach (var baseDados in BasesTotvs.Todas)
        {
            var linhas = await repository.ListarSaldosAsync(
                baseDados, inicio, fim, codColigada, codFilial, cancellationToken);
            if (linhas.Count == 0) continue;
            basesComMovimento.Add(baseDados);

            foreach (var linha in linhas)
            {
                if (!saldos.TryGetValue(linha.CodConta, out var saldo))
                {
                    saldo = new Saldo();
                    saldos.Add(linha.CodConta, saldo);
                }
                saldo.SaldoInicial += linha.SaldoInicial;
                saldo.SaldoFinal += linha.SaldoFinal;
                saldo.Debitos += linha.Debitos;
                saldo.Creditos += linha.Creditos;
                saldo.MovimentoSemApuracao += linha.MovimentoSemApuracao;
            }
        }

        var baseCalculo = MontarBaseCalculo(saldos);
        var (dfc, codigosParaDescrever) = MontarDfc(saldos);
        var descricoes = await ListarDescricoesAsync(basesComMovimento, codigosParaDescrever, codColigada, cancellationToken);
        foreach (var item in dfc.Grupos.SelectMany(g => g.Linhas).SelectMany(l => l.Composicao))
            item.Descricao = descricoes.GetValueOrDefault(item.CodConta, string.Empty);

        return new IndicadoresViewModel
        {
            BaseCalculo = baseCalculo,
            Indices = MontarIndices(baseCalculo),
            Dfc = dfc,
        };
    }

    private static BaseCalculoIndicadoresViewModel MontarBaseCalculo(Dictionary<string, Saldo> saldos)
    {
        decimal Somar(string prefixo, Func<Saldo, decimal> valor) =>
            saldos.Where(s => Pertence(s.Key, prefixo)).Sum(s => valor(s.Value));
        decimal SaldoFinal(string prefixo) => Somar(prefixo, s => s.SaldoFinal);
        decimal Resultado(string prefixo) => Somar(prefixo, s => s.MovimentoSemApuracao);

        var passivoCirculante = -SaldoFinal("2.1");
        var passivoNaoCirculante = -SaldoFinal("2.2");
        // Antes do encerramento o resultado ainda está nas contas 3 a 8, e faz parte do PL.
        var patrimonioLiquido = -SaldoFinal("2.3") - ContasResultado.Sum(SaldoFinal);
        var disponivel = SaldoFinal(Caixa);
        var dividaBruta = -(SaldoFinal("2.1.1.08") + SaldoFinal("2.2.2.02"));

        var receitaLiquida = -Resultado("3");
        var custos = Resultado("4");
        var resultadoFinanceiro = -Resultado("6");
        var impostoRenda = Resultado("8");
        var lucroLiquido = -ContasResultado.Sum(Resultado);
        // Equivalência patrimonial (7.3) não é resultado operacional da própria empresa.
        var ebit = -(Resultado("3") + Resultado("4") + Resultado("5") + Resultado("7") - Resultado("7.3"));
        var depreciacao = Somar("1.2.3.05", s => s.Creditos) + Somar("1.2.4.02", s => s.Creditos);

        return new BaseCalculoIndicadoresViewModel
        {
            AtivoTotal = SaldoFinal("1"),
            AtivoCirculante = SaldoFinal("1.1"),
            Disponivel = disponivel,
            Estoques = SaldoFinal("1.1.2.08"),
            RealizavelLongoPrazo = SaldoFinal("1.2.1"),
            PassivoCirculante = passivoCirculante,
            PassivoNaoCirculante = passivoNaoCirculante,
            PatrimonioLiquido = patrimonioLiquido,
            DividaBruta = dividaBruta,
            DividaLiquida = dividaBruta - disponivel,
            ReceitaLiquida = receitaLiquida,
            Custos = custos,
            LucroBruto = receitaLiquida - custos,
            ResultadoFinanceiro = resultadoFinanceiro,
            ImpostoRendaContribuicao = impostoRenda,
            LucroLiquido = lucroLiquido,
            Ebit = ebit,
            DepreciacaoAmortizacao = depreciacao,
            Ebitda = ebit + depreciacao,
        };
    }

    private static List<IndiceFinanceiroViewModel> MontarIndices(BaseCalculoIndicadoresViewModel b)
    {
        static decimal? Dividir(decimal numerador, decimal denominador) =>
            denominador == 0 ? null : Math.Round(numerador / denominador, 6);

        var passivoTotal = b.PassivoCirculante + b.PassivoNaoCirculante;

        IndiceFinanceiroViewModel Indice(
            string codigo, string categoria, string nome, string formula, string interpretacao,
            string formato, decimal? valor) => new()
            {
                Codigo = codigo, Categoria = categoria, Nome = nome, Formula = formula,
                Interpretacao = interpretacao, Formato = formato, Valor = valor,
            };

        return
        [
            Indice("liquidez-corrente", "Liquidez", "Liquidez corrente",
                "Ativo circulante ÷ Passivo circulante",
                "Acima de 1: ativos suficientes para cobrir dívidas de até 1 ano.",
                "indice", Dividir(b.AtivoCirculante, b.PassivoCirculante)),
            Indice("liquidez-seca", "Liquidez", "Liquidez seca",
                "(Ativo circulante − Estoque) ÷ Passivo circulante",
                "Visão mais conservadora; exclui estoques do cálculo.",
                "indice", Dividir(b.AtivoCirculante - b.Estoques, b.PassivoCirculante)),
            Indice("liquidez-imediata", "Liquidez", "Liquidez imediata",
                "Disponibilidades ÷ Passivo circulante",
                "Acima de 1: cobre obrigações imediatas só com caixa e equivalentes.",
                "indice", Dividir(b.Disponivel, b.PassivoCirculante)),
            Indice("liquidez-geral", "Liquidez", "Liquidez geral",
                "(Ativo circulante + Realizável a longo prazo) ÷ (Passivo circulante + Passivo não circulante)",
                "Visão de longo prazo (mais de 12 meses).",
                "indice", Dividir(b.AtivoCirculante + b.RealizavelLongoPrazo, passivoTotal)),

            Indice("endividamento-geral", "Endividamento", "Endividamento Geral (EG)",
                "Passivo total ÷ Ativo total",
                "A proporção dos ativos da empresa que foi financiada por terceiros.",
                "percentual", Dividir(passivoTotal, b.AtivoTotal)),
            Indice("composicao-endividamento", "Endividamento", "Composição do Endividamento (CE)",
                "Passivo circulante ÷ Passivo total",
                "O percentual das dívidas que vence no curto prazo.",
                "percentual", Dividir(b.PassivoCirculante, passivoTotal)),
            Indice("divida-liquida-ebitda", "Endividamento", "Dívida Líquida / EBITDA",
                "(Dívida bruta − Caixa) ÷ EBITDA",
                "Quantos anos de geração operacional de caixa levariam para quitar a dívida.",
                "vezes", Dividir(b.DividaLiquida, b.Ebitda)),
            Indice("divida-bruta-pl", "Endividamento", "Dívida Bruta / Patrimônio Líquido",
                "Dívida bruta ÷ Patrimônio líquido",
                "A proporção entre o capital de terceiros e o capital próprio.",
                "vezes", Dividir(b.DividaBruta, b.PatrimonioLiquido)),
            Indice("pct", "Endividamento", "Participação de Capitais de Terceiros (PCT)",
                "Passivo total ÷ (Passivo total + Patrimônio líquido)",
                "O grau de dependência da empresa em relação a recursos de credores.",
                "percentual", Dividir(passivoTotal, passivoTotal + b.PatrimonioLiquido)),

            Indice("roe", "Rentabilidade", "ROE (Return on Equity)",
                "Lucro líquido ÷ Patrimônio líquido",
                "Retorno sobre o Patrimônio Líquido (ganho dos acionistas).",
                "percentual", Dividir(b.LucroLiquido, b.PatrimonioLiquido)),
            Indice("roa", "Rentabilidade", "ROA (Return on Assets)",
                "Lucro líquido ÷ Ativo total",
                "Retorno sobre o Ativo Total (eficiência dos investimentos).",
                "percentual", Dividir(b.LucroLiquido, b.AtivoTotal)),
            Indice("margem-bruta", "Rentabilidade", "Margem Bruta",
                "Lucro bruto ÷ Receita líquida",
                "Percentual de lucro após o custo dos produtos/serviços.",
                "percentual", Dividir(b.LucroBruto, b.ReceitaLiquida)),
            Indice("margem-liquida", "Rentabilidade", "Margem Líquida",
                "Lucro líquido ÷ Receita líquida",
                "Lucro líquido gerado a cada R$ 1 de venda.",
                "percentual", Dividir(b.LucroLiquido, b.ReceitaLiquida)),
            Indice("ebitda", "Resultado", "EBITDA",
                "EBIT + Depreciação + Amortização",
                "Desempenho operacional puro (sem juros, impostos, depreciação e amortização).",
                "moeda", b.Ebitda),
        ];
    }

    private static (DfcViewModel Dfc, HashSet<string> Codigos) MontarDfc(Dictionary<string, Saldo> saldos)
    {
        var codigos = new HashSet<string>(StringComparer.Ordinal);
        var cobertura = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var linhasPorGrupo = OrdemGrupos.ToDictionary(g => g, _ => new List<DfcLinhaViewModel>());

        foreach (var linha in DeParaDfc)
        {
            var composicao = new List<DfcComposicaoViewModel>();
            foreach (var parcela in linha.Parcelas)
            {
                var valor = 0m;
                foreach (var (codConta, saldo) in saldos.Where(s => Pertence(s.Key, parcela.Conta)))
                {
                    var contribuicao = parcela.Fator * saldo.Valor(parcela.Medida);
                    valor += contribuicao;
                    cobertura[codConta] = cobertura.GetValueOrDefault(codConta) + contribuicao;
                }
                if (valor == 0) continue;
                codigos.Add(parcela.Conta);
                composicao.Add(new DfcComposicaoViewModel
                {
                    CodConta = parcela.Conta,
                    Medida = NomeDaMedida(parcela.Medida),
                    Valor = Math.Round(valor, 2),
                });
            }

            var total = Math.Round(composicao.Sum(c => c.Valor), 2);
            if (total == 0 && linha.OcultarQuandoZero) continue;
            linhasPorGrupo[linha.Grupo].Add(new DfcLinhaViewModel
            {
                Descricao = total >= 0 ? linha.SePositivo : linha.SeNegativo,
                Valor = total,
                Composicao = composicao,
            });
        }

        // Toda conta fora do caixa deveria estar coberta em -1 x variação. O que
        // faltar (conta nova, conta de outra coligada fora do de-para) aparece aqui
        // em vez de sumir — e a DFC continua fechando com a variação do caixa.
        var naoClassificadas = saldos
            .Where(s => !Pertence(s.Key, Caixa))
            .Select(s => (CodConta: s.Key, Valor: Math.Round(-s.Value.Variacao - cobertura.GetValueOrDefault(s.Key), 2)))
            .Where(s => s.Valor != 0)
            .OrderBy(s => s.CodConta, StringComparer.Ordinal)
            .ToList();
        if (naoClassificadas.Count > 0)
        {
            codigos.UnionWith(naoClassificadas.Select(s => s.CodConta));
            linhasPorGrupo[NaoClassificadas].Add(new DfcLinhaViewModel
            {
                Descricao = "Contas sem linha definida na DFC",
                Valor = naoClassificadas.Sum(s => s.Valor),
                NaoClassificada = true,
                Composicao = naoClassificadas.Select(s => new DfcComposicaoViewModel
                {
                    CodConta = s.CodConta,
                    Medida = NomeDaMedida(Medida.Variacao),
                    Valor = s.Valor,
                }).ToList(),
            });
        }

        var caixa = saldos.Where(s => Pertence(s.Key, Caixa)).Select(s => s.Value).ToList();
        var dfc = new DfcViewModel
        {
            Grupos = OrdemGrupos
                .Where(g => linhasPorGrupo[g].Count > 0)
                .Select(g => new DfcGrupoViewModel
                {
                    Nome = g,
                    Linhas = linhasPorGrupo[g],
                    Total = linhasPorGrupo[g].Sum(l => l.Valor),
                })
                .ToList(),
            SaldoInicialCaixa = Math.Round(caixa.Sum(s => s.SaldoInicial), 2),
            SaldoFinalCaixa = Math.Round(caixa.Sum(s => s.SaldoFinal), 2),
            VariacaoCaixa = Math.Round(caixa.Sum(s => s.Variacao), 2),
        };
        return (dfc, codigos);
    }

    private async Task<Dictionary<string, string>> ListarDescricoesAsync(
        IEnumerable<BaseTotvs> bases, IReadOnlyCollection<string> codigos, short codColigada,
        CancellationToken cancellationToken)
    {
        var descricoes = new Dictionary<string, string>(StringComparer.Ordinal);
        if (codigos.Count == 0) return descricoes;

        foreach (var baseDados in bases)
        {
            var linhas = await balanceteRepository.ListarDescricoesAsync(
                baseDados, codigos, codColigada, cancellationToken);
            // A descrição própria da coligada vence a do plano padrão (coligada 0).
            foreach (var linha in linhas.Where(l => !string.IsNullOrWhiteSpace(l.Descricao))
                         .OrderByDescending(l => l.CodColigada))
                descricoes.TryAdd(linha.CodConta, linha.Descricao!.Trim());
        }
        return descricoes;
    }
}
