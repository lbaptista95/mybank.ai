using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Interfaces;

namespace TransactionService.Infrastructure.Kafka;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 10000,
            RetryBackoffMs = 500
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var kafkaMessage = new Message<string, string>
        {
            Key = key,
            Value = json
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, kafkaMessage, ct);
            _logger.LogInformation(
                "Published to {Topic} [{Partition}@{Offset}] key={Key}",
                topic, result.Partition, result.Offset, key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish to {Topic}: {Reason}", topic, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose() => _producer?.Dispose();
}
