using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NotificationService.Infrastructure.Data;

namespace NotificationService.IntegrationTests.Fixtures;

public class NotificationServiceWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL with InMemory
            services.RemoveAll<DbContextOptions<NotificationDbContext>>();
            services.RemoveAll<NotificationDbContext>();
            services.AddDbContext<NotificationDbContext>(options =>
                options.UseInMemoryDatabase($"NotificationTestDb_{Guid.NewGuid()}"));

            // Remove Kafka background consumer (no Kafka in tests)
            services.RemoveAll<IHostedService>();
        });

        builder.UseEnvironment("Development");
    }
}
