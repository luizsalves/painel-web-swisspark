namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// Perfis aceitos pela bi_users. Espelha o CHECK da coluna role — mudar aqui exige
/// mudar a constraint no banco (docs/sql/001_bi_users.sql).
/// </summary>
public static class PerfisUsuario
{
    public const string Administrador = "Administrador";
    public const string Gerente = "Gerente";
    public const string Analista = "Analista";

    public static readonly IReadOnlyList<string> Todos = [Administrador, Gerente, Analista];

    public static bool EhValido(string? perfil) =>
        perfil is not null && Todos.Contains(perfil, StringComparer.Ordinal);
}
