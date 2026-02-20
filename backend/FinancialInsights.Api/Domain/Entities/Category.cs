using FinancialInsights.Api.Domain.Enums;

namespace FinancialInsights.Api.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; set; }

    public CategoryType CategoryType { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<TransactionAnnotation> TransactionTypeAnnotations { get; set; } = [];

    public List<TransactionAnnotation> SpendTypeAnnotations { get; set; } = [];
}
