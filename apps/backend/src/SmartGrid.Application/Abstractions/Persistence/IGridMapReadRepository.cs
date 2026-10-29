using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;

namespace SmartGrid.Application.Abstractions.Persistence;

/// <summary>
/// Read-only repository for querying geographic transformer markers
/// combined with aggregated alert metrics and historical peak loading.
/// </summary>
public interface IGridMapReadRepository
{
    /// <summary>
    /// Retrieves a collection of valid map markers filtered according to the specified criteria.
    /// </summary>
    /// <param name="criteria">Geographic, temporal, and alert filtering criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of enriched transformer map markers.</returns>
    Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(
        GridMapCriteria criteria,
        CancellationToken cancellationToken = default);
}
