namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>Balanço patrimonial no layout da contabilidade: ativo e passivo lado a lado.</summary>
public class BalancoViewModel
{
    public List<BalancoLinhaViewModel> Ativo { get; set; } = [];
    public List<BalancoLinhaViewModel> Passivo { get; set; } = [];
    public decimal TotalAtivo { get; set; }
    public decimal TotalPassivo { get; set; }
}

public class BalancoLinhaViewModel
{
    /// <summary>linha, grupo (Disponível, Realizável...), bloco (Ativo Circulante...) ou total (ATIVO/PASSIVO).</summary>
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    /// <summary>Contas que formam a linha; só as linhas têm.</summary>
    public List<BalancoComposicaoViewModel> Composicao { get; set; } = [];
}

public class BalancoComposicaoViewModel
{
    /// <summary>Conta analítica, ou o grupo da DRE ("3", "4"...) nas linhas de resultado.</summary>
    public string CodConta { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
