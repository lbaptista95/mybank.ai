using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Application.UseCases;
using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Interfaces;
using FraudDetectionService.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FraudDetectionService.IntegrationTests.Controllers;

public class FraudControllerTests : IClassFixture<FraudServiceWebFactory>
{
    private readonly FraudServiceWebFactory _factory;
    private readonly HttpClient _client;

    public FraudControllerTests(FraudServiceWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedAnalysisAsync(Guid transactionId, decimal riskScore, string decision)
    {
        _factory.GroqServiceMock
            .Setup(g => g.AnalyzeTransactionAsync(It.IsAny<TransactionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroqAnalysisResponse(riskScore, decision, $"Seeded analysis: {decision}"));

        _factory.KafkaProducerMock
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AnalyzeTransactionUseCase>();
        var txEvent = new TransactionCreatedEvent(transactionId, Guid.NewGuid(), Guid.NewGuid(), 100m, "BRL", "test", DateTime.UtcNow);
        await useCase.ExecuteAsync(txEvent);
    }

    [Fact]
    public async Task GetByTransaction_ExistingAnalysis_ShouldReturn200()
    {
        var transactionId = Guid.NewGuid();
        await SeedAnalysisAsync(transactionId, 0.2m, "Approved");

        var response = await _client.GetAsync($"/analyses/{transactionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FraudAnalysisResult>();
        body.Should().NotBeNull();
        body!.TransactionId.Should().Be(transactionId);
        body.Decision.Should().Be("Approved");
        body.RiskScore.Should().Be(0.2m);
    }

    [Fact]
    public async Task GetByTransaction_NonexistentAnalysis_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/analyses/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnalyzeTransaction_HighRisk_ShouldPublishFraudAlert()
    {
        var transactionId = Guid.NewGuid();

        _factory.GroqServiceMock
            .Setup(g => g.AnalyzeTransactionAsync(It.IsAny<TransactionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroqAnalysisResponse(0.95m, "Rejected", "Extremely high amount, suspicious pattern."));

        _factory.KafkaProducerMock
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AnalyzeTransactionUseCase>();
        await useCase.ExecuteAsync(new TransactionCreatedEvent(transactionId, Guid.NewGuid(), Guid.NewGuid(), 99999m, "BRL", null, DateTime.UtcNow));

        _factory.KafkaProducerMock.Verify(k => k.PublishAsync(
            "fraud.alert", It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _factory.KafkaProducerMock.Verify(k => k.PublishAsync(
            "transaction.rejected", It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AnalyzeTransaction_WhenGroqFails_ShouldApproveByDefaultAndNotThrow()
    {
        var transactionId = Guid.NewGuid();

        _factory.GroqServiceMock
            .Setup(g => g.AnalyzeTransactionAsync(It.IsAny<TransactionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Groq API down"));

        _factory.KafkaProducerMock
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AnalyzeTransactionUseCase>();

        var act = () => useCase.ExecuteAsync(
            new TransactionCreatedEvent(transactionId, Guid.NewGuid(), Guid.NewGuid(), 100m, "BRL", null, DateTime.UtcNow));

        await act.Should().NotThrowAsync();

        _factory.KafkaProducerMock.Verify(k => k.PublishAsync(
            "transaction.approved", It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
