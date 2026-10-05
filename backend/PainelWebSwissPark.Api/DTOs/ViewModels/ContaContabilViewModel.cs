namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// Uma conta movimentada na CPARTIDA do TOTVS RM, pronta para preencher o
/// combobox de Conta Contábil do front.
/// </summary>
public class ContaContabilViewModel
{
    /// <summary>Identificador único por coligada e conta, unificado entre as bases.</summary>
    public string Id { get; set; } = string.Empty;
    public short CodColigada { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public List<short> CodFiliais { get; set; } = [];
    /// <summary>Código reduzido da CCONTA da coligada, ou da coligada global (0).</summary>
    public string Reduzido { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Flag do painel. Desativada some do combobox de Fechamentos.</summary>
    public bool Ativo { get; set; } = true;
}
