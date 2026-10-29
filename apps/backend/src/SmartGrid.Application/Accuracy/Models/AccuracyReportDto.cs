namespace SmartGrid.Application.Accuracy.Models;

/// <summary>
/// Overall detection accuracy report, combining per-type results and aggregate metrics.
/// </summary>
/// <remarks>
/// Overall precision and recall are computed from aggregate counts:
/// <code>
///   Precision = TP / (TP + FP)
///   Recall    = TP / (TP + FN)
/// </code>
/// They are <b>never</b> calculated by averaging individual event-type percentages.
/// Both are nullable if their denominator is zero.
/// </remarks>
public sealed record AccuracyReportDto
{
    /// <summary>Breakdown of accuracy by event type.</summary>
    public required IReadOnlyList<AccuracyByTypeDto> ByType { get; init; }

    /// <summary>Total true positives across all event types.</summary>
    public required int TotalTruePositives { get; init; }

    /// <summary>Total false positives across all event types.</summary>
    public required int TotalFalsePositives { get; init; }

    /// <summary>Total false negatives across all event types.</summary>
    public required int TotalFalseNegatives { get; init; }

    /// <summary>
    /// Overall precision = TP / (TP + FP). Null if (TP + FP) is zero.
    /// </summary>
    public required decimal? OverallPrecision { get; init; }

    /// <summary>
    /// Overall recall = TP / (TP + FN). Null if (TP + FN) is zero.
    /// </summary>
    public required decimal? OverallRecall { get; init; }
}
