using System.Text.Json;
using Confluent.Kafka;
using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Application.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudDetectionService.Infrastructure.Kafka;

/// <summary>
/// Background service that consumes <c>transaction.created</c> events
/// and triggers fraud analysis for each one.
/// </summary>
public class TransactionConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TransactionConsumer> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TransactionConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<TransactionConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TransactionConsumer starting...");

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["Kafka:GroupId"] ?? "fraud-detection-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = false,
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 300000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .SetPartitionsAssignedHandler((c, partitions) =>
                _logger.LogInformation("Assigned partitions: {Partitions}", string.Join(", ", partitions)))
            .Build();

        consumer.Subscribe("transaction.created");
        _logger.LogInformation("Subscribed to topic: transaction.created");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? consumeResult = null;
            try
            {
                consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult is null) continue;

                _logger.LogInformation(
                    "Received message from [{Topic}/{Partition}@{Offset}] key={Key}",
                    consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumeResult.Message.Key);

                var transactionEvent = JsonSerializer.Deserialize<TransactionCreatedEvent>(
                    consumeResult.Message.Value, _jsonOptions);

                if (transactionEvent is null)
                {
                    _logger.LogWarning("Could not deserialize message: {Value}", consumeResult.Message.Value);
                    consumer.Commit(consumeResult);
                    continue;
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var useCase = scope.ServiceProvider.GetRequiredService<AnalyzeTransactionUseCase>();
                await useCase.ExecuteAsync(transactionEvent, stoppingToken);

                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TransactionConsumer stopping.");
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message. Committing to avoid poison-pill loop.");
                // Commit even on processing errors to avoid infinite retry of a bad message.
                if (consumeResult is not null)
                {
                    try { consumer.Commit(consumeResult); } catch { /* best effort */ }
                }
            }
        }

        consumer.Close();
    }
}
