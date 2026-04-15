using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.UseCases;

public class SendNotificationUseCase
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<SendNotificationUseCase> _logger;

    public SendNotificationUseCase(
        INotificationRepository repository,
        ILogger<SendNotificationUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ExecuteAsync(NotificationEvent notificationEvent, CancellationToken ct = default)
    {
        var notification = Notification.Create(
            notificationEvent.Type,
            notificationEvent.AccountId,
            notificationEvent.Message,
            notificationEvent.TransactionId);

        switch (notificationEvent.Type)
        {
            case "fraud_alert":
                _logger.LogWarning(
                    "[FRAUD ALERT] Account={AccountId} Transaction={TransactionId} | {Message}",
                    notificationEvent.AccountId,
                    notificationEvent.TransactionId,
                    notificationEvent.Message);
                break;

            case "transaction_confirmed":
                _logger.LogInformation(
                    "[TRANSACTION CONFIRMED] Account={AccountId} Transaction={TransactionId} | {Message}",
                    notificationEvent.AccountId,
                    notificationEvent.TransactionId,
                    notificationEvent.Message);
                break;

            default:
                _logger.LogInformation(
                    "[NOTIFICATION] Type={Type} Account={AccountId} | {Message}",
                    notificationEvent.Type,
                    notificationEvent.AccountId,
                    notificationEvent.Message);
                break;
        }

        notification.MarkSent();

        await _repository.AddAsync(notification, ct);
        await _repository.SaveChangesAsync(ct);
    }
}
