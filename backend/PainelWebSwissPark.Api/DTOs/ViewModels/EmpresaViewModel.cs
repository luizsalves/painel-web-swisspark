namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// Uma coligada/filial do TOTVS RM (GFILIAL), pronta para preencher o combobox
/// de Empresa do front.
/// </summary>
public class EmpresaViewModel
{
    /// <summary>Identificador único (coligada+filial).</summary>
    public string Id { get; set; } = string.Empty;
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;

    /// <summary>Código da filial concatenado com o nome fantasia, ex.: "1 - AGV Participações", pronto pro combobox de Filial.</summary>
    public string NomeFilial { get; set; } = string.Empty;

    public string Cgc { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    /// <summary>Flag do painel (não existe em GFILIAL). Desativada some dos comboboxes de Fechamentos.</summary>
    public bool Ativo { get; set; } = true;
}
