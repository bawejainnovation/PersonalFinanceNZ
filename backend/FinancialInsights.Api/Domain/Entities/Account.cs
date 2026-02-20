namespace FinancialInsights.Api.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; set; }

    public string AkahuAccountId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? InstitutionName { get; set; }

    public string? AccountNumber { get; set; }

    public decimal? CurrentBalance { get; set; }

    public string Currency { get; set; } = "NZD";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? LastSyncedAtUtc { get; set; }

    public AccountProfile? Profile { get; set; }

    public List<Transaction> Transactions { get; set; } = [];
}
