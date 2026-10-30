using System.Text.Json;
using System.Text.RegularExpressions;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

namespace PainelWebSwissPark.Api.AgenteContabil;

/// <summary>
/// Confere o bloco ```grafico da resposta da Tati antes de ele chegar à tela: o formato, os limites
/// e, principalmente, se cada número do bloco — em qualquer campo, não só nos que a tela desenha
/// hoje — saiu de uma ferramenta chamada nesta resposta. Se algo não bate, o bloco sai do texto e
/// o motivo vai para o log; o texto segue sem gráfico. Classe pura, sem banco e sem modelo.
/// </summary>
public static partial class ValidadorGraficoTati
{
    /// <summary>O JSON que uma ferramenta devolveu nesta resposta.</summary>
    public sealed record ResultadoDeFerramenta(string Ferramenta, string Json);

    /// <summary>O texto que segue para a tela e, se o gráfico saiu, o motivo (sem os valores).</summary>
    public sealed record Resultado(string Texto, string? Motivo);

    public const int MaximoDeBarras = 15, MaximoDeCategorias = 12, MaximoDePontos = 12, MaximoDeSeries = 3;

    private static readonly HashSet<string> Unidades = new(StringComparer.Ordinal) { "R$", "%", "x", "indice", "contas" };

    // Números das ferramentas que identificam a consulta, e não são dado: não servem de origem
    // para um valor do gráfico (uma "liquidez 17" não pode passar por causa da coligada 17).
    private static readonly HashSet<string> Identificadores = new(StringComparer.Ordinal)
    {
        "coligada", "codColigada", "filial", "codFilial", "meses", "totalDeEmpresas", "quantidadeDeEmpresas",
        "totalLinhas", "nivel", "nivelMaximo",
    };

    // O mesmo bloco que o front lê, inclusive cortado no fim da resposta.
    [GeneratedRegex(@"```grafico\s*([\s\S]*?)(?:```|$)")]
    private static partial Regex BlocoGrafico();

    public static Resultado Validar(string texto, IReadOnlyList<ResultadoDeFerramenta> resultados)
    {
        // Remover um bloco cortado pode deixar à mostra outro ```grafico que vinha colado nele: confere
        // de novo até não sobrar bloco sem conferência. Cada volta tira um bloco, então o laço acaba.
        var motivos = new List<string>();
        while (BlocoGrafico().Matches(texto) is { Count: > 0 } blocos)
        {
            var motivo = blocos.Count > 1
                ? "mais de um gráfico na resposta"
                : Conferir(blocos[0].Groups[1].Value, resultados);
            if (motivo is null) break;

            motivos.Add(motivo);
            texto = Remover(texto, blocos);
        }
        return new Resultado(texto, motivos.Count == 0 ? null : string.Join("; ", motivos));
    }

    private static string? Conferir(string bruto, IReadOnlyList<ResultadoDeFerramenta> resultados)
    {
        JsonElement grafico;
        try
        {
            using var documento = JsonDocument.Parse(bruto);
            grafico = documento.RootElement.Clone();
        }
        catch (JsonException)
        {
            return "o bloco não é um JSON válido";
        }
        if (grafico.ValueKind != JsonValueKind.Object) return "o bloco não é um objeto";

        // Os dois formatos no mesmo bloco não: a tela poderia desenhar justamente o que não foi lido aqui.
        if (grafico.TryGetProperty("itens", out _) && (grafico.TryGetProperty("eixoX", out _) || grafico.TryGetProperty("series", out _)))
        {
            return "o bloco mistura itens com eixoX/series";
        }

        var tipo = Texto(grafico, "tipo");
        string? motivo = tipo switch
        {
            // Sem tipo: o formato antigo, barras com itens.
            null => LerItens(grafico),
            "barras" => grafico.TryGetProperty("itens", out _) ? LerItens(grafico) : LerSeries(grafico, MaximoDeBarras, 1, aceitaDemais: true),
            "colunas" => LerSeries(grafico, MaximoDeCategorias, MaximoDeSeries, aceitaDemais: false),
            "linha" => LerSeries(grafico, MaximoDePontos, MaximoDeSeries, aceitaDemais: false),
            _ => $"tipo \"{tipo}\" desconhecido",
        };
        if (motivo is not null) return motivo;

        // No formato novo, a unidade e a fonte são obrigatórias e a fonte tem de ter sido chamada.
        var chamadas = resultados.Select(r => r.Ferramenta).ToHashSet(StringComparer.Ordinal);
        if (tipo is not null && (Texto(grafico, "unidade") is not { } unidade || !Unidades.Contains(unidade)))
        {
            return "unidade ausente ou fora de R$, %, x, indice e contas";
        }
        // A fonte vira a linha "Fonte:" da tela: é obrigatória no formato novo e, quando vem no
        // antigo, também tem de apontar para uma ferramenta chamada nesta resposta.
        var temFonte = grafico.TryGetProperty("fonte", out var fonte);
        if (tipo is not null || temFonte)
        {
            if (!temFonte || fonte.ValueKind != JsonValueKind.Object || Texto(fonte, "ferramenta") is not { } ferramenta)
            {
                return "gráfico sem fonte.ferramenta";
            }
            if (!chamadas.Contains(ferramenta))
            {
                return $"a fonte {ferramenta} não foi chamada nesta resposta";
            }
        }
        if (chamadas.Count == 0) return "nenhuma ferramenta foi chamada nesta resposta";

        // Cada número do bloco, em qualquer campo, precisa estar nos resultados das ferramentas desta
        // resposta, em 2 casas. O sinal trocado só vale para o balancete, que mostra passivo, PL e
        // receitas negativos.
        var conhecidos = new HashSet<decimal>();
        var doBalancete = new HashSet<decimal>();
        foreach (var resultado in resultados)
        {
            var noBalancete = resultado.Ferramenta == ConsultarBalancete.NomeFerramenta;
            foreach (var numero in Numeros(resultado.Json, ignorarIdentificadores: true))
            {
                conhecidos.Add(numero);
                if (noBalancete) doBalancete.Add(numero);
            }
        }
        var semOrigem = Numeros(grafico, ignorarIdentificadores: false).Count(n => !conhecidos.Contains(n) && !doBalancete.Contains(-n));
        return semOrigem == 0 ? null : $"{semOrigem} número(s) do gráfico sem origem nas ferramentas desta resposta";
    }

    /// <summary>Formato antigo: itens [{label, valor}], até 15 mais um "Demais".</summary>
    private static string? LerItens(JsonElement grafico)
    {
        if (!grafico.TryGetProperty("itens", out var itens) || itens.ValueKind != JsonValueKind.Array || itens.GetArrayLength() == 0)
        {
            return "barras sem itens";
        }

        var lista = itens.EnumerateArray().ToList();
        var temDemais = lista[^1].ValueKind == JsonValueKind.Object && EhDemais(Texto(lista[^1], "label"));
        if (lista.Count > MaximoDeBarras + (temDemais ? 1 : 0))
        {
            return $"mais de {MaximoDeBarras} barras";
        }
        foreach (var item in lista)
        {
            if (item.ValueKind != JsonValueKind.Object || Texto(item, "label") is null
                || !item.TryGetProperty("valor", out var valor) || valor.ValueKind != JsonValueKind.Number)
            {
                return "item de barra sem label ou sem valor numérico";
            }
        }
        return null;
    }

    /// <summary>Formato novo: eixoX com as categorias e series [{nome, valores}] do mesmo tamanho.</summary>
    private static string? LerSeries(JsonElement grafico, int maximoDeCategorias, int maximoDeSeries, bool aceitaDemais)
    {
        if (!grafico.TryGetProperty("eixoX", out var eixoX) || eixoX.ValueKind != JsonValueKind.Array
            || eixoX.GetArrayLength() == 0 || eixoX.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String))
        {
            return "eixoX ausente ou com item que não é texto";
        }
        // Nas barras, como no formato antigo, cabe mais um "Demais" depois das 15.
        var temDemais = aceitaDemais && EhDemais(eixoX[eixoX.GetArrayLength() - 1].GetString());
        if (eixoX.GetArrayLength() > maximoDeCategorias + (temDemais ? 1 : 0))
        {
            return $"mais de {maximoDeCategorias} categorias no eixoX";
        }

        if (!grafico.TryGetProperty("series", out var series) || series.ValueKind != JsonValueKind.Array || series.GetArrayLength() == 0)
        {
            return "gráfico sem séries";
        }
        if (series.GetArrayLength() > maximoDeSeries) return $"mais de {maximoDeSeries} séries";

        foreach (var serie in series.EnumerateArray())
        {
            if (serie.ValueKind != JsonValueKind.Object || Texto(serie, "nome") is null
                || !serie.TryGetProperty("valores", out var valores) || valores.ValueKind != JsonValueKind.Array)
            {
                return "série sem nome ou sem valores";
            }
            if (valores.GetArrayLength() != eixoX.GetArrayLength()) return "série com quantidade de valores diferente do eixoX";

            // null é buraco no gráfico (mês sem dados), não zero.
            if (valores.EnumerateArray().Any(v => v.ValueKind is not (JsonValueKind.Number or JsonValueKind.Null)))
            {
                return "valor que não é número nem null";
            }
        }
        return null;
    }

    private static bool EhDemais(string? rotulo) => rotulo is not null && rotulo.StartsWith("Demais", StringComparison.OrdinalIgnoreCase);

    /// <summary>Os números de um JSON de ferramenta, em qualquer nível, em 2 casas.</summary>
    private static List<decimal> Numeros(string json, bool ignorarIdentificadores)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);
            return Numeros(documento.RootElement, ignorarIdentificadores);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// Os números de um elemento, em qualquer nível, em 2 casas. Com
    /// <paramref name="ignorarIdentificadores"/>, ficam de fora os números de campos como coligada e
    /// filial, que identificam a consulta.
    /// </summary>
    private static List<decimal> Numeros(JsonElement raiz, bool ignorarIdentificadores)
    {
        var numeros = new List<decimal>();
        var pendentes = new Stack<JsonElement>([raiz]);
        while (pendentes.TryPop(out var elemento))
        {
            switch (elemento.ValueKind)
            {
                case JsonValueKind.Number:
                    // Fora do alcance do decimal, não tem como bater com nada: vale como número sem origem.
                    numeros.Add(elemento.TryGetDecimal(out var numero) ? Arredondar(numero) : decimal.MaxValue);
                    break;
                case JsonValueKind.Object:
                    foreach (var propriedade in elemento.EnumerateObject())
                    {
                        if (ignorarIdentificadores && propriedade.Value.ValueKind == JsonValueKind.Number
                            && Identificadores.Contains(propriedade.Name))
                        {
                            continue;
                        }
                        pendentes.Push(propriedade.Value);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in elemento.EnumerateArray()) pendentes.Push(item);
                    break;
            }
        }
        return numeros;
    }

    private static readonly string[] BlocosQueFicam = ["opcoes", "grafico"];

    private static string Remover(string texto, MatchCollection blocos)
    {
        for (var i = blocos.Count - 1; i >= 0; i--)
        {
            // Bloco sem a cerca de fechamento (resposta cortada) termina nas crases que abrem o
            // bloco seguinte: as do ```opcoes (que o GarantirOpcoesAsync acrescenta) ficam, e as de
            // outro ```grafico também, para ele ser conferido na volta seguinte do Validar.
            var fim = blocos[i].Index + blocos[i].Length;
            var abreOutroBloco = blocos[i].Value.EndsWith("```", StringComparison.Ordinal)
                && BlocosQueFicam.Any(bloco => string.CompareOrdinal(texto, fim, bloco, 0, bloco.Length) == 0);
            texto = texto.Remove(blocos[i].Index, blocos[i].Length - (abreOutroBloco ? 3 : 0));
        }
        // Sem o bloco, não sobra um buraco de linhas em branco no meio do texto.
        return LinhasEmBrancoSeguidas().Replace(texto, "\n\n").Trim();
    }

    [GeneratedRegex(@"(?:\r?\n){3,}")]
    private static partial Regex LinhasEmBrancoSeguidas();

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static string? Texto(JsonElement elemento, string nome) =>
        elemento.TryGetProperty(nome, out var valor) && valor.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(valor.GetString())
                ? valor.GetString()!.Trim()
                : null;
}
