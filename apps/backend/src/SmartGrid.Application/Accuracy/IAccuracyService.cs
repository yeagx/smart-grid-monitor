using SmartGrid.Application.Accuracy.Models;
using SmartGrid.Application.Accuracy.Queries;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Accuracy;

public interface IAccuracyService
{
    Task<AccuracyReportDto> GetReportAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<AccuracyDetailDto>> GetDetailsAsync(
        AccuracyDetailCriteria criteria,
        CancellationToken cancellationToken = default);
}
