using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.UseCases;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Kafka;
using NotificationService.Infrastructure.Repositories;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npg => npg.EnableRetryOnFailure(3)));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<SendNotificationUseCase>();
        services.AddHostedService<NotificationConsumer>();

        return services;
    }
}
