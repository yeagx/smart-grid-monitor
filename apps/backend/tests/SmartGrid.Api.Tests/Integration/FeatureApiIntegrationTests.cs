using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartGrid.Application.Accuracy.Models;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Transformers.Models;

namespace SmartGrid.Api.Tests.Integration;

/// <summary>
/// End-to-end checks (mock provider) for the alert log, transformers and accuracy endpoints.
/// </summary>
public class FeatureApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FeatureApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private sealed record PagedJson<T>(List<T> Items, int Page, int PageSize, long TotalCount, int TotalPages);

    [Fact]
    public async Task Alerts_Search_ReturnsNewestFirstWithDistrict()
    {
        var page = await _client.GetFromJsonAsync<PagedJson<AlertDto>>("/api/alerts?from=2026-08-01&to=2026-08-02&pageSize=5");

        Assert.NotNull(page);
        Assert.Equal(11, page.TotalCount);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.Items[0].StartTimestamp >= page.Items[^1].StartTimestamp);
        Assert.All(page.Items, a => Assert.NotEqual("Unknown", a.District));
    }

    [Fact]
    public async Task Alerts_Search_FiltersByTypeAndTransformer()
    {
        var page = await _client.GetFromJsonAsync<PagedJson<AlertDto>>("/api/alerts?type=overload&transformerId=TR-0089");

        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, a =>
        {
            Assert.Equal(EventTypes.Overload, a.EventType);
            Assert.Equal("TR-0089", a.TransformerId);
        });
    }

    [Theory]
    [InlineData("/api/alerts?type=fire")]
    [InlineData("/api/alerts?from=2026-08-01")]
    [InlineData("/api/alerts?pageSize=500")]
    [InlineData("/api/transformers?sort=random")]
    [InlineData("/api/accuracy/details?status=maybe")]
    [InlineData("/api/transformers/TR-0089/load-profile?from=2026-01-01&to=2026-03-01")]
    public async Task InvalidQueries_Return400(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Alerts_Summary_ReturnsAllThreeTypes()
    {
        var summary = await _client.GetFromJsonAsync<List<AlertSummaryByTypeDto>>("/api/alerts/summary?from=2026-08-01&to=2026-08-02");

        Assert.NotNull(summary);
        Assert.Equal(EventTypes.All, summary.Select(s => s.EventType));
        Assert.Equal(11, summary.Sum(s => s.TotalAlerts));
    }

    [Fact]
    public async Task Alerts_GetById_UnknownReturns404()
    {
        var response = await _client.GetAsync("/api/alerts/NOPE-123");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Transformers_SortByAlerts_PutsMostAlertedFirst()
    {
        var page = await _client.GetFromJsonAsync<PagedJson<TransformerListItemDto>>("/api/transformers?sort=alerts&pageSize=100");

        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.Equal(page.Items.Max(t => t.AlertCount), page.Items[0].AlertCount);
        Assert.DoesNotContain(page.Items, t => t.TransformerId == "TR-9999");
    }

    [Fact]
    public async Task Transformers_GetById_ReturnsDetailsOr404()
    {
        var ok = await _client.GetFromJsonAsync<TransformerDetailsDto>("/api/transformers/TR-0089");
        Assert.NotNull(ok);
        Assert.Equal("Heliopolis", ok.District);

        var missing = await _client.GetAsync("/api/transformers/TR-0000");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Transformers_LoadProfile_ReturnsHourlyPointsShowingOverload()
    {
        var points = await _client.GetFromJsonAsync<List<TransformerLoadPointDto>>(
            "/api/transformers/TR-0089/load-profile?from=2026-08-01&to=2026-08-02");

        Assert.NotNull(points);
        Assert.Equal(48, points.Count);
        Assert.All(points, p => Assert.Equal(4, p.ReadingCount));
        // TR-0089 (500 kW) has an overload alert at 13:00–13:30 on Aug 1.
        Assert.True(points.Single(p => p.HourTimestamp == new DateTimeOffset(2026, 8, 1, 13, 0, 0, TimeSpan.Zero)).MaxKw > 500m);
    }

    [Fact]
    public async Task Accuracy_Report_TotalsMatchByType()
    {
        var report = await _client.GetFromJsonAsync<AccuracyReportDto>("/api/accuracy");

        Assert.NotNull(report);
        Assert.Equal(3, report.ByType.Count);
        Assert.Equal(report.ByType.Sum(t => t.TruePositives), report.TotalTruePositives);
        Assert.InRange(report.OverallPrecision!.Value, 0m, 1m);
        Assert.InRange(report.OverallRecall!.Value, 0m, 1m);
    }

    [Fact]
    public async Task Accuracy_Details_FiltersByStatus()
    {
        var page = await _client.GetFromJsonAsync<PagedJson<AccuracyDetailDto>>("/api/accuracy/details?status=False%20Negative");

        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, d => Assert.Equal(DetectionStatuses.FalseNegative, d.DetectionStatus));
    }
}
