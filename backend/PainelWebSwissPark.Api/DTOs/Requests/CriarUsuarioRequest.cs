namespace PainelWebSwissPark.Api.DTOs.Requests;

public class CriarUsuarioRequest
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Em quais módulos a pessoa entra, e com qual perfil em cada um.
    /// Só é possível conceder módulos que quem está criando administra.
    /// </summary>
    public List<VinculoDeModuloRequest> Modulos { get; set; } = [];

    /// <summary>
    /// Senha inicial definida pelo administrador. Entra como provisória: a pessoa
    /// é obrigada a trocar no primeiro acesso.
    /// </summary>
    public string SenhaInicial { get; set; } = string.Empty;
}
