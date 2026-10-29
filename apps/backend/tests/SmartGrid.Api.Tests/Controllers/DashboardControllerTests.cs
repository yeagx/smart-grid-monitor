using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartGrid.Api.Controllers;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Dashboard;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Api.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task GetSummary_WithValidRange_ReturnsOkWithOverviewDto()
    {
        var expectedDto = new DashboardOverviewDto
        {
            PeriodStart = new DateOnly(2026, 8, 1),
            PeriodEnd = new DateOnly(2026, 8, 2),
            TotalEnergyKwh = 120000m,
            PeakNetworkKw = 8200m,
            TotalAlerts = 10,
            TheftTransformers = 2,
            OverloadTransformers = 4,
            LinedropTransformers = 1,
            AffectedTransformers = 6,
            LatestReadingTimestamp = DateTimeOffset.UtcNow
        };

        var fakeService = new FakeDashboardService { OverviewResult = expectedDto };
        var controller = CreateController(fakeService, "/api/dashboard/summary");

        var actionResult = await controller.GetSummary("2026-08-01", "2026-08-02", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Same(expectedDto, okResult.Value);
        Assert.NotNull(fakeService.LastOverviewPeriod);
        Assert.Equal(new DateOnly(2026, 8, 1), fakeService.LastOverviewPeriod.Start);
        Assert.Equal(new DateOnly(2026, 8, 2), fakeService.LastOverviewPeriod.End);
    }

    [Theory]
    [InlineData(null, "2026-08-02", "The 'from' query parameter is required.")]
    [InlineData("", "2026-08-02", "The 'from' query parameter is required.")]
    [InlineData("2026-08-01", null, "The 'to' query parameter is required.")]
    [InlineData("2026-08-01", "", "The 'to' query parameter is required.")]
    public async Task GetSummary_WithMissingDates_ReturnsBadRequestProblemDetails(string? from, string? to, string expectedDetail)
    {
        var fakeService = new FakeDashboardService();
        var controller = CreateController(fakeService, "/api/dashboard/summary");

        var actionResult = await controller.GetSummary(from, to, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains(expectedDetail, problem.Detail);
    }

    [Theory]
    [InlineData("invalid-date", "2026-08-02", "from")]
    [InlineData("2026-08-01", "bad-date", "to")]
    public async Task GetSummary_WithInvalidDateFormat_ReturnsBadRequestProblemDetails(string from, string to, string paramName)
    {
        var fakeService = new FakeDashboardService();
        var controller = CreateController(fakeService, "/api/dashboard/summary");

        var actionResult = await controller.GetSummary(from, to, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains(paramName, problem.Detail);
        Assert.Contains("not a valid date", problem.Detail);
    }

    [Fact]
    public async Task GetSummary_WithStartAfterEnd_ReturnsBadRequestProblemDetails()
    {
        var fakeService = new FakeDashboardService();
        var controller = CreateController(fakeService, "/api/dashboard/summary");

        var actionResult = await controller.GetSummary("2026-08-05", "2026-08-01", CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("must be earlier than or equal to", problem.Detail);
    }

    [Fact]
    public async Task GetSummary_WithRangeExceedingMaxDays_ReturnsBadRequestProblemDetails()
    {
        var fakeService = new FakeDashboardService();
        var controller = CreateController(fakeService, "/api/dashboard/summary");

        // 368 days
        var actionResult = await controller.GetSummary("2025-01-01", "2026-01-03", CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("exceeds the maximum allowed limit of 366 days", problem.Detail);
    }

    [Fact]
    public async Task GetSummary_WhenServiceThrows_PropagatesExceptionWithoutFabricatingData()
    {
        var fakeService = new FakeDashboardService
        {
            ExceptionToThrow = new InvalidOperationException("Snowflake driver query failed")
        };
        var controller = CreateController(fakeService, "/api/dashboard/summary");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.GetSummary("2026-08-01", "2026-08-02", CancellationToken.None));

        Assert.Equal("Snowflake driver query failed", ex.Message);
    }

    [Fact]
    public async Task GetDemand_WithValidRange_ReturnsOkWithDemandPoints()
    {
        var expectedPoints = new List<DailyDemandPointDto>
        {
            new(new DateOnly(2026, 8, 1), 60000m, 4500m),
            new(new DateOnly(2026, 8, 2), 62000m, 4650m)
        };

        var fakeService = new FakeDashboardService { DemandResult = expectedPoints };
        var controller = CreateController(fakeService, "/api/dashboard/demand");

        var actionResult = await controller.GetDemand("2026-08-01", "2026-08-02", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Same(expectedPoints, okResult.Value);
        Assert.NotNull(fakeService.LastDemandPeriod);
        Assert.Equal(new DateOnly(2026, 8, 1), fakeService.LastDemandPeriod.Start);
        Assert.Equal(new DateOnly(2026, 8, 2), fakeService.LastDemandPeriod.End);
    }

    [Theory]
    [InlineData(null, "2026-08-02", "The 'from' query parameter is required.")]
    [InlineData("2026-08-01", null, "The 'to' query parameter is required.")]
    public async Task GetDemand_WithMissingDates_ReturnsBadRequestProblemDetails(string? from, string? to, string expectedDetail)
    {
        var fakeService = new FakeDashboardService();
        var controller = CreateController(fakeService, "/api/dashboard/demand");

        var actionResult = await controller.GetDemand(from, to, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains(expectedDetail, problem.Detail);
    }

    [Fact]
    public async Task GetDemand_WithStartAfterEnd_ReturnsBadRequestProblemDetails()
    {
        var fakeService = new FakeDashboardService();
        var controller = CreateController(fakeService, "/api/dashboard/demand");

        var actionResult = await controller.GetDemand("2026-08-10", "2026-08-02", CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("must be earlier than or equal to", problem.Detail);
    }

    [Fact]
    public async Task GetDemand_WithRangeExceedingMaxDays_ReturnsBadRequestProblemDetails()
    {
        var fakeService = new FakeDashboardService();
        var controller = CreateController(fakeService, "/api/dashboard/demand");

        var actionResult = await controller.GetDemand("2025-01-01", "2026-01-05", CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("exceeds the maximum allowed limit of 366 days", problem.Detail);
    }

    [Fact]
    public async Task GetDemand_WhenServiceThrows_PropagatesExceptionWithoutFabricatingData()
    {
        var fakeService = new FakeDashboardService
        {
            ExceptionToThrow = new TimeoutException("Warehouse query timed out")
        };
        var controller = CreateController(fakeService, "/api/dashboard/demand");

        var ex = await Assert.ThrowsAsync<TimeoutException>(
            () => controller.GetDemand("2026-08-01", "2026-08-02", CancellationToken.None));

        Assert.Equal("Warehouse query timed out", ex.Message);
    }

    private static DashboardController CreateController(IDashboardService service, string path)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        var controller = new DashboardController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return controller;
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public DashboardOverviewDto? OverviewResult { get; set; }
        public IReadOnlyList<DailyDemandPointDto>? DemandResult { get; set; }
        public Exception? ExceptionToThrow { get; set; }

        public DateRange? LastOverviewPeriod { get; private set; }
        public CancellationToken LastOverviewToken { get; private set; }

        public DateRange? LastDemandPeriod { get; private set; }
        public CancellationToken LastDemandToken { get; private set; }

        public Task<DashboardOverviewDto> GetOverviewAsync(DateRange period, CancellationToken cancellationToken = default)
        {
            LastOverviewPeriod = period;
            LastOverviewToken = cancellationToken;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(OverviewResult ?? new DashboardOverviewDto
            {
                PeriodStart = period.Start,
                PeriodEnd = period.End
            });
        }

        public Task<IReadOnlyList<DailyDemandPointDto>> GetDailyDemandAsync(DateRange period, CancellationToken cancellationToken = default)
        {
            LastDemandPeriod = period;
            LastDemandToken = cancellationToken;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(DemandResult ?? (IReadOnlyList<DailyDemandPointDto>)Array.Empty<DailyDemandPointDto>());
        }
    }
}
