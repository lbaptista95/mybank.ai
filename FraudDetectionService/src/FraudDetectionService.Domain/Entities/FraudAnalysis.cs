using FraudDetectionService.Domain.Enums;

namespace FraudDetectionService.Domain.Entities;

public class FraudAnalysis
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal RiskScore { get; private set; }   // 0.0 - 1.0
    public string Reasoning { get; private set; } = string.Empty;
    public FraudDecision Decision { get; private set; }
    public DateTime AnalyzedAt { get; private set; }

    private FraudAnalysis() { }

    public static FraudAnalysis Create(
        Guid transactionId,
        decimal riskScore,
        string reasoning,
        FraudDecision decision)
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("TransactionId cannot be empty.");

        if (riskScore < 0 || riskScore > 1)
            throw new ArgumentOutOfRangeException(nameof(riskScore), "Risk score must be between 0 and 1.");

        return new FraudAnalysis
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            RiskScore = riskScore,
            Reasoning = reasoning,
            Decision = decision,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    public bool IsHighRisk => RiskScore >= 0.7m;
}
