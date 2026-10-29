using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Accuracy.Queries;

public sealed class AccuracyDetailCriteria
{
    public string? EventType { get; init; }

    public string? DetectionStatus { get; init; }

    public string? TransformerId { get; init; }

    public required PageRequest Pagination { get; init; }
}
