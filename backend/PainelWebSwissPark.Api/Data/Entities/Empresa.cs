namespace PainelWebSwissPark.Api.Data.Entities;

/// <summary>
/// Espelha a tabela de empresas/filiais (equivalente a GFILIAL no TOTVS RM).
/// Campos e nomes ainda não confirmados contra o schema real do SQL Server.
/// </summary>
public class Empresa
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;

    public List<ContaContabil> ContasContabeis { get; set; } = [];
    public List<Fechamento> Fechamentos { get; set; } = [];
}
