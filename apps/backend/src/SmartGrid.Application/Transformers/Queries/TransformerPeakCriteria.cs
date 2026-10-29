using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Transformers.Queries;

/// <summary>
/// Criteria for filtering transformer peak results.
/// </summary>
/// <remarks>
/// The underlying <c>kpi_transformer_peak</c> model has no date column,
/// so date filtering is not supported for peak queries.
/// </remarks>
public sealed class TransformerPeakCriteria
{
    /// <summary>Optional district filter.</summary>
    public string? District { get; init; }

    /// <summary>Pagination request.</summary>
    public required PageRequest Pagination { get; init; }
}
