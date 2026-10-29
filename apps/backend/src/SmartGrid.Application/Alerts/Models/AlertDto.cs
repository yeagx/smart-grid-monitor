namespace SmartGrid.Application.Alerts.Models;

/// <summary>
/// An individual alert (anomaly flag) enriched with transformer metadata.
/// </summary>
/// <remarks>
/// Source: <c>anomaly_flags</c> joined to <c>dim_transformers</c>.
/// The anomaly_flags model does not carry district/governorate;
/// these must be obtained via a join on <c>transformer_id</c>.
/// <para>All timestamps are in UTC.</para>
/// </remarks>
public sealed record AlertDto
{
    /// <summary>Unique anomaly identifier (e.g. "T-0042-OVL-20240515120000").</summary>
    public required string AnomalyId { get; init; }

    /// <summary>Event type: "theft", "overload", or "line_drop".</summary>
    public required string EventType { get; init; }

    /// <summary>The entity that raised the alert (currently always the transformer ID).</summary>
    public required string EntityId { get; init; }

    /// <summary>Transformer ID.</summary>
    public required string TransformerId { get; init; }

    /// <summary>District from dim_transformers (joined on transformer_id).</summary>
    public required string District { get; init; }

    /// <summary>Governorate from dim_transformers (joined on transformer_id).</summary>
    public required string Governorate { get; init; }

    /// <summary>Start of the anomaly window (UTC).</summary>
    public required DateTimeOffset StartTimestamp { get; init; }

    /// <summary>End of the anomaly window (UTC).</summary>
    public required DateTimeOffset EndTimestamp { get; init; }
}
