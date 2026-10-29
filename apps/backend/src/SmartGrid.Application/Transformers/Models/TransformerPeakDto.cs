namespace SmartGrid.Application.Transformers.Models;

/// <summary>
/// Historical peak load for a transformer.
/// </summary>
/// <remarks>
/// Source: <c>kpi_transformer_peak</c>.
/// <para>
/// This model calculates the maximum kW across <b>all</b> currently available readings.
/// It does not retain a date column and cannot answer date-specific peak queries on its own.
/// Do not describe this as the peak for a selected date range.
/// </para>
/// Units: kW for power, percent for load percentage.
/// </remarks>
public sealed record TransformerPeakDto(
    string TransformerId,
    string District,
    decimal CapacityKw,
    decimal PeakKwRecorded,
    decimal PeakLoadPercentage);
