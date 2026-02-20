namespace FinancialInsights.Api.Domain.Entities;

public sealed class ContactAlias
{
    public Guid Id { get; set; }

    public Guid ContactId { get; set; }

    public string Alias { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public Contact Contact { get; set; } = null!;
}
