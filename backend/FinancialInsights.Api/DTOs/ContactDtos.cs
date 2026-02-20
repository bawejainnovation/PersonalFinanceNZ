namespace FinancialInsights.Api.DTOs;

public sealed class ContactResponse
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Confidence { get; init; } = string.Empty;

    public int TransactionCount { get; init; }

    public decimal MoneyIn { get; init; }

    public decimal MoneyOut { get; init; }
}

public sealed class ContactMonthlyCashflowResponse
{
    public int Year { get; init; }

    public int Month { get; init; }

    public decimal MoneyIn { get; init; }

    public decimal MoneyOut { get; init; }
}

public sealed class ContactDetailResponse
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Confidence { get; init; } = string.Empty;

    public IReadOnlyList<ContactMonthlyCashflowResponse> MonthlyCashflow { get; init; } = [];

    public IReadOnlyList<ContactTransactionResponse> Transactions { get; init; } = [];
}

public sealed class ContactTransactionResponse
{
    public Guid TransactionId { get; init; }

    public DateTime TransactionDateUtc { get; init; }

    public string Description { get; init; } = string.Empty;

    public string AccountName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Direction { get; init; } = string.Empty;
}
