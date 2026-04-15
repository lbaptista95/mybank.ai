using TransactionService.Domain.Enums;
using TransactionService.Domain.Exceptions;

namespace TransactionService.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid FromAccountId { get; private set; }
    public Guid ToAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public TransactionStatus Status { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string currency,
        string? description = null)
    {
        if (fromAccountId == Guid.Empty) throw new DomainException("FromAccountId cannot be empty.");
        if (toAccountId == Guid.Empty) throw new DomainException("ToAccountId cannot be empty.");
        if (fromAccountId == toAccountId) throw new DomainException("Source and destination accounts must differ.");
        if (amount <= 0) throw new DomainException("Amount must be positive.");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Status = TransactionStatus.Pending,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve()
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException($"Cannot approve a transaction in '{Status}' status.");

        Status = TransactionStatus.Approved;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException($"Cannot reject a transaction in '{Status}' status.");

        Status = TransactionStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        if (Status != TransactionStatus.Approved)
            throw new DomainException("Only approved transactions can be completed.");

        Status = TransactionStatus.Completed;
    }
}
