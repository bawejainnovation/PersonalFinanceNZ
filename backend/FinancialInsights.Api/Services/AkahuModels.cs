namespace FinancialInsights.Api.Services;

public sealed class AkahuAccount
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? InstitutionName { get; init; }

    public string? AccountNumber { get; init; }

    public decimal? CurrentBalance { get; init; }

    public string Currency { get; init; } = "NZD";
}

public sealed class AkahuTransaction
{
    public string Id { get; init; } = string.Empty;

    public string AccountId { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? MerchantName { get; init; }

    public string? Reference { get; init; }

    public string? TransactionType { get; init; }

    public DateTime DateUtc { get; init; }
}
