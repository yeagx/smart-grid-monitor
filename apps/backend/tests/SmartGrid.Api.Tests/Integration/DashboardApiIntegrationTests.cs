using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Api.Tests.Integration;

public class DashboardApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DashboardApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSummary_WithValidRange_Returns200WithMockFixtureValues()
    {
        var response = await _client.GetAsync("/api/dashboard/summary?from=2026-08-01&to=2026-08-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var overview = await response.Content.ReadFromJsonAsync<DashboardOverviewDto>();

        Assert.NotNull(overview);
        Assert.Equal(new DateOnly(2026, 8, 1), overview.PeriodStart);
        Assert.Equal(new DateOnly(2026, 8, 2), overview.PeriodEnd);
        Assert.Equal(2905251.25m, overview.TotalEnergyKwh);
        Assert.Equal(91200.80m, overview.PeakNetworkKw);
        Assert.Equal(11, overview.TotalAlerts);
        Assert.Equal(3, overview.TheftTransformers);
        Assert.Equal(3, overview.OverloadTransformers);
        Assert.Equal(2, overview.LinedropTransformers);
        Assert.Equal(7, overview.AffectedTransformers);
        Assert.Equal(new DateTimeOffset(2026, 8, 2, 23, 45, 0, TimeSpan.Zero), overview.LatestReadingTimestamp);

        // Verify JSON camelCase formatting directly
        var rawJson = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(rawJson);
        var root = jsonDoc.RootElement;
        Assert.True(root.TryGetProperty("totalEnergyKwh", out _));
        Assert.True(root.TryGetProperty("peakNetworkKw", out _));
        Assert.True(root.TryGetProperty("latestReadingTimestamp", out _));
    }

    [Fact]
    public async Task GetSummary_WhenRangeHasNoData_Returns200WithNullMetrics()
    {
        var response = await _client.GetAsync("/api/dashboard/summary?from=2026-07-01&to=2026-07-05");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var overview = await response.Content.ReadFromJsonAsync<DashboardOverviewDto>();

        Assert.NotNull(overview);
        Assert.Equal(new DateOnly(2026, 7, 1), overview.PeriodStart);
        Assert.Equal(new DateOnly(2026, 7, 5), overview.PeriodEnd);
        Assert.Null(overview.TotalEnergyKwh);
        Assert.Null(overview.PeakNetworkKw);
        Assert.Null(overview.TotalAlerts);
        Assert.Null(overview.TheftTransformers);
        Assert.Null(overview.OverloadTransformers);
        Assert.Null(overview.LinedropTransformers);
        Assert.Null(overview.AffectedTransformers);
        Assert.Null(overview.LatestReadingTimestamp);
    }

    [Theory]
    [InlineData("/api/dashboard/summary", "The 'from' query parameter is required.")]
    [InlineData("/api/dashboard/summary?from=2026-08-01", "The 'to' query parameter is required.")]
    [InlineData("/api/dashboard/summary?from=bad-date&to=2026-08-02", "not a valid date")]
    [InlineData("/api/dashboard/summary?from=2026-08-05&to=2026-08-01", "must be earlier than or equal to")]
    [InlineData("/api/dashboard/summary?from=2024-01-01&to=2025-01-05", "exceeds the maximum allowed limit of 366 days")]
    public async Task GetSummary_WithInvalidParameters_Returns400ProblemDetails(string url, string expectedErrorSubstr)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Contains(expectedErrorSubstr, problem.Detail);
    }

    [Fact]
    public async Task GetDemand_WithValidRange_Returns200OrderedByDate()
    {
        var response = await _client.GetAsync("/api/dashboard/demand?from=2026-08-01&to=2026-08-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var points = await response.Content.ReadFromJsonAsync<List<DailyDemandPointDto>>();

        Assert.NotNull(points);
        Assert.Equal(2, points.Count);

        // Ordered by date ascending
        Assert.Equal(new DateOnly(2026, 8, 1), points[0].DemandDate);
        Assert.Equal(1425000.50m, points[0].TotalDailyKwh);
        Assert.Equal(88400.25m, points[0].PeakNetworkKw);

        Assert.Equal(new DateOnly(2026, 8, 2), points[1].DemandDate);
        Assert.Equal(1480250.75m, points[1].TotalDailyKwh);
        Assert.Equal(91200.80m, points[1].PeakNetworkKw);

        // Verify JSON camelCase formatting directly
        var rawJson = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(rawJson);
        var firstItem = jsonDoc.RootElement[0];
        Assert.True(firstItem.TryGetProperty("demandDate", out _));
        Assert.True(firstItem.TryGetProperty("totalDailyKwh", out _));
        Assert.True(firstItem.TryGetProperty("peakNetworkKw", out _));
    }

    [Fact]
    public async Task GetDemand_WhenRangeHasNoData_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/dashboard/demand?from=2026-07-01&to=2026-07-05");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var points = await response.Content.ReadFromJsonAsync<List<DailyDemandPointDto>>();

        Assert.NotNull(points);
        Assert.Empty(points);
    }

    [Theory]
    [InlineData("/api/dashboard/demand", "The 'from' query parameter is required.")]
    [InlineData("/api/dashboard/demand?from=2026-08-01", "The 'to' query parameter is required.")]
    [InlineData("/api/dashboard/demand?from=2026-08-01&to=invalid", "not a valid date")]
    [InlineData("/api/dashboard/demand?from=2026-08-05&to=2026-08-01", "must be earlier than or equal to")]
    public async Task GetDemand_WithInvalidParameters_Returns400ProblemDetails(string url, string expectedErrorSubstr)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Contains(expectedErrorSubstr, problem.Detail);
    }
}
