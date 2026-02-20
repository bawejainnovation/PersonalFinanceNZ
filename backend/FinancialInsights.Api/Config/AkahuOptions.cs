namespace FinancialInsights.Api.Config;

public sealed class AkahuOptions
{
    public const string SectionName = "Akahu";

    public string BaseUrl { get; init; } = "https://api.akahu.io/v1";

    public string AppToken { get; init; } = string.Empty;

    public string UserToken { get; init; } = string.Empty;

    public int RequestTimeoutSeconds { get; init; } = 30;
}
