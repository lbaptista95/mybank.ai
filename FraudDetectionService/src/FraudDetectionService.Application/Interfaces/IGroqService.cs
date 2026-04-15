using FraudDetectionService.Application.DTOs;

namespace FraudDetectionService.Application.Interfaces;

public interface IGroqService
{
    Task<GroqAnalysisResponse> AnalyzeTransactionAsync(TransactionCreatedEvent transaction, CancellationToken ct = default);
}
