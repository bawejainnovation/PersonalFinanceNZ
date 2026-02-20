using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/dev")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class DevSeedController(
    AppDbContext dbContext,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<IActionResult> SeedAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        if (await dbContext.Accounts.AnyAsync(cancellationToken))
        {
            return Ok(new { status = "already-seeded" });
        }

        var now = DateTime.UtcNow;

        var accountOne = new Account
        {
            Id = Guid.NewGuid(),
            AkahuAccountId = "acc_demo_1",
            Name = "Daily Account",
            InstitutionName = "ANZ New Zealand",
            Currency = "NZD",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            LastSyncedAtUtc = now,
            Profile = new AccountProfile
            {
                Id = Guid.NewGuid(),
                NzBankKey = "anz",
                CustomDescription = "Primary spending",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        var accountTwo = new Account
        {
            Id = Guid.NewGuid(),
            AkahuAccountId = "acc_demo_2",
            Name = "Savings Account",
            InstitutionName = "ASB Bank",
            Currency = "NZD",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            LastSyncedAtUtc = now,
            Profile = new AccountProfile
            {
                Id = Guid.NewGuid(),
                NzBankKey = "asb",
                CustomDescription = "Emergency savings",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        dbContext.Accounts.AddRange(accountOne, accountTwo);

        var salaryCategory = new Category
        {
            Id = Guid.NewGuid(),
            CategoryType = CategoryType.TransactionType,
            Name = "Salary",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var groceryCategory = new Category
        {
            Id = Guid.NewGuid(),
            CategoryType = CategoryType.TransactionType,
            Name = "Groceries",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var unavoidableCategory = new Category
        {
            Id = Guid.NewGuid(),
            CategoryType = CategoryType.SpendType,
            Name = "Unavoidable",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var leisureCategory = new Category
        {
            Id = Guid.NewGuid(),
            CategoryType = CategoryType.SpendType,
            Name = "Leisure",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Categories.AddRange(salaryCategory, groceryCategory, unavoidableCategory, leisureCategory);

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AkahuTransactionId = "txn_demo_1",
                AccountId = accountOne.Id,
                Amount = 3500m,
                Direction = MoneyDirection.In,
                Description = "Monthly salary",
                TransactionType = "deposit",
                TransactionDateUtc = now.AddDays(-10),
                IsBankTransfer = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                AkahuTransactionId = "txn_demo_2",
                AccountId = accountOne.Id,
                Amount = -127.40m,
                Direction = MoneyDirection.Out,
                Description = "Supermarket",
                TransactionType = "card",
                TransactionDateUtc = now.AddDays(-8),
                IsBankTransfer = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                AkahuTransactionId = "txn_demo_3",
                AccountId = accountOne.Id,
                Amount = -500m,
                Direction = MoneyDirection.Out,
                Description = "Transfer to savings",
                TransactionType = "transfer",
                TransactionDateUtc = now.AddDays(-5),
                IsBankTransfer = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                AkahuTransactionId = "txn_demo_4",
                AccountId = accountTwo.Id,
                Amount = 500m,
                Direction = MoneyDirection.In,
                Description = "Transfer from daily",
                TransactionType = "transfer",
                TransactionDateUtc = now.AddDays(-5),
                IsBankTransfer = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        dbContext.Transactions.AddRange(transactions);

        dbContext.TransactionAnnotations.AddRange(
            new TransactionAnnotation
            {
                Id = Guid.NewGuid(),
                TransactionId = transactions[0].Id,
                TransactionTypeCategoryId = salaryCategory.Id,
                SpendTypeCategoryId = unavoidableCategory.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new TransactionAnnotation
            {
                Id = Guid.NewGuid(),
                TransactionId = transactions[1].Id,
                TransactionTypeCategoryId = groceryCategory.Id,
                SpendTypeCategoryId = unavoidableCategory.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "seeded" });
    }
}
