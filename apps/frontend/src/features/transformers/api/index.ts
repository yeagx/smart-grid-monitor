/**
 * Transformer API.
 * - GET /api/transformers?search&district&sort&onlyWithAlerts&page&pageSize
 * - GET /api/transformers/{id}
 * - GET /api/transformers/{id}/load-profile?from&to
 */
import { apiFetch } from '@/lib/api-client';
import { toQueryString, type PagedResult } from '@/lib/paging';
import type { TransformerDetails, TransformerListItem, TransformerLoadPoint, TransformerSearchParams } from '../types';

export function fetchTransformers(params: TransformerSearchParams, signal?: AbortSignal): Promise<PagedResult<TransformerListItem>> {
  return apiFetch<PagedResult<TransformerListItem>>(`/api/transformers${toQueryString({ ...params })}`, { signal });
}

export function fetchTransformer(id: string, signal?: AbortSignal): Promise<TransformerDetails> {
  return apiFetch<TransformerDetails>(`/api/transformers/${encodeURIComponent(id)}`, { signal });
}

export function fetchLoadProfile(id: string, from: string, to: string, signal?: AbortSignal): Promise<TransformerLoadPoint[]> {
  return apiFetch<TransformerLoadPoint[]>(
    `/api/transformers/${encodeURIComponent(id)}/load-profile${toQueryString({ from, to })}`,
    { signal },
  );
}
