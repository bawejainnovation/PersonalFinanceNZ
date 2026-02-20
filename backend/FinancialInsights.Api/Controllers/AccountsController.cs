using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.DTOs;
using FinancialInsights.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(
    AppDbContext dbContext,
    INzBankCatalogService bankCatalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Include(account => account.Profile)
            .OrderBy(account => account.Name)
            .Select(account => new AccountResponse
            {
                Id = account.Id,
                AkahuAccountId = account.AkahuAccountId,
                Name = account.Name,
                InstitutionName = account.InstitutionName,
                AccountNumber = account.AccountNumber,
                Currency = account.Currency,
                CurrentBalance = account.CurrentBalance,
                NzBankKey = account.Profile != null ? account.Profile.NzBankKey : null,
                CustomDescription = account.Profile != null ? account.Profile.CustomDescription : null,
                LastSyncedAtUtc = account.LastSyncedAtUtc,
                TransactionCount = account.Transactions.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(accounts);
    }

    [HttpPut("{id:guid}/profile")]
    public async Task<ActionResult<AccountResponse>> UpdateProfileAsync(
        Guid id,
        [FromBody] UpdateAccountProfileRequest request,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .Include(entity => entity.Profile)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (account is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.NzBankKey) && !bankCatalogService.Exists(request.NzBankKey))
        {
            return BadRequest(new { error = "Selected bank key is invalid." });
        }

        var profile = account.Profile;
        if (profile is null)
        {
            profile = new AccountProfile
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.AccountProfiles.Add(profile);
            account.Profile = profile;
        }

        profile.NzBankKey = string.IsNullOrWhiteSpace(request.NzBankKey)
            ? null
            : request.NzBankKey.Trim().ToLowerInvariant();
        profile.CustomDescription = string.IsNullOrWhiteSpace(request.CustomDescription)
            ? null
            : request.CustomDescription.Trim();
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AccountResponse
        {
            Id = account.Id,
            AkahuAccountId = account.AkahuAccountId,
            Name = account.Name,
            InstitutionName = account.InstitutionName,
            AccountNumber = account.AccountNumber,
            Currency = account.Currency,
            CurrentBalance = account.CurrentBalance,
            NzBankKey = profile.NzBankKey,
            CustomDescription = profile.CustomDescription,
            LastSyncedAtUtc = account.LastSyncedAtUtc,
            TransactionCount = await dbContext.Transactions.CountAsync(
                transaction => transaction.AccountId == account.Id,
                cancellationToken)
        });
    }

    [HttpPut("profiles")]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> UpdateProfilesBulkAsync(
        [FromBody] UpdateAccountProfilesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest(new { error = "At least one account profile update is required." });
        }

        var accountIds = request.Items.Select(item => item.AccountId).Distinct().ToList();
        var accounts = await dbContext.Accounts
            .Include(account => account.Profile)
            .Where(account => accountIds.Contains(account.Id))
            .ToListAsync(cancellationToken);

        if (accounts.Count != accountIds.Count)
        {
            return BadRequest(new { error = "One or more account ids were not found." });
        }

        foreach (var patch in request.Items)
        {
            if (!string.IsNullOrWhiteSpace(patch.NzBankKey) && !bankCatalogService.Exists(patch.NzBankKey))
            {
                return BadRequest(new { error = $"Selected bank key is invalid for account {patch.AccountId}." });
            }

            var account = accounts.First(item => item.Id == patch.AccountId);
            var profile = account.Profile;
            if (profile is null)
            {
                profile = new AccountProfile
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    CreatedAtUtc = DateTime.UtcNow
                };
                dbContext.AccountProfiles.Add(profile);
                account.Profile = profile;
            }

            profile.NzBankKey = string.IsNullOrWhiteSpace(patch.NzBankKey)
                ? null
                : patch.NzBankKey.Trim().ToLowerInvariant();
            profile.CustomDescription = string.IsNullOrWhiteSpace(patch.CustomDescription)
                ? null
                : patch.CustomDescription.Trim();
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var transactionCounts = await dbContext.Transactions
            .Where(transaction => accountIds.Contains(transaction.AccountId))
            .GroupBy(transaction => transaction.AccountId)
            .Select(group => new { AccountId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.AccountId, item => item.Count, cancellationToken);

        var response = accounts
            .OrderBy(account => account.Name)
            .Select(account => new AccountResponse
            {
                Id = account.Id,
                AkahuAccountId = account.AkahuAccountId,
                Name = account.Name,
                InstitutionName = account.InstitutionName,
                AccountNumber = account.AccountNumber,
                Currency = account.Currency,
                CurrentBalance = account.CurrentBalance,
                NzBankKey = account.Profile?.NzBankKey,
                CustomDescription = account.Profile?.CustomDescription,
                LastSyncedAtUtc = account.LastSyncedAtUtc,
                TransactionCount = transactionCounts.GetValueOrDefault(account.Id)
            })
            .ToList();

        return Ok(response);
    }
}
