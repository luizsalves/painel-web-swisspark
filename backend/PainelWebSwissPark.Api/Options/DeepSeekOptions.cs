namespace PainelWebSwissPark.Api.Options;

/// <summary>
/// Configuração da CFO Tati (tela IA Planning). A chave em si é segredo e vem
/// de DeepSeek:ApiKey no keys.json.
/// </summary>
public class DeepSeekOptions
{
    public const string SectionName = "DeepSeek";

    public string? ApiKey { get; init; }

    public string Model { get; init; } = "deepseek-chat";

    public string Endpoint { get; init; } = "https://api.deepseek.com";
}
