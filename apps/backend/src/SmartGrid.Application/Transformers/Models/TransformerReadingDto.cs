namespace SmartGrid.Application.Transformers.Models;

/// <summary>
/// A single 15-minute reading from a transformer.
/// </summary>
/// <remarks>
/// Source: <c>fact_transformer_readings</c>.
/// Units: kW for active power, kWh for energy, V for voltage.
/// <para>Timestamps are in UTC.</para>
/// </remarks>
public sealed record TransformerReadingDto(
    string TransformerId,
    DateTimeOffset ReadingTimestamp,
    decimal ActivePowerKw,
    decimal EnergyKwh,
    decimal Voltage);
