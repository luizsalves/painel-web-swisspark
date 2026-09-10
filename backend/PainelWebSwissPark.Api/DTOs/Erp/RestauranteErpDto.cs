namespace PainelWebSwissPark.Api.DTOs.Erp;

public class RestauranteErpDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class PratoErpDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Imagem { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int Restaurante { get; set; }
}

public class PaginaErpDto<T>
{
    public int Count { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public List<T> Results { get; set; } = [];
}
