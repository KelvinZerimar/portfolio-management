
using Application.Common.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Serilog;
using System.Diagnostics;
using System.Reflection;
using Infrastructure.Common.Options;
using Infrastructure.Common.Persistence.Contexts;
using Infrastructure.Common.Security;
using Infrastructure.Portfolios;
using Infrastructure.Users;
using Infrastructure.Exchanges;
using Infrastructure.CryptoCurrencies;
using Infrastructure.PortfolioEntries;
using Application.Common.Security;
using Application.Portfolios.Interfaces;
using Application.Users.Interfaces;
using Application.Exchanges.Interfaces;
using Application.CryptoCurrencies.Interfaces;
using Application.PortfolioEntries.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        ConfigurationManager configuration, IHostEnvironment environment)
    {
        services
            .AddConfigurationOptions(configuration)
            .AddLoggingConfiguration(configuration)
            .AddBackgroundServices(configuration)
            .AddDbContexts(configuration)
            .AddRepositories()
            .AddPackages(configuration)
            .AddAdapters()
            .AddSecurity()
            .AddCaching()
            .AddHealthChecksForDependencies(configuration);

        return services;
    }

    private static IServiceCollection AddBackgroundServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }

    private static IServiceCollection AddDbContexts(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");
        services.AddDbContext<AppDbContext>(
            options => options
                .UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default)
                    .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }

    private static IServiceCollection AddLoggingConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSerilog(loggerConfiguration => loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .WriteTo.Conditional(static _ => Debugger.IsAttached, static writeTo => writeTo.Console()));

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IExchangeRepository, ExchangeRepository>();
        services.AddScoped<ICryptoCurrencyRepository, CryptoCurrencyRepository>();
        services.AddScoped<IPortfolioEntryRepository, PortfolioEntryRepository>();

        return services;
    }

    private static IServiceCollection AddAdapters(this IServiceCollection services)
    {
        services.AddHttpClient<ICoinGeckoClient, CoinGeckoClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CoinGeckoOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PortfolioManagementMinimalApi/1.0");
            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("x-cg-demo-api-key", options.ApiKey);
            }
        });

        return services;
    }

    private static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services)
    {
        // In-process only for now (no IDistributedCache/Redis registered): still gives every
        // cached read stampede protection and a shared TTL. Adding Redis later as an L2 tier
        // is a config-only change; HybridCache picks it up automatically once registered.
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new()
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(10),
            };
        });

        return services;
    }



    private static IServiceCollection AddPackages(this IServiceCollection services, IConfigurationRoot configuration)
    {
        return services;
    }

    private static IServiceCollection AddConfigurationOptions(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetRequiredSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<CoinGeckoOptions>()
            .Bind(configuration.GetRequiredSection(CoinGeckoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    private static void AddHealthChecksForDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!, name: "postgres");

        services.AddHealthChecksUI(setup =>
        {
            setup.AddHealthCheckEndpoint("General", "/health/json");
            setup.SetHeaderText(Assembly.GetEntryAssembly()?.GetName().Name ?? "Healthcheck");
        }
        ).AddInMemoryStorage();
    }
}