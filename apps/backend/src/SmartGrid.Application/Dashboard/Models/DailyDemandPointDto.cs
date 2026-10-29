namespace SmartGrid.Application.Dashboard.Models;

/// <summary>
/// One row from kpi_daily_demand: total energy and peak power for a single day.
/// </summary>
/// <remarks>
/// Source: <c>kpi_daily_demand</c> (demand_date, total_daily_kwh, peak_network_kw).
/// Units: kWh for energy, kW for power.
/// </remarks>
public sealed record DailyDemandPointDto(
    DateOnly DemandDate,
    decimal TotalDailyKwh,
    decimal PeakNetworkKw);
