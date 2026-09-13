namespace PainelWebSwissPark.Api.DTOs.Requests;

public class TrocarSenhaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;

    public string NovaSenha { get; set; } = string.Empty;
}
