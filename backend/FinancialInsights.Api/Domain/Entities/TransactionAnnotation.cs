namespace FinancialInsights.Api.Domain.Entities;

public sealed class TransactionAnnotation
{
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }

    public Guid? TransactionTypeCategoryId { get; set; }

    public Guid? SpendTypeCategoryId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Transaction Transaction { get; set; } = null!;

    public Category? TransactionTypeCategory { get; set; }

    public Category? SpendTypeCategory { get; set; }
}
