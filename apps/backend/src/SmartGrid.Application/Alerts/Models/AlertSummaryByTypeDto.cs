namespace SmartGrid.Application.Alerts.Models;

/// <summary>
/// Summary of alert counts and affected transformers for a single event type.
/// </summary>
/// <remarks>
/// Source: aggregation over <c>anomaly_flags</c>.
/// </remarks>
public sealed record AlertSummaryByTypeDto(
    string EventType,
    int TotalAlerts,
    int AffectedTransformers);
