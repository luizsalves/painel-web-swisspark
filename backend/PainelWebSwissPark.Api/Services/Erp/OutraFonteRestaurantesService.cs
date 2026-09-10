using PainelWebSwissPark.Api.DTOs.Erp;

namespace PainelWebSwissPark.Api.Services.Erp;

public class OutraFonteRestaurantesService(IHttpClientFactory httpClientFactory) : IOutraFonteRestaurantesService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("OutraFonteApi");

    public async Task<List<RestauranteErpDto>> ObterRestaurantesAsync(CancellationToken cancellationToken)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<List<RestauranteErpDto>>("restaurantes", cancellationToken)
            ?? [];
    }
}
