using SmartGrid.Application.Accuracy.Models;
using SmartGrid.Application.Accuracy.Queries;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Abstractions.Persistence;

/// <summary>
/// Read-only access to detection accuracy (detected anomalies vs injected ground truth).
/// </summary>
public interface IAccuracyReadRepository
{
    Task<AccuracyReportDto> GetReportAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<AccuracyDetailDto>> GetDetailsAsync(
        AccuracyDetailCriteria criteria,
        CancellationToken cancellationToken = default);
}
