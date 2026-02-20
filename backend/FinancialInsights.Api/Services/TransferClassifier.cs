using FinancialInsights.Api.Domain.Entities;

namespace FinancialInsights.Api.Services;

public sealed class TransferClassifier : ITransferClassifier
{
    private static readonly string[] TransferKeywords =
    [
        "transfer", "xfer", "between accounts", "internal transfer", "payment to"
    ];

    public void Classify(IReadOnlyCollection<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            transaction.IsBankTransfer = ContainsTransferKeyword(transaction.Description);
        }

        var grouped = transactions
            .GroupBy(transaction => Math.Abs(transaction.Amount))
            .Where(group => group.Count() > 1);

        foreach (var amountGroup in grouped)
        {
            var ordered = amountGroup.OrderBy(transaction => transaction.TransactionDateUtc).ToList();
            for (var index = 0; index < ordered.Count; index++)
            {
                var left = ordered[index];
                for (var innerIndex = index + 1; innerIndex < ordered.Count; innerIndex++)
                {
                    var right = ordered[innerIndex];
                    if (left.AccountId == right.AccountId)
                    {
                        continue;
                    }

                    if (left.Amount * right.Amount >= 0)
                    {
                        continue;
                    }

                    if (Math.Abs((left.TransactionDateUtc - right.TransactionDateUtc).TotalDays) > 1.0)
                    {
                        continue;
                    }

                    if (ContainsTransferKeyword(left.Description) ||
                        ContainsTransferKeyword(right.Description) ||
                        TextOverlapScore(left.Description, right.Description) > 0.45m)
                    {
                        left.IsBankTransfer = true;
                        right.IsBankTransfer = true;
                    }
                }
            }
        }
    }

    private static bool ContainsTransferKeyword(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return TransferKeywords.Any(normalized.Contains);
    }

    private static decimal TextOverlapScore(string left, string right)
    {
        var leftSet = Tokenize(left);
        var rightSet = Tokenize(right);

        if (leftSet.Count == 0 || rightSet.Count == 0)
        {
            return 0m;
        }

        var intersection = leftSet.Intersect(rightSet).Count();
        var union = leftSet.Union(rightSet).Count();
        return union == 0 ? 0m : intersection / (decimal)union;
    }

    private static HashSet<string> Tokenize(string value)
    {
        return value
            .ToLowerInvariant()
            .Split([' ', '-', '/', '\\', '.', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 2)
            .ToHashSet(StringComparer.Ordinal);
    }
}
