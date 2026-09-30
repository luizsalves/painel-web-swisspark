namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>Diferença = saldo do razão − composição do saldo, por conta, no fim do mês.</summary>
public class DiferencasViewModel
{
    public string AnoMes { get; set; } = string.Empty;
    public int TotalEmpresas { get; set; }
    public int TotalContas { get; set; }
    /// <summary>Soma das diferenças em valor absoluto.</summary>
    public decimal ValorTotal { get; set; }
    public DateTimeOffset CalculadoEm { get; set; }
    public List<DiferencaEmpresaViewModel> Empresas { get; set; } = [];
    public List<DiferencaContaViewModel> Contas { get; set; } = [];
}

public class DiferencaEmpresaViewModel
{
    public string Empresa { get; set; } = string.Empty;
    public int Contas { get; set; }
    /// <summary>Soma das diferenças em valor absoluto — é o que ordena o ranking.</summary>
    public decimal ValorAbsoluto { get; set; }
    public decimal ValorLiquido { get; set; }
}

public class DiferencaContaViewModel
{
    public string Empresa { get; set; } = string.Empty;
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string NomeFilial { get; set; } = string.Empty;
    public string CodConta { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal SaldoRazao { get; set; }
    public decimal Composicao { get; set; }
    public decimal Diferenca { get; set; }
    public int Documentos { get; set; }
}
