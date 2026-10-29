using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Application.Dashboard;

/// <summary>
/// Implements dashboard analytics operations by coordinating with the read repository.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardReadRepository _repository;

    public DashboardService(IDashboardReadRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<DashboardOverviewDto> GetOverviewAsync(DateRange period, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);
        return _repository.GetOverviewAsync(period, cancellationToken);
    }

    public Task<IReadOnlyList<DailyDemandPointDto>> GetDailyDemandAsync(DateRange period, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);
        return _repository.GetDailyDemandAsync(period, cancellationToken);
    }
}
