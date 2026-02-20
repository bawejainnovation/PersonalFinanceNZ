using FinancialInsights.Api.Domain.Enums;

namespace FinancialInsights.Api.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; set; }

    public string AkahuTransactionId { get; set; } = string.Empty;

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public MoneyDirection Direction { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? MerchantName { get; set; }

    public string? Reference { get; set; }

    public string? TransactionType { get; set; }

    public DateTime TransactionDateUtc { get; set; }

    public bool IsBankTransfer { get; set; }

    public Guid? ContactId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Account Account { get; set; } = null!;

    public Contact? Contact { get; set; }

    public TransactionAnnotation? Annotation { get; set; }
}
