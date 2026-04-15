using FluentAssertions;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Enums;
using TransactionService.Domain.Exceptions;
using Xunit;

namespace TransactionService.UnitTests.Domain;

public class TransactionTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreatePendingTransaction()
    {
        // Arrange
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        // Act
        var tx = Transaction.Create(from, to, 100m, "BRL", "Test payment");

        // Assert
        tx.FromAccountId.Should().Be(from);
        tx.ToAccountId.Should().Be(to);
        tx.Amount.Should().Be(100m);
        tx.Currency.Should().Be("BRL");
        tx.Status.Should().Be(TransactionStatus.Pending);
        tx.Description.Should().Be("Test payment");
        tx.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_SameFromAndTo_ShouldThrowDomainException()
    {
        var id = Guid.NewGuid();
        var act = () => Transaction.Create(id, id, 100m, "BRL");
        act.Should().Throw<DomainException>().WithMessage("*must differ*");
    }

    [Fact]
    public void Create_NegativeAmount_ShouldThrow()
    {
        var act = () => Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), -50m, "BRL");
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void Approve_PendingTransaction_ShouldSetApprovedStatus()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "BRL");
        tx.Approve();
        tx.Status.Should().Be(TransactionStatus.Approved);
        tx.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_PendingTransaction_ShouldSetRejectedStatus()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "BRL");
        tx.Reject("High risk");
        tx.Status.Should().Be(TransactionStatus.Rejected);
    }

    [Fact]
    public void Approve_AlreadyApproved_ShouldThrow()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "BRL");
        tx.Approve();
        var act = () => tx.Approve();
        act.Should().Throw<DomainException>();
    }
}
