namespace PainelWebSwissPark.Api.Options;

public class PainelDbOptions
{
    public const string SectionName = "PainelDb";

    /// <summary>
    /// Connection string do Postgres onde vive a bi_users.
    /// Nunca fica no appsettings.json — vem do keys.json (fora do repositório).
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Schema da bi_users. Hoje "public" no banco contabil; quando o banco do painel
    /// existir, basta apontar para "painel" sem tocar em código.
    /// </summary>
    public string Schema { get; init; } = "public";

    /// <summary>
    /// Segundos de cache da consulta de autorização por usuário.
    /// 0 (padrão) = sem cache, revogação vale já no próximo request.
    /// Só aumente se a consulta virar gargalo — o atraso é o preço.
    /// </summary>
    public int CacheSeconds { get; init; }
}
