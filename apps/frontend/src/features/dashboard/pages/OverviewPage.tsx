import { useState, useCallback } from 'react';
import { DashboardHeader } from '../components/DashboardHeader';
import { KpiGrid } from '../components/KpiGrid';
import { DemandChart } from '../components/DemandChart';
import { AlertSummary } from '../components/AlertSummary';
import {
  useDashboardSummary,
  useDailyDemand,
  useRefreshDashboard,
} from '../hooks';
import type { DateRangeParams } from '../types';

/**
 * Default date range matches the mock fixture dates (Aug 1–2, 2026).
 * This ensures the dashboard shows useful data on first load
 * when running against the mock backend provider.
 */
const DEFAULT_DATE_RANGE: DateRangeParams = {
  from: '2026-08-01',
  to: '2026-08-02',
};

/**
 * Overview Dashboard page.
 *
 * Fetches summary and demand data independently so partial failures
 * don't take down the entire page. Uses TanStack Query for caching
 * and refetching.
 */
export function OverviewPage() {
  // Input state (what the user is editing)
  const [inputFrom, setInputFrom] = useState(DEFAULT_DATE_RANGE.from);
  const [inputTo, setInputTo] = useState(DEFAULT_DATE_RANGE.to);
  const [dateError, setDateError] = useState<string | null>(null);

  // Active query params (what we're actually fetching)
  const [activeRange, setActiveRange] = useState<DateRangeParams>(DEFAULT_DATE_RANGE);

  const summaryQuery = useDashboardSummary(activeRange);
  const demandQuery = useDailyDemand(activeRange);
  const refresh = useRefreshDashboard();

  const isFetching = summaryQuery.isFetching || demandQuery.isFetching;

  const handleApply = useCallback(() => {
    // Validate
    if (!inputFrom || !inputTo) {
      setDateError('Both dates are required.');
      return;
    }
    if (inputFrom > inputTo) {
      setDateError('Start date must not be later than end date.');
      return;
    }

    // Check 366-day limit
    const from = new Date(inputFrom);
    const to = new Date(inputTo);
    const diffDays = Math.ceil((to.getTime() - from.getTime()) / (1000 * 60 * 60 * 24)) + 1;
    if (diffDays > 366) {
      setDateError('Date range cannot exceed 366 days.');
      return;
    }

    setDateError(null);
    setActiveRange({ from: inputFrom, to: inputTo });
  }, [inputFrom, inputTo]);

  // Derive the "last fetched at" from whichever query updated most recently
  const lastFetchedAt = Math.max(
    summaryQuery.dataUpdatedAt || 0,
    demandQuery.dataUpdatedAt || 0,
  ) || undefined;

  return (
    <div className="space-y-6">
      <DashboardHeader
        dateFrom={inputFrom}
        dateTo={inputTo}
        latestTimestamp={summaryQuery.data?.latestReadingTimestamp}
        lastFetchedAt={lastFetchedAt}
        isFetching={isFetching}
        onRefresh={refresh}
        onDateFromChange={setInputFrom}
        onDateToChange={setInputTo}
        onApply={handleApply}
        dateError={dateError}
      />

      <KpiGrid
        data={summaryQuery.data}
        isLoading={summaryQuery.isLoading}
        isError={summaryQuery.isError}
        error={summaryQuery.error}
        onRetry={() => summaryQuery.refetch()}
      />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <div className="xl:col-span-2">
          <DemandChart
            data={demandQuery.data}
            isLoading={demandQuery.isLoading}
            isError={demandQuery.isError}
            error={demandQuery.error}
            onRetry={() => demandQuery.refetch()}
          />
        </div>
        <div className="xl:col-span-1">
          <AlertSummary
            data={summaryQuery.data}
            isLoading={summaryQuery.isLoading}
          />
        </div>
      </div>
    </div>
  );
}
