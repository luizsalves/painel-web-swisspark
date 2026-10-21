namespace PainelWebSwissPark.Api.DTOs.Requests;

/// <summary>
/// A pergunta à CFO Tati com todos os filtros da tela — os mesmos da Produtividade.
/// <see cref="PerguntarCfoTatiRequest.AnoMes"/> é o último mês do período.
/// </summary>
public class PerguntarCfoTatiComFiltrosRequest : PerguntarCfoTatiRequest
{
    /// <summary>AAAA-MM do primeiro mês do período; vazio quando o período é um mês só.</summary>
    public string? AnoMesInicial { get; set; }

    public short? CodColigada { get; set; }
    public short? CodFilial { get; set; }
    public string? CodConta { get; set; }

    /// <summary>Pendente, Preparado, Conferido ou Fechado; vazio para todos.</summary>
    public string? Status { get; set; }
}
