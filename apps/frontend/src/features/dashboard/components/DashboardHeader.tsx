import { RefreshCw, Clock } from 'lucide-react';
import { formatRelativeTime, formatTimestamp } from '@/lib/formatters';

interface DashboardHeaderProps {
  dateFrom: string;
  dateTo: string;
  latestTimestamp: string | null | undefined;
  lastFetchedAt: number | undefined;
  isFetching: boolean;
  onRefresh: () => void;
  onDateFromChange: (value: string) => void;
  onDateToChange: (value: string) => void;
  onApply: () => void;
  dateError: string | null;
}

export function DashboardHeader({
  dateFrom,
  dateTo,
  latestTimestamp,
  lastFetchedAt,
  isFetching,
  onRefresh,
  onDateFromChange,
  onDateToChange,
  onApply,
  dateError,
}: DashboardHeaderProps) {
  const browserRefreshText = lastFetchedAt
    ? `Browser refreshed ${formatRelativeTime(new Date(lastFetchedAt).toISOString())}`
    : null;

  return (
    <div className="space-y-4">
      {/* Title row */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-gray-100">Overview Dashboard</h1>
          <p className="mt-0.5 text-sm text-gray-500">
            Grid health summary and daily demand analysis
          </p>
        </div>

        {/* Freshness + refresh */}
        <div className="flex items-center gap-3 shrink-0">
          {/* Data freshness */}
          <div className="flex flex-col items-end gap-0.5">
            <div className="flex items-center gap-1.5 text-xs text-gray-400" title="Latest transformer reading timestamp from the warehouse">
              <Clock className="h-3 w-3" aria-hidden="true" />
              <span>
                Latest reading: {latestTimestamp ? formatTimestamp(latestTimestamp) : 'none in period'}
              </span>
            </div>
            {browserRefreshText && (
              <span className="text-[10px] text-gray-600">{browserRefreshText}</span>
            )}
          </div>

          <button
            onClick={onRefresh}
            disabled={isFetching}
            className="btn-ghost"
            type="button"
            aria-label="Refresh dashboard data"
          >
            <RefreshCw
              className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`}
              aria-hidden="true"
            />
          </button>
        </div>
      </div>

      {/* Date range controls */}
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex flex-col gap-1">
          <label htmlFor="date-from" className="text-xs font-medium text-gray-500 uppercase tracking-wider">
            From
          </label>
          <input
            id="date-from"
            type="date"
            value={dateFrom}
            onChange={(e) => onDateFromChange(e.target.value)}
            className="h-9 rounded-lg border border-grid-border bg-grid-panel px-3 text-sm text-gray-200 outline-none focus:border-brand focus:ring-1 focus:ring-brand transition-colors"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="date-to" className="text-xs font-medium text-gray-500 uppercase tracking-wider">
            To
          </label>
          <input
            id="date-to"
            type="date"
            value={dateTo}
            onChange={(e) => onDateToChange(e.target.value)}
            className="h-9 rounded-lg border border-grid-border bg-grid-panel px-3 text-sm text-gray-200 outline-none focus:border-brand focus:ring-1 focus:ring-brand transition-colors"
          />
        </div>

        <button onClick={onApply} className="btn-primary h-9" type="button">
          Apply
        </button>

        {dateError && (
          <p className="text-xs text-status-theft" role="alert">{dateError}</p>
        )}
      </div>
    </div>
  );
}
