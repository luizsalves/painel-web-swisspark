namespace PainelWebSwissPark.Api.Options;

public class OutraFonteApiOptions
{
    public const string SectionName = "OutraFonteApi";

    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}
