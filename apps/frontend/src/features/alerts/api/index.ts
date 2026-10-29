/**
 * Alert log API.
 * - GET /api/alerts?from&to&type&district&transformerId&page&pageSize
 * - GET /api/alerts/summary?from&to
 */
import { apiFetch } from '@/lib/api-client';
import { toQueryString, type PagedResult } from '@/lib/paging';
import type { Alert, AlertSearchParams, AlertSummaryByType } from '../types';

export function fetchAlerts(params: AlertSearchParams, signal?: AbortSignal): Promise<PagedResult<Alert>> {
  return apiFetch<PagedResult<Alert>>(`/api/alerts${toQueryString({ ...params })}`, { signal });
}

export function fetchAlertSummary(from: string, to: string, signal?: AbortSignal): Promise<AlertSummaryByType[]> {
  return apiFetch<AlertSummaryByType[]>(`/api/alerts/summary${toQueryString({ from, to })}`, { signal });
}

/**
 * Fetches every page for the given filters (100 per request) — used by CSV export.
 * Stops at `maxRows` so a very wide filter can't hang the browser.
 */
export async function fetchAllAlerts(params: AlertSearchParams, maxRows = 5000): Promise<Alert[]> {
  const rows: Alert[] = [];
  for (let page = 1; rows.length < maxRows; page++) {
    const result = await fetchAlerts({ ...params, page, pageSize: 100 });
    rows.push(...result.items);
    if (page >= result.totalPages || result.items.length === 0) break;
  }
  return rows.slice(0, maxRows);
}
