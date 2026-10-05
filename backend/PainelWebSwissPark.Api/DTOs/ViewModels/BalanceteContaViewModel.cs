namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class BalanceteContaViewModel
{
    public short CodColigada { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string CodConta { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal SaldoAnterior { get; set; }
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
    public decimal SaldoAtual { get; set; }
    public int Nivel { get; set; }
    public bool Sintetica { get; set; }
}
