namespace SmartGrid.Application.Transformers.Models;

/// <summary>
/// Full transformer details for the details page.
/// </summary>
/// <remarks>
/// Source: <c>dim_transformers</c>.
/// Contains all metadata and location fields.
/// </remarks>
public sealed record TransformerDetailsDto
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
}
