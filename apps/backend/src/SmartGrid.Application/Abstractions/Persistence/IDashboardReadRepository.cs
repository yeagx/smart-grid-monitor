using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Application.Abstractions.Persistence;

/// <summary>
/// Read-only repository for dashboard-level analytics data.
/// </summary>
/// <remarks>
/// Backed by:
/// <list type="bullet">
///   <item><c>kpi_daily_demand</c></item>
///   <item><c>anomaly_flags</c></item>
///   <item><c>fact_transformer_readings</c> (for freshness timestamp)</item>
///   <item><c>dim_transformers</c> (for district joins)</item>
/// </list>
/// </remarks>
public interface IDashboardReadRepository
{
    /// <summary>
    /// Retrieves dashboard overview metrics for the specified inclusive date range.
    /// </summary>
    Task<DashboardOverviewDto> GetOverviewAsync(DateRange period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves daily demand points (energy and peak power) for the specified inclusive date range,
    /// ordered by date ascending.
    /// </summary>
    Task<IReadOnlyList<DailyDemandPointDto>> GetDailyDemandAsync(DateRange period, CancellationToken cancellationToken = default);
}
