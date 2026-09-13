namespace PainelWebSwissPark.Api.DTOs.Requests;

/// <summary>O perfil de uma pessoa dentro de um módulo.</summary>
public class VinculoDeModuloRequest
{
    public string ModuleKey { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
