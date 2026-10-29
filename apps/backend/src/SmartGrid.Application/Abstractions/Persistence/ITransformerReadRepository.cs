using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Transformers.Models;
using SmartGrid.Application.Transformers.Queries;

namespace SmartGrid.Application.Abstractions.Persistence;

/// <summary>
/// Read-only repository for transformer metadata, peaks, and readings.
/// </summary>
/// <remarks>
/// Backed by:
/// <list type="bullet">
///   <item><c>dim_transformers</c> – metadata and location.</item>
///   <item><c>kpi_transformer_peak</c> – historical peak load (no date column).</item>
///   <item><c>fact_transformer_readings</c> – time-series readings.</item>
/// </list>
/// </remarks>
public interface ITransformerReadRepository
{
    /// <summary>
    /// Searches transformers by metadata criteria and returns a paginated result.
    /// </summary>
    Task<PagedResult<TransformerListItemDto>> SearchAsync(
        TransformerSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single transformer by its ID.
    /// Returns <c>null</c> when no matching transformer exists.
    /// </summary>
    Task<TransformerDetailsDto?> GetByIdAsync(
        string transformerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated historical peak results.
    /// The underlying data has no date column, so date filtering is not supported.
    /// </summary>
    Task<PagedResult<TransformerPeakDto>> GetPeaksAsync(
        TransformerPeakCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated readings for a specific transformer within the specified date range.
    /// Results are ordered by reading timestamp ascending.
    /// </summary>
    Task<PagedResult<TransformerReadingDto>> GetReadingsAsync(
        TransformerReadingCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransformerLoadPointDto>> GetLoadProfileAsync(
        string transformerId,
        DateRange period,
        CancellationToken cancellationToken = default);
}
