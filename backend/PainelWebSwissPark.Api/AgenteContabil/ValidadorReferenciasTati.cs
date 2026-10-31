using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.Services.BusinessLogic;
using static PainelWebSwissPark.Api.AgenteContabil.ValidadorGraficoTati;

namespace PainelWebSwissPark.Api.AgenteContabil;

/// <summary>
/// Confere os links dos números da Tati para os cards das telas — "[R$ 1,2 mi](card:resultado/receitaLiquida)"
/// — antes de a resposta chegar à tela: o card tem de existir e cada número do link tem de estar nesse card,
/// nas ferramentas desta resposta (o valor, o anterior, a variação ou um texto que a tela mostra), com a
/// tolerância da última casa que o link mostra ("R$ 1,2 mi" vale de 1,15 a 1,25 mi). O card é o da tela: um
/// indicador, um pilar ou um número da conciliação é um card cada; a cascata do resultado e a DFC são um
/// card só cada uma, e o balanço tem os dois lados. No resumo do mês (<c>estrito</c>), a linha com um link
/// que não confere sai inteira, e também a linha com um valor sem link (R$, %, x, mil, mi ou decimal) que
/// não está em nenhuma ferramenta da resposta: o resumo só cita número que existe nas telas. Fora dele, o
/// link que não confere vira texto. O motivo vai para o log, sem os valores. Classe pura, sem banco e sem modelo.
/// </summary>
public static partial class ValidadorReferenciasTati
{
    /// <summary>O texto que segue para a tela e, se alguma linha saiu ou algum link foi desfeito, o motivo.</summary>
    public sealed record Resultado(string Texto, string? Motivo);

    /// <summary>Um número de um texto e o valor da última casa que ele mostra ("1,2 mi": 0,1 mi).</summary>
    private readonly record struct Numero(decimal Valor, decimal Casa);

    /// <summary>Um card da tela, pelo alvo do link, e os números dele nas ferramentas da resposta.</summary>
    private sealed record Card(string Alvo, List<Numero> Numeros);

    [GeneratedRegex(@"\[([^\[\]\r\n]{1,120})\]\(card:([A-Za-z0-9_/-]{1,80})\)")]
    private static partial Regex Link();

    // O modelo nem sempre escreve o alvo como pedido: sem o "card:", com ":" no lugar da barra ou com
    // espaço antes do parêntese. Tudo vira a forma que a tela lê, [número](card:tipo/chave).
    [GeneratedRegex(
        @"\]\s*\(\s*(?:card\s*:\s*)?(indicador|pilar|resultado|dfc|balanco|conciliacao|veredito)(?:\s*[/:]\s*([A-Za-z0-9_-]{1,60}))?\s*\)",
        RegexOptions.IgnoreCase)]
    private static partial Regex AlvoForaDoPadrao();

    // Datas e anos não são valores: 2026-09, 30/09/2026, 09/2026, set/2026 e o ano solto.
    [GeneratedRegex(@"\b\d{4}-\d{2}(?:-\d{2})?\b|\b\d{1,2}/\d{1,2}/\d{2,4}\b|\b\d{1,2}/\d{4}\b|\b\p{L}{3,9}/\d{2,4}\b|(?<!R\$\s?)\b(?:19|20)\d{2}\b(?![.,]\d)")]
    private static partial Regex Data();

    // Um número em português: sinal, R$, milhar com ponto, decimal com vírgula, a escala (mil, mi, bi)
    // e a unidade (%, x, vezes, p.p.).
    [GeneratedRegex(
        @"(?<![\p{L}\d,.])(?<sinal>[-−+])?\s?(?<moeda>R\$\s?)?(?<sinal2>[-−+])?(?<numero>\d{1,3}(?:\.\d{3})+(?:,\d+)?|\d+(?:,\d+)?)" +
        @"(?:\s?(?<escala>milh(?:ão|ões|ao|oes)|mil|mi|bilh(?:ão|ões|ao|oes)|bi)(?!\p{L}))?" +
        @"(?:\s?(?<unidade>%|x(?!\p{L})|vezes|p\.\s?p\.?))?",
        RegexOptions.IgnoreCase)]
    private static partial Regex NumeroEmTexto();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s")]
    private static partial Regex Titulo();

    [GeneratedRegex(@"(?:\r?\n){3,}")]
    private static partial Regex LinhasEmBrancoSeguidas();

    // Os campos que identificam a consulta não são dado, como no gráfico; as contagens de empresas da
    // conciliação, no texto, são ("72 empresas").
    private static readonly HashSet<string> IdentificadoresDaConsulta =
        Identificadores.Except(["totalDeEmpresas", "quantidadeDeEmpresas"]).ToHashSet(StringComparer.Ordinal);

    // Campos de texto das ferramentas que explicam a convenção e não são dado ("12.5 = 12,5%").
    private static readonly HashSet<string> TextosQueNaoSaoDado = new(StringComparer.Ordinal)
    {
        "convencaoDeUnidades", "convencaoDeSinais", "formula", "metodo", "erro",
    };

    // Os nomes da ferramenta (sem acento e espaço) para os códigos dos pilares.
    private static readonly Dictionary<string, string> CodigoDoPilar = new(StringComparer.Ordinal)
    {
        ["RESULTADO"] = "resultado", ["CAIXA"] = "caixa", ["DIVIDA"] = "divida", ["CONTASDECURTOPRAZO"] = "curtoPrazo",
    };

    private static readonly HashSet<string> ChavesDoAtivo = new(StringComparer.Ordinal)
    {
        "ativo", "ativoCirculante", "ativoNaoCirculante",
    };

    private static readonly HashSet<string> ChavesDoPassivo = new(StringComparer.Ordinal)
    {
        "passivo", "passivoExigivel", "patrimonioLiquido", "passivoMaisPl", "passivoCirculante", "passivoNaoCirculante",
    };

    private static readonly string[] CamposDaConciliacao = ["backlogTotal", "preparados", "conferidos", "pendentes", "fechados"];

    public static Resultado Validar(string texto, IReadOnlyList<ResultadoDeFerramenta> resultados, bool estrito)
    {
        texto = AlvoForaDoPadrao().Replace(texto, alvo => alvo.Groups[2].Success
            ? $"](card:{alvo.Groups[1].Value.ToLowerInvariant()}/{alvo.Groups[2].Value})"
            : $"](card:{alvo.Groups[1].Value.ToLowerInvariant()})");
        if (!estrito && !texto.Contains("](card:", StringComparison.Ordinal)) return new(texto, null);

        var cards = Cards(resultados);
        var motivos = new List<string>();
        var saida = new List<string>();
        List<Numero>? todos = null;
        var dentroDeBloco = false;
        var removeuLinha = false;

        foreach (var linha in texto.Split('\n'))
        {
            // Os blocos ```opcoes e ```grafico passam como estão: o gráfico tem o validador dele.
            if (linha.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                dentroDeBloco = !dentroDeBloco;
                saida.Add(linha);
                continue;
            }
            if (dentroDeBloco)
            {
                saida.Add(linha);
                continue;
            }

            var falhas = new List<string>();
            var linhaConferida = Link().Replace(linha, link =>
            {
                var (rotulo, alvo) = (link.Groups[1].Value, link.Groups[2].Value);
                // O modelo às vezes põe o próprio alvo no texto do link ("[card:pilar/caixa](card:pilar/caixa)"),
                // que a tela mostraria: vira "ver".
                var semTexto = rotulo.TrimStart().StartsWith("card:", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(rotulo.Trim(), alvo, StringComparison.OrdinalIgnoreCase);
                var motivo = Conferir(semTexto ? "" : rotulo, alvo, cards);
                if (motivo is null) return semTexto ? $"[ver](card:{alvo})" : link.Value;
                falhas.Add(motivo);
                return semTexto ? "" : rotulo;
            });

            if (estrito)
            {
                if (falhas.Count == 0)
                {
                    todos ??= NumerosDasFerramentas(resultados);
                    var soltos = NumerosDoTexto(Link().Replace(linha, " "), soValores: true);
                    if (soltos.Any(numero => !todos.Any(conhecido => Bate(numero, conhecido))))
                    {
                        falhas.Add("valor sem link e sem origem nas ferramentas");
                    }
                }
                if (falhas.Count > 0)
                {
                    motivos.AddRange(falhas.Select(falha => $"linha removida ({falha})"));
                    removeuLinha = true;
                    continue;
                }
            }
            else
            {
                motivos.AddRange(falhas.Select(falha => $"link desfeito ({falha})"));
            }
            saida.Add(linhaConferida);
        }

        // Sem linha removida, as linhas voltam como vieram, com os links conferidos.
        var conferido = removeuLinha
            ? LinhasEmBrancoSeguidas().Replace(string.Join('\n', SemTitulosVazios(saida)), "\n\n").Trim()
            : string.Join('\n', saida);
        return new(conferido, motivos.Count == 0 ? null : string.Join("; ", motivos));
    }

    private static string? Conferir(string rotulo, string alvo, List<Card> cards)
    {
        var chave = CardDoAlvo(alvo);
        var card = chave is null ? null : cards.Find(c => string.Equals(c.Alvo, chave, StringComparison.OrdinalIgnoreCase));
        if (card is null) return $"{alvo}: card desconhecido ou fora das ferramentas desta resposta";

        // Link sem número (o status, por exemplo) só aponta o card.
        return NumerosDoTexto(rotulo, soValores: false).All(numero => card.Numeros.Any(conhecido => Bate(numero, conhecido)))
            ? null
            : $"{alvo}: número sem origem no card";
    }

    /// <summary>
    /// O card da tela para onde o alvo leva — a mesma regra de frontend/src/componentes/tati/cardsDaTati.ts
    /// e a lista de alvos da seção "Resumo do mês" das instruções. Nulo para alvo desconhecido.
    /// </summary>
    private static string? CardDoAlvo(string alvo)
    {
        var barra = alvo.IndexOf('/');
        var tipo = barra < 0 ? alvo : alvo[..barra];
        var chave = barra < 0 ? "" : alvo[(barra + 1)..];
        if (chave.Contains('/')) return null;
        return tipo switch
        {
            "indicador" when chave.Length > 0 => $"indicador/{chave.ToUpperInvariant()}",
            "pilar" when CodigoDoPilar.ContainsValue(chave) => $"pilar/{chave}",
            "veredito" when chave.Length == 0 => "veredito",
            // A cascata e a DFC são um card cada: o campo não muda o destino.
            "resultado" => "resultado",
            "dfc" => "dfc",
            "balanco" when ChavesDoAtivo.Contains(chave) => "balanco/ativo",
            "balanco" when ChavesDoPassivo.Contains(chave) => "balanco/passivo",
            "conciliacao" when chave == "empresas" || CamposDaConciliacao.Contains(chave) => $"conciliacao/{chave}",
            _ => null,
        };
    }

    /// <summary>Os cards que as ferramentas desta resposta trouxeram, com os números de cada um.</summary>
    private static List<Card> Cards(IReadOnlyList<ResultadoDeFerramenta> resultados)
    {
        var cards = new List<Card>();
        void Somar(string alvo, JsonElement parte)
        {
            var card = cards.Find(c => c.Alvo == alvo);
            if (card is null) cards.Add(card = new(alvo, []));
            Coletar(parte, card.Numeros);
        }

        foreach (var resultado in resultados)
        {
            using var documento = Ler(resultado.Json);
            if (documento is null) continue;
            var raiz = documento.RootElement;
            switch (resultado.Ferramenta)
            {
                case ConsultarIndicadores.NomeFerramenta:
                    foreach (var indicador in Itens(raiz, "indicadores"))
                    {
                        if (Texto(indicador, "codigo") is { } codigo) Somar($"indicador/{codigo.ToUpperInvariant()}", indicador);
                    }
                    // Na tela, o card da saúde geral traz os quatro pilares dentro dele.
                    foreach (var pilar in Itens(raiz, "pilares"))
                    {
                        if (CodigoDoPilar.GetValueOrDefault(SoLetras(Texto(pilar, "pilar"))) is { } codigo) Somar($"pilar/{codigo}", pilar);
                        Somar("veredito", pilar);
                    }
                    foreach (var parte in Campos(raiz, "veredito", "statusGeral")) Somar("veredito", parte);
                    foreach (var parte in Campos(raiz, "resultado")) Somar("resultado", parte);
                    foreach (var parte in Campos(raiz, "caixaFinal")) Somar("dfc", parte);
                    break;
                case ConsultarDfc.NomeFerramenta:
                    Somar("dfc", raiz);
                    break;
                case ConsultarBalanco.NomeFerramenta:
                {
                    var totais = Campo(raiz, "totais");
                    foreach (var parte in Campos(totais, "ativo")) Somar("balanco/ativo", parte);
                    foreach (var parte in Campos(totais, "passivoExigivel", "patrimonioLiquido", "passivoMaisPl")) Somar("balanco/passivo", parte);
                    foreach (var bloco in Itens(raiz, "blocos"))
                    {
                        Somar(Texto(bloco, "lado") == "ativo" ? "balanco/ativo" : "balanco/passivo", bloco);
                    }
                    break;
                }
                case ConsultarBacklogStatus.NomeFerramenta:
                    foreach (var campo in CamposDaConciliacao)
                    {
                        foreach (var parte in Campos(raiz, campo)) Somar($"conciliacao/{campo}", parte);
                    }
                    break;
                case ConsultarBacklogPorEmpresa.NomeFerramenta:
                    Somar("conciliacao/empresas", raiz);
                    break;
            }
        }
        return cards;
    }

    /// <summary>
    /// Bate se a diferença cabe em meia casa do número menos preciso. Vale o valor absoluto: o sinal
    /// o texto diz com palavras ("consumiu", "caiu", "prejuízo").
    /// </summary>
    private static bool Bate(Numero doTexto, Numero conhecido) =>
        Math.Abs(Math.Abs(doTexto.Valor) - Math.Abs(conhecido.Valor)) <= Math.Max(doTexto.Casa, conhecido.Casa) / 2;

    /// <summary>
    /// Os números de um texto em português. Com <paramref name="soValores"/>, só os que são valor (com
    /// R$, escala, unidade ou casas decimais); "12 meses" e "3 pilares" ficam de fora.
    /// </summary>
    private static List<Numero> NumerosDoTexto(string texto, bool soValores)
    {
        var numeros = new List<Numero>();
        foreach (Match numero in NumeroEmTexto().Matches(Data().Replace(texto, " ")))
        {
            var bruto = numero.Groups["numero"].Value;
            var virgula = bruto.IndexOf(',');
            var temEscala = numero.Groups["escala"].Success;
            if (soValores && virgula < 0 && !temEscala && !numero.Groups["moeda"].Success && !numero.Groups["unidade"].Success)
            {
                continue;
            }

            var escala = temEscala ? Escala(numero.Groups["escala"].Value) : 1m;
            var casa = escala;
            for (var i = virgula < 0 ? 0 : bruto.Length - virgula - 1; i > 0; i--) casa /= 10;

            // Fora do alcance do decimal, não bate com nada: vale como número sem origem.
            if (!decimal.TryParse(bruto.Replace(".", "").Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor)
                || valor > 1_000_000_000_000m)
            {
                numeros.Add(new(decimal.MaxValue, 0));
                continue;
            }
            var negativo = numero.Groups["sinal"].Value is "-" or "−" || numero.Groups["sinal2"].Value is "-" or "−";
            numeros.Add(new((negativo ? -valor : valor) * escala, casa));
        }
        return numeros;
    }

    private static decimal Escala(string escala) => escala.ToLowerInvariant() switch
    {
        "mil" => 1_000m,
        "mi" or "milhão" or "milhões" or "milhao" or "milhoes" => 1_000_000m,
        _ => 1_000_000_000m,
    };

    private static List<Numero> NumerosDasFerramentas(IReadOnlyList<ResultadoDeFerramenta> resultados)
    {
        var numeros = new List<Numero>();
        foreach (var resultado in resultados)
        {
            using var documento = Ler(resultado.Json);
            if (documento is not null) Coletar(documento.RootElement, numeros);
        }
        return numeros;
    }

    /// <summary>
    /// Os números de um trecho de JSON, em qualquer nível: os valores (exatos) e os que aparecem nos
    /// textos que a tela mostra ("84,1 mil", "Subiu R$ 120,5 mil no mês"). Ficam de fora os campos que
    /// identificam a consulta (coligada, filial...) e os textos de convenção.
    /// </summary>
    private static void Coletar(JsonElement raiz, List<Numero> numeros)
    {
        var pendentes = new Stack<(string? Nome, JsonElement Elemento)>([(null, raiz)]);
        while (pendentes.TryPop(out var item))
        {
            var (nome, elemento) = item;
            switch (elemento.ValueKind)
            {
                case JsonValueKind.Number when nome is null || !IdentificadoresDaConsulta.Contains(nome):
                    numeros.Add(elemento.TryGetDecimal(out var valor) ? new(valor, 0) : new(decimal.MaxValue, 0));
                    break;
                case JsonValueKind.String when nome is null || !TextosQueNaoSaoDado.Contains(nome):
                    numeros.AddRange(NumerosDoTexto(elemento.GetString()!, soValores: false));
                    break;
                case JsonValueKind.Object:
                    foreach (var propriedade in elemento.EnumerateObject()) pendentes.Push((propriedade.Name, propriedade.Value));
                    break;
                case JsonValueKind.Array:
                    foreach (var filho in elemento.EnumerateArray()) pendentes.Push((nome, filho));
                    break;
            }
        }
    }

    /// <summary>Sem as linhas que saíram, um título que ficou sem nada embaixo sai também.</summary>
    private static List<string> SemTitulosVazios(List<string> linhas)
    {
        var resultado = new List<string>(linhas.Count);
        for (var i = 0; i < linhas.Count; i++)
        {
            if (Titulo().IsMatch(linhas[i]))
            {
                var seguinte = linhas.Skip(i + 1).FirstOrDefault(linha => !string.IsNullOrWhiteSpace(linha));
                if (seguinte is null || Titulo().IsMatch(seguinte) || seguinte.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    continue;
                }
            }
            resultado.Add(linhas[i]);
        }
        return resultado;
    }

    private static JsonDocument? Ler(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<JsonElement> Itens(JsonElement objeto, string nome) =>
        objeto.ValueKind == JsonValueKind.Object && objeto.TryGetProperty(nome, out var lista) && lista.ValueKind == JsonValueKind.Array
            ? lista.EnumerateArray()
            : [];

    private static JsonElement Campo(JsonElement objeto, string nome) =>
        objeto.ValueKind == JsonValueKind.Object && objeto.TryGetProperty(nome, out var valor) ? valor : default;

    private static IEnumerable<JsonElement> Campos(JsonElement objeto, params string[] nomes) =>
        nomes.Select(nome => Campo(objeto, nome)).Where(valor => valor.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null));

    private static string? Texto(JsonElement objeto, string nome) =>
        Campo(objeto, nome) is { ValueKind: JsonValueKind.String } valor ? valor.GetString() : null;

    private static string SoLetras(string? texto) =>
        string.Concat(EmpresasConsolidadas.Normalizar(texto).Where(char.IsLetterOrDigit));
}
