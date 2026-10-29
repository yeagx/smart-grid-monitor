using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Transformers.Models;

namespace SmartGrid.Infrastructure.Data;

/// <summary>
/// Deterministic 15-minute transformer readings for the mock provider (2026-08-01 to 2026-08-02),
/// so the Transformers page shows a realistic load curve without Snowflake.
/// Overload alert windows push load above capacity; line-drop windows pull voltage down,
/// so the chart lines up with the mock alert log.
/// </summary>
public static class MockReadingData
{
    public static readonly DateOnly FirstDay = new(2026, 8, 1);
    public static readonly DateOnly LastDay = new(2026, 8, 2);

    private const decimal NominalVoltage = 230m;

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<TransformerReadingDto>>> ReadingsById = new(Build);

    public static IReadOnlyList<TransformerReadingDto> For(string transformerId)
        => ReadingsById.Value.TryGetValue(transformerId, out var list) ? list : [];

    private static IReadOnlyDictionary<string, IReadOnlyList<TransformerReadingDto>> Build()
    {
        var result = new Dictionary<string, IReadOnlyList<TransformerReadingDto>>(StringComparer.OrdinalIgnoreCase);
        var start = new DateTimeOffset(FirstDay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(LastDay.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        foreach (var t in MockGridMapData.Transformers.Where(t => MockGridMapData.IsValidCoordinate(t.Latitude, t.Longitude)))
        {
            var alerts = MockDashboardData.AlertFixtures
                .Where(a => string.Equals(a.TransformerId, t.TransformerId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Typical peak share of capacity; defaults to 70 % when the KPI is missing.
            var peakShare = (double)((t.PeakLoadPercentage ?? 70m) / 100m);
            var seed = t.TransformerId.Aggregate(17, (acc, c) => unchecked(acc * 31 + c));
            var random = new Random(seed);
            var readings = new List<TransformerReadingDto>();

            for (var ts = start; ts < end; ts = ts.AddMinutes(15))
            {
                var hour = ts.Hour + ts.Minute / 60.0;
                var shape = DailyShape(hour);
                var noise = 1 + (random.NextDouble() - 0.5) * 0.04;
                var kw = (double)t.CapacityKw * peakShare * shape * noise;
                var voltage = (double)NominalVoltage - 6 * shape + (random.NextDouble() - 0.5) * 1.2;

                foreach (var a in alerts.Where(a => ts >= a.StartTimestamp && ts < a.EndTimestamp))
                {
                    if (a.EventType == EventTypes.Overload)
                    {
                        kw = (double)t.CapacityKw * (1.05 + random.NextDouble() * 0.06);
                    }
                    else if (a.EventType == EventTypes.LineDrop)
                    {
                        voltage *= 0.87;
                    }
                }

                readings.Add(new TransformerReadingDto(
                    t.TransformerId,
                    ts,
                    Math.Round((decimal)kw, 2),
                    Math.Round((decimal)(kw * 0.25), 3),
                    Math.Round((decimal)voltage, 1)));
            }

            result[t.TransformerId] = readings;
        }

        return result;
    }

    // Residential-style day: low at night, small morning bump, main evening peak (~20:30). Max = 1.
    private static double DailyShape(double hour)
    {
        var raw = 0.30
            + 0.12 * Math.Exp(-Math.Pow((hour - 8) / 1.5, 2))
            + 0.45 * Math.Exp(-Math.Pow((hour - 20.5) / 2.0, 2));
        return raw / 0.75;
    }
}
