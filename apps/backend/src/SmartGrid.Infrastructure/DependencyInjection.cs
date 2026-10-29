using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Infrastructure.Repositories;

namespace SmartGrid.Infrastructure;

/// <summary>
/// Service collection extensions for configuring Smart Grid Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public const string DefaultProvider = "Mock";
    public const string ProviderConfigKey = "DataSource:Provider";
    public const string SnowflakeConnectionStringKey = "ConnectionStrings:Snowflake";

    /// <summary>
    /// Registers the infrastructure services, configuring the appropriate data repository
    /// (Mock or Snowflake) according to the <c>DataSource:Provider</c> configuration setting.
    /// </summary>
    public static IServiceCollection AddSmartGridInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration[ProviderConfigKey];
        if (string.IsNullOrWhiteSpace(provider))
        {
            provider = DefaultProvider;
        }

        if (string.Equals(provider, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IDashboardReadRepository, MockDashboardRepository>();
            services.AddScoped<IGridMapReadRepository, MockGridMapRepository>();
            services.AddScoped<IAlertReadRepository, MockAlertRepository>();
            services.AddScoped<ITransformerReadRepository, MockTransformerRepository>();
            services.AddScoped<IAccuracyReadRepository, MockAccuracyRepository>();
        }
        else if (string.Equals(provider, "Snowflake", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("Snowflake")
                ?? configuration[SnowflakeConnectionStringKey];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Snowflake connection string is missing or empty. Please configure '{SnowflakeConnectionStringKey}' or use User Secrets.");
            }

            services.AddScoped<IDashboardReadRepository>(_ => new SnowflakeDashboardRepository(connectionString));
            services.AddScoped<IGridMapReadRepository>(_ => new SnowflakeGridMapRepository(connectionString));
            services.AddScoped<IAlertReadRepository>(_ => new SnowflakeAlertRepository(connectionString));
            services.AddScoped<ITransformerReadRepository>(_ => new SnowflakeTransformerRepository(connectionString));
            services.AddScoped<IAccuracyReadRepository>(_ => new SnowflakeAccuracyRepository(connectionString));
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported DataSource:Provider '{provider}'. Supported values are 'Mock' and 'Snowflake'.");
        }

        return services;
    }
}
