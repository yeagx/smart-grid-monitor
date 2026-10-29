namespace SmartGrid.Application.Accuracy.Models;

/// <summary>
/// One row of ACCURACY_METRICS: an injected anomaly (truth window) matched, or not,
/// with a detected anomaly window.
/// True Positive = both windows present; False Negative = truth only; False Positive = detection only.
/// </summary>
public sealed record AccuracyDetailDto
{
    public required string TransformerId { get; init; }

    public required string EventType { get; init; }

    public required string DetectionStatus { get; init; }

    public DateTimeOffset? TruthStartTimestamp { get; init; }

    public DateTimeOffset? TruthEndTimestamp { get; init; }

    public DateTimeOffset? DetectedStartTimestamp { get; init; }

    public DateTimeOffset? DetectedEndTimestamp { get; init; }

    public int? FlagRows { get; init; }
}
