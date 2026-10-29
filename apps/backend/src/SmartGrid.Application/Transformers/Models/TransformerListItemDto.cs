namespace SmartGrid.Application.Transformers.Models;

/// <summary>
/// Transformer metadata for list views.
/// </summary>
/// <remarks>
/// Source: <c>dim_transformers</c>.
/// </remarks>
public sealed record TransformerListItemDto
{
    /// <summary>Transformer ID (e.g. "T-0042").</summary>
    public required string TransformerId { get; init; }

    /// <summary>District name.</summary>
    public required string District { get; init; }

    /// <summary>Governorate name.</summary>
    public required string Governorate { get; init; }

    /// <summary>Feeder ID.</summary>
    public required string FeederId { get; init; }

    /// <summary>Substation ID.</summary>
    public required string SubstationId { get; init; }

    /// <summary>Maximum safe load of the transformer (kW).</summary>
    public required decimal CapacityKw { get; init; }

    /// <summary>Latitude.</summary>
    public required double Latitude { get; init; }

    /// <summary>Longitude.</summary>
    public required double Longitude { get; init; }

    /// <summary>
    /// Historical maximum active power recorded across all available readings (kW).
    /// Nullable if unrecorded.
    /// </summary>
    public decimal? PeakKwRecorded { get; init; }

    /// <summary>
    /// Historical maximum load percentage recorded relative to nameplate capacity (%).
    /// Nullable if unrecorded.
    /// </summary>
    public decimal? PeakLoadPercentage { get; init; }

    /// <summary>
    /// Total number of detected anomaly alert windows for this transformer.
    /// </summary>
    public int AlertCount { get; init; }
}
