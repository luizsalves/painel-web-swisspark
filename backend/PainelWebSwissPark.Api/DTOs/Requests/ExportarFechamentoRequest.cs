using System.ComponentModel.DataAnnotations;

namespace PainelWebSwissPark.Api.DTOs.Requests;

public class ExportarFechamentoRequest
{
    [Required] public string Empresa { get; set; } = "";
    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public string Periodo { get; set; } = "";
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string CodConta { get; set; } = "";
    public string DescricaoConta { get; set; } = "";
    public string Reduzido { get; set; } = "";
    public string? PreparadoPor { get; set; }
    public string? ConferidoPor { get; set; }
    public string? FinalizadoPor { get; set; }
    // Mesma data/hora local apresentada no PDF pelo navegador.
    public string? PreparadoEm { get; set; }
    public string? ConferidoEm { get; set; }
    public string? FinalizadoEm { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoRazao { get; set; }
    public decimal ComposicaoSaldo { get; set; }
    public decimal Diferenca { get; set; }
    public bool Pendencia { get; set; }
    public string Status { get; set; } = "";
    public string? Responsavel { get; set; }
    [Required, MaxLength(65500)]
    public List<LinhaExportacaoFechamento> Lancamentos { get; set; } = [];
}

public class LinhaExportacaoFechamento
{
    public string Data { get; set; } = "";
    public string IdPartida { get; set; } = "";
    [MaxLength(32767)] public string Historico { get; set; } = "";
    public decimal Valor { get; set; }
    [Required, RegularExpression("^(Débito|Crédito)$")]
    public string Tipo { get; set; } = "";
}
