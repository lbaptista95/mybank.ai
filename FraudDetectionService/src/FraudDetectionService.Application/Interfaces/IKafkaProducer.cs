namespace FraudDetectionService.Application.Interfaces;

public interface IKafkaProducer
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default);
}
