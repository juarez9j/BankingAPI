using Banking.Application.Abstractions;
using Banking.Application.Services;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBankingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Banking") ?? "Data Source=banking.db";

        services.AddDbContext<BankingDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IBankingStore, BankingStore>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRandomNumberSource, SystemRandomNumberSource>();
        services.AddSingleton<IAccountNumberGenerator, AccountNumberGenerator>();

        return services;
    }
}
