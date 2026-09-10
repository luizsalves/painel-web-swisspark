using Microsoft.AspNetCore.Mvc;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantesController(IRestaurantesService restaurantesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var restaurantes = await restaurantesService.ObterRestaurantesComPratosAsync(cancellationToken);
        return Ok(restaurantes);
    }
}
