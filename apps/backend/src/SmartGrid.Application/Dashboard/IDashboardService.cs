using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Application.Dashboard;

/// <summary>
/// Application service providing dashboard-level analytics operations.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retrieves dashboard overview metrics for the specified date range.
    /// </summary>
    Task<DashboardOverviewDto> GetOverviewAsync(DateRange period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves daily demand points for the specified date range.
    /// </summary>
    Task<IReadOnlyList<DailyDemandPointDto>> GetDailyDemandAsync(DateRange period, CancellationToken cancellationToken = default);
}
