namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// Uma conta do plano de contas do TOTVS RM (CCONTA), pronta para preencher o
/// combobox de Conta Contábil do front.
/// </summary>
public class ContaContabilViewModel
{
    /// <summary>Identificador único (base+coligada+conta) — CODCOLIGADA/CODCONTA sozinhos se repetem entre as quatro bases.</summary>
    public string Id { get; set; } = string.Empty;
    public short CodColigada { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public string Reduzido { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Flag do painel (não existe em CCONTA). Desativada some do combobox de Fechamentos.</summary>
    public bool Ativo { get; set; } = true;
}
