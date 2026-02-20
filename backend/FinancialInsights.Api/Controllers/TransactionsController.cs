using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.Domain.Enums;
using FinancialInsights.Api.DTOs;
using FinancialInsights.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(
    AppDbContext dbContext,
    ITransferMatchingService transferMatchingService) : ControllerBase
{
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsvAsync(CancellationToken cancellationToken)
    {
        var aucklandTimeZone = ResolveAucklandTimeZone();

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Account)
            .ThenInclude(account => account.Profile)
            .Include(transaction => transaction.Annotation)
            .ThenInclude(annotation => annotation!.TransactionTypeCategory)
            .Include(transaction => transaction.Annotation)
            .ThenInclude(annotation => annotation!.SpendTypeCategory)
            .Include(transaction => transaction.Contact)
            .OrderByDescending(transaction => transaction.TransactionDateUtc)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine(
            "transaction_date_utc,transaction_date_local,amount,direction,is_bank_transfer,account_name,account_description,institution_name,account_number_masked,description,merchant_name,reference,transaction_type_raw,contact_name,transaction_type_category,spend_type_category,note");

        foreach (var transaction in transactions)
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(transaction.TransactionDateUtc, aucklandTimeZone);
            csv.AppendLine(string.Join(",",
                CsvField(transaction.TransactionDateUtc.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture)),
                CsvField(localTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                CsvField(transaction.Amount.ToString(CultureInfo.InvariantCulture)),
                CsvField(transaction.Direction == MoneyDirection.In ? "In" : "Out"),
                CsvField(transaction.IsBankTransfer ? "true" : "false"),
                CsvField(transaction.Account.Name),
                CsvField(transaction.Account.Profile?.CustomDescription),
                CsvField(transaction.Account.InstitutionName),
                CsvField(MaskAccountNumber(transaction.Account.AccountNumber)),
                CsvField(transaction.Description),
                CsvField(transaction.MerchantName),
                CsvField(transaction.Reference),
                CsvField(transaction.TransactionType),
                CsvField(transaction.Contact?.DisplayName),
                CsvField(transaction.Annotation?.TransactionTypeCategory?.Name),
                CsvField(transaction.Annotation?.SpendTypeCategory?.Name),
                CsvField(transaction.Annotation?.Note)));
        }

        var fileName = $"transactions-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetFeedAsync(
        [FromQuery] TransactionFeedQuery query,
        CancellationToken cancellationToken)
    {
        var accountIds = ParseGuidList(query.AccountIds);
        var transactionTypeCategoryIds = ParseGuidList(query.TransactionTypeCategoryIds);
        var spendTypeCategoryIds = ParseGuidList(query.SpendTypeCategoryIds);
        var contactIds = ParseGuidList(query.ContactIds);

        var direction = (query.Direction ?? "all").Trim().ToLowerInvariant();

        var transactionsQuery = dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Account)
            .ThenInclude(account => account.Profile)
            .Include(transaction => transaction.Annotation)
            .ThenInclude(annotation => annotation!.TransactionTypeCategory)
            .Include(transaction => transaction.Annotation)
            .ThenInclude(annotation => annotation!.SpendTypeCategory)
            .Include(transaction => transaction.Contact)
            .AsQueryable();

        if (accountIds.Count > 0)
        {
            transactionsQuery = transactionsQuery.Where(transaction => accountIds.Contains(transaction.AccountId));
        }

        if (query.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(query.FromDate.Value.Date, DateTimeKind.Utc);
            transactionsQuery = transactionsQuery.Where(transaction => transaction.TransactionDateUtc >= fromUtc);
        }

        if (query.ToDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(query.ToDate.Value.Date, DateTimeKind.Utc);
            transactionsQuery = transactionsQuery.Where(transaction => transaction.TransactionDateUtc <= toUtc);
        }

        transactionsQuery = direction switch
        {
            "in" => transactionsQuery.Where(transaction => transaction.Direction == MoneyDirection.In),
            "out" => transactionsQuery.Where(transaction => transaction.Direction == MoneyDirection.Out),
            _ => transactionsQuery
        };

        if (transactionTypeCategoryIds.Count > 0)
        {
            transactionsQuery = transactionsQuery.Where(transaction =>
                transaction.Annotation != null &&
                transaction.Annotation.TransactionTypeCategoryId.HasValue &&
                transactionTypeCategoryIds.Contains(transaction.Annotation.TransactionTypeCategoryId.Value));
        }

        if (spendTypeCategoryIds.Count > 0)
        {
            transactionsQuery = transactionsQuery.Where(transaction =>
                transaction.Annotation != null &&
                transaction.Annotation.SpendTypeCategoryId.HasValue &&
                spendTypeCategoryIds.Contains(transaction.Annotation.SpendTypeCategoryId.Value));
        }

        if (contactIds.Count > 0)
        {
            transactionsQuery = transactionsQuery.Where(transaction =>
                transaction.ContactId.HasValue &&
                contactIds.Contains(transaction.ContactId.Value));
        }

        IReadOnlySet<Guid> matchedTransfers = new HashSet<Guid>();
        if (query.ExperimentalTransferMatching)
        {
            var transferCandidateQuery = dbContext.Transactions
                .AsNoTracking()
                .AsQueryable();

            if (accountIds.Count > 0)
            {
                transferCandidateQuery = transferCandidateQuery.Where(transaction => accountIds.Contains(transaction.AccountId));
            }

            if (query.FromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(query.FromDate.Value.Date, DateTimeKind.Utc).AddDays(-4);
                transferCandidateQuery = transferCandidateQuery.Where(transaction => transaction.TransactionDateUtc >= fromUtc);
            }

            if (query.ToDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(query.ToDate.Value.Date, DateTimeKind.Utc).AddDays(4);
                transferCandidateQuery = transferCandidateQuery.Where(transaction => transaction.TransactionDateUtc <= toUtc);
            }

            var transferCandidates = await transferCandidateQuery
                .Select(transaction => new TransferMatchCandidate(
                    transaction.Id,
                    transaction.AccountId,
                    transaction.Amount,
                    transaction.TransactionDateUtc))
                .ToListAsync(cancellationToken);

            matchedTransfers = transferMatchingService.FindMatchedTransferIds(transferCandidates);

            if (!query.IncludeBankTransfers && matchedTransfers.Count > 0)
            {
                transactionsQuery = transactionsQuery.Where(transaction => !matchedTransfers.Contains(transaction.Id));
            }
        }
        else if (!query.IncludeBankTransfers)
        {
            transactionsQuery = transactionsQuery.Where(transaction => !transaction.IsBankTransfer);
        }

        var transactions = await transactionsQuery
            .OrderByDescending(transaction => transaction.TransactionDateUtc)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .Select(transaction => new TransactionResponse
            {
                Id = transaction.Id,
                AkahuTransactionId = transaction.AkahuTransactionId,
                AccountId = transaction.AccountId,
                AccountName = transaction.Account.Name,
                AccountDescription = transaction.Account.Profile != null ? transaction.Account.Profile.CustomDescription : null,
                BankKey = transaction.Account.Profile != null ? transaction.Account.Profile.NzBankKey : null,
                Amount = transaction.Amount,
                Direction = transaction.Direction,
                Description = transaction.Description,
                MerchantName = transaction.MerchantName,
                TransactionDateUtc = transaction.TransactionDateUtc,
                IsBankTransfer = query.ExperimentalTransferMatching
                    ? matchedTransfers.Contains(transaction.Id)
                    : transaction.IsBankTransfer,
                TransactionTypeCategoryId = transaction.Annotation != null
                    ? transaction.Annotation.TransactionTypeCategoryId
                    : null,
                TransactionTypeCategoryName = transaction.Annotation != null &&
                                              transaction.Annotation.TransactionTypeCategory != null
                    ? transaction.Annotation.TransactionTypeCategory.Name
                    : null,
                SpendTypeCategoryId = transaction.Annotation != null ? transaction.Annotation.SpendTypeCategoryId : null,
                SpendTypeCategoryName = transaction.Annotation != null && transaction.Annotation.SpendTypeCategory != null
                    ? transaction.Annotation.SpendTypeCategory.Name
                    : null,
                Note = transaction.Annotation != null ? transaction.Annotation.Note : null,
                ContactId = transaction.ContactId,
                ContactName = transaction.Contact != null ? transaction.Contact.DisplayName : null
            })
            .ToListAsync(cancellationToken);

        transactions = transactions.Take(5000).ToList();

        return Ok(transactions);
    }

    [HttpPut("{id:guid}/annotation")]
    public async Task<IActionResult> UpdateAnnotationAsync(
        Guid id,
        [FromBody] UpdateTransactionAnnotationRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .Include(entity => entity.Annotation)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound();
        }

        if (request.TransactionTypeCategoryId.HasValue)
        {
            var categoryExists = await dbContext.Categories.AnyAsync(category =>
                category.Id == request.TransactionTypeCategoryId &&
                category.CategoryType == CategoryType.TransactionType, cancellationToken);

            if (!categoryExists)
            {
                return BadRequest(new { error = "Invalid Transaction Type category." });
            }
        }

        if (request.SpendTypeCategoryId.HasValue)
        {
            var categoryExists = await dbContext.Categories.AnyAsync(category =>
                category.Id == request.SpendTypeCategoryId &&
                category.CategoryType == CategoryType.SpendType, cancellationToken);

            if (!categoryExists)
            {
                return BadRequest(new { error = "Invalid Type of spend category." });
            }
        }

        var annotation = transaction.Annotation;
        if (annotation is null)
        {
            annotation = new TransactionAnnotation
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            transaction.Annotation = annotation;
            dbContext.TransactionAnnotations.Add(annotation);
        }

        annotation.TransactionTypeCategoryId = request.TransactionTypeCategoryId;
        annotation.SpendTypeCategoryId = request.SpendTypeCategoryId;
        annotation.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        annotation.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static List<Guid> ParseGuidList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var guid) ? guid : Guid.Empty)
            .Where(value => value != Guid.Empty)
            .Distinct()
            .ToList();
    }

    private static string CsvField(string? value)
    {
        var normalized = value ?? string.Empty;
        var escaped = normalized.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string? MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return null;
        }

        var digits = accountNumber.Where(char.IsDigit).ToList();
        if (digits.Count <= 4)
        {
            return accountNumber;
        }

        var digitsToMask = digits.Count - 4;
        var maskedDigitsSeen = 0;
        var result = new StringBuilder(accountNumber.Length);
        foreach (var character in accountNumber)
        {
            if (!char.IsDigit(character))
            {
                result.Append(character);
                continue;
            }

            if (maskedDigitsSeen < digitsToMask)
            {
                result.Append('*');
                maskedDigitsSeen++;
            }
            else
            {
                result.Append(character);
            }
        }

        return result.ToString();
    }

    private static TimeZoneInfo ResolveAucklandTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
