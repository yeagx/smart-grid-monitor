using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Accuracy.Models;
using SmartGrid.Application.Accuracy.Queries;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Accuracy;

public sealed class AccuracyService : IAccuracyService
{
    private readonly IAccuracyReadRepository _repository;

    public AccuracyService(IAccuracyReadRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<AccuracyReportDto> GetReportAsync(CancellationToken cancellationToken = default)
        => _repository.GetReportAsync(cancellationToken);

    public Task<PagedResult<AccuracyDetailDto>> GetDetailsAsync(
        AccuracyDetailCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (criteria.EventType is not null && !EventTypes.IsValid(criteria.EventType))
        {
            throw new ArgumentException(
                $"Unsupported event type '{criteria.EventType}'. Supported types are: {string.Join(", ", EventTypes.All)}.",
                nameof(criteria));
        }

        if (criteria.DetectionStatus is not null && !DetectionStatuses.IsValid(criteria.DetectionStatus))
        {
            throw new ArgumentException(
                $"Unsupported detection status '{criteria.DetectionStatus}'. Supported values are: {string.Join(", ", DetectionStatuses.All)}.",
                nameof(criteria));
        }

        return _repository.GetDetailsAsync(criteria, cancellationToken);
    }

    /// <summary>
    /// Builds the report from per-type counts. Precision and recall are always computed from the
    /// counts (as 0–1 ratios) so the API never depends on how the warehouse stored them.
    /// </summary>
    public static AccuracyReportDto BuildReport(IEnumerable<(string EventType, int Tp, int Fp, int Fn)> rows)
    {
        var byType = rows
            .Select(r => new AccuracyByTypeDto(r.EventType, r.Tp, r.Fp, r.Fn, Ratio(r.Tp, r.Tp + r.Fp), Ratio(r.Tp, r.Tp + r.Fn)))
            .OrderBy(r => TypeOrder(r.EventType))
            .ThenBy(r => r.EventType, StringComparer.Ordinal)
            .ToList();

        var tp = byType.Sum(r => r.TruePositives);
        var fp = byType.Sum(r => r.FalsePositives);
        var fn = byType.Sum(r => r.FalseNegatives);

        return new AccuracyReportDto
        {
            ByType = byType,
            TotalTruePositives = tp,
            TotalFalsePositives = fp,
            TotalFalseNegatives = fn,
            OverallPrecision = Ratio(tp, tp + fp),
            OverallRecall = Ratio(tp, tp + fn),
        };
    }

    // Keeps the usual theft → overload → line_drop order; unknown types go last.
    private static int TypeOrder(string eventType)
    {
        for (var i = 0; i < EventTypes.All.Count; i++)
        {
            if (string.Equals(EventTypes.All[i], eventType, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static decimal? Ratio(int numerator, int denominator)
        => denominator == 0 ? null : Math.Round((decimal)numerator / denominator, 4);
}
