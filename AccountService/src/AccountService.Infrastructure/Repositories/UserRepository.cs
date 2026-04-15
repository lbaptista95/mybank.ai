using AccountService.Domain.Entities;
using AccountService.Domain.Interfaces;
using AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AccountDbContext _context;

    public UserRepository(AccountDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
