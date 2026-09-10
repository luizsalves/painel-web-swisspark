using PainelWebSwissPark.Api.DTOs.Erp;

namespace PainelWebSwissPark.Api.Services.Erp;

public interface IOutraFonteRestaurantesService
{
    Task<List<RestauranteErpDto>> ObterRestaurantesAsync(CancellationToken cancellationToken);
}
