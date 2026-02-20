namespace FinancialInsights.Api.DTOs;

public sealed class AccountResponse
{
    public Guid Id { get; init; }

    public string AkahuAccountId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? InstitutionName { get; init; }

    public string? AccountNumber { get; init; }

    public string Currency { get; init; } = "NZD";

    public decimal? CurrentBalance { get; init; }

    public string? NzBankKey { get; init; }

    public string? CustomDescription { get; init; }

    public DateTime? LastSyncedAtUtc { get; init; }

    public int TransactionCount { get; init; }
}

public sealed class UpdateAccountProfileRequest
{
    public string? NzBankKey { get; init; }

    public string? CustomDescription { get; init; }
}

public sealed class UpdateAccountProfilesRequest
{
    public IReadOnlyList<AccountProfilePatch> Items { get; init; } = [];
}

public sealed class AccountProfilePatch
{
    public Guid AccountId { get; init; }

    public string? NzBankKey { get; init; }

    public string? CustomDescription { get; init; }
}
