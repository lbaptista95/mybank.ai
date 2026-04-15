using FluentAssertions;
using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Application.UseCases;
using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraudDetectionService.UnitTests.UseCases;

public class AnalyzeTransactionUseCaseTests
{
    private readonly Mock<IFraudAnalysisRepository> _repositoryMock = new();
    private readonly Mock<IGroqService> _groqServiceMock = new();
    private readonly Mock<IKafkaProducer> _kafkaProducerMock = new();
    private readonly Mock<ILogger<AnalyzeTransactionUseCase>> _loggerMock = new();

    private AnalyzeTransactionUseCase CreateSut() =>
        new(_repositoryMock.Object, _groqServiceMock.Object, _kafkaProducerMock.Object, _loggerMock.Object);

    private TransactionCreatedEvent CreateEvent(decimal amount = 100m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), amount, "BRL", "Test", DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_LowRiskTransaction_ShouldApproveAndPublish()
    {
        // Arrange
        var txEvent = CreateEvent(100m);
        _groqServiceMock.Setup(g => g.AnalyzeTransactionAsync(txEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroqAnalysisResponse(0.1m, "Approved", "Low risk."));

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(txEvent);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.Is<FraudAnalysis>(f =>
            f.Decision == FraudDecision.Approved &&
            f.RiskScore == 0.1m), It.IsAny<CancellationToken>()), Times.Once);

        _kafkaProducerMock.Verify(k => k.PublishAsync(
            "transaction.approved",
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_HighRiskTransaction_ShouldRejectAndPublishFraudAlert()
    {
        // Arrange
        var txEvent = CreateEvent(99999m);
        _groqServiceMock.Setup(g => g.AnalyzeTransactionAsync(txEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroqAnalysisResponse(0.95m, "Rejected", "Extremely high amount."));

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(txEvent);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.Is<FraudAnalysis>(f =>
            f.Decision == FraudDecision.Rejected &&
            f.RiskScore == 0.95m), It.IsAny<CancellationToken>()), Times.Once);

        _kafkaProducerMock.Verify(k => k.PublishAsync(
            "fraud.alert",
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _kafkaProducerMock.Verify(k => k.PublishAsync(
            "transaction.rejected",
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_GroqServiceFails_ShouldApproveByDefault()
    {
        // Arrange
        var txEvent = CreateEvent();
        _groqServiceMock.Setup(g => g.AnalyzeTransactionAsync(txEvent, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Groq API unavailable"));

        var sut = CreateSut();

        // Act - should not throw
        await sut.ExecuteAsync(txEvent);

        // Assert - approved by default
        _kafkaProducerMock.Verify(k => k.PublishAsync(
            "transaction.approved",
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
