using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases;
using NotificationService.Domain.Interfaces;
using NotificationService.IntegrationTests.Fixtures;
using Xunit;

namespace NotificationService.IntegrationTests.UseCases;

public class SendNotificationUseCaseIntegrationTests : IClassFixture<NotificationServiceWebFactory>
{
    private readonly NotificationServiceWebFactory _factory;

    public SendNotificationUseCaseIntegrationTests(NotificationServiceWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExecuteAsync_FraudAlert_ShouldPersistToDatabase()
    {
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var notificationEvent = new NotificationEvent(
            "fraud_alert", accountId, transactionId,
            "Fraud alert: suspicious activity detected (risk score: 95%)",
            DateTime.UtcNow);

        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SendNotificationUseCase>();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        await useCase.ExecuteAsync(notificationEvent);

        var notifications = await repository.GetByAccountIdAsync(accountId);
        notifications.Should().HaveCount(1);
        var saved = notifications.First();
        saved.Type.Should().Be("fraud_alert");
        saved.AccountId.Should().Be(accountId);
        saved.TransactionId.Should().Be(transactionId);
        saved.Sent.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_MultipleNotifications_ShouldPersistAll()
    {
        var accountId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SendNotificationUseCase>();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        await useCase.ExecuteAsync(new NotificationEvent("fraud_alert", accountId, Guid.NewGuid(), "Alert 1", DateTime.UtcNow));
        await useCase.ExecuteAsync(new NotificationEvent("transaction_confirmed", accountId, Guid.NewGuid(), "Confirmed 1", DateTime.UtcNow));
        await useCase.ExecuteAsync(new NotificationEvent("transaction_confirmed", accountId, Guid.NewGuid(), "Confirmed 2", DateTime.UtcNow));

        var notifications = await repository.GetByAccountIdAsync(accountId);
        notifications.Should().HaveCount(3);
        notifications.Should().AllSatisfy(n => n.Sent.Should().BeTrue());
    }

    [Fact]
    public async Task GetByAccountId_ReturnsOrderedByCreatedAtDesc()
    {
        var accountId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SendNotificationUseCase>();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        await useCase.ExecuteAsync(new NotificationEvent("transaction_confirmed", accountId, Guid.NewGuid(), "First", DateTime.UtcNow.AddMinutes(-5)));
        await Task.Delay(10); // ensure distinct timestamps
        await useCase.ExecuteAsync(new NotificationEvent("transaction_confirmed", accountId, Guid.NewGuid(), "Second", DateTime.UtcNow));

        var notifications = (await repository.GetByAccountIdAsync(accountId)).ToList();
        notifications.Should().HaveCount(2);
        notifications[0].CreatedAt.Should().BeOnOrAfter(notifications[1].CreatedAt);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
