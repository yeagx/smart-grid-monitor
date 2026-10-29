using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Alerts.Queries;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Alerts;

/// <summary>
/// Application service for querying and inspecting detected grid anomaly alerts.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Searches alerts using the specified criteria and returns paginated results.
    /// </summary>
    Task<PagedResult<AlertDto>> SearchAsync(
        AlertSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an individual alert by its anomaly ID.
    /// </summary>
    Task<AlertDto?> GetByIdAsync(
        string anomalyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertSummaryByTypeDto>> GetSummaryByTypeAsync(
        DateRange period,
        CancellationToken cancellationToken = default);
}
