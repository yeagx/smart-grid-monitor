using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Dashboard;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Application.Tests.Dashboard;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetOverviewAsync_WithValidPeriod_CallsRepositoryAndReturnsDto()
    {
        var expectedDto = new DashboardOverviewDto
        {
            PeriodStart = new DateOnly(2026, 8, 1),
            PeriodEnd = new DateOnly(2026, 8, 2),
            TotalEnergyKwh = 125000.50m,
            PeakNetworkKw = 8500.25m,
            TotalAlerts = 14,
            TheftTransformers = 3,
            OverloadTransformers = 5,
            LinedropTransformers = 2,
            AffectedTransformers = 8,
            LatestReadingTimestamp = DateTimeOffset.UtcNow
        };

        var fakeRepo = new FakeDashboardReadRepository
        {
            OverviewResult = expectedDto
        };

        var service = new DashboardService(fakeRepo);
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        var result = await service.GetOverviewAsync(period);

        Assert.NotNull(result);
        Assert.Same(expectedDto, result);
        Assert.Equal(period, fakeRepo.LastOverviewPeriod);
    }

    [Fact]
    public async Task GetOverviewAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var fakeRepo = new FakeDashboardReadRepository
        {
            OverviewResult = new DashboardOverviewDto
            {
                PeriodStart = new DateOnly(2026, 8, 1),
                PeriodEnd = new DateOnly(2026, 8, 2)
            }
        };

        var service = new DashboardService(fakeRepo);
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        await service.GetOverviewAsync(period, token);

        Assert.Equal(token, fakeRepo.LastOverviewToken);
    }

    [Fact]
    public async Task GetOverviewAsync_WhenRepositoryThrows_PropagatesExceptionWithoutFabricatingData()
    {
        var fakeRepo = new FakeDashboardReadRepository
        {
            ExceptionToThrow = new InvalidOperationException("Snowflake connection failed")
        };

        var service = new DashboardService(fakeRepo);
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetOverviewAsync(period));
        Assert.Equal("Snowflake connection failed", ex.Message);
    }

    [Fact]
    public async Task GetOverviewAsync_WithNullPeriod_ThrowsArgumentNullException()
    {
        var fakeRepo = new FakeDashboardReadRepository();
        var service = new DashboardService(fakeRepo);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetOverviewAsync(null!));
    }

    [Fact]
    public async Task GetDailyDemandAsync_WithValidPeriod_CallsRepositoryAndReturnsPoints()
    {
        var expectedPoints = new List<DailyDemandPointDto>
        {
            new(new DateOnly(2026, 8, 1), 54000.0m, 4200.5m),
            new(new DateOnly(2026, 8, 2), 56000.0m, 4350.2m)
        };

        var fakeRepo = new FakeDashboardReadRepository
        {
            DemandResult = expectedPoints
        };

        var service = new DashboardService(fakeRepo);
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        var result = await service.GetDailyDemandAsync(period);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedPoints[0].DemandDate, result[0].DemandDate);
        Assert.Equal(period, fakeRepo.LastDemandPeriod);
    }

    [Fact]
    public async Task GetDailyDemandAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var fakeRepo = new FakeDashboardReadRepository
        {
            DemandResult = []
        };

        var service = new DashboardService(fakeRepo);
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        await service.GetDailyDemandAsync(period, token);

        Assert.Equal(token, fakeRepo.LastDemandToken);
    }

    [Fact]
    public async Task GetDailyDemandAsync_WhenRepositoryThrows_PropagatesExceptionWithoutFabricatingData()
    {
        var fakeRepo = new FakeDashboardReadRepository
        {
            ExceptionToThrow = new TimeoutException("Database timeout")
        };

        var service = new DashboardService(fakeRepo);
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));

        var ex = await Assert.ThrowsAsync<TimeoutException>(() => service.GetDailyDemandAsync(period));
        Assert.Equal("Database timeout", ex.Message);
    }

    [Fact]
    public async Task GetDailyDemandAsync_WithNullPeriod_ThrowsArgumentNullException()
    {
        var fakeRepo = new FakeDashboardReadRepository();
        var service = new DashboardService(fakeRepo);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetDailyDemandAsync(null!));
    }

    private sealed class FakeDashboardReadRepository : IDashboardReadRepository
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
