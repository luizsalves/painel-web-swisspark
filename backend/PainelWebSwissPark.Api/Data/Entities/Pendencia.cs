namespace PainelWebSwissPark.Api.Data.Entities;

public class Pendencia
{
    public int Id { get; set; }
    public int FechamentoId { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Alta | Média | Regular | Baixa.</summary>
    public string Prioridade { get; set; } = "Regular";

    public bool Resolvida { get; set; }
    public DateTime CriadaEm { get; set; }

    public Fechamento? Fechamento { get; set; }
}
