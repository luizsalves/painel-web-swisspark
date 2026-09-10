using PainelWebSwissPark.Api.DTOs.Erp;

namespace PainelWebSwissPark.Api.Services.Erp;

public class RestaurantesErpService(IHttpClientFactory httpClientFactory) : IRestaurantesErpService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ErpApi");

    public async Task<List<RestauranteErpDto>> ObterRestaurantesAsync(CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var restaurantes = new List<RestauranteErpDto>();
        var proximaPagina = "restaurantes/";

        while (!string.IsNullOrEmpty(proximaPagina))
        {
            var pagina = await client.GetFromJsonAsync<PaginaErpDto<RestauranteErpDto>>(proximaPagina, cancellationToken)
                ?? throw new InvalidOperationException("Resposta inválida da API do ERP.");

            restaurantes.AddRange(pagina.Results);
            proximaPagina = pagina.Next is null ? null : ToRelativePath(pagina.Next, client.BaseAddress!);
        }

        return restaurantes;
    }

    public async Task<List<PratoErpDto>> ObterPratosDoRestauranteAsync(int restauranteId, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<List<PratoErpDto>>($"restaurantes/{restauranteId}/pratos/", cancellationToken)
            ?? [];
    }

    private static string ToRelativePath(string absoluteUrl, Uri baseAddress) =>
        baseAddress.MakeRelativeUri(new Uri(absoluteUrl)).ToString();
}
