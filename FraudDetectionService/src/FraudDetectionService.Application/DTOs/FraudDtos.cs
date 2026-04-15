namespace FraudDetectionService.Application.DTOs;

public record TransactionCreatedEvent(
    Guid TransactionId,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Currency,
    string? Description,
    DateTime CreatedAt
);

public record FraudAnalysisResult(
    Guid TransactionId,
    decimal RiskScore,
    string Decision,
    string Reasoning,
    DateTime AnalyzedAt
);

public record GroqAnalysisResponse(
    decimal RiskScore,
    string Decision,
    string Reasoning
);
