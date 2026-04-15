using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionService.Application.Interfaces;
using TransactionService.Application.UseCases;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Data;
using TransactionService.Infrastructure.Kafka;
using TransactionService.Infrastructure.Repositories;

namespace TransactionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TransactionDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npg => npg.EnableRetryOnFailure(3)));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddScoped<CreateTransactionUseCase>();

        return services;
    }
}
