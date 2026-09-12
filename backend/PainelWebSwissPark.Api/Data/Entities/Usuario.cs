namespace PainelWebSwissPark.Api.Data.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;

    public List<Fechamento> FechamentosResponsavel { get; set; } = [];
}
