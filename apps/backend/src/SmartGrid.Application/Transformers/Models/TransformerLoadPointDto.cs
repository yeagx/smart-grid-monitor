namespace SmartGrid.Application.Transformers.Models;

/// <summary>
/// One hour of transformer readings, aggregated for charting.
/// Hourly buckets keep a multi-day series small (48 points for two days)
/// while still showing peaks (MaxKw) and voltage dips (MinVoltage).
/// </summary>
public sealed record TransformerLoadPointDto(
    DateTimeOffset HourTimestamp,
    decimal AvgKw,
    decimal MaxKw,
    decimal EnergyKwh,
    decimal AvgVoltage,
    decimal MinVoltage,
    int ReadingCount);
