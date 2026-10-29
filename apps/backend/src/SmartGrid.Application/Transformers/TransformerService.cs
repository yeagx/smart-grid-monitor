using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Transformers.Models;
using SmartGrid.Application.Transformers.Queries;

namespace SmartGrid.Application.Transformers;

/// <summary>
/// Application service implementation for transformer inventory searches and lookups.
/// </summary>
public sealed class TransformerService : ITransformerService
{
    private readonly ITransformerReadRepository _repository;

    public TransformerService(ITransformerReadRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public Task<PagedResult<TransformerListItemDto>> SearchAsync(
        TransformerSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (!TransformerSortOptions.IsValid(criteria.SortBy))
        {
            throw new ArgumentException(
                $"Unsupported sort '{criteria.SortBy}'. Supported values are: {string.Join(", ", TransformerSortOptions.All)}.",
                nameof(criteria));
        }

        return _repository.SearchAsync(criteria, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TransformerDetailsDto?> GetByIdAsync(
        string transformerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transformerId))
        {
            throw new ArgumentException("Transformer ID must not be null or whitespace.", nameof(transformerId));
        }

        return _repository.GetByIdAsync(transformerId, cancellationToken);
    }

    public Task<PagedResult<TransformerPeakDto>> GetPeaksAsync(
        TransformerPeakCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        return _repository.GetPeaksAsync(criteria, cancellationToken);
    }

    /// <summary>Longest load profile we return: 31 days of hourly points (744 rows).</summary>
    public const int MaxLoadProfileDays = 31;

    public Task<IReadOnlyList<TransformerLoadPointDto>> GetLoadProfileAsync(
        string transformerId,
        DateRange period,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transformerId))
        {
            throw new ArgumentException("Transformer ID must not be null or whitespace.", nameof(transformerId));
        }

        ArgumentNullException.ThrowIfNull(period);

        if (period.DayCount > MaxLoadProfileDays)
        {
            throw new ArgumentException(
                $"The load profile range ({period.DayCount} days) exceeds the maximum of {MaxLoadProfileDays} days.",
                nameof(period));
        }

        return _repository.GetLoadProfileAsync(transformerId.Trim(), period, cancellationToken);
    }

    public Task<PagedResult<TransformerReadingDto>> GetReadingsAsync(
        TransformerReadingCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (string.IsNullOrWhiteSpace(criteria.TransformerId))
        {
            throw new ArgumentException("Transformer ID must not be null or whitespace.", nameof(criteria));
        }

        return _repository.GetReadingsAsync(criteria, cancellationToken);
    }
}
