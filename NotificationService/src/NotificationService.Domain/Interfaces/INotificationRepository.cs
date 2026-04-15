using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Notification>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
