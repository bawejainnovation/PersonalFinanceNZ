using FinancialInsights.Api.Domain.Entities;

namespace FinancialInsights.Api.Services;

public interface ITransferClassifier
{
    void Classify(IReadOnlyCollection<Transaction> transactions);
}
