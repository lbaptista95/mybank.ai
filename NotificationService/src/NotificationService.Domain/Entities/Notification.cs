namespace NotificationService.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public Guid AccountId { get; private set; }
    public Guid? TransactionId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public bool Sent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private Notification() { }

    public static Notification Create(string type, Guid accountId, string message, Guid? transactionId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            AccountId = accountId,
            TransactionId = transactionId,
            Message = message,
            Sent = false,
            CreatedAt = DateTime.UtcNow
        };

    public void MarkSent()
    {
        Sent = true;
        SentAt = DateTime.UtcNow;
    }
}
