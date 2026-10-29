using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Dashboard.Models;
using SmartGrid.Infrastructure.Data;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Mock implementation of <see cref="IDashboardReadRepository"/> that serves deterministic
/// development fixtures without connecting to Snowflake.
/// </summary>
public sealed class MockDashboardRepository : IDashboardReadRepository
{
    public Task<DashboardOverviewDto> GetOverviewAsync(DateRange period, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(period);

        var overview = MockDashboardData.GetOverview(period);
        return Task.FromResult(overview);
    }

    public Task<IReadOnlyList<DailyDemandPointDto>> GetDailyDemandAsync(DateRange period, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(period);

        var demandPoints = MockDashboardData.GetDemandPoints(period);
        return Task.FromResult(demandPoints);
    }
}
