/**
 * API functions for the Dashboard feature.
 *
 * Endpoints consumed:
 * - GET /api/dashboard/summary?from=YYYY-MM-DD&to=YYYY-MM-DD
 * - GET /api/dashboard/demand?from=YYYY-MM-DD&to=YYYY-MM-DD
 */

import { apiFetch } from '@/lib/api-client';
import type { DashboardOverview, DailyDemandPoint, DateRangeParams } from '../types';

/** Fetch the dashboard overview/summary for a date range. */
export async function fetchDashboardSummary(
  params: DateRangeParams,
  signal?: AbortSignal,
): Promise<DashboardOverview> {
  const qs = new URLSearchParams({
    from: params.from,
    to: params.to,
  });
  return apiFetch<DashboardOverview>(`/api/dashboard/summary?${qs}`, { signal });
}

/** Fetch daily demand data points for a date range. */
export async function fetchDailyDemand(
  params: DateRangeParams,
  signal?: AbortSignal,
): Promise<DailyDemandPoint[]> {
  const qs = new URLSearchParams({
    from: params.from,
    to: params.to,
  });
  return apiFetch<DailyDemandPoint[]>(`/api/dashboard/demand?${qs}`, { signal });
}
