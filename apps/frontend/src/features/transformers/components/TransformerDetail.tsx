import { Link } from 'react-router-dom';
import { Bell, Cpu, MapPin } from 'lucide-react';
import { EmptyState, ErrorCard, EventTypeBadge, Skeleton } from '@/components/ui';
import { formatDate, formatDuration, formatKw, formatPercent, formatShortTimestamp } from '@/lib/formatters';
import { useAlerts } from '@/features/alerts/hooks';
import { useLoadProfile, useTransformer } from '../hooks';
import { LoadProfileChart } from './LoadProfileChart';
import { OVERLOAD_THRESHOLD_RATIO, VOLTAGE_DROP_THRESHOLD_V } from '../types';

interface TransformerDetailProps {
  transformerId: string;
  from: string;
  to: string;
}

function Stat({ label, value, hint, tone }: { label: string; value: string; hint?: string; tone?: 'bad' | 'warn' }) {
  return (
    <div className="rounded-lg border border-grid-border bg-grid-surface/50 p-3">
      <span className="text-[10px] uppercase tracking-wider text-gray-500">{label}</span>
      <p className={`mt-0.5 font-semibold font-mono ${tone === 'bad' ? 'text-status-theft' : tone === 'warn' ? 'text-status-overload' : 'text-gray-100'}`}>
        {value}
      </p>
      {hint && <span className="text-[10px] text-gray-500">{hint}</span>}
    </div>
  );
}

/**
 * Selected transformer: specs, period statistics computed from the hourly profile,
 * load/voltage charts with alert bands, and its alerts in the period.
 */
export function TransformerDetail({ transformerId, from, to }: TransformerDetailProps) {
  const detailQuery = useTransformer(transformerId);
  const profileQuery = useLoadProfile(transformerId, from, to);
  const alertsQuery = useAlerts({ transformerId, from, to, page: 1, pageSize: 100 });

  if (detailQuery.isError) {
    const notFound = (detailQuery.error as { status?: number } | null)?.status === 404;
    return (
      <ErrorCard
        title={notFound ? `Transformer ${transformerId} not found` : 'Failed to load transformer'}
        message={notFound ? 'Check the ID or pick a transformer from the list above.' : detailQuery.error?.message ?? 'Unexpected error.'}
        onRetry={notFound ? undefined : () => detailQuery.refetch()}
      />
    );
  }

  const t = detailQuery.data;
  const points = profileQuery.data ?? [];
  // Exact match only — the alert search is a partial match (TR-001 would also match TR-0012).
  const alerts = (alertsQuery.data?.items ?? []).filter((a) => a.transformerId.toUpperCase() === transformerId.toUpperCase());

  const peakKw = points.length ? Math.max(...points.map((p) => p.maxKw)) : null;
  const peakPct = peakKw != null && t?.capacityKw ? (peakKw / t.capacityKw) * 100 : null;
  const minVoltage = points.length ? Math.min(...points.map((p) => p.minVoltage)) : null;
  const energy = points.length ? points.reduce((sum, p) => sum + p.energyKwh, 0) : null;

  return (
    <div className="card p-5 space-y-5">
      {/* Header */}
      <div className="flex flex-col gap-3 border-b border-grid-border pb-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <Cpu className="h-4 w-4 text-brand-light" aria-hidden="true" />
            <span className="text-lg font-semibold text-gray-100 font-mono">{transformerId}</span>
            {t && (
              <span className="rounded bg-grid-surface px-2 py-0.5 text-xs text-gray-400">
                {t.district}, {t.governorate}
              </span>
            )}
          </div>
          {t ? (
            <p className="mt-1 text-xs text-gray-500">
              Feeder <span className="font-mono text-gray-300">{t.feederId}</span> · Substation{' '}
              <span className="font-mono text-gray-300">{t.substationId}</span> · {t.latitude.toFixed(4)}, {t.longitude.toFixed(4)}
            </p>
          ) : (
            <Skeleton className="mt-2 h-4 w-64" />
          )}
          <p className="mt-1 text-xs text-gray-500">Period: {formatDate(from)} — {formatDate(to)} (UTC)</p>
        </div>
        <div className="flex gap-2">
          {t && (
            <Link to={`/alerts/map?district=${encodeURIComponent(t.district)}&from=${from}&to=${to}`} className="btn-ghost text-xs">
              <MapPin className="h-3.5 w-3.5" aria-hidden="true" />
              Map
            </Link>
          )}
          <Link to={`/alert-log?transformerId=${encodeURIComponent(transformerId)}&from=${from}&to=${to}`} className="btn-primary text-xs">
            <Bell className="h-3.5 w-3.5" aria-hidden="true" />
            Alert log
          </Link>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
        <Stat label="Capacity" value={t ? formatKw(t.capacityKw) : '—'} />
        <Stat
          label="Peak in period"
          value={formatKw(peakKw)}
          hint={peakPct != null ? `${formatPercent(peakPct)} of capacity` : undefined}
          tone={peakPct != null && peakPct >= 100 ? 'bad' : peakPct != null && peakPct >= OVERLOAD_THRESHOLD_RATIO * 100 ? 'warn' : undefined}
        />
        <Stat
          label="Lowest voltage"
          value={minVoltage != null ? `${minVoltage.toFixed(1)} V` : '—'}
          hint={`threshold ${VOLTAGE_DROP_THRESHOLD_V} V`}
          tone={minVoltage != null && minVoltage < VOLTAGE_DROP_THRESHOLD_V ? 'bad' : undefined}
        />
        <Stat label="Energy delivered" value={energy != null ? `${energy.toLocaleString('en-US', { maximumFractionDigits: 0 })} kWh` : '—'} />
        <Stat label="Alerts in period" value={String(alerts.length)} tone={alerts.length > 0 ? 'warn' : undefined} />
      </div>

      <LoadProfileChart
        points={profileQuery.data}
        capacityKw={t?.capacityKw}
        alerts={alerts}
        isLoading={profileQuery.isLoading || detailQuery.isLoading}
        isError={profileQuery.isError}
        error={profileQuery.error}
        onRetry={() => profileQuery.refetch()}
      />

      {/* Alerts for this transformer */}
      <div>
        <h3 className="mb-2 text-xs font-medium uppercase tracking-wider text-gray-500">Alerts in period</h3>
        {alertsQuery.isLoading ? (
          <Skeleton className="h-16 w-full" />
        ) : alerts.length === 0 ? (
          <EmptyState title="No alerts for this transformer" description="No theft, overload or voltage drop was detected in the selected period." className="p-4" />
        ) : (
          <ul className="divide-y divide-grid-border/60 rounded-lg border border-grid-border">
            {alerts.map((a) => (
              <li key={a.anomalyId} className="flex flex-wrap items-center gap-3 px-3 py-2 text-xs">
                <EventTypeBadge type={a.eventType} />
                <span className="font-mono text-gray-300">
                  {formatShortTimestamp(a.startTimestamp)} → {formatShortTimestamp(a.endTimestamp)}
                </span>
                <span className="text-gray-500">{formatDuration(a.startTimestamp, a.endTimestamp)}</span>
                <span className="ml-auto font-mono text-[10px] text-gray-600">{a.anomalyId}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
