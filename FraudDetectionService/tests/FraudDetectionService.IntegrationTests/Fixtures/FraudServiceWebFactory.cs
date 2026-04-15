using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace FraudDetectionService.IntegrationTests.Fixtures;

public class FraudServiceWebFactory : WebApplicationFactory<Program>
{
    public Mock<IGroqService> GroqServiceMock { get; } = new();
    public Mock<IKafkaProducer> KafkaProducerMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL with InMemory
            services.RemoveAll<DbContextOptions<FraudDbContext>>();
            services.RemoveAll<FraudDbContext>();
            services.AddDbContext<FraudDbContext>(options =>
                options.UseInMemoryDatabase($"FraudTestDb_{Guid.NewGuid()}"));

            // Replace external dependencies with mocks
            services.RemoveAll<IGroqService>();
            services.AddScoped(_ => GroqServiceMock.Object);

            services.RemoveAll<IKafkaProducer>();
            services.AddSingleton(KafkaProducerMock.Object);

            // Remove Kafka background consumer (no Kafka in tests)
            services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
        });

        builder.UseEnvironment("Development");
    }
}
