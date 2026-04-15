using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FraudDetectionService.Application.UseCases;

public class AnalyzeTransactionUseCase
{
    private readonly IFraudAnalysisRepository _repository;
    private readonly IGroqService _groqService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<AnalyzeTransactionUseCase> _logger;

    public AnalyzeTransactionUseCase(
        IFraudAnalysisRepository repository,
        IGroqService groqService,
        IKafkaProducer kafkaProducer,
        ILogger<AnalyzeTransactionUseCase> logger)
    {
        _repository = repository;
        _groqService = groqService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task ExecuteAsync(TransactionCreatedEvent transactionEvent, CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing transaction {TransactionId} for fraud...", transactionEvent.TransactionId);

        GroqAnalysisResponse groqResult;
        try
        {
            groqResult = await _groqService.AnalyzeTransactionAsync(transactionEvent, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq analysis failed for {TransactionId}. Approving by default.", transactionEvent.TransactionId);
            groqResult = new GroqAnalysisResponse(0.1m, "Approved", "AI analysis unavailable, approved by default.");
        }

        var decision = ParseDecision(groqResult.Decision, groqResult.RiskScore);

        var analysis = FraudAnalysis.Create(
            transactionEvent.TransactionId,
            groqResult.RiskScore,
            groqResult.Reasoning,
            decision);

        await _repository.AddAsync(analysis, ct);
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Fraud analysis complete: {TransactionId} → {Decision} (risk={RiskScore:P0})",
            transactionEvent.TransactionId, decision, groqResult.RiskScore);

        // Publish decision back to transaction service
        var decisionTopic = decision == FraudDecision.Approved ? "transaction.approved" : "transaction.rejected";
        await _kafkaProducer.PublishAsync(decisionTopic, transactionEvent.TransactionId.ToString(), new
        {
            transactionId = transactionEvent.TransactionId,
            riskScore = groqResult.RiskScore,
            decision = decision.ToString(),
            reasoning = groqResult.Reasoning,
            analyzedAt = analysis.AnalyzedAt
        }, ct);

        // If high risk, also publish fraud alert and notification
        if (analysis.IsHighRisk)
        {
            var fraudAlertPayload = new
            {
                transactionId = transactionEvent.TransactionId,
                fromAccountId = transactionEvent.FromAccountId,
                riskScore = groqResult.RiskScore,
                reasoning = groqResult.Reasoning,
                alertedAt = DateTime.UtcNow
            };

            await _kafkaProducer.PublishAsync("fraud.alert", transactionEvent.TransactionId.ToString(), fraudAlertPayload, ct);
            await _kafkaProducer.PublishAsync("notification.send", transactionEvent.TransactionId.ToString(), new
            {
                type = "fraud_alert",
                accountId = transactionEvent.FromAccountId,
                transactionId = transactionEvent.TransactionId,
                message = $"High risk transaction detected (score: {groqResult.RiskScore:P0}). {groqResult.Reasoning}",
                sentAt = DateTime.UtcNow
            }, ct);

            _logger.LogWarning("Fraud alert published for {TransactionId}", transactionEvent.TransactionId);
        }
        else
        {
            // Publish confirmation notification
            await _kafkaProducer.PublishAsync("notification.send", transactionEvent.TransactionId.ToString(), new
            {
                type = "transaction_confirmed",
                accountId = transactionEvent.FromAccountId,
                transactionId = transactionEvent.TransactionId,
                message = $"Transaction of {transactionEvent.Amount} {transactionEvent.Currency} {(decision == FraudDecision.Approved ? "approved" : "requires review")}.",
                sentAt = DateTime.UtcNow
            }, ct);
        }
    }

    public async Task<FraudAnalysisResult?> GetByTransactionAsync(Guid transactionId, CancellationToken ct = default)
    {
        var analysis = await _repository.GetByTransactionIdAsync(transactionId, ct);
        if (analysis is null) return null;

        return new FraudAnalysisResult(
            analysis.TransactionId,
            analysis.RiskScore,
            analysis.Decision.ToString(),
            analysis.Reasoning,
            analysis.AnalyzedAt);
    }

    private static FraudDecision ParseDecision(string decision, decimal riskScore) =>
        decision.ToLowerInvariant() switch
        {
            "approved" => FraudDecision.Approved,
            "rejected" => FraudDecision.Rejected,
            "manual_review" or "manualreview" => FraudDecision.ManualReview,
            _ => riskScore >= 0.7m ? FraudDecision.Rejected : FraudDecision.Approved
        };
}
