using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;

namespace TransactionService.Infrastructure.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasPrecision(18, 4).IsRequired();
            e.Property(t => t.Currency).IsRequired().HasMaxLength(3);
            e.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Description).HasMaxLength(500);
            e.Property(t => t.CreatedAt).IsRequired();

            // Indexes for statement queries (cursor-based pagination)
            e.HasIndex(t => new { t.FromAccountId, t.CreatedAt });
            e.HasIndex(t => new { t.ToAccountId, t.CreatedAt });
        });
    }
}
