using AccountService.Domain.Entities;
using AccountService.Domain.Enums;
using AccountService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace AccountService.UnitTests.Domain;

public class AccountTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateActiveAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var account = Account.Create(userId, "BRL");

        // Assert
        account.UserId.Should().Be(userId);
        account.Balance.Should().Be(0m);
        account.Currency.Should().Be("BRL");
        account.Status.Should().Be(AccountStatus.Active);
        account.Id.Should().NotBeEmpty();
        account.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Credit_WithPositiveAmount_ShouldIncreaseBalance()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), "BRL");

        // Act
        account.Credit(500m);

        // Assert
        account.Balance.Should().Be(500m);
    }

    [Fact]
    public void Debit_WithSufficientBalance_ShouldDecreaseBalance()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), "BRL");
        account.Credit(1000m);

        // Act
        account.Debit(300m);

        // Assert
        account.Balance.Should().Be(700m);
    }

    [Fact]
    public void Debit_WithInsufficientBalance_ShouldThrowDomainException()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), "BRL");
        account.Credit(100m);

        // Act
        var act = () => account.Debit(200m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Insufficient balance*");
    }

    [Fact]
    public void Credit_ToFrozenAccount_ShouldThrowDomainException()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), "BRL");
        account.Freeze();

        // Act
        var act = () => account.Credit(100m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*inactive account*");
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        var act = () => Account.Create(Guid.Empty, "BRL");
        act.Should().Throw<ArgumentException>();
    }
}
