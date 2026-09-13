namespace PainelWebSwissPark.Api.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Chave que assina os tokens. Mínimo de 32 bytes — HMAC-SHA256 recusa menos.
    /// Vem do keys.json, nunca do appsettings.json.
    /// Gere uma com: openssl rand -base64 48
    /// </summary>
    public string JwtSigningKey { get; init; } = string.Empty;

    public string Issuer { get; init; } = "painel-swisspark";

    public string Audience { get; init; } = "painel-swisspark";

    /// <summary>
    /// Validade do token em minutos. Pode ser generosa: o acesso é conferido na
    /// bi_users a cada requisição, então desativar alguém vale na hora — não
    /// depende de o token expirar.
    /// </summary>
    public int TokenLifetimeMinutes { get; init; } = 480;

    /// <summary>Tamanho mínimo da senha.</summary>
    public int MinimumPasswordLength { get; init; } = 8;
}
