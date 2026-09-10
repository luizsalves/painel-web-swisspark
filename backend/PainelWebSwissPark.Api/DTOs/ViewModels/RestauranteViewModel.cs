namespace PainelWebSwissPark.Api.DTOs.ViewModels;

public class RestauranteViewModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public List<PratoViewModel> Pratos { get; set; } = [];
}

public class PratoViewModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Imagem { get; set; } = string.Empty;
}
