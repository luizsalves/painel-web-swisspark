using System.ComponentModel.DataAnnotations;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.DTOs.Requests;

/// <summary>A DFC exatamente como está na tela, para virar planilha.</summary>
public class ExportarDfcRequest
{
    [Required, MaxLength(200)] public string Empresa { get; set; } = "";
    [Range(1, short.MaxValue)] public short CodColigada { get; set; }
    /// <summary>Código da filial; vazio = todas.</summary>
    [MaxLength(10)] public string? Filial { get; set; }
    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public string Inicio { get; set; } = "";
    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public string Fim { get; set; } = "";
    [Required] public DfcViewModel Dfc { get; set; } = new();
}
