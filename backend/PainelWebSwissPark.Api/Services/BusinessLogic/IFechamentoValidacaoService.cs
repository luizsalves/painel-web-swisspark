using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public interface IFechamentoValidacaoService
{
    Task<FechamentoValidacaoViewModel> ObterStatusAtualAsync(
        short codColigada, short codFilial, string codConta, string anoMes, CancellationToken cancellationToken);

    Task<FechamentoValidacaoViewModel> PrepararAsync(
        PrepararFechamentoRequest request, UsuarioAutorizado autor, CancellationToken cancellationToken);

    Task<FechamentoValidacaoViewModel> ConferirAsync(
        ChaveFechamentoRequest request, UsuarioAutorizado autor, CancellationToken cancellationToken);

    Task<FechamentoValidacaoViewModel> RealizarFechamentoAsync(
        RealizarFechamentoRequest request, UsuarioAutorizado autor, CancellationToken cancellationToken);

    Task<FechamentoValidacaoViewModel> ReverterAsync(
        ChaveFechamentoRequest request, UsuarioAutorizado autor, CancellationToken cancellationToken);
}
