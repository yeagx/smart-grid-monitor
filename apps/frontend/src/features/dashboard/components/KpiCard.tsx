import { type ReactNode } from 'react';
import { cn } from '@/lib/utils';
import { Skeleton } from '@/components/ui';

interface KpiCardProps {
  label: string;
  value: string;
  icon: ReactNode;
  subtitle?: string;
  accentClass?: string;
  isLoading?: boolean;
}

/**
 * A single KPI metric card used in the dashboard overview.
 *
 * Displays a label, formatted value, icon, and optional subtitle.
 * Shows a skeleton while loading.
 */
export function KpiCard({
  label,
  value,
  icon,
  subtitle,
  accentClass = 'text-brand-light',
  isLoading = false,
}: KpiCardProps) {
  return (
    <div className="card-hover p-4 sm:p-5">
      <div className="flex items-start justify-between">
        <div className="min-w-0 flex-1">
          <p className="text-xs font-medium uppercase tracking-wider text-gray-500">{label}</p>
          {isLoading ? (
            <Skeleton className="mt-2 h-7 w-24" />
          ) : (
            <p className={cn('mt-1 whitespace-nowrap text-xl font-semibold tracking-tight sm:text-2xl', accentClass)}>
              {value}
            </p>
          )}
          {subtitle && !isLoading && (
            <p className="mt-1 text-xs text-gray-500">{subtitle}</p>
          )}
          {isLoading && <Skeleton className="mt-2 h-3 w-16" />}
        </div>
        <div className={cn('shrink-0 rounded-lg bg-grid-surface p-2', accentClass)}>
          {icon}
        </div>
      </div>
    </div>
  );
}
