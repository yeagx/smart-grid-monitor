using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Infrastructure;
using SmartGrid.Infrastructure.Repositories;

namespace SmartGrid.Api.Tests.DependencyInjection;

public class ProviderSelectionTests
{
    [Fact]
    public void AddSmartGridInfrastructure_WhenProviderIsMock_ResolvesMockRepository()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["DataSource:Provider"] = "Mock"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddSmartGridInfrastructure(configuration);

        var provider = services.BuildServiceProvider();
        var dashboardRepo = provider.GetRequiredService<IDashboardReadRepository>();
        var gridMapRepo = provider.GetRequiredService<IGridMapReadRepository>();

        Assert.NotNull(dashboardRepo);
        Assert.IsType<MockDashboardRepository>(dashboardRepo);
        Assert.NotNull(gridMapRepo);
        Assert.IsType<MockGridMapRepository>(gridMapRepo);
        Assert.IsType<MockAlertRepository>(provider.GetRequiredService<IAlertReadRepository>());
        Assert.IsType<MockTransformerRepository>(provider.GetRequiredService<ITransformerReadRepository>());
        Assert.IsType<MockAccuracyRepository>(provider.GetRequiredService<IAccuracyReadRepository>());
    }

    [Fact]
    public void AddSmartGridInfrastructure_WhenProviderIsMissing_DefaultsToMockRepository()
    {
        var inMemorySettings = new Dictionary<string, string?>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddSmartGridInfrastructure(configuration);

        var provider = services.BuildServiceProvider();
        var dashboardRepo = provider.GetRequiredService<IDashboardReadRepository>();
        var gridMapRepo = provider.GetRequiredService<IGridMapReadRepository>();

        Assert.NotNull(dashboardRepo);
        Assert.IsType<MockDashboardRepository>(dashboardRepo);
        Assert.NotNull(gridMapRepo);
        Assert.IsType<MockGridMapRepository>(gridMapRepo);
    }

    [Fact]
    public void AddSmartGridInfrastructure_WhenProviderIsSnowflakeWithConnectionString_ResolvesSnowflakeRepositoryWithoutConnecting()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["DataSource:Provider"] = "Snowflake",
            ["ConnectionStrings:Snowflake"] = "account=myorg-test;user=testuser;password=testpw;db=SMART_GRID;schema=MARTS;warehouse=COMPUTE_WH"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddSmartGridInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        // Must construct Snowflake repositories without attempting to open connection
        var dashboardRepo = provider.GetRequiredService<IDashboardReadRepository>();
        var gridMapRepo = provider.GetRequiredService<IGridMapReadRepository>();

        Assert.NotNull(dashboardRepo);
        Assert.IsType<SnowflakeDashboardRepository>(dashboardRepo);
        Assert.NotNull(gridMapRepo);
        Assert.IsType<SnowflakeGridMapRepository>(gridMapRepo);
        Assert.IsType<SnowflakeAlertRepository>(provider.GetRequiredService<IAlertReadRepository>());
        Assert.IsType<SnowflakeTransformerRepository>(provider.GetRequiredService<ITransformerReadRepository>());
        Assert.IsType<SnowflakeAccuracyRepository>(provider.GetRequiredService<IAccuracyReadRepository>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddSmartGridInfrastructure_WhenProviderIsSnowflakeWithoutConnectionString_ThrowsInvalidOperationException(string? connStr)
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["DataSource:Provider"] = "Snowflake"
        };

        if (connStr != null)
        {
            inMemorySettings["ConnectionStrings:Snowflake"] = connStr;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddSmartGridInfrastructure(configuration));

        Assert.Contains("Snowflake connection string is missing or empty", ex.Message);
    }

    [Fact]
    public void AddSmartGridInfrastructure_WhenProviderIsUnsupported_ThrowsInvalidOperationException()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["DataSource:Provider"] = "PostgreSQL"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddSmartGridInfrastructure(configuration));

        Assert.Contains("Unsupported DataSource:Provider 'PostgreSQL'", ex.Message);
    }
}
