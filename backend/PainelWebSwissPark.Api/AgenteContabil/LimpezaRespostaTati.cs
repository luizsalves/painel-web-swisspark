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

    // As tags de chamada de ferramenta, abrindo ou fechando, e os tokens especiais da DeepSeek
    // (<｜tool▁calls▁end｜>). Só dentro de uma linha, para não engolir texto entre um "<" e um ">".
    [GeneratedRegex(@"</?(?:parameter|invoke|function_calls|tool_call)\b[^<>\r\n]*>|<｜[^｜<>\r\n]*｜>", RegexOptions.IgnoreCase)]
    private static partial Regex MarcacaoDeFerramenta();
}
