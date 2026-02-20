namespace FinancialInsights.Api.Services;

public interface ITransferMatchingService
{
    IReadOnlySet<Guid> FindMatchedTransferIds(IEnumerable<TransferMatchCandidate> candidates);
}

public readonly record struct TransferMatchCandidate(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    DateTime TransactionDateUtc);
