using AccountService.Domain.Entities;
using AccountService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Data;

public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Name).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Balance).HasPrecision(18, 4).IsRequired();
            e.Property(a => a.Currency).IsRequired().HasMaxLength(3);
            e.Property(a => a.CreatedAt).IsRequired();
            e.Property(a => a.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            e.HasIndex(a => a.UserId);
        });
    }
}
