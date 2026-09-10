using PainelWebSwissPark.Api.DTOs.Erp;

namespace PainelWebSwissPark.Api.Services.Erp;

public interface IRestaurantesErpService
{
    Task<List<RestauranteErpDto>> ObterRestaurantesAsync(CancellationToken cancellationToken);
    Task<List<PratoErpDto>> ObterPratosDoRestauranteAsync(int restauranteId, CancellationToken cancellationToken);
}
