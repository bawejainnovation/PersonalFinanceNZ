using System.Net.Http.Json;
using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.Domain.Enums;
using FinancialInsights.Api.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialInsights.Api.Tests.Integration;

public sealed class ExperimentalTransferReadApiTests(FinancialInsightsWebApplicationFactory factory) : IClassFixture<FinancialInsightsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task TransactionsFeed_ShouldClassifyTransfersOnRead_AndFilterWhenExcluded()
    {
        var (accountA, accountB) = await SeedTransferScenarioAsync(factory.Services);
        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20));
        var toDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var all = await _client.GetFromJsonAsync<List<TransactionResponse>>(
            $"/api/transactions?accountIds={accountA},{accountB}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&direction=all&includeBankTransfers=true&experimentalTransferMatching=true");

        all.Should().NotBeNull();
        all!.Count.Should().Be(4);
        all.Count(transaction => transaction.IsBankTransfer).Should().Be(2);

        var filtered = await _client.GetFromJsonAsync<List<TransactionResponse>>(
            $"/api/transactions?accountIds={accountA},{accountB}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&direction=all&includeBankTransfers=false&experimentalTransferMatching=true");

        filtered.Should().NotBeNull();
        filtered!.Count.Should().Be(2);
        filtered.Any(transaction => transaction.IsBankTransfer).Should().BeFalse();

        var partialWindow = await _client.GetFromJsonAsync<List<TransactionResponse>>(
            $"/api/transactions?accountIds={accountA},{accountB}&fromDate={DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)):yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&direction=all&includeBankTransfers=true&experimentalTransferMatching=true");

        partialWindow.Should().NotBeNull();
        partialWindow!.Any(transaction => transaction.Description == "Transfer candidate in" && transaction.IsBankTransfer).Should().BeTrue();
    }

    [Fact]
    public async Task Analytics_ShouldExcludeReadTimeTransfers_WhenExperimentalModeEnabled()
    {
        var (accountA, accountB) = await SeedTransferScenarioAsync(factory.Services);
        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20));
        var toDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var withoutExclusion = await _client.GetFromJsonAsync<List<CategoryCashflowResponse>>(
            $"/api/analytics/category-cashflow?categoryType=TransactionType&accountIds={accountA},{accountB}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&experimentalTransferMatching=false");

        withoutExclusion.Should().NotBeNull();
        withoutExclusion!.Should().ContainSingle();
        withoutExclusion[0].MoneyIn.Should().Be(180m);
        withoutExclusion[0].MoneyOut.Should().Be(140m);

        var withExclusion = await _client.GetFromJsonAsync<List<CategoryCashflowResponse>>(
            $"/api/analytics/category-cashflow?categoryType=TransactionType&accountIds={accountA},{accountB}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&experimentalTransferMatching=true");

        withExclusion.Should().NotBeNull();
        withExclusion!.Should().ContainSingle();
        withExclusion[0].MoneyIn.Should().Be(80m);
        withExclusion[0].MoneyOut.Should().Be(40m);
    }

    private static async Task<(Guid AccountA, Guid AccountB)> SeedTransferScenarioAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.TransactionAnnotations.RemoveRange(db.TransactionAnnotations);
        db.Transactions.RemoveRange(db.Transactions);
        db.AccountProfiles.RemoveRange(db.AccountProfiles);
        db.Accounts.RemoveRange(db.Accounts);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var accountA = new Account
        {
            Id = Guid.NewGuid(),
            AkahuAccountId = "acc_exp_a",
            Name = "Exp A",
            Currency = "NZD",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var accountB = new Account
        {
            Id = Guid.NewGuid(),
            AkahuAccountId = "acc_exp_b",
            Name = "Exp B",
            Currency = "NZD",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var transferOut = new Transaction
        {
            Id = Guid.NewGuid(),
            AkahuTransactionId = "txn_transfer_out",
            AccountId = accountA.Id,
            Amount = -100m,
            Direction = MoneyDirection.Out,
            Description = "Transfer candidate out",
            TransactionDateUtc = now.AddDays(-6),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var transferIn = new Transaction
        {
            Id = Guid.NewGuid(),
            AkahuTransactionId = "txn_transfer_in",
            AccountId = accountB.Id,
            Amount = 100m,
            Direction = MoneyDirection.In,
            Description = "Transfer candidate in",
            TransactionDateUtc = now.AddDays(-3),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var normalOut = new Transaction
        {
            Id = Guid.NewGuid(),
            AkahuTransactionId = "txn_normal_out",
            AccountId = accountA.Id,
            Amount = -40m,
            Direction = MoneyDirection.Out,
            Description = "Card payment",
            TransactionDateUtc = now.AddDays(-2),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var normalIn = new Transaction
        {
            Id = Guid.NewGuid(),
            AkahuTransactionId = "txn_normal_in",
            AccountId = accountB.Id,
            Amount = 80m,
            Direction = MoneyDirection.In,
            Description = "Salary part",
            TransactionDateUtc = now.AddDays(-1),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Accounts.AddRange(accountA, accountB);
        db.Transactions.AddRange(transferOut, transferIn, normalOut, normalIn);
        await db.SaveChangesAsync();

        return (accountA.Id, accountB.Id);
    }
}
