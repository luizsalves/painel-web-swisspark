using System.ComponentModel.DataAnnotations;

namespace PainelWebSwissPark.Api.DTOs.Requests;

/// <summary>
/// O Excel do fechamento: a aba RAZAO e uma aba por mês, de janeiro ao mês do fechamento,
/// como estão na tela — o backend só monta a planilha, não recalcula nada.
/// </summary>
public class ExportarFechamentoAnualRequest
{
    [Required] public string Empresa { get; set; } = "";
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    [Required] public string CodConta { get; set; } = "";
    public string DescricaoConta { get; set; } = "";
    public string Reduzido { get; set; } = "";

    /// <summary>Do mês mais antigo ao mês do fechamento.</summary>
    [Required, MinLength(1), MaxLength(12)]
    public List<MesFechamentoAnual> Meses { get; set; } = [];
}

/// <summary>Um mês no resumo do histórico, mais o que a aba do fechamento daquele mês mostra.</summary>
public class MesFechamentoAnual : MesHistoricoFechamento
{
    public decimal ComposicaoSaldo { get; set; }
    public bool Pendencia { get; set; }
    public string? PreparadoPor { get; set; }
    public string? ConferidoPor { get; set; }
    public string? FinalizadoPor { get; set; }
    // Mesma data/hora local apresentada na tela pelo navegador.
    public string? PreparadoEm { get; set; }
    public string? ConferidoEm { get; set; }
    public string? FinalizadoEm { get; set; }
}
