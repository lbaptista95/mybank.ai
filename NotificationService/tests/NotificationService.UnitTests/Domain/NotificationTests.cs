using FluentAssertions;
using NotificationService.Domain.Entities;
using Xunit;

namespace NotificationService.UnitTests.Domain;

public class NotificationTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUnsentNotification()
    {
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        var notification = Notification.Create("fraud_alert", accountId, "Suspicious transaction detected.", transactionId);

        notification.Id.Should().NotBeEmpty();
        notification.Type.Should().Be("fraud_alert");
        notification.AccountId.Should().Be(accountId);
        notification.TransactionId.Should().Be(transactionId);
        notification.Message.Should().Be("Suspicious transaction detected.");
        notification.Sent.Should().BeFalse();
        notification.SentAt.Should().BeNull();
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithoutTransactionId_ShouldCreateNotificationWithNullTransactionId()
    {
        var notification = Notification.Create("system", Guid.NewGuid(), "Welcome to MyBankAI!");

        notification.TransactionId.Should().BeNull();
        notification.Sent.Should().BeFalse();
    }

    [Fact]
    public void MarkSent_ShouldSetSentTrueAndSentAt()
    {
        var notification = Notification.Create("transaction_confirmed", Guid.NewGuid(), "Your transfer was confirmed.");

        notification.MarkSent();

        notification.Sent.Should().BeTrue();
        notification.SentAt.Should().NotBeNull();
        notification.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkSent_CalledTwice_ShouldUpdateSentAt()
    {
        var notification = Notification.Create("fraud_alert", Guid.NewGuid(), "Alert.");
        notification.MarkSent();
        var firstSentAt = notification.SentAt;

        notification.MarkSent();

        notification.Sent.Should().BeTrue();
        notification.SentAt.Should().BeOnOrAfter(firstSentAt!.Value);
    }
}
