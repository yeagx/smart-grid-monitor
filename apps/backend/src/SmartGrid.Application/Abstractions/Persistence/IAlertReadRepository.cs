using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Alerts.Queries;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Abstractions.Persistence;

/// <summary>
/// Read-only repository for anomaly alerts.
/// </summary>
/// <remarks>
/// Backed by <c>anomaly_flags</c> and <c>dim_transformers</c> (for district/governorate joins).
/// </remarks>
public interface IAlertReadRepository
{
    /// <summary>
    /// Searches alerts using the specified criteria, returning a paginated result.
    /// Results are ordered by end timestamp descending.
    /// </summary>
    Task<PagedResult<AlertDto>> SearchAsync(
        AlertSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single alert by its anomaly ID.
    /// Returns <c>null</c> when no matching alert exists.
    /// </summary>
    Task<AlertDto?> GetByIdAsync(
        string anomalyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves alert counts and distinct affected-transformer counts grouped by event type
    /// for the specified inclusive date range.
    /// </summary>
    Task<IReadOnlyList<AlertSummaryByTypeDto>> GetSummaryByTypeAsync(
        DateRange period,
        CancellationToken cancellationToken = default);
}
