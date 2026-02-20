using FinancialInsights.Api.Domain.Enums;

namespace FinancialInsights.Api.DTOs;

public sealed class TransactionFeedQuery
{
    public string? AccountIds { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public bool IncludeBankTransfers { get; init; } = true;

    public string Direction { get; init; } = "all";

    public string? TransactionTypeCategoryIds { get; init; }

    public string? SpendTypeCategoryIds { get; init; }

    public string? ContactIds { get; init; }

    public bool ExperimentalTransferMatching { get; init; }
}

public sealed class TransactionResponse
{
    public Guid Id { get; init; }

    public string AkahuTransactionId { get; init; } = string.Empty;

    public Guid AccountId { get; init; }

    public string AccountName { get; init; } = string.Empty;

    public string? AccountDescription { get; init; }

    public string? BankKey { get; init; }

    public decimal Amount { get; init; }

    public MoneyDirection Direction { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? MerchantName { get; init; }

    public DateTime TransactionDateUtc { get; init; }

    public bool IsBankTransfer { get; init; }

    public Guid? TransactionTypeCategoryId { get; init; }

    public string? TransactionTypeCategoryName { get; init; }

    public Guid? SpendTypeCategoryId { get; init; }

    public string? SpendTypeCategoryName { get; init; }

    public string? Note { get; init; }

    public Guid? ContactId { get; init; }

    public string? ContactName { get; init; }
}

public sealed class UpdateTransactionAnnotationRequest
{
    public Guid? TransactionTypeCategoryId { get; init; }

    public Guid? SpendTypeCategoryId { get; init; }

    public string? Note { get; init; }
}
