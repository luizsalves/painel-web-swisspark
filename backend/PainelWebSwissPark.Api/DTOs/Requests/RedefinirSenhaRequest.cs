namespace PainelWebSwissPark.Api.DTOs.Requests;

/// <summary>
/// Um administrador define uma senha nova para outra pessoa. Ela entra como
/// provisória: quem receber é obrigado a trocar no próximo acesso.
/// </summary>
public class RedefinirSenhaRequest
{
    public string NovaSenha { get; set; } = string.Empty;
}
