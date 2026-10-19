using System.ComponentModel.DataAnnotations;

namespace PainelWebSwissPark.Api.DTOs.Requests;

/// <summary>
/// O histórico de partidas da aba Histórico do fechamento, como está na tela —
/// o backend só monta a planilha, não recalcula nada.
/// </summary>
public class ExportarHistoricoFechamentoRequest
{
    [Required] public string Empresa { get; set; } = "";
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    [Required] public string CodConta { get; set; } = "";
    public string DescricaoConta { get; set; } = "";
    public string Reduzido { get; set; } = "";

    /// <summary>Do mês mais antigo ao mês do fechamento.</summary>
    [Required, MinLength(1), MaxLength(12)]
    public List<MesHistoricoFechamento> Meses { get; set; } = [];
}

public class MesHistoricoFechamento
{
    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public string AnoMes { get; set; } = "";
    public decimal SaldoAnterior { get; set; }
    public decimal Debitos { get; set; }
    public decimal Creditos { get; set; }
    public decimal SaldoMes { get; set; }
    [MaxLength(60000)]
    public List<LinhaExportacaoFechamento> Lancamentos { get; set; } = [];
}
