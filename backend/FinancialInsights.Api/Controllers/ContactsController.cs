using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Enums;
using FinancialInsights.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/contacts")]
public sealed class ContactsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContactResponse>>> GetContactsAsync(
        [FromQuery] string? accountIds,
        CancellationToken cancellationToken)
    {
        var applyAccountFilter = HttpContext.Request.Query.ContainsKey("accountIds");
        var accountIdList = ParseGuidList(accountIds);
        var sourceContacts = await dbContext.Contacts
            .AsNoTracking()
            .Include(contact => contact.Transactions)
            .ToListAsync(cancellationToken);

        var contacts = sourceContacts
            .Select(contact =>
            {
                var transactions = accountIdList.Count > 0
                    ? contact.Transactions.Where(transaction => accountIdList.Contains(transaction.AccountId)).ToList()
                    : (applyAccountFilter ? [] : contact.Transactions);

                return new ContactResponse
                {
                    Id = contact.Id,
                    DisplayName = contact.DisplayName,
                    Confidence = contact.Confidence,
                    TransactionCount = transactions.Count,
                    MoneyIn = transactions.Where(transaction => transaction.Direction == MoneyDirection.In)
                        .Sum(transaction => Math.Abs(transaction.Amount)),
                    MoneyOut = transactions.Where(transaction => transaction.Direction == MoneyDirection.Out)
                        .Sum(transaction => Math.Abs(transaction.Amount))
                };
            })
            .Where(contact => contact.TransactionCount > 0)
            .OrderBy(contact => contact.DisplayName)
            .ToList();

        return Ok(contacts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDetailResponse>> GetContactDetailAsync(
        Guid id,
        [FromQuery] string? accountIds,
        CancellationToken cancellationToken)
    {
        var applyAccountFilter = HttpContext.Request.Query.ContainsKey("accountIds");
        var accountIdList = ParseGuidList(accountIds);
        var contact = await dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (contact is null)
        {
            return NotFound();
        }

        var monthly = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.ContactId == id &&
                                  (accountIdList.Count > 0
                                      ? accountIdList.Contains(transaction.AccountId)
                                      : !applyAccountFilter))
            .GroupBy(transaction => new { transaction.TransactionDateUtc.Year, transaction.TransactionDateUtc.Month })
            .Select(group => new ContactMonthlyCashflowResponse
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                MoneyIn = group.Where(transaction => transaction.Direction == MoneyDirection.In).Sum(transaction => Math.Abs(transaction.Amount)),
                MoneyOut = group.Where(transaction => transaction.Direction == MoneyDirection.Out).Sum(transaction => Math.Abs(transaction.Amount))
            })
            .OrderBy(row => row.Year)
            .ThenBy(row => row.Month)
            .ToListAsync(cancellationToken);

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Account)
            .Where(transaction => transaction.ContactId == id &&
                                  (accountIdList.Count > 0
                                      ? accountIdList.Contains(transaction.AccountId)
                                      : !applyAccountFilter))
            .OrderByDescending(transaction => transaction.TransactionDateUtc)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .Take(500)
            .Select(transaction => new ContactTransactionResponse
            {
                TransactionId = transaction.Id,
                TransactionDateUtc = transaction.TransactionDateUtc,
                Description = transaction.Description,
                AccountName = transaction.Account.Name,
                Amount = transaction.Amount,
                Direction = transaction.Direction == MoneyDirection.In ? "In" : "Out"
            })
            .ToListAsync(cancellationToken);

        return Ok(new ContactDetailResponse
        {
            Id = contact.Id,
            DisplayName = contact.DisplayName,
            Confidence = contact.Confidence,
            MonthlyCashflow = monthly,
            Transactions = transactions
        });
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
}
