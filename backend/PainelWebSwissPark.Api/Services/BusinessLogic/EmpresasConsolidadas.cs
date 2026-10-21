using System.Globalization;
using System.Text;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Empresas que fecham com todas as filiais juntas (regra da contabilidade): no
/// Fechamento e na Produtividade cada conta tem um fechamento só por mês, gravado em
/// bi_fechamento_validacao com a filial <see cref="Filial"/>. Balancete, Balanço,
/// Indicadores e DFC continuam por filial.
/// </summary>
public static class EmpresasConsolidadas
{
    /// <summary>A filial que representa "todas as filiais" no fechamento.</summary>
    public const short Filial = 0;

    // Comparados sem acento, sem caixa e sem espaço repetido: no GFILIAL o mesmo nome
    // aparece como "AGV PARTICIPAÇÕES LTDA" e como "AGV Participações Ltda".
    private static readonly HashSet<string> Nomes = ["AEPL AGROPECUARIA LTDA", "AGV PARTICIPACOES LTDA"];

    public static bool Consolida(string? nome) => Nomes.Contains(Normalizar(nome));

    public static string Normalizar(string? nome)
    {
        var semAcento = new StringBuilder();
        foreach (var letra in (nome ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(letra);
        }

        return string.Join(' ', semAcento.ToString().ToUpperInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
