namespace PainelWebSwissPark.Api.DTOs.Requests;

public class CriarUsuarioRequest
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Senha inicial definida pelo administrador. Entra como provisória: a pessoa
    /// é obrigada a trocar no primeiro acesso.
    /// </summary>
    public string SenhaInicial { get; set; } = string.Empty;
}
