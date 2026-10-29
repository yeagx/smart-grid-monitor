using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;

namespace SmartGrid.Application.GridMap;

/// <summary>
/// Application service implementation for grid map operations.
/// </summary>
public sealed class GridMapService : IGridMapService
{
    private readonly IGridMapReadRepository _repository;

    public GridMapService(IGridMapReadRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(
        GridMapCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (!criteria.TryValidate(out var validationError))
        {
            throw new ArgumentException(validationError, nameof(criteria));
        }

        return _repository.GetMapMarkersAsync(criteria, cancellationToken);
    }
}
