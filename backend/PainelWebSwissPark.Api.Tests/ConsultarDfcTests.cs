using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Tests;

/// <summary>
/// A ferramenta consultar_dfc com a DFC falsa da AEPL Participações (coligada 17) no 1º trimestre de
/// 2026: a base de março do caso combinado com a contabilidade e linhas de teste em cada atividade.
/// </summary>
public class ConsultarDfcTests
{
    private static readonly DateTime Janeiro = new(2026, 1, 1);

    private static IndicadoresViewModel Resultado() => new()
    {
        BaseCalculo = RegrasIndicadoresTests.BaseAepl(),
        Dfc = new DfcViewModel
        {
            Grupos =
            [
                new()
                {
                    Nome = "Atividades Operacionais",
                    Total = -100,
                    Linhas =
                    [
                        new() { Descricao = "Lucro do Exercício", Valor = -50 },
                        new() { Descricao = "(+) Provisões", Valor = 0 },
                        new() { Descricao = "Aumento de Duplicatas a Receber", Valor = -50 },
                    ],
                },
                new()
                {
                    Nome = "Variações não classificadas",
                    Total = 120,
                    Linhas =
                    [
                        new()
                        {
                            Descricao = "Contas sem linha definida na DFC",
                            Valor = 120,
                            NaoClassificada = true,
                            Composicao = Enumerable.Range(1, 12)
                                .Select(i => new DfcComposicaoViewModel { CodConta = $"1.1.2.05.01.{i:0000}", Descricao = $"Conta {i}", Valor = i == 12 ? 54 : 6 })
                                .ToList(),
                        },
                    ],
                },
            ],
            SaldoInicialCaixa = 1_000,
            SaldoFinalCaixa = 1_020,
            VariacaoCaixa = 20,
        },
    };

    private static (ConsultarDfc Ferramenta, IndicadoresFalso Servico) Criar(Dictionary<DateTime, IndicadoresViewModel> porInicio)
    {
        var servico = new IndicadoresFalso(porInicio);
        return (new ConsultarDfc(EmpresasFalsas.DoPainel(), servico, NullLogger<ConsultarDfc>.Instance), servico);
    }

    [Fact]
    public async Task Periodo_valido_traz_as_atividades_e_o_caixa()
    {
        var (ferramenta, servico) = Criar(new() { [Janeiro] = Resultado() });

        var saida = await Ferramenta.Executar(
            ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMesInicial":"2026-01","anoMes":"2026-03"}""");

        Assert.Equal(17, saida.GetProperty("coligada").GetInt32());
        Assert.Equal("2026-01-01", saida.GetProperty("periodo").GetProperty("inicio").GetString());
        Assert.Equal("2026-03-31", saida.GetProperty("periodo").GetProperty("fim").GetString());
        Assert.Equal(3, saida.GetProperty("periodo").GetProperty("meses").GetInt32());

        var operacionais = saida.GetProperty("atividades")[0];
        Assert.Equal("Atividades Operacionais", operacionais.GetProperty("atividade").GetString());
        Assert.Equal(-100m, operacionais.GetProperty("total").GetDecimal());
        // A linha zerada (Provisões) fica de fora.
        Assert.Equal(
            new[] { "Lucro do Exercício", "Aumento de Duplicatas a Receber" },
            operacionais.GetProperty("linhas").EnumerateArray().Select(l => l.GetProperty("linha").GetString()));

        Assert.Equal(1_000m, saida.GetProperty("caixaInicial").GetDecimal());
        Assert.Equal(20m, saida.GetProperty("variacaoCaixa").GetDecimal());
        Assert.Equal(1_020m, saida.GetProperty("caixaFinal").GetDecimal());

        // O período inteiro vai ao service, com o fim exclusivo, como o controller da tela.
        Assert.Equal((Janeiro, new DateTime(2026, 4, 1), (short)17, (short?)null), Assert.Single(servico.Chamadas));
    }

    [Fact]
    public async Task Contas_nao_classificadas_vem_com_as_maiores_primeiro()
    {
        var (ferramenta, _) = Criar(new() { [Janeiro] = Resultado() });

        var saida = await Ferramenta.Executar(
            ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMesInicial":"2026-01","anoMes":"2026-03"}""");

        var naoClassificadas = saida.GetProperty("contasNaoClassificadas");
        Assert.Equal(12, naoClassificadas.GetProperty("quantidade").GetInt32());
        Assert.Equal(10, naoClassificadas.GetProperty("maiores").GetArrayLength());
        Assert.Equal("1.1.2.05.01.0012", naoClassificadas.GetProperty("maiores")[0].GetProperty("conta").GetString());
    }

    [Fact]
    public async Task Sem_lancamentos_volta_semDados()
    {
        var (ferramenta, _) = Criar(new());

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, """{"empresa":"aepl participacoes","anoMes":"2015-12"}""");

        Assert.True(saida.GetProperty("semDados").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(saida.GetProperty("motivo").GetString()));
        Assert.False(saida.TryGetProperty("caixaFinal", out _));
    }

    [Theory]
    [InlineData("""{"anoMes":"2026-03"}""", "Empresa não informada")]
    [InlineData("""{"empresa":"aepl participacoes","anoMes":"2026-3"}""", "Período inválido")]
    [InlineData("""{"empresa":"aepl participacoes","anoMesInicial":"2026-04","anoMes":"2026-03"}""", "mês inicial")]
    [InlineData("""{"empresa":"aepl participacoes","anoMes":"2026-03","codColigada":"dezessete"}""", "Parâmetro inválido: codColigada")]
    public async Task Parametro_faltando_ou_invalido_volta_erro_sem_chamar_o_service(string entrada, string trecho)
    {
        var (ferramenta, servico) = Criar(new() { [Janeiro] = Resultado() });

        var saida = await Ferramenta.Executar(ferramenta.ExecutarAsync, entrada);

        Assert.Contains(trecho, Ferramenta.Erro(saida));
        Assert.Empty(servico.Chamadas);
    }
}
