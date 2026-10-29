using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Alerts.Queries;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Alerts;

/// <summary>
/// Application service implementation for alert searches and lookups.
/// </summary>
public sealed class AlertService : IAlertService
{
    private readonly IAlertReadRepository _repository;

    public AlertService(IAlertReadRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public Task<PagedResult<AlertDto>> SearchAsync(
        AlertSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (criteria.EventType is not null && !EventTypes.IsValid(criteria.EventType))
        {
            throw new ArgumentException(
                $"Unsupported event type '{criteria.EventType}'. Supported types are: {string.Join(", ", EventTypes.All)}.",
                nameof(criteria));
        }

        return _repository.SearchAsync(criteria, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AlertDto?> GetByIdAsync(
        string anomalyId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(anomalyId))
        {
            throw new ArgumentException("Anomaly ID must not be null or whitespace.", nameof(anomalyId));
        }

        return _repository.GetByIdAsync(anomalyId, cancellationToken);
    }

    public Task<IReadOnlyList<AlertSummaryByTypeDto>> GetSummaryByTypeAsync(
        DateRange period,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);
        return _repository.GetSummaryByTypeAsync(period, cancellationToken);
    }
}
