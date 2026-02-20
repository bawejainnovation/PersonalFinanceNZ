using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.Domain.Enums;
using FinancialInsights.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Services;

public sealed class SyncService(
    AppDbContext dbContext,
    IAkahuClient akahuClient,
    IContactResolutionService contactResolutionService,
    ILogger<SyncService> logger) : ISyncService
{
    public async Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = ResolveWindow(request);

        var accounts = await akahuClient.GetAccountsAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;

        var accountMap = await dbContext.Accounts
            .AsTracking()
            .ToDictionaryAsync(account => account.AkahuAccountId, cancellationToken);

        foreach (var sourceAccount in accounts)
        {
            if (!accountMap.TryGetValue(sourceAccount.Id, out var targetAccount))
            {
                targetAccount = new Account
                {
                    Id = Guid.NewGuid(),
                    AkahuAccountId = sourceAccount.Id,
                    CreatedAtUtc = nowUtc
                };
                dbContext.Accounts.Add(targetAccount);
                accountMap[sourceAccount.Id] = targetAccount;
            }

            targetAccount.Name = sourceAccount.Name;
            targetAccount.InstitutionName = sourceAccount.InstitutionName;
            targetAccount.AccountNumber = sourceAccount.AccountNumber;
            targetAccount.CurrentBalance = sourceAccount.CurrentBalance;
            targetAccount.Currency = sourceAccount.Currency;
            targetAccount.UpdatedAtUtc = nowUtc;
            targetAccount.LastSyncedAtUtc = nowUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var existingTransactions = await dbContext.Transactions
            .AsTracking()
            .Where(transaction => transaction.TransactionDateUtc >= fromUtc.AddDays(-2) && transaction.TransactionDateUtc <= toUtc.AddDays(2))
            .ToDictionaryAsync(transaction => transaction.AkahuTransactionId, cancellationToken);

        var transactionsSynced = 0;
        foreach (var account in accounts)
        {
            var accountTransactions = await akahuClient.GetTransactionsAsync(account.Id, fromUtc, toUtc, cancellationToken);
            foreach (var sourceTransaction in accountTransactions)
            {
                if (!accountMap.TryGetValue(sourceTransaction.AccountId, out var mappedAccount))
                {
                    continue;
                }

                if (!existingTransactions.TryGetValue(sourceTransaction.Id, out var targetTransaction))
                {
                    targetTransaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        AkahuTransactionId = sourceTransaction.Id,
                        AccountId = mappedAccount.Id,
                        CreatedAtUtc = nowUtc
                    };
                    dbContext.Transactions.Add(targetTransaction);
                    existingTransactions[sourceTransaction.Id] = targetTransaction;
                }

                targetTransaction.AccountId = mappedAccount.Id;
                targetTransaction.Amount = sourceTransaction.Amount;
                targetTransaction.Direction = sourceTransaction.Amount >= 0 ? MoneyDirection.In : MoneyDirection.Out;
                targetTransaction.Description = sourceTransaction.Description;
                targetTransaction.MerchantName = sourceTransaction.MerchantName;
                targetTransaction.Reference = sourceTransaction.Reference;
                targetTransaction.TransactionType = sourceTransaction.TransactionType;
                targetTransaction.TransactionDateUtc = sourceTransaction.DateUtc;
                targetTransaction.UpdatedAtUtc = nowUtc;

                transactionsSynced++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var classificationCandidates = await dbContext.Transactions
            .AsTracking()
            .Where(transaction => transaction.TransactionDateUtc >= fromUtc.AddDays(-2) && transaction.TransactionDateUtc <= toUtc.AddDays(2))
            .ToListAsync(cancellationToken);

        await contactResolutionService.ResolveContactsAsync(classificationCandidates, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Sync completed. Accounts: {AccountsSynced}, Transactions: {TransactionsSynced}, Window: {FromUtc} -> {ToUtc}",
            accounts.Count,
            transactionsSynced,
            fromUtc,
            toUtc);

        return new SyncResponse
        {
            AccountsSynced = accounts.Count,
            TransactionsSynced = transactionsSynced,
            FromDateUtc = fromUtc,
            ToDateUtc = toUtc
        };
    }

    public static (DateTime FromUtc, DateTime ToUtc) ResolveWindow(SyncRequest request)
    {
        if (request.FromDate.HasValue || request.ToDate.HasValue)
        {
            if (!request.FromDate.HasValue || !request.ToDate.HasValue)
            {
                throw new ArgumentException("Both fromDate and toDate must be supplied together.");
            }

            var fromDate = DateTime.SpecifyKind(request.FromDate.Value.Date, DateTimeKind.Utc);
            var toDate = DateTime.SpecifyKind(request.ToDate.Value.Date, DateTimeKind.Utc);
            if (fromDate > toDate)
            {
                throw new ArgumentException("fromDate cannot be after toDate.");
            }

            if ((toDate - fromDate).TotalDays > 730)
            {
                throw new ArgumentException("The maximum sync range is 24 months.");
            }

            return (fromDate, toDate);
        }

        var monthsBack = request.MonthsBack ?? 6;
        if (monthsBack is < 1 or > 24)
        {
            throw new ArgumentException("monthsBack must be between 1 and 24.");
        }

        var toUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var fromUtc = toUtc.AddMonths(-monthsBack);
        return (fromUtc, toUtc);
    }
}
