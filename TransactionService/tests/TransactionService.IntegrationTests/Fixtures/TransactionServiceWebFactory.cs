using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using TransactionService.Application.Interfaces;
using TransactionService.Infrastructure.Data;

namespace TransactionService.IntegrationTests.Fixtures;

public class TransactionServiceWebFactory : WebApplicationFactory<Program>
{
    public Mock<IKafkaProducer> KafkaProducerMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL with InMemory
            services.RemoveAll<DbContextOptions<TransactionDbContext>>();
            services.RemoveAll<TransactionDbContext>();
            services.AddDbContext<TransactionDbContext>(options =>
                options.UseInMemoryDatabase($"TransactionTestDb_{Guid.NewGuid()}"));

            // Replace Kafka producer with mock (no Kafka needed in tests)
            services.RemoveAll<IKafkaProducer>();
            services.AddSingleton(KafkaProducerMock.Object);

            builder.UseSetting("Jwt:Secret", "test-secret-key-for-integration-tests-minimum-32-chars");
            builder.UseSetting("Jwt:Issuer", "MyBankAI");
            builder.UseSetting("Jwt:Audience", "MyBankAI.Clients");
        });

        builder.UseEnvironment("Development");
    }
}
