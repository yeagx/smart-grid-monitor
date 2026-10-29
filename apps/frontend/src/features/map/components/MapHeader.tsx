import { RefreshCw, MapPin } from 'lucide-react';
import { formatDate } from '@/lib/formatters';

interface MapHeaderProps {
  dateFrom: string;
  dateTo: string;
  lastFetchedAt?: number;
  isFetching: boolean;
  onRefresh: () => void;
}

export function MapHeader({
  dateFrom,
  dateTo,
  lastFetchedAt,
  isFetching,
  onRefresh,
}: MapHeaderProps) {
  const fromFormatted = formatDate(dateFrom);
  const toFormatted = formatDate(dateTo);

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <div className="flex items-center gap-2">
          <MapPin className="h-5 w-5 text-brand-light" aria-hidden="true" />
          <h1 className="text-xl font-semibold text-gray-100">Alert Map</h1>
        </div>
        <p className="mt-0.5 text-sm text-gray-400">
          Explore transformer locations and investigate detected grid anomalies across the network.
        </p>
      </div>

      <div className="flex items-center gap-3 shrink-0">
        <div className="flex flex-col items-end gap-0.5 text-xs text-gray-400">
          <div>
            Period: <span className="font-medium text-gray-200">{fromFormatted} — {toFormatted}</span>
          </div>
          {lastFetchedAt && (
            <span className="text-[10px] text-gray-500">
              Refreshed {new Date(lastFetchedAt).toLocaleTimeString()}
            </span>
          )}
        </div>

        <button
          type="button"
          onClick={onRefresh}
          disabled={isFetching}
          className="btn-ghost"
          aria-label="Refresh map data"
          title="Refresh map markers"
        >
          <RefreshCw
            className={`h-4 w-4 ${isFetching ? 'animate-spin text-brand-light' : ''}`}
            aria-hidden="true"
          />
        </button>
      </div>
    </div>
  );
}
