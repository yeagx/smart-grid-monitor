import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchLoadProfile, fetchTransformer, fetchTransformers } from '../api';
import type { TransformerSearchParams } from '../types';

export const transformerKeys = {
  all: ['transformers'] as const,
  list: (params: TransformerSearchParams) => ['transformers', 'list', params] as const,
  detail: (id: string) => ['transformers', 'detail', id] as const,
  profile: (id: string, from: string, to: string) => ['transformers', 'profile', id, { from, to }] as const,
};

export function useTransformers(params: TransformerSearchParams) {
  return useQuery({
    queryKey: transformerKeys.list(params),
    queryFn: ({ signal }) => fetchTransformers(params, signal),
    placeholderData: keepPreviousData,
  });
}

export function useTransformer(id: string | null) {
  return useQuery({
    queryKey: transformerKeys.detail(id ?? ''),
    queryFn: ({ signal }) => fetchTransformer(id!, signal),
    enabled: Boolean(id),
  });
}

export function useLoadProfile(id: string | null, from: string, to: string) {
  return useQuery({
    queryKey: transformerKeys.profile(id ?? '', from, to),
    queryFn: ({ signal }) => fetchLoadProfile(id!, from, to, signal),
    enabled: Boolean(id),
  });
}

export function useRefreshTransformers() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: transformerKeys.all });
}
