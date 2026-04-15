using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Interfaces;
using FraudDetectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FraudDetectionService.Infrastructure.Repositories;

public class FraudAnalysisRepository : IFraudAnalysisRepository
{
    private readonly FraudDbContext _context;

    public FraudAnalysisRepository(FraudDbContext context) => _context = context;

    public async Task<FraudAnalysis?> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default) =>
        await _context.FraudAnalyses.FirstOrDefaultAsync(f => f.TransactionId == transactionId, ct);

    public async Task<IEnumerable<FraudAnalysis>> GetHighRiskAsync(int limit = 50, CancellationToken ct = default) =>
        await _context.FraudAnalyses
            .Where(f => f.RiskScore >= 0.7m)
            .OrderByDescending(f => f.AnalyzedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task AddAsync(FraudAnalysis analysis, CancellationToken ct = default) =>
        await _context.FraudAnalyses.AddAsync(analysis, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
