using System.Text;
using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Data;

namespace TransactionService.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _context;

    public TransactionRepository(TransactionDbContext context) => _context = context;

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IReadOnlyList<Transaction> Items, string? NextCursor)> GetStatementAsync(
        Guid accountId,
        string? afterCursor,
        int pageSize,
        CancellationToken ct = default)
    {
        // Cursor encodes a DateTime (ticks as base64) to avoid offset attacks
        DateTime? cursorDate = DecodeCursor(afterCursor);

        var query = _context.Transactions
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId);

        if (cursorDate.HasValue)
            query = query.Where(t => t.CreatedAt < cursorDate.Value);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(pageSize + 1)   // fetch one extra to detect next page
            .ToListAsync(ct);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            items.RemoveAt(items.Count - 1);
            nextCursor = EncodeCursor(items[^1].CreatedAt);
        }

        return (items.AsReadOnly(), nextCursor);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default) =>
        await _context.Transactions.AddAsync(transaction, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);

    private static string EncodeCursor(DateTime dt) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(dt.Ticks.ToString()));

    private static DateTime? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var ticks = long.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)));
            return new DateTime(ticks, DateTimeKind.Utc);
        }
        catch
        {
            return null;
        }
    }
}
