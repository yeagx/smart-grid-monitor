import { type ReactNode } from 'react';
import { cn } from '@/lib/utils';
import { AlertTriangle, ChevronLeft, ChevronRight, RefreshCw } from 'lucide-react';
import { eventTypeLabel, isEventType, EVENT_TYPES } from '@/lib/event-types';

/* ------------------------------------------------------------------ */
/*  Skeleton                                                          */
/* ------------------------------------------------------------------ */
export function Skeleton({ className }: { className?: string }) {
  return <div className={cn('skeleton', className)} />;
}

/* ------------------------------------------------------------------ */
/*  ErrorCard                                                         */
/* ------------------------------------------------------------------ */
interface ErrorCardProps {
  title?: string;
  message: string;
  onRetry?: () => void;
  className?: string;
}

export function ErrorCard({ title = 'Something went wrong', message, onRetry, className }: ErrorCardProps) {
  return (
    <div
      className={cn(
        'card flex flex-col items-center justify-center gap-3 p-8 text-center',
        className,
      )}
      role="alert"
    >
      <AlertTriangle className="h-8 w-8 text-status-overload" aria-hidden="true" />
      <h3 className="text-sm font-semibold text-gray-200">{title}</h3>
      <p className="text-xs text-gray-400 max-w-md">{message}</p>
      {onRetry && (
        <button onClick={onRetry} className="btn-ghost mt-2" type="button">
          <RefreshCw className="h-3.5 w-3.5" aria-hidden="true" />
          Retry
        </button>
      )}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  EmptyState                                                        */
/* ------------------------------------------------------------------ */
interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description?: string;
  className?: string;
}

export function EmptyState({ icon, title, description, className }: EmptyStateProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center gap-2 p-8 text-center', className)}>
      {icon && <div className="text-gray-500">{icon}</div>}
      <h3 className="text-sm font-medium text-gray-300">{title}</h3>
      {description && <p className="text-xs text-gray-500 max-w-sm">{description}</p>}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  StatusBadge                                                       */
/* ------------------------------------------------------------------ */
type BadgeVariant = 'healthy' | 'theft' | 'overload' | 'linedrop' | 'neutral';

const badgeStyles: Record<BadgeVariant, string> = {
  healthy: 'bg-status-healthy/15 text-status-healthy border-status-healthy/30',
  theft: 'bg-status-theft/15 text-status-theft border-status-theft/30',
  overload: 'bg-status-overload/15 text-status-overload border-status-overload/30',
  linedrop: 'bg-status-linedrop/15 text-status-linedrop border-status-linedrop/30',
  neutral: 'bg-gray-500/15 text-gray-400 border-gray-500/30',
};

interface StatusBadgeProps {
  variant: BadgeVariant;
  children: ReactNode;
  className?: string;
}

export function StatusBadge({ variant, children, className }: StatusBadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 px-2.5 py-0.5 text-xs font-medium rounded-full border',
        badgeStyles[variant],
        className,
      )}
    >
      <span
        className={cn('h-1.5 w-1.5 rounded-full', {
          'bg-status-healthy': variant === 'healthy',
          'bg-status-theft': variant === 'theft',
          'bg-status-overload': variant === 'overload',
          'bg-status-linedrop': variant === 'linedrop',
          'bg-gray-400': variant === 'neutral',
        })}
        aria-hidden="true"
      />
      {children}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/*  EventTypeBadge — one badge style for theft / overload / line_drop   */
/* ------------------------------------------------------------------ */
export function EventTypeBadge({ type, className }: { type: string; className?: string }) {
  const variant: BadgeVariant = isEventType(type) ? EVENT_TYPES[type].variant : 'neutral';
  return (
    <StatusBadge variant={variant} className={className}>
      {eventTypeLabel(type)}
    </StatusBadge>
  );
}

/* ------------------------------------------------------------------ */
/*  PageHeader                                                         */
/* ------------------------------------------------------------------ */
interface PageHeaderProps {
  icon: React.ElementType;
  title: string;
  description: string;
  isFetching?: boolean;
  onRefresh?: () => void;
  children?: ReactNode;
}

export function PageHeader({ icon: Icon, title, description, isFetching, onRefresh, children }: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <div className="flex items-center gap-2">
          <Icon className="h-5 w-5 text-brand-light" aria-hidden="true" />
          <h1 className="text-xl font-semibold text-gray-100">{title}</h1>
        </div>
        <p className="mt-0.5 text-sm text-gray-400">{description}</p>
      </div>
      <div className="flex items-center gap-3 shrink-0">
        {children}
        {onRefresh && (
          <button
            type="button"
            onClick={onRefresh}
            disabled={isFetching}
            className="btn-ghost"
            aria-label={`Refresh ${title}`}
            title="Refresh"
          >
            <RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin text-brand-light')} aria-hidden="true" />
          </button>
        )}
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  Form field styles                                                  */
/* ------------------------------------------------------------------ */
export const fieldClass =
  'h-9 rounded-lg border border-grid-border bg-grid-panel px-3 text-sm text-gray-200 outline-none focus:border-brand focus:ring-1 focus:ring-brand transition-colors';

export function Field({ label, htmlFor, children }: { label: string; htmlFor: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={htmlFor} className="text-xs font-medium text-gray-500 uppercase tracking-wider">
        {label}
      </label>
      {children}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  Pagination                                                         */
/* ------------------------------------------------------------------ */
interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  className?: string;
}

export function Pagination({ page, pageSize, totalCount, onPageChange, className }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const first = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, totalCount);

  return (
    <div className={cn('flex items-center justify-between gap-3 text-xs text-gray-400', className)}>
      <span>
        {totalCount === 0 ? 'No results' : (
          <>Showing <span className="text-gray-200 font-mono">{first}–{last}</span> of{' '}
          <span className="text-gray-200 font-mono">{totalCount.toLocaleString('en-US')}</span></>
        )}
      </span>
      <div className="flex items-center gap-1">
        <button
          type="button"
          className="btn-ghost px-2 py-1 disabled:opacity-40 disabled:cursor-not-allowed"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Previous page"
        >
          <ChevronLeft className="h-4 w-4" aria-hidden="true" />
        </button>
        <span className="px-1 font-mono text-gray-300">
          {page} / {totalPages}
        </span>
        <button
          type="button"
          className="btn-ghost px-2 py-1 disabled:opacity-40 disabled:cursor-not-allowed"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Next page"
        >
          <ChevronRight className="h-4 w-4" aria-hidden="true" />
        </button>
      </div>
    </div>
  );
}
