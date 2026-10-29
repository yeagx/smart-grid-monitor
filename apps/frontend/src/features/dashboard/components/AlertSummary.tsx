import { Link } from 'react-router-dom';
import { ShieldAlert, AlertTriangle, Activity, ArrowRight } from 'lucide-react';
import { StatusBadge, Skeleton } from '@/components/ui';
import { formatInt } from '@/lib/formatters';
import type { DashboardOverview } from '../types';

interface AlertSummaryProps {
  data: DashboardOverview | undefined;
  isLoading: boolean;
}

interface AlertTypeRow {
  label: string;
  description: string;
  count: number | null | undefined;
  variant: 'theft' | 'overload' | 'linedrop';
  type: 'theft' | 'overload' | 'line_drop';
  icon: React.ElementType;
}

/**
 * Compact alert breakdown panel showing the three alert types
 * and their affected transformer counts from the summary endpoint.
 */
export function AlertSummary({ data, isLoading }: AlertSummaryProps) {
  const range = data ? `from=${data.periodStart}&to=${data.periodEnd}` : '';

  const rows: AlertTypeRow[] = [
    {
      label: 'Theft',
      description: 'Transformer-level energy loss detected',
      count: data?.theftTransformers,
      variant: 'theft',
      type: 'theft',
      icon: ShieldAlert,
    },
    {
      label: 'Overload',
      description: 'Load ratio ≥ 85% sustained',
      count: data?.overloadTransformers,
      variant: 'overload',
      type: 'overload',
      icon: AlertTriangle,
    },
    {
      label: 'Voltage Drop',
      description: 'Voltage < 207V sustained',
      count: data?.linedropTransformers,
      variant: 'linedrop',
      type: 'line_drop',
      icon: Activity,
    },
  ];

  return (
    <div className="card p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold text-gray-200">Alert Breakdown</h2>
        {data?.totalAlerts != null && (
          <StatusBadge variant={data.totalAlerts > 0 ? 'overload' : 'healthy'}>
            {formatInt(data.totalAlerts)} total alert{data.totalAlerts !== 1 ? 's' : ''}
          </StatusBadge>
        )}
      </div>

      <div className="space-y-3">
        {rows.map((row) => {
          const Icon = row.icon;
          return (
            <Link
              key={row.variant}
              to={`/alert-log?type=${row.type}${range ? `&${range}` : ''}`}
              className="flex items-center gap-3 rounded-lg border border-grid-border bg-grid-surface/50 p-3 transition-colors hover:border-grid-border-light hover:bg-grid-surface"
              title={`Open the alert log filtered to ${row.label}`}
            >
              <div
                className={`shrink-0 rounded-lg p-2 ${
                  row.variant === 'theft'
                    ? 'bg-status-theft/10 text-status-theft'
                    : row.variant === 'overload'
                      ? 'bg-status-overload/10 text-status-overload'
                      : 'bg-status-linedrop/10 text-status-linedrop'
                }`}
              >
                <Icon className="h-4 w-4" aria-hidden="true" />
              </div>

              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium text-gray-200">{row.label}</span>
                </div>
                <p className="text-xs text-gray-500 mt-0.5">{row.description}</p>
              </div>

              <div className="text-right shrink-0">
                {isLoading ? (
                  <Skeleton className="h-6 w-8" />
                ) : (
                  <span className="text-lg font-semibold text-gray-200 font-mono">
                    {formatInt(row.count)}
                  </span>
                )}
                <p className="text-[10px] text-gray-600 uppercase tracking-wider">
                  transformers
                </p>
              </div>
            </Link>
          );
        })}
      </div>

      <Link
        to={`/alert-log${range ? `?${range}` : ''}`}
        className="mt-4 inline-flex items-center gap-1.5 text-xs text-sky-400 hover:text-sky-300 hover:underline"
      >
        <span>Open the full alert log</span>
        <ArrowRight className="h-3 w-3" aria-hidden="true" />
      </Link>
    </div>
  );
}
