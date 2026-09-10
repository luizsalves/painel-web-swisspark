using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.Erp;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class RestaurantesService(
    IRestaurantesErpService erpService,
    IOutraFonteRestaurantesService outraFonteService) : IRestaurantesService
{
    public async Task<List<RestauranteViewModel>> ObterRestaurantesComPratosAsync(CancellationToken cancellationToken)
    {
        var restaurantesErp = await erpService.ObterRestaurantesAsync(cancellationToken);
        var restaurantesOutraFonte = await outraFonteService.ObterRestaurantesAsync(cancellationToken);

        var restaurantes = restaurantesErp.Concat(restaurantesOutraFonte).ToList();

        var viewModels = new List<RestauranteViewModel>();

        foreach (var restaurante in restaurantes)
        {
            var pratos = await erpService.ObterPratosDoRestauranteAsync(restaurante.Id, cancellationToken);

            viewModels.Add(new RestauranteViewModel
            {
                Id = restaurante.Id,
                Nome = restaurante.Nome,
                Pratos = pratos.Select(p => new PratoViewModel
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Tag = p.Tag,
                    Descricao = p.Descricao,
                    Imagem = p.Imagem,
                }).ToList(),
            });
        }

        return viewModels;
    }
}
