using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Transformers.Queries;

/// <summary>
/// Criteria for retrieving readings for a specific transformer.
/// </summary>
public sealed class TransformerReadingCriteria
{
    /// <summary>The transformer to retrieve readings for.</summary>
    public required string TransformerId { get; init; }

    /// <summary>Inclusive date range for the readings.</summary>
    public required DateRange Period { get; init; }

    /// <summary>Pagination request.</summary>
    public required PageRequest Pagination { get; init; }
}
