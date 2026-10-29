/**
 * TanStack Query hooks for the Dashboard feature.
 *
 * Query keys include the date range to ensure different periods
 * don't share cached results.
 */

import { useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchDashboardSummary, fetchDailyDemand } from '../api';
import type { DateRangeParams, DashboardOverview, DailyDemandPoint } from '../types';

/** Query key factories for dashboard queries. */
export const dashboardKeys = {
  all: ['dashboard'] as const,
  summary: (params: DateRangeParams) => ['dashboard', 'summary', params] as const,
  demand: (params: DateRangeParams) => ['dashboard', 'demand', params] as const,
};

/** Fetch and cache the dashboard summary for a given date range. */
export function useDashboardSummary(params: DateRangeParams) {
  return useQuery<DashboardOverview>({
    queryKey: dashboardKeys.summary(params),
    queryFn: ({ signal }) => fetchDashboardSummary(params, signal),
  });
}

/** Fetch and cache daily demand data for a given date range. */
export function useDailyDemand(params: DateRangeParams) {
  return useQuery<DailyDemandPoint[]>({
    queryKey: dashboardKeys.demand(params),
    queryFn: ({ signal }) => fetchDailyDemand(params, signal),
  });
}

/** Hook to invalidate all dashboard queries (manual refresh). */
export function useRefreshDashboard() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: dashboardKeys.all });
}
