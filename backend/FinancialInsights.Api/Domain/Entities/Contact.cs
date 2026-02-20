namespace FinancialInsights.Api.Domain.Entities;

public sealed class Contact
{
    public Guid Id { get; set; }

    public string CanonicalKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Confidence { get; set; } = "low";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<ContactAlias> Aliases { get; set; } = [];

    public List<Transaction> Transactions { get; set; } = [];
}
