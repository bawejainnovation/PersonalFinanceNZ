namespace FinancialInsights.Api.Services;

public sealed class TransferMatchingService : ITransferMatchingService
{
    private const double MaxTransferWindowDays = 4.0;

    public IReadOnlySet<Guid> FindMatchedTransferIds(IEnumerable<TransferMatchCandidate> candidates)
    {
        var matched = new HashSet<Guid>();

        var grouped = candidates
            .GroupBy(candidate => Math.Abs(candidate.Amount))
            .Where(group => group.Count() > 1);

        foreach (var group in grouped)
        {
            var ordered = group
                .OrderBy(candidate => candidate.TransactionDateUtc)
                .ThenBy(candidate => candidate.Id)
                .ToList();

            for (var index = 0; index < ordered.Count; index++)
            {
                var left = ordered[index];
                for (var innerIndex = index + 1; innerIndex < ordered.Count; innerIndex++)
                {
                    var right = ordered[innerIndex];
                    if ((right.TransactionDateUtc - left.TransactionDateUtc).TotalDays > MaxTransferWindowDays)
                    {
                        break;
                    }

                    if (left.AccountId == right.AccountId)
                    {
                        continue;
                    }

                    if (!HasOppositeSigns(left.Amount, right.Amount))
                    {
                        continue;
                    }

                    matched.Add(left.Id);
                    matched.Add(right.Id);
                }
            }
        }

        return matched;
    }

    private static bool HasOppositeSigns(decimal left, decimal right)
    {
        if (left == 0m || right == 0m)
        {
            return false;
        }

        return (left > 0m && right < 0m) || (left < 0m && right > 0m);
    }
}
