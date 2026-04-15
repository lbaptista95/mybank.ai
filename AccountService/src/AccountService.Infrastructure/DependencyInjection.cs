using AccountService.Application.Interfaces;
using AccountService.Application.UseCases;
using AccountService.Domain.Interfaces;
using AccountService.Infrastructure.Data;
using AccountService.Infrastructure.Repositories;
using AccountService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AccountDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npg => npg.EnableRetryOnFailure(3)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IJwtService, JwtService>();

        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<CreateAccountUseCase>();

        return services;
    }
}
