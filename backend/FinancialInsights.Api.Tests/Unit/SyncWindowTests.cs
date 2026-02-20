using FinancialInsights.Api.DTOs;
using FinancialInsights.Api.Services;
using FluentAssertions;

namespace FinancialInsights.Api.Tests.Unit;

public sealed class SyncWindowTests
{
    [Fact]
    public void ResolveWindow_ShouldUseMonthsBackWhenProvided()
    {
        var request = new SyncRequest { MonthsBack = 3 };

        var (from, to) = SyncService.ResolveWindow(request);

        to.Should().Be(DateTime.UtcNow.Date);
        from.Should().Be(to.AddMonths(-3));
    }

    [Fact]
    public void ResolveWindow_ShouldThrowIfOnlyOneDateProvided()
    {
        var request = new SyncRequest
        {
            FromDate = DateTime.UtcNow.Date.AddDays(-10)
        };

        Action action = () => SyncService.ResolveWindow(request);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*fromDate*toDate*");
    }
}
