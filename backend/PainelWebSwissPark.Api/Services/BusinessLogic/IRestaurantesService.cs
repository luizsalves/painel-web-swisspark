using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IRestaurantesService
{
    Task<List<RestauranteViewModel>> ObterRestaurantesComPratosAsync(CancellationToken cancellationToken);
}
