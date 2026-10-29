using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;

namespace SmartGrid.Application.GridMap;

/// <summary>
/// Application service for retrieving and managing grid map visualization data.
/// </summary>
public interface IGridMapService
{
    /// <summary>
    /// Retrieves map markers for the given search criteria.
    /// </summary>
    Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(
        GridMapCriteria criteria,
        CancellationToken cancellationToken = default);
}
