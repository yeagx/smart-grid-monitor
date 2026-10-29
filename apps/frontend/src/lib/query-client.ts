import { QueryClient } from '@tanstack/react-query';

/**
 * Shared TanStack Query client.
 *
 * Default configuration:
 * - 3 retries with exponential backoff for transient errors.
 * - Stale time of 2 minutes — data is considered fresh for re-renders.
 * - GC time of 5 minutes — cached entries are evicted 5 min after last usage.
 * - Network-error retries are limited; 4xx errors are not retried.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 2 * 60 * 1000,
      gcTime: 5 * 60 * 1000,
      retry: (failureCount, error) => {
        // Don't retry client errors (4xx)
        if (error && 'status' in error && typeof error.status === 'number') {
          if (error.status >= 400 && error.status < 500) return false;
        }
        return failureCount < 3;
      },
      refetchOnWindowFocus: false,
    },
  },
});
