import {
  Zap,
  TrendingUp,
  ShieldAlert,
  AlertTriangle,
  Activity,
  Users,
} from 'lucide-react';
import { KpiCard } from './KpiCard';
import { ErrorCard } from '@/components/ui';
import { formatEnergyGwh, formatPowerMw, formatInt } from '@/lib/formatters';
import type { DashboardOverview } from '../types';

interface KpiGridProps {
  data: DashboardOverview | undefined;
  isLoading: boolean;
  isError: boolean;
  error: Error | null;
  onRetry: () => void;
}

/**
 * Grid of KPI cards rendered from the dashboard summary response.
 *
 * Displays: Total Energy, Peak Power, Theft, Overload, Line Drop, Total Affected.
 * Handles loading, error, and null/empty data states independently.
 */
export function KpiGrid({ data, isLoading, isError, error, onRetry }: KpiGridProps) {
  if (isError) {
    return (
      <ErrorCard
        title="Failed to load summary"
        message={error?.message ?? 'An unexpected error occurred.'}
        onRetry={onRetry}
      />
    );
  }

  const totalAlertsSubtitle = data?.totalAlerts != null
    ? `${formatInt(data.totalAlerts)} alert window${data.totalAlerts !== 1 ? 's' : ''} in period`
    : undefined;

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-6">
      <KpiCard
        label="Total Grid Demand"
        value={formatEnergyGwh(data?.totalEnergyKwh)}
        icon={<Zap className="h-5 w-5" aria-hidden="true" />}
        accentClass="text-blue-400"
        isLoading={isLoading}
      />
      <KpiCard
        label="Network Peak"
        value={formatPowerMw(data?.peakNetworkKw)}
        icon={<TrendingUp className="h-5 w-5" aria-hidden="true" />}
        subtitle="Max combined power"
        accentClass="text-purple-400"
        isLoading={isLoading}
      />
      <KpiCard
        label="Theft-Affected"
        value={formatInt(data?.theftTransformers)}
        icon={<ShieldAlert className="h-5 w-5" aria-hidden="true" />}
        subtitle="Transformers"
        accentClass="text-status-theft"
        isLoading={isLoading}
      />
      <KpiCard
        label="Overload-Affected"
        value={formatInt(data?.overloadTransformers)}
        icon={<AlertTriangle className="h-5 w-5" aria-hidden="true" />}
        subtitle="Transformers"
        accentClass="text-status-overload"
        isLoading={isLoading}
      />
      <KpiCard
        label="Voltage Drop"
        value={formatInt(data?.linedropTransformers)}
        icon={<Activity className="h-5 w-5" aria-hidden="true" />}
        subtitle="Transformers"
        accentClass="text-status-linedrop"
        isLoading={isLoading}
      />
      <KpiCard
        label="Total Affected"
        value={formatInt(data?.affectedTransformers)}
        icon={<Users className="h-5 w-5" aria-hidden="true" />}
        subtitle={totalAlertsSubtitle}
        accentClass="text-gray-300"
        isLoading={isLoading}
      />
    </div>
  );
}
