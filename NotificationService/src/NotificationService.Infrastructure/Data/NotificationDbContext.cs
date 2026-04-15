using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Type).IsRequired().HasMaxLength(50);
            e.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            e.Property(n => n.CreatedAt).IsRequired();

            e.HasIndex(n => n.AccountId);
            e.HasIndex(n => new { n.AccountId, n.CreatedAt });
        });
    }
}
