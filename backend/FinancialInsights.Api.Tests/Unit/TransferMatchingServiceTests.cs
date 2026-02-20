using FinancialInsights.Api.Services;
using FluentAssertions;

namespace FinancialInsights.Api.Tests.Unit;

public sealed class TransferMatchingServiceTests
{
    [Fact]
    public void FindMatchedTransferIds_ShouldMatchOppositeSignsSameAmountWithinFourDays()
    {
        var service = new TransferMatchingService();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var matches = service.FindMatchedTransferIds(
        [
            new TransferMatchCandidate(Guid.Parse("11111111-1111-1111-1111-111111111111"), accountA, -125m, now),
            new TransferMatchCandidate(Guid.Parse("22222222-2222-2222-2222-222222222222"), accountB, 125m, now.AddDays(4))
        ]);

        matches.Should().BeEquivalentTo(
        [
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222")
        ]);
    }

    [Fact]
    public void FindMatchedTransferIds_ShouldNotMatchWhenDateWindowExceedsFourDays()
    {
        var service = new TransferMatchingService();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var matches = service.FindMatchedTransferIds(
        [
            new TransferMatchCandidate(Guid.NewGuid(), accountA, -90m, now),
            new TransferMatchCandidate(Guid.NewGuid(), accountB, 90m, now.AddDays(5))
        ]);

        matches.Should().BeEmpty();
    }

    [Fact]
    public void FindMatchedTransferIds_ShouldNotMatchWhenSignsAreSame()
    {
        var service = new TransferMatchingService();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var matches = service.FindMatchedTransferIds(
        [
            new TransferMatchCandidate(Guid.NewGuid(), accountA, 90m, now),
            new TransferMatchCandidate(Guid.NewGuid(), accountB, 90m, now.AddDays(1))
        ]);

        matches.Should().BeEmpty();
    }
}
