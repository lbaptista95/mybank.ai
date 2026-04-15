using System.Text.Json;
using Confluent.Kafka;
using FraudDetectionService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FraudDetectionService.Infrastructure.Kafka;

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
            MessageTimeoutMs = 10000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        try
        {
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = json
            }, ct);

            _logger.LogInformation("Published to {Topic} [{Partition}@{Offset}]", topic, result.Partition, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka produce error on {Topic}: {Reason}", topic, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose() => _producer?.Dispose();
}
