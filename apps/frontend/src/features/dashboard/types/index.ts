/**
 * TypeScript types matching the backend C# DTOs.
 *
 * These are aligned with:
 * - DashboardOverviewDto (SmartGrid.Application.Dashboard.Models)
 * - DailyDemandPointDto (SmartGrid.Application.Dashboard.Models)
 *
 * JSON property names use camelCase (ASP.NET Core default serialization).
 *
 * Units:
 * - Energy: kWh (totalEnergyKwh, totalDailyKwh)
 * - Power: kW (peakNetworkKw)
 * - Timestamps: ISO 8601 UTC
 */

/** GET /api/dashboard/summary response */
export interface DashboardOverview {
  periodStart: string;
  periodEnd: string;
  totalEnergyKwh: number | null;
  peakNetworkKw: number | null;
  totalAlerts: number | null;
  theftTransformers: number | null;
  overloadTransformers: number | null;
  linedropTransformers: number | null;
  affectedTransformers: number | null;
  latestReadingTimestamp: string | null;
}

/** GET /api/dashboard/demand response (array element) */
export interface DailyDemandPoint {
  demandDate: string;
  totalDailyKwh: number;
  peakNetworkKw: number;
}

/** Date range used for query parameters. */
export interface DateRangeParams {
  from: string; // YYYY-MM-DD
  to: string;   // YYYY-MM-DD
}
