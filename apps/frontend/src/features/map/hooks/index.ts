import { useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchMapMarkers } from '../api';
import type { MapFilterParams } from '../types';

export const mapQueryKeys = {
  all: ['grid', 'map-markers'] as const,
  markers: (params: MapFilterParams) =>
    [
      ...mapQueryKeys.all,
      {
        from: params.from,
        to: params.to,
        district: params.district ?? null,
        type: params.type ?? null,
        onlyWithAlerts: params.onlyWithAlerts ?? false,
      },
    ] as const,
};

/**
 * Hook to retrieve transformer map markers matching the specified filters.
 */
export function useMapMarkers(params: MapFilterParams) {
  return useQuery({
    queryKey: mapQueryKeys.markers(params),
    queryFn: ({ signal }) => fetchMapMarkers(params, signal),
    enabled: Boolean(params.from && params.to),
  });
}

/**
 * Hook providing a callback to invalidate map queries and force refetch.
 */
export function useRefreshMap() {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: mapQueryKeys.all });
  };
}
