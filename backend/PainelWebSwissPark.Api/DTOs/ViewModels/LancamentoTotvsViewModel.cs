namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// Um lançamento do razão (CPARTIDA) do TOTVS RM, ou a linha extra de
/// SALDO_RAZAO (data nula) somando o período pra uma conta.
/// </summary>
public class LancamentoTotvsViewModel
{
    public string AnoMes { get; set; } = string.Empty;
    public DateTime? Data { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public short CodColigada { get; set; }
    public string Reduzido { get; set; } = string.Empty;
    public string CodConta { get; set; } = string.Empty;
    public short CodFilial { get; set; }

    /// <summary>Código da filial concatenado com o nome fantasia, ex.: "1 - AGV Participações".</summary>
    public string NomeFilial { get; set; } = string.Empty;

    public string Complemento { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public int? IdLancamento { get; set; }
    public int? IdPartida { get; set; }
    public string? Usuario { get; set; }
    public decimal Debitos { get; set; }
    public decimal Creditos { get; set; }

    /// <summary>Só preenchido na linha-resumo (Data nula): saldo anterior ao período + débitos + créditos do período.</summary>
    public decimal? SaldoRazao { get; set; }

    /// <summary>Só preenchido na linha-resumo (Data nula): saldo acumulado antes do período (sem os débitos/créditos dele).</summary>
    public decimal? SaldoAnterior { get; set; }
}
