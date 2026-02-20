using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Enums;
using FinancialInsights.Api.DTOs;
using FinancialInsights.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController(
    AppDbContext dbContext,
    ITransferMatchingService transferMatchingService) : ControllerBase
{
    [HttpGet("category-cashflow")]
    public async Task<ActionResult<IReadOnlyList<CategoryCashflowResponse>>> GetCategoryCashflowAsync(
        [FromQuery] CategoryType categoryType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? accountIds,
        [FromQuery] string? excludedAccountIds,
        [FromQuery] string? contactIds,
        [FromQuery] bool experimentalTransferMatching,
        CancellationToken cancellationToken)
    {
        var query = BuildTransactionQuery(fromDate, toDate, accountIds, excludedAccountIds, contactIds);

        var rows = await query
            .Select(transaction => new
            {
                transaction.Id,
                transaction.AccountId,
                transaction.Amount,
                transaction.Direction,
                transaction.TransactionDateUtc,
                CategoryId = categoryType == CategoryType.TransactionType
                    ? transaction.Annotation!.TransactionTypeCategoryId
                    : transaction.Annotation!.SpendTypeCategoryId,
                CategoryName = categoryType == CategoryType.TransactionType
                    ? (transaction.Annotation != null && transaction.Annotation.TransactionTypeCategory != null
                        ? transaction.Annotation.TransactionTypeCategory.Name
                        : "Unclassified")
                    : (transaction.Annotation != null && transaction.Annotation.SpendTypeCategory != null
                        ? transaction.Annotation.SpendTypeCategory.Name
                        : "Unclassified")
            })
            .ToListAsync(cancellationToken);

        if (experimentalTransferMatching)
        {
            var transferIds = await GetExpandedTransferIdsAsync(fromDate, toDate, accountIds, excludedAccountIds, cancellationToken);

            rows = rows.Where(row => !transferIds.Contains(row.Id)).ToList();
        }

        var result = rows
            .GroupBy(row => new { row.CategoryId, row.CategoryName })
            .Select(group => new CategoryCashflowResponse
            {
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.CategoryName,
                CategoryType = categoryType,
                MoneyIn = group.Where(item => item.Direction == MoneyDirection.In).Sum(item => Math.Abs(item.Amount)),
                MoneyOut = group.Where(item => item.Direction == MoneyDirection.Out).Sum(item => Math.Abs(item.Amount))
            })
            .OrderByDescending(row => Math.Abs(row.Net))
            .ToList();

        return Ok(result);
    }

    [HttpGet("monthly-overview")]
    public async Task<ActionResult<IReadOnlyList<MonthlyCategoryOverviewResponse>>> GetMonthlyOverviewAsync(
        [FromQuery] CategoryType categoryType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? accountIds,
        [FromQuery] string? excludedAccountIds,
        [FromQuery] string? contactIds,
        [FromQuery] bool experimentalTransferMatching,
        CancellationToken cancellationToken)
    {
        var query = BuildTransactionQuery(fromDate, toDate, accountIds, excludedAccountIds, contactIds);

        var rows = await query
            .Select(transaction => new
            {
                transaction.Id,
                transaction.AccountId,
                transaction.Amount,
                transaction.Direction,
                transaction.TransactionDateUtc,
                Year = transaction.TransactionDateUtc.Year,
                Month = transaction.TransactionDateUtc.Month,
                CategoryId = categoryType == CategoryType.TransactionType
                    ? transaction.Annotation!.TransactionTypeCategoryId
                    : transaction.Annotation!.SpendTypeCategoryId,
                CategoryName = categoryType == CategoryType.TransactionType
                    ? (transaction.Annotation != null && transaction.Annotation.TransactionTypeCategory != null
                        ? transaction.Annotation.TransactionTypeCategory.Name
                        : "Unclassified")
                    : (transaction.Annotation != null && transaction.Annotation.SpendTypeCategory != null
                        ? transaction.Annotation.SpendTypeCategory.Name
                        : "Unclassified")
            })
            .ToListAsync(cancellationToken);

        if (experimentalTransferMatching)
        {
            var transferIds = await GetExpandedTransferIdsAsync(fromDate, toDate, accountIds, excludedAccountIds, cancellationToken);

            rows = rows.Where(row => !transferIds.Contains(row.Id)).ToList();
        }

        var result = rows
            .GroupBy(row => new { row.Year, row.Month, row.CategoryId, row.CategoryName })
            .Select(group => new MonthlyCategoryOverviewResponse
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.CategoryName,
                CategoryType = categoryType,
                MoneyIn = group.Where(item => item.Direction == MoneyDirection.In).Sum(item => Math.Abs(item.Amount)),
                MoneyOut = group.Where(item => item.Direction == MoneyDirection.Out).Sum(item => Math.Abs(item.Amount))
            })
            .OrderBy(row => row.Year)
            .ThenBy(row => row.Month)
            .ThenBy(row => row.CategoryName)
            .ToList();

        return Ok(result);
    }

    [HttpGet("account-balances")]
    public async Task<ActionResult<IReadOnlyList<AccountBalanceResponse>>> GetAccountBalancesAsync(
        [FromQuery] string? accountIds,
        [FromQuery] string? excludedAccountIds,
        CancellationToken cancellationToken)
    {
        var includeAccounts = ParseGuidList(accountIds);
        var excludeAccounts = ParseGuidList(excludedAccountIds);

        var query = dbContext.Accounts.AsNoTracking().AsQueryable();
        if (includeAccounts.Count > 0)
        {
            query = query.Where(account => includeAccounts.Contains(account.Id));
        }

        if (excludeAccounts.Count > 0)
        {
            query = query.Where(account => !excludeAccounts.Contains(account.Id));
        }

        var result = await query
            .OrderBy(account => account.Name)
            .Select(account => new AccountBalanceResponse
            {
                AccountId = account.Id,
                AccountName = account.Name,
                AccountNumber = account.AccountNumber,
                CurrentBalance = account.CurrentBalance,
                Currency = account.Currency
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    private IQueryable<Domain.Entities.Transaction> BuildTransactionQuery(
        DateTime? fromDate,
        DateTime? toDate,
        string? accountIds,
        string? excludedAccountIds,
        string? contactIds)
    {
        var includeAccounts = ParseGuidList(accountIds);
        var excludeAccounts = ParseGuidList(excludedAccountIds);
        var contacts = ParseGuidList(contactIds);

        var query = dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Annotation)
            .ThenInclude(annotation => annotation!.TransactionTypeCategory)
            .Include(transaction => transaction.Annotation)
            .ThenInclude(annotation => annotation!.SpendTypeCategory)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(transaction => transaction.TransactionDateUtc >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(toDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(transaction => transaction.TransactionDateUtc <= toUtc);
        }

        if (includeAccounts.Count > 0)
        {
            query = query.Where(transaction => includeAccounts.Contains(transaction.AccountId));
        }

        if (excludeAccounts.Count > 0)
        {
            query = query.Where(transaction => !excludeAccounts.Contains(transaction.AccountId));
        }

        if (contacts.Count > 0)
        {
            query = query.Where(transaction => transaction.ContactId.HasValue && contacts.Contains(transaction.ContactId.Value));
        }

        return query;
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

    private async Task<IReadOnlySet<Guid>> GetExpandedTransferIdsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? accountIds,
        string? excludedAccountIds,
        CancellationToken cancellationToken)
    {
        var includeAccounts = ParseGuidList(accountIds);
        var excludeAccounts = ParseGuidList(excludedAccountIds);

        var candidateQuery = dbContext.Transactions
            .AsNoTracking()
            .AsQueryable();

        if (includeAccounts.Count > 0)
        {
            candidateQuery = candidateQuery.Where(transaction => includeAccounts.Contains(transaction.AccountId));
        }

        if (excludeAccounts.Count > 0)
        {
            candidateQuery = candidateQuery.Where(transaction => !excludeAccounts.Contains(transaction.AccountId));
        }

        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc).AddDays(-4);
            candidateQuery = candidateQuery.Where(transaction => transaction.TransactionDateUtc >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(toDate.Value.Date, DateTimeKind.Utc).AddDays(4);
            candidateQuery = candidateQuery.Where(transaction => transaction.TransactionDateUtc <= toUtc);
        }

        var candidates = await candidateQuery
            .Select(transaction => new TransferMatchCandidate(
                transaction.Id,
                transaction.AccountId,
                transaction.Amount,
                transaction.TransactionDateUtc))
            .ToListAsync(cancellationToken);

        return transferMatchingService.FindMatchedTransferIds(candidates);
    }
}
