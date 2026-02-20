namespace FinancialInsights.Api.Domain.Entities;

public sealed class AccountProfile
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string? NzBankKey { get; set; }

    public string? CustomDescription { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Account Account { get; set; } = null!;
}
