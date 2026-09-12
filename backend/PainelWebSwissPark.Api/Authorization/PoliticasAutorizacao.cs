namespace PainelWebSwissPark.Api.Authorization;

public static class PoliticasAutorizacao
{
    /// <summary>Autenticado no Entra ID e ativo na bi_users. É a política padrão da API.</summary>
    public const string UsuarioAtivo = "UsuarioAtivo";

    /// <summary>Usuário ativo com perfil Administrador. Protege a área administrativa.</summary>
    public const string Administrador = "Administrador";
}

public static class ClaimsPainel
{
    /// <summary>Id do registro na bi_users — não confundir com o oid do Entra.</summary>
    public const string BiUserId = "painel:bi_user_id";
}
