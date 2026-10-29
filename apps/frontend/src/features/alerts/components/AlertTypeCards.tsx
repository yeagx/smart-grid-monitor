import { cn } from '@/lib/utils';
import { EVENT_TYPE_LIST, type EventType } from '@/lib/event-types';
import { formatInt } from '@/lib/formatters';
import { Skeleton } from '@/components/ui';
import type { AlertSummaryByType } from '../types';

interface AlertTypeCardsProps {
  data: AlertSummaryByType[] | undefined;
  isLoading: boolean;
  activeType: EventType | '';
  onSelect: (type: EventType | '') => void;
}

/** Three clickable cards (one per type). Clicking filters the log; clicking again clears it. */
export function AlertTypeCards({ data, isLoading, activeType, onSelect }: AlertTypeCardsProps) {
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
      {EVENT_TYPE_LIST.map((meta) => {
        const row = data?.find((d) => d.eventType === meta.value);
        const active = activeType === meta.value;
        return (
          <button
            key={meta.value}
            type="button"
            onClick={() => onSelect(active ? '' : meta.value)}
            aria-pressed={active}
            className={cn(
              'card-hover p-4 text-left',
              active && 'ring-1 ring-offset-0',
            )}
            style={active ? { borderColor: meta.color, boxShadow: `0 0 0 1px ${meta.color}` } : undefined}
          >
            <div className="flex items-center gap-2">
              <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: meta.color }} aria-hidden="true" />
              <span className="text-xs font-medium uppercase tracking-wider text-gray-400">{meta.label}</span>
            </div>
            {isLoading ? (
              <Skeleton className="mt-2 h-7 w-16" />
            ) : (
              <p className="mt-1 text-2xl font-semibold font-mono" style={{ color: meta.color }}>
                {formatInt(row?.totalAlerts ?? 0)}
              </p>
            )}
            <p className="text-[11px] text-gray-500">
              alerts on {formatInt(row?.affectedTransformers ?? 0)} transformer{row?.affectedTransformers === 1 ? '' : 's'}
            </p>
          </button>
        );
      })}
    </div>
  );
}
