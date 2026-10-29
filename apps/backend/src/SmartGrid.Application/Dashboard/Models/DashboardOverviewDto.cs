namespace SmartGrid.Application.Dashboard.Models;

/// <summary>
/// Overview metrics for the dashboard, aggregated over the requested date range.
/// </summary>
/// <remarks>
/// Sourced from multiple warehouse models:
/// <list type="bullet">
///   <item><c>kpi_daily_demand</c> – energy and peak power.</item>
///   <item><c>anomaly_flags + dim_transformers</c> – alert and transformer counts.</item>
///   <item><c>fact_transformer_readings</c> – latest reading timestamp for data freshness.</item>
/// </list>
/// The <see cref="PeakNetworkKw"/> value is the maximum recorded combined network power
/// across the requested days — not the sum of daily peaks.
/// <para>All values are nullable because the warehouse may have no data for the period.</para>
/// </remarks>
public sealed record DashboardOverviewDto
{
    /// <summary>Inclusive start of the requested period.</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Inclusive end of the requested period.</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Total energy delivered during the period (kWh).</summary>
    public decimal? TotalEnergyKwh { get; init; }

    /// <summary>
    /// Peak combined network power during the period (kW).
    /// This is the single highest value of the network-wide sum of transformer kW
    /// across all 15-minute slots in the period — not the sum of daily peaks.
    /// </summary>
    public decimal? PeakNetworkKw { get; init; }

    /// <summary>Total alert windows (anomaly rows) during the period.</summary>
    public int? TotalAlerts { get; init; }

    /// <summary>Distinct transformers with at least one theft alert during the period.</summary>
    public int? TheftTransformers { get; init; }

    /// <summary>Distinct transformers with at least one overload alert during the period.</summary>
    public int? OverloadTransformers { get; init; }

    /// <summary>Distinct transformers with at least one line-drop alert during the period.</summary>
    public int? LinedropTransformers { get; init; }

    /// <summary>Distinct transformers affected by at least one alert of any type during the period.</summary>
    public int? AffectedTransformers { get; init; }

    /// <summary>
    /// Latest available transformer-reading timestamp (UTC), if any readings exist.
    /// Used to indicate data freshness.
    /// </summary>
    public DateTimeOffset? LatestReadingTimestamp { get; init; }
}
