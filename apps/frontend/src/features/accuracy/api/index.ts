/**
 * Accuracy API.
 * - GET /api/accuracy
 * - GET /api/accuracy/details?type&status&transformerId&page&pageSize
 */
import { apiFetch } from '@/lib/api-client';
import { toQueryString, type PagedResult } from '@/lib/paging';
import type { AccuracyDetail, AccuracyDetailParams, AccuracyReport } from '../types';

export function fetchAccuracyReport(signal?: AbortSignal): Promise<AccuracyReport> {
  return apiFetch<AccuracyReport>('/api/accuracy', { signal });
}

export function fetchAccuracyDetails(params: AccuracyDetailParams, signal?: AbortSignal): Promise<PagedResult<AccuracyDetail>> {
  return apiFetch<PagedResult<AccuracyDetail>>(`/api/accuracy/details${toQueryString({ ...params })}`, { signal });
}
