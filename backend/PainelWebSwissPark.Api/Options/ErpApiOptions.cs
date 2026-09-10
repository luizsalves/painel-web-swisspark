namespace PainelWebSwissPark.Api.Options;

public class ErpApiOptions
{
    public const string SectionName = "ErpApi";

    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}
