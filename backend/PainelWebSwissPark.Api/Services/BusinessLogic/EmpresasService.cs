using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class EmpresasService(IEmpresaRepository empresaRepository) : IEmpresasService
{
    public async Task<List<EmpresaViewModel>> ListarAsync(CancellationToken cancellationToken)
    {
        var filiais = await empresaRepository.ListarTodasColigadasAsync(cancellationToken);

        return filiais
            .Select(f => new EmpresaViewModel
            {
                Id = $"{f.CodColigada}-{f.CodFilial}",
                CodColigada = f.CodColigada,
                CodFilial = f.CodFilial,
                Nome = f.Nome ?? string.Empty,
                NomeFantasia = f.NomeFantasia ?? string.Empty,
                NomeFilial = $"{f.CodFilial} - {f.NomeFantasia}",
                Cgc = f.Cgc ?? string.Empty,
                Cidade = f.Cidade ?? string.Empty,
                Estado = f.Estado ?? string.Empty,
            })
            .ToList();
    }
}
