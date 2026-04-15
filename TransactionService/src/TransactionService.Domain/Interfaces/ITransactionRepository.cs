using TransactionService.Domain.Entities;

namespace TransactionService.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cursor-based pagination: returns up to <paramref name="pageSize"/> transactions
    /// for the given account, after the cursor (<paramref name="afterCursor"/> = last seen CreatedAt).
    /// </summary>
    Task<(IReadOnlyList<Transaction> Items, string? NextCursor)> GetStatementAsync(
        Guid accountId,
        string? afterCursor,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
