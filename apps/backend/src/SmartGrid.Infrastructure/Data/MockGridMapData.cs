using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;

namespace SmartGrid.Infrastructure.Data;

/// <summary>
/// Deterministic mock fixtures representing transformer infrastructure, coordinates,
/// recorded peak loads, and alert window fixtures.
/// </summary>
public static class MockGridMapData
{
    /// <summary>
    /// Metadata record for a simulated transformer.
    /// </summary>
    public sealed record MockTransformerMetadata(
        string TransformerId,
        string District,
        string Governorate,
        string FeederId,
        string SubstationId,
        decimal CapacityKw,
        double Latitude,
        double Longitude,
        decimal? PeakKwRecorded,
        decimal? PeakLoadPercentage);

    /// <summary>
    /// Validates whether coordinates represent a valid, usable geographic location.
    /// Filters out Null Island (0,0), NaN, infinities, and values outside [-90,90] / [-180,180].
    /// </summary>
    public static bool IsValidCoordinate(double lat, double lon)
    {
        if (double.IsNaN(lat) || double.IsNaN(lon) ||
            double.IsInfinity(lat) || double.IsInfinity(lon))
        {
            return false;
        }

        if (lat < -90.0 || lat > 90.0 || lon < -180.0 || lon > 180.0)
        {
            return false;
        }

        // Filter out Null Island (0, 0)
        if (Math.Abs(lat) < 0.000001 && Math.Abs(lon) < 0.000001)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Deterministic transformer inventory across the 5 districts in Cairo and Giza.
    /// Includes transformers with alerts, transformers without alerts, transformers with null peak metrics,
    /// and invalid-coordinate entries for validation testing.
    /// </summary>
    public static readonly IReadOnlyList<MockTransformerMetadata> Transformers =
    [
        // Transformers referenced in AlertFixtures
        new("TR-0012", "Dokki", "Giza", "F-01", "SS-DOKKI", 500m, 30.0384, 31.2109, 420.50m, 84.10m),
        new("TR-0045", "Nasr City", "Cairo", "F-06", "SS-NASR", 630m, 30.0566, 31.3302, 585.90m, 93.00m),
        new("TR-0078", "Maadi", "Cairo", "F-11", "SS-MAADI", 400m, 29.9602, 31.2569, 310.00m, 77.50m),
        new("TR-0089", "Heliopolis", "Cairo", "F-16", "SS-HELIO", 500m, 30.0911, 31.3227, 475.00m, 95.00m),
        new("TR-0102", "Shubra", "Cairo", "F-21", "SS-SHUBRA", 630m, 30.0866, 31.2444, 541.80m, 86.00m),
        new("TR-0210", "Dokki", "Giza", "F-02", "SS-DOKKI", 400m, 30.0350, 31.2150, 290.00m, 72.50m),
        new("TR-0215", "Nasr City", "Cairo", "F-07", "SS-NASR", 500m, 30.0600, 31.3250, 410.00m, 82.00m),

        // Healthy transformers with NO alerts across all districts
        new("TR-0301", "Dokki", "Giza", "F-03", "SS-DOKKI", 500m, 30.0400, 31.2080, 350.00m, 70.00m),
        new("TR-0302", "Nasr City", "Cairo", "F-08", "SS-NASR", 630m, 30.0520, 31.3350, 420.00m, 66.67m),
        new("TR-0303", "Maadi", "Cairo", "F-12", "SS-MAADI", 400m, 29.9650, 31.2600, 280.00m, 70.00m),
        new("TR-0304", "Heliopolis", "Cairo", "F-17", "SS-HELIO", 500m, 30.0950, 31.3180, 380.00m, 76.00m),
        new("TR-0305", "Shubra", "Cairo", "F-22", "SS-SHUBRA", 630m, 30.0820, 31.2400, 450.00m, 71.43m),

        // Transformers with null peak metrics (unrecorded)
        new("TR-0306", "Dokki", "Giza", "F-04", "SS-DOKKI", 400m, 30.0320, 31.2180, null, null),
        new("TR-0307", "Heliopolis", "Cairo", "F-18", "SS-HELIO", 500m, 30.0880, 31.3260, null, null),

        // Invalid coordinate entries (must be filtered out)
        new("TR-9999", "Dokki", "Giza", "F-99", "SS-DOKKI", 400m, 0.0, 0.0, null, null),
        new("TR-9998", "Shubra", "Cairo", "F-99", "SS-SHUBRA", 400m, 999.0, 999.0, null, null)
    ];

    /// <summary>
    /// Computes map markers matching the specified query criteria using deterministic in-memory fixtures.
    /// </summary>
    public static IReadOnlyList<MapMarkerDto> GetMarkers(GridMapCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        // Interpret dates at UTC boundaries
        var startInclusive = new DateTimeOffset(criteria.Period.Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endExclusive = new DateTimeOffset(criteria.Period.End.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // Find alert windows overlapping the requested period
        var overlappingAlerts = MockDashboardData.AlertFixtures
            .Where(a => a.StartTimestamp < endExclusive && a.EndTimestamp >= startInclusive)
            .ToList();

        var alertsByTransformer = overlappingAlerts
            .GroupBy(a => a.TransformerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<MapMarkerDto>();

        foreach (var t in Transformers)
        {
            // Filter invalid coordinates
            if (!IsValidCoordinate(t.Latitude, t.Longitude))
            {
                continue;
            }

            // District filter
            if (!string.IsNullOrWhiteSpace(criteria.District) &&
                !string.Equals(t.District, criteria.District, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            alertsByTransformer.TryGetValue(t.TransformerId, out var trAlerts);
            trAlerts ??= [];

            var theftCount = trAlerts.Count(a => a.EventType == EventTypes.Theft);
            var overloadCount = trAlerts.Count(a => a.EventType == EventTypes.Overload);
            var lineDropCount = trAlerts.Count(a => a.EventType == EventTypes.LineDrop);
            var totalCount = theftCount + overloadCount + lineDropCount;
            var latestAlertAt = trAlerts.Count > 0 ? trAlerts.Max(a => a.EndTimestamp) : (DateTimeOffset?)null;

            // Determine eligibility based on threat type and onlyWithAlerts
            bool matchesThreatType = criteria.EventType switch
            {
                EventTypes.Theft => theftCount > 0,
                EventTypes.Overload => overloadCount > 0,
                EventTypes.LineDrop => lineDropCount > 0,
                _ => totalCount > 0
            };

            if (criteria.OnlyWithAlerts)
            {
                if (!matchesThreatType)
                {
                    continue;
                }
            }
            else if (criteria.EventType is not null)
            {
                // When onlyWithAlerts is false and a type filter is specified,
                // transformers with 0 alerts still appear, but transformers with alerts
                // must match the selected threat type.
                if (totalCount > 0 && !matchesThreatType)
                {
                    continue;
                }
            }

            result.Add(new MapMarkerDto
            {
                TransformerId = t.TransformerId,
                District = t.District,
                Governorate = t.Governorate,
                FeederId = t.FeederId,
                SubstationId = t.SubstationId,
                Latitude = t.Latitude,
                Longitude = t.Longitude,
                CapacityKw = t.CapacityKw,
                PeakKwRecorded = t.PeakKwRecorded,
                PeakLoadPercentage = t.PeakLoadPercentage,
                TheftAlertCount = theftCount,
                OverloadAlertCount = overloadCount,
                LineDropAlertCount = lineDropCount,
                TotalAlertCount = totalCount,
                LatestAlertAt = latestAlertAt
            });
        }

        return result;
    }
}
