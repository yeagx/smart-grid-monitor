using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.GridMap.Queries;

/// <summary>
/// Criteria for querying transformer map markers and their alert aggregations.
/// </summary>
public sealed record GridMapCriteria
{
    /// <summary>Inclusive calendar date range for alert aggregation.</summary>
    public required DateRange Period { get; init; }

    /// <summary>Optional district filter to restrict results to a single district.</summary>
    public string? District { get; init; }

    /// <summary>
    /// Optional threat-type filter. When specified, must be one of:
    /// 'theft', 'overload', 'line_drop'.
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>
    /// When true, only transformers matching the alert criteria are returned.
    /// When false, transformers without qualifying alerts are also included.
    /// </summary>
    public bool OnlyWithAlerts { get; init; }

    /// <summary>
    /// Validates the criteria and returns an error message if invalid.
    /// </summary>
    public bool TryValidate(out string? error)
    {
        if (Period is null)
        {
            error = "Period is required.";
            return false;
        }

        if (EventType is not null && !EventTypes.IsValid(EventType))
        {
            error = $"Unsupported threat type '{EventType}'. Supported types are: {string.Join(", ", EventTypes.All)}.";
            return false;
        }

        error = null;
        return true;
    }
}
