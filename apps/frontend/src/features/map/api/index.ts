import { apiFetch } from '@/lib/api-client';
import type { MapMarker, MapFilterParams } from '../types';

/**
 * Fetches transformer map markers and their alert aggregations for the specified criteria.
 */
export async function fetchMapMarkers(
  params: MapFilterParams,
  signal?: AbortSignal,
): Promise<MapMarker[]> {
  const searchParams = new URLSearchParams({
    from: params.from,
    to: params.to,
  });

  if (params.district) {
    searchParams.set('district', params.district);
  }

  if (params.type) {
    searchParams.set('type', params.type);
  }

  if (params.onlyWithAlerts !== undefined) {
    searchParams.set('onlyWithAlerts', String(params.onlyWithAlerts));
  }

  return apiFetch<MapMarker[]>(`/api/grid/map-markers?${searchParams.toString()}`, {
    signal,
  });
}
