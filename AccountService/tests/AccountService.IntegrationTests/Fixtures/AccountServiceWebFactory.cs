using AccountService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AccountService.IntegrationTests.Fixtures;

public class AccountServiceWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL with InMemory for tests
            services.RemoveAll<DbContextOptions<AccountDbContext>>();
            services.RemoveAll<AccountDbContext>();

            services.AddDbContext<AccountDbContext>(options =>
                options.UseInMemoryDatabase($"AccountTestDb_{Guid.NewGuid()}"));

            // Override JWT config with test values
            builder.UseSetting("Jwt:Secret", "test-secret-key-for-integration-tests-minimum-32-chars");
            builder.UseSetting("Jwt:Issuer", "MyBankAI");
            builder.UseSetting("Jwt:Audience", "MyBankAI.Clients");
            builder.UseSetting("Jwt:ExpiryMinutes", "60");
        });

        builder.UseEnvironment("Development");
    }
}
