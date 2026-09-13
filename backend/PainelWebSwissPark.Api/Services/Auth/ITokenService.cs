using PainelWebSwissPark.Api.Entities;

namespace PainelWebSwissPark.Api.Services.Auth;

public sealed record TokenEmitido(string AccessToken, DateTimeOffset ExpiraEm);

public interface ITokenService
{
    TokenEmitido Emitir(BiUser usuario);
}
