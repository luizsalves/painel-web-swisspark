namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// Um par de filiais que compartilham o mesmo CODFILIAL em coligadas diferentes.
/// </summary>
public class FilialRelacionadaViewModel
{
    public short CodColigadaA { get; set; }
    public string NomeA { get; set; } = string.Empty;

    public short CodColigadaB { get; set; }
    public string NomeB { get; set; } = string.Empty;

    public short CodFilial { get; set; }
}
