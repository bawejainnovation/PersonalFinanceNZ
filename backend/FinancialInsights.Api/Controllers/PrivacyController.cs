using FinancialInsights.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/privacy")]
public sealed class PrivacyController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("export")]
    public async Task<IActionResult> ExportAsync(CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts.Include(account => account.Profile).AsNoTracking().ToListAsync(cancellationToken);
        var transactions = await dbContext.Transactions.AsNoTracking().ToListAsync(cancellationToken);
        var categories = await dbContext.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var annotations = await dbContext.TransactionAnnotations.AsNoTracking().ToListAsync(cancellationToken);

        return Ok(new
        {
            exportedAtUtc = DateTime.UtcNow,
            accounts,
            transactions,
            categories,
            annotations
        });
    }

    [HttpDelete("purge")]
    public async Task<IActionResult> PurgeAsync(CancellationToken cancellationToken)
    {
        dbContext.TransactionAnnotations.RemoveRange(dbContext.TransactionAnnotations);
        dbContext.Transactions.RemoveRange(dbContext.Transactions);
        dbContext.AccountProfiles.RemoveRange(dbContext.AccountProfiles);
        dbContext.Accounts.RemoveRange(dbContext.Accounts);
        dbContext.Categories.RemoveRange(dbContext.Categories);

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
