namespace PainelWebSwissPark.Api.DTOs.Requests;

public class CriarUsuarioRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>E-mail corporativo do Entra ID. O oid é vinculado no primeiro login.</summary>
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
