using FinancialInsights.Api.Domain.Enums;

namespace FinancialInsights.Api.DTOs;

public sealed class CategoryCashflowResponse
{
    public Guid? CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public CategoryType CategoryType { get; init; }

    public decimal MoneyIn { get; init; }

    public decimal MoneyOut { get; init; }

    public decimal Net => MoneyIn - MoneyOut;
}

public sealed class MonthlyCategoryOverviewResponse
{
    public int Year { get; init; }

    public int Month { get; init; }

    public Guid? CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public CategoryType CategoryType { get; init; }

    public decimal MoneyIn { get; init; }

    public decimal MoneyOut { get; init; }
}

public sealed class AccountBalanceResponse
{
    public Guid AccountId { get; init; }

    public string AccountName { get; init; } = string.Empty;

    public string? AccountNumber { get; init; }

    public decimal? CurrentBalance { get; init; }

    public string Currency { get; init; } = "NZD";
}
