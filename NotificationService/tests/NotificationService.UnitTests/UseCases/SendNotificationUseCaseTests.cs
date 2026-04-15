using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Xunit;

namespace NotificationService.UnitTests.UseCases;

public class SendNotificationUseCaseTests
{
    private readonly Mock<INotificationRepository> _repositoryMock = new();
    private readonly Mock<ILogger<SendNotificationUseCase>> _loggerMock = new();

    private SendNotificationUseCase CreateSut() =>
        new(_repositoryMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ExecuteAsync_FraudAlert_ShouldPersistAndMarkSent()
    {
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var notificationEvent = new NotificationEvent(
            "fraud_alert", accountId, transactionId,
            "Fraud alert: high risk (risk score: 95%)",
            DateTime.UtcNow);

        var sut = CreateSut();

        await sut.ExecuteAsync(notificationEvent);

        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<Notification>(n =>
                n.Type == "fraud_alert" &&
                n.AccountId == accountId &&
                n.TransactionId == transactionId &&
                n.Sent == true),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_TransactionConfirmed_ShouldPersistAndMarkSent()
    {
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var notificationEvent = new NotificationEvent(
            "transaction_confirmed", accountId, transactionId,
            "Your transfer of R$100.00 was confirmed.",
            DateTime.UtcNow);

        var sut = CreateSut();

        await sut.ExecuteAsync(notificationEvent);

        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<Notification>(n =>
                n.Type == "transaction_confirmed" &&
                n.Sent == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownType_ShouldStillPersist()
    {
        var notificationEvent = new NotificationEvent(
            "system_maintenance", Guid.NewGuid(), null,
            "Scheduled maintenance tonight.",
            DateTime.UtcNow);

        var sut = CreateSut();

        await sut.ExecuteAsync(notificationEvent);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
