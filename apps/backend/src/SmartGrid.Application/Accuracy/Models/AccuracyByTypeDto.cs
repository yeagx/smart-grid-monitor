namespace SmartGrid.Application.Accuracy.Models;

/// <summary>
/// Detection accuracy metrics for a single event type.
/// </summary>
/// <remarks>
/// Source: <c>accuracy_summary</c>.
/// Precision and recall are nullable because their denominators may be zero.
/// </remarks>
public sealed record AccuracyByTypeDto(
    string EventType,
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    decimal? Precision,
    decimal? Recall);
