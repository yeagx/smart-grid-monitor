namespace SmartGrid.Application.GridMap.Models;

/// <summary>
/// Represents a grid transformer marker enriched with geospatial coordinates,
/// metadata, historical recorded peak metrics, and period-specific alert aggregates.
/// </summary>
public sealed record MapMarkerDto
{
    /// <summary>Unique transformer identifier (e.g. "TR-0012").</summary>
    public required string TransformerId { get; init; }

    /// <summary>Administrative district (e.g. "Dokki", "Nasr City").</summary>
    public required string District { get; init; }

    /// <summary>Administrative governorate (e.g. "Cairo", "Giza").</summary>
    public required string Governorate { get; init; }

    /// <summary>Connected feeder identifier.</summary>
    public required string FeederId { get; init; }

    /// <summary>Connected substation identifier.</summary>
    public required string SubstationId { get; init; }

    /// <summary>Geographic latitude (WGS 84).</summary>
    public required double Latitude { get; init; }

    /// <summary>Geographic longitude (WGS 84).</summary>
    public required double Longitude { get; init; }

    /// <summary>Nameplate capacity in kilowatts (kW).</summary>
    public required decimal CapacityKw { get; init; }

    /// <summary>
    /// Historical maximum active power recorded across all available readings (kW).
    /// Nullable if no readings have been recorded for this transformer.
    /// </summary>
    public decimal? PeakKwRecorded { get; init; }

    /// <summary>
    /// Historical maximum load percentage recorded relative to nameplate capacity (%).
    /// Nullable if no readings have been recorded.
    /// </summary>
    public decimal? PeakLoadPercentage { get; init; }

    /// <summary>
    /// Total number of theft alert windows overlapping the requested period.
    /// </summary>
    public int TheftAlertCount { get; init; }

    /// <summary>
    /// Total number of overload alert windows overlapping the requested period.
    /// </summary>
    public int OverloadAlertCount { get; init; }

    /// <summary>
    /// Total number of line drop (voltage anomaly) alert windows overlapping the requested period.
    /// </summary>
    public int LineDropAlertCount { get; init; }

    /// <summary>
    /// Total number of alert windows across all threat types overlapping the requested period.
    /// Equals TheftAlertCount + OverloadAlertCount + LineDropAlertCount.
    /// </summary>
    public int TotalAlertCount { get; init; }

    /// <summary>
    /// End timestamp of the latest qualifying alert window within the requested period.
    /// Null if the transformer has zero alerts in the period.
    /// </summary>
    public DateTimeOffset? LatestAlertAt { get; init; }
}
