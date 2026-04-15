using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context) => _context = context;

    public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
        await _context.Notifications.AddAsync(notification, ct);

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IEnumerable<Notification>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default) =>
        await _context.Notifications
            .Where(n => n.AccountId == accountId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
