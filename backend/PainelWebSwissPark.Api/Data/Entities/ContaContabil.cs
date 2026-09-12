namespace PainelWebSwissPark.Api.Data.Entities;

/// <summary>
/// Espelha o plano de contas (equivalente a CCONTA no TOTVS RM).
/// </summary>
public class ContaContabil
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Reduzido { get; set; }

    public Empresa? Empresa { get; set; }
    public List<Fechamento> Fechamentos { get; set; } = [];
}
