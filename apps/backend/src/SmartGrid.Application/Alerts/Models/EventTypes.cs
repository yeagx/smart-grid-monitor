namespace SmartGrid.Application.Alerts.Models;

/// <summary>
/// Supported grid anomaly event types.
/// </summary>
public static class EventTypes
{
    public const string Theft = "theft";
    public const string Overload = "overload";
    public const string LineDrop = "line_drop";

    /// <summary>All supported event type values.</summary>
    public static readonly IReadOnlyList<string> All = [Theft, Overload, LineDrop];

    /// <summary>Returns true if the value is a recognised event type.</summary>
    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value);
}
