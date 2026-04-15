using AccountService.Domain.Enums;
using AccountService.Domain.Exceptions;

namespace AccountService.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public DateTime CreatedAt { get; private set; }
    public AccountStatus Status { get; private set; }

    private Account() { }

    public static Account Create(Guid userId, string currency = "BRL")
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 0m,
            Currency = currency.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            Status = AccountStatus.Active
        };
    }

    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Credit amount must be positive.");

        if (Status != AccountStatus.Active)
            throw new DomainException("Cannot credit an inactive account.");

        Balance += amount;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Debit amount must be positive.");

        if (Status != AccountStatus.Active)
            throw new DomainException("Cannot debit an inactive account.");

        if (Balance < amount)
            throw new DomainException($"Insufficient balance. Available: {Balance}, Requested: {amount}.");

        Balance -= amount;
    }

    public void Freeze() => Status = AccountStatus.Frozen;
    public void Close() => Status = AccountStatus.Closed;
    public void Activate() => Status = AccountStatus.Active;
}
