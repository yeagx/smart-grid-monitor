using SmartGrid.Application.Common.Models;
using SmartGrid.Infrastructure.Data;
using SmartGrid.Infrastructure.Repositories;

namespace SmartGrid.Api.Tests.Repositories;

public class MockDashboardRepositoryTests
{
    private readonly MockDashboardRepository _repository = new();

    [Fact]
    public async Task GetOverviewAsync_ForBothFixtureDays_ReturnsDeterministicTotalsAndDistinctCounts()
    {
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        var overview1 = await _repository.GetOverviewAsync(period);
        var overview2 = await _repository.GetOverviewAsync(period);

        // Deterministic check
        Assert.Equal(overview1.TotalEnergyKwh, overview2.TotalEnergyKwh);
        Assert.Equal(overview1.PeakNetworkKw, overview2.PeakNetworkKw);
        Assert.Equal(overview1.TotalAlerts, overview2.TotalAlerts);

        // Agrees with fixture records:
        // Day 1: 1425000.50, Day 2: 1480250.75 => Total: 2905251.25
        Assert.Equal(2905251.25m, overview1.TotalEnergyKwh);

        // Peak across days is Max(88400.25, 91200.80) => 91200.80
        Assert.Equal(91200.80m, overview1.PeakNetworkKw);

        // Alert windows count = 11
        Assert.Equal(11, overview1.TotalAlerts);

        // Distinct transformer counts (not alert window counts):
        // Theft alerts: 4 windows, but distinct TRs: TR-0012, TR-0045, TR-0078 => 3
        Assert.Equal(3, overview1.TheftTransformers);

        // Overload alerts: 4 windows, but distinct TRs: TR-0089, TR-0102, TR-0045 => 3
        Assert.Equal(3, overview1.OverloadTransformers);

        // Line drop alerts: 3 windows, but distinct TRs: TR-0210, TR-0215 => 2
        Assert.Equal(2, overview1.LinedropTransformers);

        // Total distinct affected: 7 (TR-0045 has both theft and overload, TR-0012, TR-0078, TR-0089, TR-0102, TR-0210, TR-0215)
        Assert.Equal(7, overview1.AffectedTransformers);

        // Latest reading timestamp
        Assert.Equal(new DateTimeOffset(2026, 8, 2, 23, 45, 0, TimeSpan.Zero), overview1.LatestReadingTimestamp);
    }

    [Fact]
    public async Task GetOverviewAsync_ForSingleDay_AggregatesOnlyThatDay()
    {
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 1));

        var overview = await _repository.GetOverviewAsync(period);

        // Only Day 1 values
        Assert.Equal(1425000.50m, overview.TotalEnergyKwh);
        Assert.Equal(88400.25m, overview.PeakNetworkKw);

        // Day 1 alerts = 5 windows
        Assert.Equal(5, overview.TotalAlerts);
        // Day 1 theft: TR-0012, TR-0045 => 2
        Assert.Equal(2, overview.TheftTransformers);
        // Day 1 overload: TR-0089 (2 alerts) => 1 distinct
        Assert.Equal(1, overview.OverloadTransformers);
        // Day 1 line drop: TR-0210 => 1
        Assert.Equal(1, overview.LinedropTransformers);
        // Day 1 distinct affected: TR-0012, TR-0045, TR-0089, TR-0210 => 4
        Assert.Equal(4, overview.AffectedTransformers);

        Assert.Equal(new DateTimeOffset(2026, 8, 1, 23, 45, 0, TimeSpan.Zero), overview.LatestReadingTimestamp);
    }

    [Fact]
    public async Task GetOverviewAsync_WhenRangeHasNoData_ReturnsNullMetrics()
    {
        var period = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));

        var overview = await _repository.GetOverviewAsync(period);

        Assert.Equal(period.Start, overview.PeriodStart);
        Assert.Equal(period.End, overview.PeriodEnd);
        Assert.Null(overview.TotalEnergyKwh);
        Assert.Null(overview.PeakNetworkKw);
        Assert.Null(overview.TotalAlerts);
        Assert.Null(overview.TheftTransformers);
        Assert.Null(overview.OverloadTransformers);
        Assert.Null(overview.LinedropTransformers);
        Assert.Null(overview.AffectedTransformers);
        Assert.Null(overview.LatestReadingTimestamp);
    }

    [Fact]
    public async Task GetDailyDemandAsync_ReturnsPointsOrderedByDateAscending()
    {
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        var points = await _repository.GetDailyDemandAsync(period);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateOnly(2026, 8, 1), points[0].DemandDate);
        Assert.Equal(1425000.50m, points[0].TotalDailyKwh);
        Assert.Equal(88400.25m, points[0].PeakNetworkKw);

        Assert.Equal(new DateOnly(2026, 8, 2), points[1].DemandDate);
        Assert.Equal(1480250.75m, points[1].TotalDailyKwh);
        Assert.Equal(91200.80m, points[1].PeakNetworkKw);
    }

    [Fact]
    public async Task GetDailyDemandAsync_WhenRangeHasNoData_ReturnsEmptyList()
    {
        var period = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));

        var points = await _repository.GetDailyDemandAsync(period);

        Assert.NotNull(points);
        Assert.Empty(points);
    }
}
