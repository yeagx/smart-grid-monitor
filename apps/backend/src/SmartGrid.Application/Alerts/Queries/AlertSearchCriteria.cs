using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Alerts.Queries;

/// <summary>
/// Criteria for searching alerts. All filters are optional.
/// </summary>
public sealed class AlertSearchCriteria
{
    /// <summary>
    /// Optional inclusive date range. Filters on the alert's <c>end_ts</c>.
    /// </summary>
    public DateRange? Period { get; init; }

    /// <summary>
    /// Optional event type filter (e.g. "theft", "overload", "line_drop").
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>
    /// Optional district filter. Requires a join to dim_transformers.
    /// </summary>
    public string? District { get; init; }

    /// <summary>
    /// Optional transformer ID filter.
    /// </summary>
    public string? TransformerId { get; init; }

    /// <summary>Pagination request.</summary>
    public required PageRequest Pagination { get; init; }
}
