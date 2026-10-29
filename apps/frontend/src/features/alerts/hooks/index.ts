import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchAlertSummary, fetchAlerts } from '../api';
import type { AlertSearchParams } from '../types';

export const alertKeys = {
  all: ['alerts'] as const,
  list: (params: AlertSearchParams) => ['alerts', 'list', params] as const,
  summary: (from: string, to: string) => ['alerts', 'summary', { from, to }] as const,
};

/** Paged alert search. Keeps the previous page on screen while the next one loads. */
export function useAlerts(params: AlertSearchParams, enabled = true) {
  return useQuery({
    queryKey: alertKeys.list(params),
    queryFn: ({ signal }) => fetchAlerts(params, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

export function useAlertSummary(from: string, to: string) {
  return useQuery({
    queryKey: alertKeys.summary(from, to),
    queryFn: ({ signal }) => fetchAlertSummary(from, to, signal),
  });
}

export function useRefreshAlerts() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: alertKeys.all });
}
