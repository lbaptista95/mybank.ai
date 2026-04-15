using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases;

namespace NotificationService.Infrastructure.Kafka;

/// <summary>
/// Background service consuming <c>notification.send</c> and <c>fraud.alert</c> topics.
/// </summary>
public class NotificationConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationConsumer> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] Topics = ["notification.send", "fraud.alert"];

    public NotificationConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<NotificationConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationConsumer starting...");

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["Kafka:GroupId"] ?? "notification-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Reason}", e.Reason))
            .Build();

        consumer.Subscribe(Topics);
        _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", Topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? consumeResult = null;
            try
            {
                consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult is null) continue;

                _logger.LogInformation(
                    "Received [{Topic}] key={Key}",
                    consumeResult.Topic, consumeResult.Message.Key);

                NotificationEvent? notificationEvent = null;

                if (consumeResult.Topic == "fraud.alert")
                {
                    // Map fraud.alert format to NotificationEvent
                    var raw = JsonSerializer.Deserialize<FraudAlertRaw>(consumeResult.Message.Value, _jsonOptions);
                    if (raw is not null)
                    {
                        notificationEvent = new NotificationEvent(
                            "fraud_alert",
                            raw.FromAccountId,
                            raw.TransactionId,
                            $"Fraud alert: {raw.Reasoning} (risk score: {raw.RiskScore:P0})",
                            raw.AlertedAt);
                    }
                }
                else
                {
                    notificationEvent = JsonSerializer.Deserialize<NotificationEvent>(
                        consumeResult.Message.Value, _jsonOptions);
                }

                if (notificationEvent is not null)
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var useCase = scope.ServiceProvider.GetRequiredService<SendNotificationUseCase>();
                    await useCase.ExecuteAsync(notificationEvent, stoppingToken);
                }

                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing error");
                if (consumeResult is not null)
                {
                    try { consumer.Commit(consumeResult); } catch { /* best effort */ }
                }
            }
        }

        consumer.Close();
    }

    private record FraudAlertRaw(
        Guid TransactionId,
        Guid FromAccountId,
        decimal RiskScore,
        string Reasoning,
        DateTime AlertedAt
    );
}
