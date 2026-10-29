using SmartGrid.Application.Common.Models;
using SmartGrid.Infrastructure.Repositories;

namespace SmartGrid.Api.Tests.Integration;

/// <summary>
/// Optional live Snowflake smoke test.
/// Only runs when RUN_SNOWFLAKE_INTEGRATION_TESTS=true and a valid Snowflake connection string is configured.
/// </summary>
public class SnowflakeLiveSmokeTests
{
    private const string OptInEnvVar = "RUN_SNOWFLAKE_INTEGRATION_TESTS";
    private const string ConnStrEnvVar = "SNOWFLAKE_CONNECTION_STRING";

    [Fact]
    public async Task LiveSnowflake_SmokeTest_QueriesMartsModelsWhenOptedIn()
    {
        var optIn = Environment.GetEnvironmentVariable(OptInEnvVar);
        if (!string.Equals(optIn, "true", StringComparison.OrdinalIgnoreCase))
        {
            // Explicitly skipped when opt-in flag is not set
            return;
        }

        var connectionString = Environment.GetEnvironmentVariable(ConnStrEnvVar)
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Snowflake");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Opt-in flag {OptInEnvVar}=true was set, but no connection string was provided via {ConnStrEnvVar} or ConnectionStrings__Snowflake.");
        }

        var repo = new SnowflakeDashboardRepository(connectionString);
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        var overview = await repo.GetOverviewAsync(period);
        Assert.NotNull(overview);
        Assert.Equal(period.Start, overview.PeriodStart);
        Assert.Equal(period.End, overview.PeriodEnd);

        var demandPoints = await repo.GetDailyDemandAsync(period);
        Assert.NotNull(demandPoints);

        // New feature queries: each must run against the live MARTS views without errors.
        var alertRepo = new SnowflakeAlertRepository(connectionString);
        var alerts = await alertRepo.SearchAsync(new SmartGrid.Application.Alerts.Queries.AlertSearchCriteria
        {
            Period = period,
            Pagination = new PageRequest(1, 5)
        });
        Assert.NotNull(alerts);
        Assert.Equal(3, (await alertRepo.GetSummaryByTypeAsync(period)).Count);

        var transformerRepo = new SnowflakeTransformerRepository(connectionString);
        var transformers = await transformerRepo.SearchAsync(new SmartGrid.Application.Transformers.Queries.TransformerSearchCriteria
        {
            SortBy = "alerts",
            Pagination = new PageRequest(1, 5)
        });
        Assert.NotEmpty(transformers.Items);
        var profile = await transformerRepo.GetLoadProfileAsync(transformers.Items[0].TransformerId, period);
        Assert.NotNull(profile);

        var accuracyRepo = new SnowflakeAccuracyRepository(connectionString);
        var report = await accuracyRepo.GetReportAsync();
        Assert.NotNull(report);
        var details = await accuracyRepo.GetDetailsAsync(new SmartGrid.Application.Accuracy.Queries.AccuracyDetailCriteria
        {
            Pagination = new PageRequest(1, 5)
        });
        Assert.NotNull(details);
    }
}
