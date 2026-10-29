using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Infrastructure.Data;

/// <summary>
/// Deterministic mock fixtures representing the simulated smart grid analytical results
/// for the simulation dates of August 1 and August 2, 2026.
/// </summary>
public static class MockDashboardData
{
    /// <summary>
    /// Mock fixture row representing an anomaly flag.
    /// </summary>
    public sealed record MockAlertRecord(
        string AnomalyId,
        string EventType,
        string TransformerId,
        DateTimeOffset StartTimestamp,
        DateTimeOffset EndTimestamp);

    /// <summary>
    /// Deterministic daily demand points for the simulation dates.
    /// </summary>
    public static readonly IReadOnlyList<DailyDemandPointDto> DailyDemandFixtures =
    [
        new(new DateOnly(2026, 8, 1), 1425000.50m, 88400.25m),
        new(new DateOnly(2026, 8, 2), 1480250.75m, 91200.80m)
    ];

    /// <summary>
    /// Deterministic alert records with deliberate overlaps to test distinct counting:
    /// - TR-0012 has theft alerts on both Aug 1 and Aug 2.
    /// - TR-0089 has two separate overload alerts on Aug 1.
    /// - TR-0045 has a theft alert on Aug 1 and an overload alert on Aug 2.
    /// - TR-0210 has line-drop alerts on both Aug 1 and Aug 2.
    /// </summary>
    public static readonly IReadOnlyList<MockAlertRecord> AlertFixtures =
    [
        // Day 1: 2026-08-01
        new("TR-0012-THEFT-20260801060000", EventTypes.Theft, "TR-0012",
            new DateTimeOffset(2026, 8, 1, 2, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 6, 0, 0, TimeSpan.Zero)),

        new("TR-0045-THEFT-20260801140000", EventTypes.Theft, "TR-0045",
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 14, 0, 0, TimeSpan.Zero)),

        new("TR-0089-OVL-20260801133000", EventTypes.Overload, "TR-0089",
            new DateTimeOffset(2026, 8, 1, 13, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 13, 30, 0, TimeSpan.Zero)),

        new("TR-0089-OVL-20260801183000", EventTypes.Overload, "TR-0089",
            new DateTimeOffset(2026, 8, 1, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 18, 30, 0, TimeSpan.Zero)),

        new("TR-0210-LD-20260801090000", EventTypes.LineDrop, "TR-0210",
            new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero)),

        // Day 2: 2026-08-02
        new("TR-0012-THEFT-20260802070000", EventTypes.Theft, "TR-0012",
            new DateTimeOffset(2026, 8, 2, 3, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 2, 7, 0, 0, TimeSpan.Zero)),

        new("TR-0078-THEFT-20260802160000", EventTypes.Theft, "TR-0078",
            new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 2, 16, 0, 0, TimeSpan.Zero)),

        new("TR-0102-OVL-20260802143000", EventTypes.Overload, "TR-0102",
            new DateTimeOffset(2026, 8, 2, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 2, 14, 30, 0, TimeSpan.Zero)),

        new("TR-0045-OVL-20260802193000", EventTypes.Overload, "TR-0045",
            new DateTimeOffset(2026, 8, 2, 19, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 2, 19, 30, 0, TimeSpan.Zero)),

        new("TR-0210-LD-20260802090000", EventTypes.LineDrop, "TR-0210",
            new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 2, 9, 0, 0, TimeSpan.Zero)),

        new("TR-0215-LD-20260802160000", EventTypes.LineDrop, "TR-0215",
            new DateTimeOffset(2026, 8, 2, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 2, 16, 0, 0, TimeSpan.Zero))
    ];

    /// <summary>
    /// Latest reading timestamps per date for data freshness.
    /// </summary>
    public static readonly IReadOnlyDictionary<DateOnly, DateTimeOffset> LatestReadingsByDate = new Dictionary<DateOnly, DateTimeOffset>
    {
        [new DateOnly(2026, 8, 1)] = new(2026, 8, 1, 23, 45, 0, TimeSpan.Zero),
        [new DateOnly(2026, 8, 2)] = new(2026, 8, 2, 23, 45, 0, TimeSpan.Zero)
    };

    /// <summary>
    /// Computes daily demand points for the requested inclusive date range, ordered by date ascending.
    /// </summary>
    public static IReadOnlyList<DailyDemandPointDto> GetDemandPoints(DateRange period)
    {
        ArgumentNullException.ThrowIfNull(period);

        return DailyDemandFixtures
            .Where(d => d.DemandDate >= period.Start && d.DemandDate <= period.End)
            .OrderBy(d => d.DemandDate)
            .ToList();
    }

    /// <summary>
    /// Dynamically computes the dashboard overview from the fixture records for the requested inclusive date range.
    /// When no records fall in the requested range, returns nulls for metric fields.
    /// </summary>
    public static DashboardOverviewDto GetOverview(DateRange period)
    {
        ArgumentNullException.ThrowIfNull(period);

        var demandPoints = GetDemandPoints(period);

        var alerts = AlertFixtures
            .Where(a =>
            {
                var alertDate = DateOnly.FromDateTime(a.EndTimestamp.UtcDateTime);
                return alertDate >= period.Start && alertDate <= period.End;
            })
            .ToList();

        var readingsInRange = LatestReadingsByDate
            .Where(kvp => kvp.Key >= period.Start && kvp.Key <= period.End)
            .Select(kvp => (DateTimeOffset?)kvp.Value)
            .ToList();

        // If no demand, no alerts, and no readings in range, this is an empty range
        if (demandPoints.Count == 0 && alerts.Count == 0 && readingsInRange.Count == 0)
        {
            return new DashboardOverviewDto
            {
                PeriodStart = period.Start,
                PeriodEnd = period.End,
                TotalEnergyKwh = null,
                PeakNetworkKw = null,
                TotalAlerts = null,
                TheftTransformers = null,
                OverloadTransformers = null,
                LinedropTransformers = null,
                AffectedTransformers = null,
                LatestReadingTimestamp = null
            };
        }

        var totalEnergy = demandPoints.Count > 0 ? demandPoints.Sum(d => d.TotalDailyKwh) : (decimal?)null;
        var peakNetwork = demandPoints.Count > 0 ? demandPoints.Max(d => d.PeakNetworkKw) : (decimal?)null;

        var totalAlerts = alerts.Count;
        var theftCount = alerts.Where(a => a.EventType == EventTypes.Theft).Select(a => a.TransformerId).Distinct().Count();
        var overloadCount = alerts.Where(a => a.EventType == EventTypes.Overload).Select(a => a.TransformerId).Distinct().Count();
        var lineDropCount = alerts.Where(a => a.EventType == EventTypes.LineDrop).Select(a => a.TransformerId).Distinct().Count();
        var affectedCount = alerts.Select(a => a.TransformerId).Distinct().Count();

        var latestReading = readingsInRange.Count > 0 ? readingsInRange.Max() : null;

        return new DashboardOverviewDto
        {
            PeriodStart = period.Start,
            PeriodEnd = period.End,
            TotalEnergyKwh = totalEnergy,
            PeakNetworkKw = peakNetwork,
            TotalAlerts = totalAlerts,
            TheftTransformers = theftCount,
            OverloadTransformers = overloadCount,
            LinedropTransformers = lineDropCount,
            AffectedTransformers = affectedCount,
            LatestReadingTimestamp = latestReading
        };
    }
}
