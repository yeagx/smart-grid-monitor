using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.GridMap.Queries;
using SmartGrid.Infrastructure.Data;
using SmartGrid.Infrastructure.Repositories;

namespace SmartGrid.Api.Tests.Repositories;

public class MockGridMapRepositoryTests
{
    private readonly MockGridMapRepository _repository = new();

    [Fact]
    public async Task GetMapMarkersAsync_ForBothSimulationDays_ReturnsAllValidTransformers()
    {
        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2))
        };

        var markers = await _repository.GetMapMarkersAsync(criteria);

        // Does not include invalid coordinate entries (TR-9999, TR-9998)
        Assert.All(markers, m =>
        {
            Assert.True(MockGridMapData.IsValidCoordinate(m.Latitude, m.Longitude));
            Assert.False(m.Latitude == 0.0 && m.Longitude == 0.0);
            Assert.False(m.TransformerId == "TR-9999");
            Assert.False(m.TransformerId == "TR-9998");
        });

        // Each transformer appears at most once
        var distinctIds = markers.Select(m => m.TransformerId).Distinct().Count();
        Assert.Equal(markers.Count, distinctIds);

        // Verified transformer with multiple alerts across days: TR-0012 has 2 theft alerts
        var tr0012 = markers.Single(m => m.TransformerId == "TR-0012");
        Assert.Equal("Dokki", tr0012.District);
        Assert.Equal("Giza", tr0012.Governorate);
        Assert.Equal(2, tr0012.TheftAlertCount);
        Assert.Equal(0, tr0012.OverloadAlertCount);
        Assert.Equal(0, tr0012.LineDropAlertCount);
        Assert.Equal(2, tr0012.TotalAlertCount);
        Assert.Equal(new DateTimeOffset(2026, 8, 2, 7, 0, 0, TimeSpan.Zero), tr0012.LatestAlertAt);
        Assert.Equal(420.50m, tr0012.PeakKwRecorded);
        Assert.Equal(84.10m, tr0012.PeakLoadPercentage);

        // Transformer with multiple alert types: TR-0045 has 1 theft (Day 1) and 1 overload (Day 2)
        var tr0045 = markers.Single(m => m.TransformerId == "TR-0045");
        Assert.Equal(1, tr0045.TheftAlertCount);
        Assert.Equal(1, tr0045.OverloadAlertCount);
        Assert.Equal(0, tr0045.LineDropAlertCount);
        Assert.Equal(2, tr0045.TotalAlertCount);
        Assert.Equal(new DateTimeOffset(2026, 8, 2, 19, 30, 0, TimeSpan.Zero), tr0045.LatestAlertAt);

        // Transformer with multiple overloads on Day 1: TR-0089 has 2 overload alerts
        var tr0089 = markers.Single(m => m.TransformerId == "TR-0089");
        Assert.Equal(0, tr0089.TheftAlertCount);
        Assert.Equal(2, tr0089.OverloadAlertCount);
        Assert.Equal(0, tr0089.LineDropAlertCount);
        Assert.Equal(2, tr0089.TotalAlertCount);

        // Transformer with line-drop alerts: TR-0210 has 2 line drops
        var tr0210 = markers.Single(m => m.TransformerId == "TR-0210");
        Assert.Equal(0, tr0210.TheftAlertCount);
        Assert.Equal(0, tr0210.OverloadAlertCount);
        Assert.Equal(2, tr0210.LineDropAlertCount);
        Assert.Equal(2, tr0210.TotalAlertCount);

        // Transformer with zero alerts: TR-0301
        var tr0301 = markers.Single(m => m.TransformerId == "TR-0301");
        Assert.Equal(0, tr0301.TotalAlertCount);
        Assert.Equal(0, tr0301.TheftAlertCount);
        Assert.Equal(0, tr0301.OverloadAlertCount);
        Assert.Equal(0, tr0301.LineDropAlertCount);
        Assert.Null(tr0301.LatestAlertAt);

        // Transformer with null peak metrics: TR-0306
        var tr0306 = markers.Single(m => m.TransformerId == "TR-0306");
        Assert.Null(tr0306.PeakKwRecorded);
        Assert.Null(tr0306.PeakLoadPercentage);
    }

    [Fact]
    public async Task GetMapMarkersAsync_WithDistrictFilter_ReturnsOnlyTransformersInDistrict()
    {
        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2)),
            District = "Dokki"
        };

        var markers = await _repository.GetMapMarkersAsync(criteria);

        Assert.NotEmpty(markers);
        Assert.All(markers, m => Assert.Equal("Dokki", m.District, ignoreCase: true));
    }

    [Fact]
    public async Task GetMapMarkersAsync_WithOnlyWithAlertsTrue_ReturnsOnlyTransformersHavingAlerts()
    {
        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2)),
            OnlyWithAlerts = true
        };

        var markers = await _repository.GetMapMarkersAsync(criteria);

        Assert.NotEmpty(markers);
        Assert.All(markers, m => Assert.True(m.TotalAlertCount > 0));
        Assert.DoesNotContain(markers, m => m.TransformerId == "TR-0301");
    }

    [Fact]
    public async Task GetMapMarkersAsync_WithTheftFilterAndOnlyWithAlertsTrue_ReturnsOnlyTheftTransformers()
    {
        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2)),
            EventType = EventTypes.Theft,
            OnlyWithAlerts = true
        };

        var markers = await _repository.GetMapMarkersAsync(criteria);

        // Only TR-0012, TR-0045, TR-0078 have theft in Aug 1-2
        Assert.Equal(3, markers.Count);
        Assert.All(markers, m => Assert.True(m.TheftAlertCount > 0));
        Assert.Contains(markers, m => m.TransformerId == "TR-0012");
        Assert.Contains(markers, m => m.TransformerId == "TR-0045");
        Assert.Contains(markers, m => m.TransformerId == "TR-0078");

        // The returned marker for TR-0045 must still report its full alert breakdown (including overload)
        var tr0045 = markers.Single(m => m.TransformerId == "TR-0045");
        Assert.Equal(1, tr0045.TheftAlertCount);
        Assert.Equal(1, tr0045.OverloadAlertCount);
        Assert.Equal(2, tr0045.TotalAlertCount);
    }

    [Fact]
    public async Task GetMapMarkersAsync_ForSingleDay_AggregatesOnlyOverlappingAlerts()
    {
        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 1)),
            OnlyWithAlerts = true
        };

        var markers = await _repository.GetMapMarkersAsync(criteria);

        // Day 1 affected: TR-0012 (theft), TR-0045 (theft), TR-0089 (overload), TR-0210 (line drop) => 4
        Assert.Equal(4, markers.Count);
        Assert.Contains(markers, m => m.TransformerId == "TR-0012");
        Assert.Contains(markers, m => m.TransformerId == "TR-0045");
        Assert.Contains(markers, m => m.TransformerId == "TR-0089");
        Assert.Contains(markers, m => m.TransformerId == "TR-0210");

        // TR-0012 on Day 1 has only 1 theft alert (not 2)
        var tr0012 = markers.Single(m => m.TransformerId == "TR-0012");
        Assert.Equal(1, tr0012.TheftAlertCount);
        Assert.Equal(1, tr0012.TotalAlertCount);

        // TR-0045 on Day 1 has only theft (overload was on Day 2)
        var tr0045 = markers.Single(m => m.TransformerId == "TR-0045");
        Assert.Equal(1, tr0045.TheftAlertCount);
        Assert.Equal(0, tr0045.OverloadAlertCount);
        Assert.Equal(1, tr0045.TotalAlertCount);
    }

    [Fact]
    public async Task GetMapMarkersAsync_WhenDateRangeHasNoAlerts_ReturnsTransformersWithZeroAlerts()
    {
        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5)),
            OnlyWithAlerts = false
        };

        var markers = await _repository.GetMapMarkersAsync(criteria);

        Assert.NotEmpty(markers);
        Assert.All(markers, m =>
        {
            Assert.Equal(0, m.TotalAlertCount);
            Assert.Equal(0, m.TheftAlertCount);
            Assert.Equal(0, m.OverloadAlertCount);
            Assert.Equal(0, m.LineDropAlertCount);
            Assert.Null(m.LatestAlertAt);
        });
    }

    [Fact]
    public async Task GetMapMarkersAsync_WhenDateRangeHasNoAlertsAndOnlyWithAlertsTrue_ReturnsEmptyList()
    {
        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5)),
            OnlyWithAlerts = true
        };

        var markers = await _repository.GetMapMarkersAsync(criteria);

        Assert.Empty(markers);
    }
}
