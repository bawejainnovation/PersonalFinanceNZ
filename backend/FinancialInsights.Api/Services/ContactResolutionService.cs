using System.Globalization;
using System.Text.RegularExpressions;
using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Services;

public sealed class ContactResolutionService(AppDbContext dbContext) : IContactResolutionService
{
    public async Task ResolveContactsAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken)
    {
        var existingContacts = await dbContext.Contacts
            .Include(contact => contact.Aliases)
            .AsTracking()
            .ToDictionaryAsync(contact => contact.CanonicalKey, cancellationToken);

        var nowUtc = DateTime.UtcNow;
        foreach (var transaction in transactions)
        {
            if (transaction.ContactId.HasValue)
            {
                continue;
            }

            var candidate = BuildCandidate(transaction);
            if (candidate is null)
            {
                continue;
            }

            if (!existingContacts.TryGetValue(candidate.CanonicalKey, out var contact))
            {
                contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    CanonicalKey = candidate.CanonicalKey,
                    DisplayName = candidate.DisplayName,
                    Confidence = candidate.Confidence,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };
                dbContext.Contacts.Add(contact);
                existingContacts[candidate.CanonicalKey] = contact;
            }
            else
            {
                contact.UpdatedAtUtc = nowUtc;
            }

            if (!string.IsNullOrWhiteSpace(candidate.Alias) &&
                contact.Aliases.All(alias => !string.Equals(alias.Alias, candidate.Alias, StringComparison.OrdinalIgnoreCase)))
            {
                contact.Aliases.Add(new ContactAlias
                {
                    Id = Guid.NewGuid(),
                    ContactId = contact.Id,
                    Alias = candidate.Alias,
                    CreatedAtUtc = nowUtc
                });
            }

            transaction.ContactId = contact.Id;
        }
    }

    private static ContactCandidate? BuildCandidate(Transaction transaction)
    {
        if (!string.IsNullOrWhiteSpace(transaction.Reference))
        {
            var accountNumber = TryExtractAccountNumber(transaction.Reference!);
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                return new ContactCandidate($"acct:{accountNumber}", accountNumber, "high", transaction.Reference!);
            }
        }

        if (!string.IsNullOrWhiteSpace(transaction.MerchantName))
        {
            var normalizedMerchant = NormalizeName(transaction.MerchantName!);
            if (!string.IsNullOrWhiteSpace(normalizedMerchant))
            {
                return new ContactCandidate($"name:{normalizedMerchant}", transaction.MerchantName!, "medium", transaction.MerchantName!);
            }
        }

        if (!string.IsNullOrWhiteSpace(transaction.Description))
        {
            var accountNumber = TryExtractAccountNumber(transaction.Description);
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                return new ContactCandidate($"acct:{accountNumber}", accountNumber, "high", transaction.Description);
            }

            var fingerprint = BuildDescriptionFingerprint(transaction.Description);
            if (!string.IsNullOrWhiteSpace(fingerprint))
            {
                return new ContactCandidate($"desc:{fingerprint}", transaction.Description, "low", transaction.Description);
            }
        }

        return null;
    }

    private static string? TryExtractAccountNumber(string input)
    {
        var match = Regex.Match(input, "\\b\\d{2}-\\d{4}-\\d{7}-\\d{2,3}\\b");
        if (match.Success)
        {
            return match.Value;
        }

        var compact = Regex.Match(input.Replace(" ", string.Empty), "\\b\\d{13,16}\\b");
        return compact.Success ? compact.Value : null;
    }

    private static string NormalizeName(string name)
    {
        var cleaned = Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9 ]", " ");
        var tokens = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token is not ("ltd" or "limited" or "nz" or "the"))
            .ToArray();

        return string.Join(' ', tokens);
    }

    private static string BuildDescriptionFingerprint(string description)
    {
        var lowered = description.ToLowerInvariant();
        lowered = Regex.Replace(lowered, "\\b\\d{1,2}[/-]\\d{1,2}([/-]\\d{2,4})?\\b", " ");
        lowered = Regex.Replace(lowered, "\\b\\d+\\b", " ");
        lowered = Regex.Replace(lowered, "[^a-z ]", " ");

        var tokens = lowered
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 3)
            .Take(6)
            .ToArray();

        return string.Join('-', tokens);
    }

    private sealed record ContactCandidate(string CanonicalKey, string DisplayName, string Confidence, string Alias);
}
