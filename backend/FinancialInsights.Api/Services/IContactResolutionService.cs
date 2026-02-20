using FinancialInsights.Api.Domain.Entities;

namespace FinancialInsights.Api.Services;

public interface IContactResolutionService
{
    Task ResolveContactsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken);
}
