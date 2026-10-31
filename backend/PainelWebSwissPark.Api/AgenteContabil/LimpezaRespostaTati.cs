using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace PainelWebSwissPark.Api.AgenteContabil;

/// <summary>
/// Tira do texto do modelo as sobras da marcação de chamada de ferramenta (um "&lt;/parameter&gt;"
/// solto depois das opções, por exemplo), que a tela mostraria como texto. O resto fica como veio.
/// </summary>
public static partial class LimpezaRespostaTati
{
    [return: NotNullIfNotNull(nameof(texto))]
    public static string? Limpar(string? texto)
    {
        if (string.IsNullOrEmpty(texto) || !MarcacaoDeFerramenta().IsMatch(texto)) return texto;
        return MarcacaoDeFerramenta().Replace(texto, string.Empty).Trim();
    }

    /// <summary>
    /// Se o modelo escreveu uma chamada de ferramenta inteira no texto (a abertura do invoke ou do
    /// function_calls), e não só uma tag solta: da limpeza sobrariam os valores dos parâmetros.
    /// </summary>
    public static bool TemChamadaNoTexto(string? texto) => !string.IsNullOrEmpty(texto) && AberturaDeChamada().IsMatch(texto);

    [GeneratedRegex(
        @"<\s*[｜|]*\s*(?:DSML\s*[｜|]+\s*)?(?:(?:invoke|function_calls|tool_call)\b|tool[▁_]calls?[▁_]begin)",
        RegexOptions.IgnoreCase)]
    private static partial Regex AberturaDeChamada();

    // As tags de chamada de ferramenta, abrindo ou fechando, também com o prefixo da DeepSeek
    // (</｜｜DSML｜｜ parameter>), e os tokens especiais dela (<｜tool▁calls▁end｜>). Só dentro de
    // uma linha, para não engolir texto entre um "<" e um ">".
    [GeneratedRegex(
        @"</?(?:parameter|invoke|function_calls|tool_call)\b[^<>\r\n]*>" +
        @"|</?\s*[｜|]+\s*DSML\s*[｜|]+[^<>\r\n]*>" +
        @"|<[｜|][^｜|<>\r\n]*[｜|]>",
        RegexOptions.IgnoreCase)]
    private static partial Regex MarcacaoDeFerramenta();
}
