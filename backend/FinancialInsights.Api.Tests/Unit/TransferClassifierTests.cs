using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.Services;
using FluentAssertions;

namespace FinancialInsights.Api.Tests.Unit;

public sealed class TransferClassifierTests
{
    [Fact]
    public void Classify_ShouldMarkMatchingOppositeTransactionsAsBankTransfer()
    {
        var classifier = new TransferClassifier();

        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        var source = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountA,
            Amount = -100m,
            Description = "Transfer to savings",
            TransactionDateUtc = new DateTime(2025, 5, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var destination = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountB,
            Amount = 100m,
            Description = "Transfer from spending",
            TransactionDateUtc = new DateTime(2025, 5, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        classifier.Classify([source, destination]);

        source.IsBankTransfer.Should().BeTrue();
        destination.IsBankTransfer.Should().BeTrue();
    }

    [Fact]
    public void Classify_ShouldNotMarkDifferentAmountTransactions()
    {
        var classifier = new TransferClassifier();

        var source = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = -120m,
            Description = "Card payment",
            TransactionDateUtc = new DateTime(2025, 5, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var destination = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            Description = "Salary",
            TransactionDateUtc = new DateTime(2025, 5, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        classifier.Classify([source, destination]);

        source.IsBankTransfer.Should().BeFalse();
        destination.IsBankTransfer.Should().BeFalse();
    }
}
