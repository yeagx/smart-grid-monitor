using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Alerts.Queries;
using SmartGrid.Application.Common.Models;
using SmartGrid.Infrastructure.Data;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Mock repository implementation for anomaly alerts using deterministic in-memory fixtures.
/// </summary>
public sealed class MockAlertRepository : IAlertReadRepository
{
    private static readonly Lazy<IReadOnlyDictionary<string, MockGridMapData.MockTransformerMetadata>> TransformersById =
        new(() => MockGridMapData.Transformers.ToDictionary(t => t.TransformerId, StringComparer.OrdinalIgnoreCase));

    private static AlertDto MapToDto(MockDashboardData.MockAlertRecord record)
    {
        TransformersById.Value.TryGetValue(record.TransformerId, out var metadata);

        return new AlertDto
        {
            AnomalyId = record.AnomalyId,
            EventType = record.EventType,
            EntityId = record.TransformerId,
            TransformerId = record.TransformerId,
            District = metadata?.District ?? "Unknown",
            Governorate = metadata?.Governorate ?? "Unknown",
            StartTimestamp = record.StartTimestamp,
            EndTimestamp = record.EndTimestamp,
        };
    }

    /// <inheritdoc />
    public Task<PagedResult<AlertDto>> SearchAsync(
        AlertSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        cancellationToken.ThrowIfCancellationRequested();

        var query = MockDashboardData.AlertFixtures.AsEnumerable();

        if (criteria.Period is not null)
        {
            var startInclusive = new DateTimeOffset(criteria.Period.Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var endExclusive = new DateTimeOffset(criteria.Period.End.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

            query = query.Where(a => a.StartTimestamp < endExclusive && a.EndTimestamp >= startInclusive);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EventType))
        {
            query = query.Where(a => string.Equals(a.EventType, criteria.EventType, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(criteria.TransformerId))
        {
            query = query.Where(a => a.TransformerId.Contains(criteria.TransformerId, StringComparison.OrdinalIgnoreCase));
        }

        var mapped = query.Select(MapToDto);

        if (!string.IsNullOrWhiteSpace(criteria.District))
        {
            mapped = mapped.Where(a => string.Equals(a.District, criteria.District, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = mapped
            .OrderByDescending(a => a.StartTimestamp)
            .ThenByDescending(a => a.AnomalyId)
            .ToList();

        var totalCount = ordered.Count;
        var pagedItems = ordered
            .Skip((criteria.Pagination.Page - 1) * criteria.Pagination.PageSize)
            .Take(criteria.Pagination.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<AlertDto>(
            pagedItems,
            criteria.Pagination.Page,
            criteria.Pagination.PageSize,
            totalCount));
    }

    /// <inheritdoc />
    public Task<AlertDto?> GetByIdAsync(
        string anomalyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anomalyId);
        cancellationToken.ThrowIfCancellationRequested();

        var record = MockDashboardData.AlertFixtures.FirstOrDefault(a =>
            string.Equals(a.AnomalyId, anomalyId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(record is not null ? MapToDto(record) : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AlertSummaryByTypeDto>> GetSummaryByTypeAsync(
        DateRange period,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);
        cancellationToken.ThrowIfCancellationRequested();

        var startInclusive = new DateTimeOffset(period.Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endExclusive = new DateTimeOffset(period.End.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var alerts = MockDashboardData.AlertFixtures
            .Where(a => a.StartTimestamp < endExclusive && a.EndTimestamp >= startInclusive)
            .ToList();

        var summaries = EventTypes.All.Select(type =>
        {
            var matching = alerts.Where(a => a.EventType == type).ToList();
            return new AlertSummaryByTypeDto(
                type,
                matching.Count,
                matching.Select(a => a.TransformerId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }).ToList();

        return Task.FromResult<IReadOnlyList<AlertSummaryByTypeDto>>(summaries);
    }
}
