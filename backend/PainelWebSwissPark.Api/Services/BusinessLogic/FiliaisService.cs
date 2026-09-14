using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class FiliaisService(IFilialRepository filialRepository) : IFiliaisService
{
    public async Task<List<FilialRelacionadaViewModel>> ObterFiliaisComMesmoCodigoAsync(
        CancellationToken cancellationToken)
    {
        var pares = await filialRepository.ObterComMesmoCodFilialAsync(cancellationToken);

        return pares
            .Select(p => new FilialRelacionadaViewModel
            {
                CodColigadaA = p.FilialA.CodColigada,
                NomeA = p.FilialA.Nome ?? string.Empty,
                CodColigadaB = p.FilialB.CodColigada,
                NomeB = p.FilialB.Nome ?? string.Empty,
                CodFilial = p.FilialA.CodFilial,
            })
            .ToList();
    }
}
