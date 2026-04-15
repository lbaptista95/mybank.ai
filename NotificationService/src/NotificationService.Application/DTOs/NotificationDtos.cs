namespace NotificationService.Application.DTOs;

public record NotificationEvent(
    string Type,
    Guid AccountId,
    Guid? TransactionId,
    string Message,
    DateTime SentAt
);
