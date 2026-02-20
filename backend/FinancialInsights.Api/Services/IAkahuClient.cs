namespace FinancialInsights.Api.Services;

public interface IAkahuClient
{
    Task<IReadOnlyList<AkahuAccount>> GetAccountsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AkahuTransaction>> GetTransactionsAsync(
        string accountId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
