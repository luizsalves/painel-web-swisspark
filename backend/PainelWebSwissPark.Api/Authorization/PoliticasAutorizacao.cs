namespace PainelWebSwissPark.Api.Authorization;

public static class PoliticasAutorizacao
{
    /// <summary>Autenticado e ativo na bi_users. É a política padrão da API.</summary>
    public const string UsuarioAtivo = "UsuarioAtivo";

    /// <summary>Usuário ativo com perfil Administrador. Protege a área administrativa.</summary>
    public const string Administrador = "Administrador";
}

public static class ClaimsPainel
{
    /// <summary>Id do usuário na bi_users. Vai no token e é por ele que a API o encontra.</summary>
    public const string Subject = "sub";

    public const string Email = "email";

    public const string Nome = "name";
}
