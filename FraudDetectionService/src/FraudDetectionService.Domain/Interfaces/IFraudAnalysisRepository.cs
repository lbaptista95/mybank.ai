using FraudDetectionService.Domain.Entities;

namespace FraudDetectionService.Domain.Interfaces;

public interface IFraudAnalysisRepository
{
    Task<FraudAnalysis?> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default);
    Task<IEnumerable<FraudAnalysis>> GetHighRiskAsync(int limit = 50, CancellationToken ct = default);
    Task AddAsync(FraudAnalysis analysis, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
