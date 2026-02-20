using System.Net;
using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialInsights.Api.Tests.Integration;

public sealed class TransactionCsvExportApiTests(FinancialInsightsWebApplicationFactory factory) : IClassFixture<FinancialInsightsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ExportCsv_ShouldReturnExpectedColumns_AndMaskedAccountNumber()
    {
        await SeedAsync(factory.Services);

        var response = await _client.GetAsync("/api/transactions/export/csv");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("transaction_date_utc,transaction_date_local,amount,direction,is_bank_transfer,account_name,account_description,institution_name,account_number_masked,description,merchant_name,reference,transaction_type_raw,contact_name,transaction_type_category,spend_type_category,note");
        payload.Should().Contain("Coffee beans");
        payload.Should().Contain("**-****-*****00-00");
        payload.Should().NotContain("01-1234-5678900-00");
    }

    private static async Task SeedAsync(IServiceProvider services)
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
        db.Categories.RemoveRange(db.Categories);
        db.ContactAliases.RemoveRange(db.ContactAliases);
        db.Contacts.RemoveRange(db.Contacts);
        db.AccountProfiles.RemoveRange(db.AccountProfiles);
        db.Accounts.RemoveRange(db.Accounts);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AkahuAccountId = "acc_csv_1",
            Name = "Everyday Account",
            InstitutionName = "ANZ",
            AccountNumber = "01-1234-5678900-00",
            Currency = "NZD",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Profile = new AccountProfile
            {
                Id = Guid.NewGuid(),
                CustomDescription = "Main spending",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            CanonicalKey = "name:coffee-roaster",
            DisplayName = "Coffee Roaster",
            Confidence = "high",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var transactionTypeCategory = new Category
        {
            Id = Guid.NewGuid(),
            CategoryType = CategoryType.TransactionType,
            Name = "Food",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var spendTypeCategory = new Category
        {
            Id = Guid.NewGuid(),
            CategoryType = CategoryType.SpendType,
            Name = "Leisure",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AkahuTransactionId = "txn_csv_1",
            AccountId = account.Id,
            ContactId = contact.Id,
            Amount = -25.50m,
            Direction = MoneyDirection.Out,
            Description = "Coffee beans",
            MerchantName = "Acme Roasters",
            Reference = "INV123",
            TransactionType = "card",
            TransactionDateUtc = new DateTime(2025, 10, 21, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var annotation = new TransactionAnnotation
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            TransactionTypeCategoryId = transactionTypeCategory.Id,
            SpendTypeCategoryId = spendTypeCategory.Id,
            Note = "Weekly coffee",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Accounts.Add(account);
        db.Contacts.Add(contact);
        db.Categories.AddRange(transactionTypeCategory, spendTypeCategory);
        db.Transactions.Add(transaction);
        db.TransactionAnnotations.Add(annotation);
        await db.SaveChangesAsync();
    }
}
