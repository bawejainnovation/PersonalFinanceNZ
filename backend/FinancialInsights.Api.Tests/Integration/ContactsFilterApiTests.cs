using System.Net;
using System.Net.Http.Json;
using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.Domain.Enums;
using FinancialInsights.Api.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialInsights.Api.Tests.Integration;

public sealed class ContactsFilterApiTests(FinancialInsightsWebApplicationFactory factory) : IClassFixture<FinancialInsightsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ContactsFilter_ShouldKeepContactWhenOneAccountStillSelected_AndHideWhenNoneSelected()
    {
        var (accountA, accountB, contactId) = await SeedTwoAccountContactAsync(factory.Services);

        var all = await _client.GetFromJsonAsync<List<ContactResponse>>($"/api/contacts?accountIds={accountA},{accountB}");
        all.Should().NotBeNull();
        all!.Should().ContainSingle(contact => contact.Id == contactId);
        all!.Single().TransactionCount.Should().Be(2);

        var onlyA = await _client.GetFromJsonAsync<List<ContactResponse>>($"/api/contacts?accountIds={accountA}");
        onlyA.Should().NotBeNull();
        onlyA!.Should().ContainSingle(contact => contact.Id == contactId);
        onlyA!.Single().TransactionCount.Should().Be(1);
        onlyA!.Single().MoneyOut.Should().Be(100m);

        var detailOnlyA = await _client.GetFromJsonAsync<ContactDetailResponse>($"/api/contacts/{contactId}?accountIds={accountA}");
        detailOnlyA.Should().NotBeNull();
        detailOnlyA!.Transactions.Should().HaveCount(1);
        detailOnlyA.Transactions.Single().AccountName.Should().Be("Account A");

        var none = await _client.GetFromJsonAsync<List<ContactResponse>>("/api/contacts?accountIds=");
        none.Should().NotBeNull();
        none!.Should().BeEmpty();

        var detailNoneResponse = await _client.GetAsync($"/api/contacts/{contactId}?accountIds=");
        detailNoneResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailNone = await detailNoneResponse.Content.ReadFromJsonAsync<ContactDetailResponse>();
        detailNone.Should().NotBeNull();
        detailNone!.Transactions.Should().BeEmpty();
    }

    private static async Task<(Guid AccountA, Guid AccountB, Guid ContactId)> SeedTwoAccountContactAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (!string.Equals(environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Destructive integration test seed is allowed only in Testing environment.");
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.TransactionAnnotations.RemoveRange(db.TransactionAnnotations);
        db.Transactions.RemoveRange(db.Transactions);
        db.ContactAliases.RemoveRange(db.ContactAliases);
        db.Contacts.RemoveRange(db.Contacts);
        db.AccountProfiles.RemoveRange(db.AccountProfiles);
        db.Accounts.RemoveRange(db.Accounts);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        var accountA = new Account
        {
            Id = Guid.NewGuid(),
            AkahuAccountId = "acc_test_a",
            Name = "Account A",
            Currency = "NZD",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var accountB = new Account
        {
            Id = Guid.NewGuid(),
            AkahuAccountId = "acc_test_b",
            Name = "Account B",
            Currency = "NZD",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            CanonicalKey = "name:test-contact",
            DisplayName = "Test Contact",
            Confidence = "medium",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var transactionA = new Transaction
        {
            Id = Guid.NewGuid(),
            AkahuTransactionId = "txn_test_a",
            AccountId = accountA.Id,
            ContactId = contact.Id,
            Amount = -100m,
            Direction = MoneyDirection.Out,
            Description = "A payment",
            TransactionDateUtc = now.AddDays(-2),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var transactionB = new Transaction
        {
            Id = Guid.NewGuid(),
            AkahuTransactionId = "txn_test_b",
            AccountId = accountB.Id,
            ContactId = contact.Id,
            Amount = 70m,
            Direction = MoneyDirection.In,
            Description = "B payment",
            TransactionDateUtc = now.AddDays(-1),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Accounts.AddRange(accountA, accountB);
        db.Contacts.Add(contact);
        db.Transactions.AddRange(transactionA, transactionB);
        await db.SaveChangesAsync();

        return (accountA.Id, accountB.Id, contact.Id);
    }
}
