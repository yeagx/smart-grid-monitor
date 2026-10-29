using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartGrid.Api.Controllers;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.GridMap;
using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;

namespace SmartGrid.Api.Tests.Controllers;

public class GridControllerTests
{
    private sealed class FakeGridMapService : IGridMapService
    {
        public GridMapCriteria? LastCriteria { get; private set; }
        public IReadOnlyList<MapMarkerDto> ReturnValue { get; set; } = [];

        public Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(GridMapCriteria criteria, CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(ReturnValue);
        }
    }

    private static GridController CreateController(IGridMapService service, string path = "/api/grid/map-markers")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        return new GridController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task GetMapMarkers_WithValidDates_ReturnsOkWithMarkers()
    {
        var expectedMarkers = new List<MapMarkerDto>
        {
            new()
            {
                TransformerId = "TR-0012",
                District = "Dokki",
                Governorate = "Giza",
                FeederId = "F-01",
                SubstationId = "SS-DOKKI",
                Latitude = 30.0384,
                Longitude = 31.2109,
                CapacityKw = 500m,
                PeakKwRecorded = 420.50m,
                PeakLoadPercentage = 84.10m,
                TheftAlertCount = 2,
                OverloadAlertCount = 0,
                LineDropAlertCount = 0,
                TotalAlertCount = 2,
                LatestAlertAt = DateTimeOffset.UtcNow
            }
        };

        var fakeService = new FakeGridMapService { ReturnValue = expectedMarkers };
        var controller = CreateController(fakeService);

        var actionResult = await controller.GetMapMarkers("2026-08-01", "2026-08-02", null, null, false);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Same(expectedMarkers, okResult.Value);
        Assert.NotNull(fakeService.LastCriteria);
        Assert.Equal(new DateOnly(2026, 8, 1), fakeService.LastCriteria.Period.Start);
        Assert.Equal(new DateOnly(2026, 8, 2), fakeService.LastCriteria.Period.End);
        Assert.False(fakeService.LastCriteria.OnlyWithAlerts);
    }

    [Fact]
    public async Task GetMapMarkers_WithAllFilters_PassesCriteriaToService()
    {
        var fakeService = new FakeGridMapService();
        var controller = CreateController(fakeService);

        var actionResult = await controller.GetMapMarkers("2026-08-01", "2026-08-02", "Dokki", EventTypes.Theft, true);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.NotNull(fakeService.LastCriteria);
        Assert.Equal("Dokki", fakeService.LastCriteria.District);
        Assert.Equal(EventTypes.Theft, fakeService.LastCriteria.EventType);
        Assert.True(fakeService.LastCriteria.OnlyWithAlerts);
    }

    [Theory]
    [InlineData(null, "2026-08-02", "The 'from' query parameter is required.")]
    [InlineData("", "2026-08-02", "The 'from' query parameter is required.")]
    [InlineData("2026-08-01", null, "The 'to' query parameter is required.")]
    [InlineData("2026-08-01", "", "The 'to' query parameter is required.")]
    public async Task GetMapMarkers_WithMissingDates_ReturnsBadRequestProblemDetails(string? from, string? to, string expectedDetail)
    {
        var fakeService = new FakeGridMapService();
        var controller = CreateController(fakeService);

        var actionResult = await controller.GetMapMarkers(from, to, null, null, false);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains(expectedDetail, problem.Detail);
    }

    [Fact]
    public async Task GetMapMarkers_WithStartAfterEnd_ReturnsBadRequestProblemDetails()
    {
        var fakeService = new FakeGridMapService();
        var controller = CreateController(fakeService);

        var actionResult = await controller.GetMapMarkers("2026-08-05", "2026-08-01", null, null, false);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("must be earlier than or equal to", problem.Detail);
    }

    [Theory]
    [InlineData("invalid_threat")]
    [InlineData("fire")]
    [InlineData("surge")]
    public async Task GetMapMarkers_WithUnsupportedThreatType_ReturnsBadRequestProblemDetails(string unsupportedType)
    {
        var fakeService = new FakeGridMapService();
        var controller = CreateController(fakeService);

        var actionResult = await controller.GetMapMarkers("2026-08-01", "2026-08-02", null, unsupportedType, false);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Invalid Threat Type", problem.Title);
        Assert.Contains("Unsupported threat type", problem.Detail);
    }
}
