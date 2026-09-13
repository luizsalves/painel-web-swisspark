namespace PainelWebSwissPark.Api.DTOs.Requests;

public class AtualizarUsuarioRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Os vínculos da pessoa nos módulos que quem edita administra. Vínculos em
    /// outros módulos não são tocados — o administrador do mkt não consegue
    /// remover ninguém da conciliação nem sem querer.
    /// </summary>
    public List<VinculoDeModuloRequest> Modulos { get; set; } = [];
}
