using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Transformers.Models;
using SmartGrid.Application.Transformers.Queries;

namespace SmartGrid.Application.Transformers;

/// <summary>
/// Application service for querying and inspecting grid transformers.
/// </summary>
public interface ITransformerService
{
    /// <summary>
    /// Searches transformers using metadata filters, ID search, and alert status, returning a paginated inventory.
    /// </summary>
    Task<PagedResult<TransformerListItemDto>> SearchAsync(
        TransformerSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single transformer by its ID.
    /// </summary>
    Task<TransformerDetailsDto?> GetByIdAsync(
        string transformerId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TransformerPeakDto>> GetPeaksAsync(
        TransformerPeakCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransformerLoadPointDto>> GetLoadProfileAsync(
        string transformerId,
        DateRange period,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TransformerReadingDto>> GetReadingsAsync(
        TransformerReadingCriteria criteria,
        CancellationToken cancellationToken = default);
}
