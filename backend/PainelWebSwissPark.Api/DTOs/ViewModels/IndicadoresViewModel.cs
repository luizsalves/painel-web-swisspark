namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class IndicadoresViewModel
{
    public BaseCalculoIndicadoresViewModel BaseCalculo { get; set; } = new();
    public List<IndiceFinanceiroViewModel> Indices { get; set; } = [];
    public DfcViewModel Dfc { get; set; } = new();
}

/// <summary>Saldos do balanço no fim do período e resultado acumulado no período.</summary>
public class BaseCalculoIndicadoresViewModel
{
    public decimal AtivoTotal { get; set; }
    public decimal AtivoCirculante { get; set; }
    public decimal Disponivel { get; set; }
    public decimal Estoques { get; set; }
    public decimal RealizavelLongoPrazo { get; set; }
    public decimal PassivoCirculante { get; set; }
    public decimal PassivoNaoCirculante { get; set; }
    public decimal PatrimonioLiquido { get; set; }
    public decimal DividaBruta { get; set; }
    public decimal DividaLiquida { get; set; }
    public decimal ReceitaLiquida { get; set; }
    public decimal Custos { get; set; }
    public decimal LucroBruto { get; set; }
    public decimal ResultadoFinanceiro { get; set; }
    public decimal ImpostoRendaContribuicao { get; set; }
    public decimal LucroLiquido { get; set; }
    public decimal Ebit { get; set; }
    public decimal DepreciacaoAmortizacao { get; set; }
    public decimal Ebitda { get; set; }
}

public class IndiceFinanceiroViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string Interpretacao { get; set; } = string.Empty;
    /// <summary>indice, percentual, vezes ou moeda.</summary>
    public string Formato { get; set; } = string.Empty;
    /// <summary>Nulo quando o denominador é zero.</summary>
    public decimal? Valor { get; set; }
}

public class DfcViewModel
{
    public List<DfcGrupoViewModel> Grupos { get; set; } = [];
    public decimal SaldoInicialCaixa { get; set; }
    public decimal SaldoFinalCaixa { get; set; }
    public decimal VariacaoCaixa { get; set; }
}

public class DfcGrupoViewModel
{
    public string Nome { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<DfcLinhaViewModel> Linhas { get; set; } = [];
}

public class DfcLinhaViewModel
{
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    /// <summary>Linha que acumula contas fora do de-para; só aparece quando há valor.</summary>
    public bool NaoClassificada { get; set; }
    public List<DfcComposicaoViewModel> Composicao { get; set; } = [];
}

public class DfcComposicaoViewModel
{
    public string CodConta { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Medida { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
