using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.GridMap;
using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;

namespace SmartGrid.Application.Tests.GridMap;

public class GridMapServiceTests
{
    private sealed class FakeGridMapRepository : IGridMapReadRepository
    {
        public GridMapCriteria? LastCriteria { get; private set; }
        public IReadOnlyList<MapMarkerDto> ReturnValue { get; set; } = [];

        public Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(GridMapCriteria criteria, CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(ReturnValue);
        }
    }

    [Fact]
    public async Task GetMapMarkersAsync_WithValidCriteria_CallsRepositoryAndReturnsMarkers()
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

        var fakeRepo = new FakeGridMapRepository { ReturnValue = expectedMarkers };
        var service = new GridMapService(fakeRepo);

        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2))
        };

        var result = await service.GetMapMarkersAsync(criteria);

        Assert.Same(expectedMarkers, result);
        Assert.NotNull(fakeRepo.LastCriteria);
        Assert.Equal(criteria.Period, fakeRepo.LastCriteria.Period);
    }

    [Fact]
    public async Task GetMapMarkersAsync_WithInvalidCriteria_ThrowsArgumentException()
    {
        var fakeRepo = new FakeGridMapRepository();
        var service = new GridMapService(fakeRepo);

        var criteria = new GridMapCriteria
        {
            Period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2)),
            EventType = "unsupported_type"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GetMapMarkersAsync(criteria));
        Assert.Contains("Unsupported threat type", ex.Message);
    }

    [Fact]
    public async Task GetMapMarkersAsync_WithNullCriteria_ThrowsArgumentNullException()
    {
        var fakeRepo = new FakeGridMapRepository();
        var service = new GridMapService(fakeRepo);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetMapMarkersAsync(null!));
    }
}
