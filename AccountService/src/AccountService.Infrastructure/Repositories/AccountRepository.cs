using AccountService.Domain.Entities;
using AccountService.Domain.Interfaces;
using AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AccountDbContext _context;

    public AccountRepository(AccountDbContext context) => _context = context;

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _context.Accounts.Where(a => a.UserId == userId).ToListAsync(ct);

    public async Task AddAsync(Account account, CancellationToken ct = default) =>
        await _context.Accounts.AddAsync(account, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
