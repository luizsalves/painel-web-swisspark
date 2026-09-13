namespace PainelWebSwissPark.Api.Authorization;

public static class PoliticasAutorizacao
{
    /// <summary>Autenticado, ativo e sem senha provisória pendente. É a política padrão da API.</summary>
    public const string UsuarioAtivo = "UsuarioAtivo";

    /// <summary>
    /// Administra pelo menos um módulo — ou é superadministrador.
    /// Libera a tela de Usuários; o recorte de QUEM ela mostra é feito no serviço.
    /// </summary>
    public const string AdministradorDeModulo = "AdministradorDeModulo";
}

public static class ClaimsPainel
{
    /// <summary>Id do usuário na bi_users. Vai no token e é por ele que a API o encontra.</summary>
    public const string Subject = "sub";

    public const string Email = "email";

    public const string Nome = "name";
}
