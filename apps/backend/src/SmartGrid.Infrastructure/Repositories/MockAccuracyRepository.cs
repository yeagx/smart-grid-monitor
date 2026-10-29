using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Accuracy;
using SmartGrid.Application.Accuracy.Models;
using SmartGrid.Application.Accuracy.Queries;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;
using SmartGrid.Infrastructure.Data;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Mock accuracy data built from the mock alert fixtures:
/// every fixture alert is a detection; most match an injected truth window (True Positive),
/// one has no truth (False Positive), and two injected anomalies were missed (False Negative).
/// </summary>
public sealed class MockAccuracyRepository : IAccuracyReadRepository
{
    private static readonly Lazy<IReadOnlyList<AccuracyDetailDto>> Details = new(BuildDetails);

    public Task<AccuracyReportDto> GetReportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = EventTypes.All.Select(type =>
        {
            var ofType = Details.Value.Where(d => d.EventType == type).ToList();
            return (type,
                ofType.Count(d => d.DetectionStatus == DetectionStatuses.TruePositive),
                ofType.Count(d => d.DetectionStatus == DetectionStatuses.FalsePositive),
                ofType.Count(d => d.DetectionStatus == DetectionStatuses.FalseNegative));
        });

        return Task.FromResult(AccuracyService.BuildReport(rows));
    }

    public Task<PagedResult<AccuracyDetailDto>> GetDetailsAsync(
        AccuracyDetailCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        cancellationToken.ThrowIfCancellationRequested();

        var query = Details.Value.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.EventType))
        {
            query = query.Where(d => string.Equals(d.EventType, criteria.EventType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.DetectionStatus))
        {
            query = query.Where(d => string.Equals(d.DetectionStatus, criteria.DetectionStatus, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(criteria.TransformerId))
        {
            query = query.Where(d => d.TransformerId.Contains(criteria.TransformerId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query
            .OrderBy(d => d.TruthStartTimestamp ?? d.DetectedStartTimestamp)
            .ThenBy(d => d.TransformerId, StringComparer.Ordinal)
            .ToList();

        var page = ordered
            .Skip((criteria.Pagination.Page - 1) * criteria.Pagination.PageSize)
            .Take(criteria.Pagination.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<AccuracyDetailDto>(page, criteria.Pagination.Page, criteria.Pagination.PageSize, ordered.Count));
    }

    private static IReadOnlyList<AccuracyDetailDto> BuildDetails()
    {
        var alerts = MockDashboardData.AlertFixtures.OrderBy(a => a.StartTimestamp).ToList();
        var falsePositiveId = alerts.LastOrDefault(a => a.EventType == EventTypes.Overload)?.AnomalyId;
        var details = new List<AccuracyDetailDto>();

        foreach (var a in alerts)
        {
            var isFalsePositive = a.AnomalyId == falsePositiveId;
            details.Add(new AccuracyDetailDto
            {
                TransformerId = a.TransformerId,
                EventType = a.EventType,
                DetectionStatus = isFalsePositive ? DetectionStatuses.FalsePositive : DetectionStatuses.TruePositive,
                TruthStartTimestamp = isFalsePositive ? null : a.StartTimestamp,
                TruthEndTimestamp = isFalsePositive ? null : a.EndTimestamp,
                DetectedStartTimestamp = a.StartTimestamp.AddMinutes(15),
                DetectedEndTimestamp = a.EndTimestamp,
                FlagRows = (int)((a.EndTimestamp - a.StartTimestamp).TotalMinutes / 15),
            });
        }

        details.Add(Missed("TR-0303", EventTypes.Theft, new DateTimeOffset(2026, 8, 2, 1, 0, 0, TimeSpan.Zero), 3));
        details.Add(Missed("TR-0304", EventTypes.LineDrop, new DateTimeOffset(2026, 8, 2, 7, 0, 0, TimeSpan.Zero), 1));

        return details;
    }

    private static AccuracyDetailDto Missed(string transformerId, string eventType, DateTimeOffset start, int hours) => new()
    {
        TransformerId = transformerId,
        EventType = eventType,
        DetectionStatus = DetectionStatuses.FalseNegative,
        TruthStartTimestamp = start,
        TruthEndTimestamp = start.AddHours(hours),
    };
}
