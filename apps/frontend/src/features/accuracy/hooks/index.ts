import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchAccuracyDetails, fetchAccuracyReport } from '../api';
import type { AccuracyDetailParams } from '../types';

export const accuracyKeys = {
  all: ['accuracy'] as const,
  report: ['accuracy', 'report'] as const,
  details: (params: AccuracyDetailParams) => ['accuracy', 'details', params] as const,
};

export function useAccuracyReport() {
  return useQuery({
    queryKey: accuracyKeys.report,
    queryFn: ({ signal }) => fetchAccuracyReport(signal),
  });
}

export function useAccuracyDetails(params: AccuracyDetailParams) {
  return useQuery({
    queryKey: accuracyKeys.details(params),
    queryFn: ({ signal }) => fetchAccuracyDetails(params, signal),
    placeholderData: keepPreviousData,
  });
}

export function useRefreshAccuracy() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: accuracyKeys.all });
}
