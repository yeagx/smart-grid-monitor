using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Transformers.Models;
using SmartGrid.Application.Transformers.Queries;
using SmartGrid.Infrastructure.Data;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Mock repository implementation for transformer inventory, metadata, and peaks using deterministic fixtures.
/// </summary>
public sealed class MockTransformerRepository : ITransformerReadRepository
{
    private static readonly Lazy<IReadOnlyDictionary<string, int>> AlertCountsByTransformer = new(() =>
        MockDashboardData.AlertFixtures
            .GroupBy(a => a.TransformerId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase));

    /// <inheritdoc />
    public Task<PagedResult<TransformerListItemDto>> SearchAsync(
        TransformerSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        cancellationToken.ThrowIfCancellationRequested();

        var query = MockGridMapData.Transformers
            .Where(t => MockGridMapData.IsValidCoordinate(t.Latitude, t.Longitude))
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.District))
        {
            query = query.Where(t => string.Equals(t.District, criteria.District, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Governorate))
        {
            query = query.Where(t => string.Equals(t.Governorate, criteria.Governorate, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.FeederId))
        {
            query = query.Where(t => string.Equals(t.FeederId, criteria.FeederId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.SubstationId))
        {
            query = query.Where(t => string.Equals(t.SubstationId, criteria.SubstationId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            query = query.Where(t => t.TransformerId.Contains(criteria.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.OnlyWithAlerts)
        {
            query = query.Where(t =>
                AlertCountsByTransformer.Value.TryGetValue(t.TransformerId, out var count) && count > 0);
        }

        int AlertsOf(string id) => AlertCountsByTransformer.Value.TryGetValue(id, out var c) ? c : 0;

        var ordered = (criteria.SortBy switch
        {
            TransformerSortOptions.Alerts => query.OrderByDescending(t => AlertsOf(t.TransformerId)).ThenBy(t => t.TransformerId),
            TransformerSortOptions.Load => query.OrderByDescending(t => t.PeakLoadPercentage ?? decimal.MinValue).ThenBy(t => t.TransformerId),
            _ => query.OrderBy(t => t.TransformerId),
        }).ToList();
        var totalCount = ordered.Count;

        var pagedItems = ordered
            .Skip((criteria.Pagination.Page - 1) * criteria.Pagination.PageSize)
            .Take(criteria.Pagination.PageSize)
            .Select(t =>
            {
                AlertCountsByTransformer.Value.TryGetValue(t.TransformerId, out var count);
                return new TransformerListItemDto
                {
                    TransformerId = t.TransformerId,
                    District = t.District,
                    Governorate = t.Governorate,
                    FeederId = t.FeederId,
                    SubstationId = t.SubstationId,
                    CapacityKw = t.CapacityKw,
                    Latitude = t.Latitude,
                    Longitude = t.Longitude,
                    PeakKwRecorded = t.PeakKwRecorded,
                    PeakLoadPercentage = t.PeakLoadPercentage,
                    AlertCount = count,
                };
            })
            .ToList();

        return Task.FromResult(new PagedResult<TransformerListItemDto>(
            pagedItems,
            criteria.Pagination.Page,
            criteria.Pagination.PageSize,
            totalCount));
    }

    /// <inheritdoc />
    public Task<TransformerDetailsDto?> GetByIdAsync(
        string transformerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transformerId);
        cancellationToken.ThrowIfCancellationRequested();

        var match = MockGridMapData.Transformers.FirstOrDefault(t =>
            string.Equals(t.TransformerId, transformerId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return Task.FromResult<TransformerDetailsDto?>(null);
        }

        var details = new TransformerDetailsDto
        {
            TransformerId = match.TransformerId,
            District = match.District,
            Governorate = match.Governorate,
            FeederId = match.FeederId,
            SubstationId = match.SubstationId,
            CapacityKw = match.CapacityKw,
            Latitude = match.Latitude,
            Longitude = match.Longitude,
        };

        return Task.FromResult<TransformerDetailsDto?>(details);
    }

    /// <inheritdoc />
    public Task<PagedResult<TransformerPeakDto>> GetPeaksAsync(
        TransformerPeakCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var peaks = MockGridMapData.Transformers
            .Where(t => t.PeakKwRecorded.HasValue && t.PeakLoadPercentage.HasValue)
            .Where(t => string.IsNullOrWhiteSpace(criteria.District)
                || string.Equals(t.District, criteria.District.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.PeakLoadPercentage)
            .ThenBy(t => t.TransformerId)
            .Select(t => new TransformerPeakDto(
                t.TransformerId,
                t.District,
                t.CapacityKw,
                t.PeakKwRecorded!.Value,
                t.PeakLoadPercentage!.Value))
            .ToList();

        var pagedPeaks = peaks
            .Skip((criteria.Pagination.Page - 1) * criteria.Pagination.PageSize)
            .Take(criteria.Pagination.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<TransformerPeakDto>(
            pagedPeaks,
            criteria.Pagination.Page,
            criteria.Pagination.PageSize,
            peaks.Count));
    }

    /// <inheritdoc />
    public Task<PagedResult<TransformerReadingDto>> GetReadingsAsync(
        TransformerReadingCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        cancellationToken.ThrowIfCancellationRequested();

        var inRange = InPeriod(criteria.TransformerId, criteria.Period);
        var page = inRange
            .Skip((criteria.Pagination.Page - 1) * criteria.Pagination.PageSize)
            .Take(criteria.Pagination.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<TransformerReadingDto>(
            page,
            criteria.Pagination.Page,
            criteria.Pagination.PageSize,
            inRange.Count));
    }

    public Task<IReadOnlyList<TransformerLoadPointDto>> GetLoadProfileAsync(
        string transformerId,
        DateRange period,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transformerId);
        ArgumentNullException.ThrowIfNull(period);
        cancellationToken.ThrowIfCancellationRequested();

        // Same hourly aggregation as the Snowflake query (DATE_TRUNC('HOUR', READING_TS)).
        IReadOnlyList<TransformerLoadPointDto> points = InPeriod(transformerId, period)
            .GroupBy(r => new DateTimeOffset(r.ReadingTimestamp.Year, r.ReadingTimestamp.Month, r.ReadingTimestamp.Day,
                r.ReadingTimestamp.Hour, 0, 0, TimeSpan.Zero))
            .OrderBy(g => g.Key)
            .Select(g => new TransformerLoadPointDto(
                g.Key,
                Math.Round(g.Average(r => r.ActivePowerKw), 2),
                Math.Round(g.Max(r => r.ActivePowerKw), 2),
                Math.Round(g.Sum(r => r.EnergyKwh), 2),
                Math.Round(g.Average(r => r.Voltage), 2),
                Math.Round(g.Min(r => r.Voltage), 2),
                g.Count()))
            .ToList();

        return Task.FromResult(points);
    }

    private static List<TransformerReadingDto> InPeriod(string transformerId, DateRange period)
    {
        var start = new DateTimeOffset(period.Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endExclusive = new DateTimeOffset(period.End.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return MockReadingData.For(transformerId.Trim())
            .Where(r => r.ReadingTimestamp >= start && r.ReadingTimestamp < endExclusive)
            .ToList();
    }
}
