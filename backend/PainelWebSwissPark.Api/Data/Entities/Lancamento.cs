namespace PainelWebSwissPark.Api.Data.Entities;

/// <summary>
/// Lançamento contábil dentro de um fechamento (equivalente a um registro de
/// CLANCA já resolvido em débito/crédito).
/// </summary>
public class Lancamento
{
    public long Id { get; set; }
    public int FechamentoId { get; set; }

    public DateOnly Data { get; set; }
    public string Historico { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }

    /// <summary>Debito | Credito.</summary>
    public string Tipo { get; set; } = "Debito";

    public string? Fornecedor { get; set; }

    public Fechamento? Fechamento { get; set; }
}
