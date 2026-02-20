namespace FinancialInsights.Api.DTOs;

public sealed class SyncRequest
{
    public int? MonthsBack { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }
}

public sealed class SyncResponse
{
    public DateTime FromDateUtc { get; init; }

    public DateTime ToDateUtc { get; init; }

    public int AccountsSynced { get; init; }

    public int TransactionsSynced { get; init; }
}
