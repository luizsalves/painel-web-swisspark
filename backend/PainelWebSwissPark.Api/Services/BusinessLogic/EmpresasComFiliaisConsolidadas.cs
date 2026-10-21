using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// A lista de filiais do <see cref="EmpresasService"/> com as filiais de cada empresa de
/// <see cref="EmpresasConsolidadas"/> juntas numa só, a filial 0. As demais passam como
/// vieram. Assim a Produtividade consolidada reaproveita os cálculos que já existem.
/// </summary>
public class EmpresasComFiliaisConsolidadas(IEmpresasService empresasService) : IEmpresasService
{
    public async Task<List<EmpresaViewModel>> ListarAsync(CancellationToken cancellationToken, bool somenteBsb = false)
    {
        var filiais = await empresasService.ListarAsync(cancellationToken, somenteBsb);

        // A mesma empresa pode existir em mais de uma coligada: consolida dentro de cada uma.
        return filiais
            .GroupBy(f => EmpresasConsolidadas.Consolida(f.Nome)
                ? $"{f.CodColigada}:{EmpresasConsolidadas.Normalizar(f.Nome)}"
                : f.Id)
            .SelectMany(g => EmpresasConsolidadas.Consolida(g.First().Nome)
                ? new[] { Consolidar(g.ToList()) }
                : g.AsEnumerable())
            .ToList();
    }

    public Task<EmpresaViewModel?> AtualizarStatusAsync(
        short codColigada, short codFilial, bool ativo, Guid autorId, CancellationToken cancellationToken) =>
        empresasService.AtualizarStatusAsync(codColigada, codFilial, ativo, autorId, cancellationToken);

    private static EmpresaViewModel Consolidar(List<EmpresaViewModel> filiais)
    {
        var primeira = filiais[0];
        return new EmpresaViewModel
        {
            Id = $"{primeira.CodColigada}-{EmpresasConsolidadas.Filial}",
            CodColigada = primeira.CodColigada,
            CodFilial = EmpresasConsolidadas.Filial,
            Nome = primeira.Nome,
            NomeFantasia = primeira.NomeFantasia,
            NomeFilial = "Todas as filiais",
            Cgc = primeira.Cgc,
            Cidade = primeira.Cidade,
            Estado = primeira.Estado,
            // Desativar uma filial no painel não tira a empresa do fechamento; desativar todas, sim.
            Ativo = filiais.Any(f => f.Ativo),
        };
    }
}
