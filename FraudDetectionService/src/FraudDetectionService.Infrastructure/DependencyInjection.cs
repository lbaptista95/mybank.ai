using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Application.UseCases;
using FraudDetectionService.Domain.Interfaces;
using FraudDetectionService.Infrastructure.Data;
using FraudDetectionService.Infrastructure.Groq;
using FraudDetectionService.Infrastructure.Kafka;
using FraudDetectionService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FraudDetectionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FraudDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npg => npg.EnableRetryOnFailure(3)));

        services.AddScoped<IFraudAnalysisRepository, FraudAnalysisRepository>();

        // Groq HTTP client
        var groqBaseUrl = configuration["Groq:BaseUrl"] ?? "https://api.groq.com/openai/v1";
        services.AddHttpClient<IGroqService, GroqService>(client =>
        {
            client.BaseAddress = new Uri(groqBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Kafka
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddHostedService<TransactionConsumer>();

        // Use cases
        services.AddScoped<AnalyzeTransactionUseCase>();

        return services;
    }
}
