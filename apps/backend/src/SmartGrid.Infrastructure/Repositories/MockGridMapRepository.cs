using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;
using SmartGrid.Infrastructure.Data;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Mock repository implementation for grid map queries using deterministic in-memory fixtures.
/// </summary>
public sealed class MockGridMapRepository : IGridMapReadRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(
        GridMapCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        cancellationToken.ThrowIfCancellationRequested();

        var markers = MockGridMapData.GetMarkers(criteria);
        return Task.FromResult(markers);
    }
}
