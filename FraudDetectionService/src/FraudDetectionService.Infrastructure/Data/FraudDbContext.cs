using FraudDetectionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FraudDetectionService.Infrastructure.Data;

public class FraudDbContext : DbContext
{
    public FraudDbContext(DbContextOptions<FraudDbContext> options) : base(options) { }

    public DbSet<FraudAnalysis> FraudAnalyses => Set<FraudAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FraudAnalysis>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.RiskScore).HasPrecision(5, 4).IsRequired();
            e.Property(f => f.Reasoning).IsRequired().HasMaxLength(2000);
            e.Property(f => f.Decision).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(f => f.AnalyzedAt).IsRequired();
            e.HasIndex(f => f.TransactionId).IsUnique();
            e.HasIndex(f => new { f.RiskScore, f.AnalyzedAt });
        });
    }
}
