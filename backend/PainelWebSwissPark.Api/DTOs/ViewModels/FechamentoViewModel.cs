namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class FechamentoViewModel
{
    public int EmpresaId { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public int ContaContabilId { get; set; }
    public string ContaContabilCodigo { get; set; } = string.Empty;
    public string ContaContabilDescricao { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;

    public decimal SaldoRazao { get; set; }
    public decimal ComposicaoSaldo { get; set; }
    public decimal Diferenca { get; set; }
    public bool PossuiPendencia { get; set; }

    public string Status { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string AtualizadoEm { get; set; } = string.Empty;

    public List<LancamentoViewModel> Lancamentos { get; set; } = [];
    public List<PendenciaViewModel> Pendencias { get; set; } = [];
}
