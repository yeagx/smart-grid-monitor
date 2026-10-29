import {
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ReferenceArea,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Activity } from 'lucide-react';
import { EmptyState, ErrorCard, Skeleton } from '@/components/ui';
import { EVENT_TYPES, isEventType } from '@/lib/event-types';
import { formatHourLabel } from '@/lib/formatters';
import type { Alert } from '@/features/alerts/types';
import {
  OVERLOAD_THRESHOLD_RATIO,
  VOLTAGE_DROP_THRESHOLD_V,
  type TransformerLoadPoint,
} from '../types';

interface LoadProfileChartProps {
  points: TransformerLoadPoint[] | undefined;
  capacityKw: number | undefined;
  alerts: Alert[];
  isLoading: boolean;
  isError: boolean;
  error: Error | null;
  onRetry: () => void;
}

interface ChartPoint {
  hour: string;
  label: string;
  avgKw: number;
  maxKw: number;
  avgVoltage: number;
  minVoltage: number;
  energyKwh: number;
}

const AXIS = { fill: '#9CA3AF', fontSize: 11 };

/** Rounds an ISO timestamp down to its hour, matching the API's hourly buckets. */
function hourKey(ts: string): string {
  const d = new Date(ts);
  d.setUTCMinutes(0, 0, 0);
  return d.toISOString();
}

function normalise(ts: string): string {
  const d = new Date(ts);
  return Number.isNaN(d.getTime()) ? ts : d.toISOString();
}

function ChartTooltip({ active, payload }: { active?: boolean; payload?: Array<{ payload: ChartPoint }> }) {
  if (!active || !payload?.length) return null;
  const p = payload[0].payload;
  return (
    <div className="card p-3 text-xs shadow-lg border-grid-border-light space-y-1">
      <p className="font-medium text-gray-200">{p.label} UTC</p>
      <p className="text-gray-400">Avg load <span className="font-mono text-gray-200">{p.avgKw.toFixed(1)} kW</span></p>
      <p className="text-gray-400">Max load <span className="font-mono text-gray-200">{p.maxKw.toFixed(1)} kW</span></p>
      <p className="text-gray-400">Energy <span className="font-mono text-gray-200">{p.energyKwh.toFixed(1)} kWh</span></p>
      <p className="text-gray-400">Avg voltage <span className="font-mono text-gray-200">{p.avgVoltage.toFixed(1)} V</span></p>
      <p className="text-gray-400">Min voltage <span className="font-mono text-gray-200">{p.minVoltage.toFixed(1)} V</span></p>
    </div>
  );
}

/**
 * Hourly load (kW) and voltage (V) for one transformer.
 * - Dashed lines: capacity, 85% overload threshold, 207 V voltage-drop threshold.
 * - Shaded bands: this transformer's alert windows, coloured by type.
 */
export function LoadProfileChart({ points, capacityKw, alerts, isLoading, isError, error, onRetry }: LoadProfileChartProps) {
  if (isError) {
    return (
      <ErrorCard title="Failed to load readings" message={error?.message ?? 'Unexpected error.'} onRetry={onRetry} />
    );
  }

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
        <Skeleton className="h-64 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!points || points.length === 0) {
    return (
      <EmptyState
        icon={<Activity className="h-8 w-8" />}
        title="No readings in this period"
        description="The warehouse has no transformer readings for the selected dates."
        className="h-48"
      />
    );
  }

  const data: ChartPoint[] = points.map((p) => ({
    hour: normalise(p.hourTimestamp),
    label: formatHourLabel(p.hourTimestamp),
    avgKw: p.avgKw,
    maxKw: p.maxKw,
    avgVoltage: p.avgVoltage,
    minVoltage: p.minVoltage,
    energyKwh: p.energyKwh,
  }));

  const hours = new Set(data.map((d) => d.hour));
  const first = data[0].hour;
  const last = data[data.length - 1].hour;

  // Alert windows → shaded bands on the hourly axis (clamped to the plotted range).
  const bands = alerts
    .map((a) => {
      let x1 = hourKey(a.startTimestamp);
      let x2 = hourKey(a.endTimestamp);
      if (x2 < first || x1 > last) return null;
      if (x1 < first) x1 = first;
      if (x2 > last) x2 = last;
      if (!hours.has(x1) || !hours.has(x2)) return null;
      const color = isEventType(a.eventType) ? EVENT_TYPES[a.eventType].color : '#6B7280';
      return { id: a.anomalyId, x1, x2, color, type: a.eventType };
    })
    .filter((b): b is NonNullable<typeof b> => b !== null);

  const overloadKw = capacityKw ? capacityKw * OVERLOAD_THRESHOLD_RATIO : undefined;
  const maxKw = Math.max(...data.map((d) => d.maxKw), capacityKw ?? 0);
  const minV = Math.min(...data.map((d) => d.minVoltage), VOLTAGE_DROP_THRESHOLD_V);
  const maxV = Math.max(...data.map((d) => d.avgVoltage));

  const xAxis = (
    <XAxis
      dataKey="hour"
      tickFormatter={(v: string) => formatHourLabel(v).replace(/^(\w+ \d+) 00:00$/, '$1')}
      tick={AXIS}
      tickLine={false}
      axisLine={{ stroke: '#1E293B' }}
      minTickGap={28}
    />
  );

  const bandElements = (yAxisId: string) =>
    bands.map((b) => (
      <ReferenceArea
        key={`${yAxisId}-${b.id}`}
        yAxisId={yAxisId}
        x1={b.x1}
        x2={b.x2}
        fill={b.color}
        fillOpacity={0.15}
        stroke={b.color}
        strokeOpacity={0.4}
        ifOverflow="extendDomain"
      />
    ));

  return (
    <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
      <div>
        <h3 className="mb-2 text-xs font-medium uppercase tracking-wider text-gray-500">Load (kW, hourly)</h3>
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <ComposedChart data={data} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1E293B" vertical={false} />
              {xAxis}
              <YAxis yAxisId="kw" tick={AXIS} tickLine={false} axisLine={false} domain={[0, Math.ceil(maxKw * 1.1)]} width={48} />
              <Tooltip content={<ChartTooltip />} />
              <Legend wrapperStyle={{ fontSize: 11, color: '#9CA3AF' }} />
              {bandElements('kw')}
              {capacityKw ? (
                <ReferenceLine yAxisId="kw" y={capacityKw} stroke="#EF4444" strokeDasharray="6 4"
                  label={{ value: 'Capacity', position: 'insideTopRight', fill: '#EF4444', fontSize: 10 }} />
              ) : null}
              {overloadKw ? (
                <ReferenceLine yAxisId="kw" y={overloadKw} stroke="#F59E0B" strokeDasharray="4 4"
                  label={{ value: '85%', position: 'insideBottomLeft', fill: '#F59E0B', fontSize: 10 }} />
              ) : null}
              <Line yAxisId="kw" type="monotone" dataKey="avgKw" name="Avg kW" stroke="#60A5FA" strokeWidth={2} dot={false} />
              <Line yAxisId="kw" type="monotone" dataKey="maxKw" name="Max kW" stroke="#F59E0B" strokeWidth={1.5} dot={false} strokeOpacity={0.8} />
            </ComposedChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div>
        <h3 className="mb-2 text-xs font-medium uppercase tracking-wider text-gray-500">Voltage (V, hourly)</h3>
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <ComposedChart data={data} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1E293B" vertical={false} />
              {xAxis}
              <YAxis yAxisId="v" tick={AXIS} tickLine={false} axisLine={false}
                domain={[Math.floor(minV - 5), Math.ceil(maxV + 5)]} width={48} />
              <Tooltip content={<ChartTooltip />} />
              <Legend wrapperStyle={{ fontSize: 11, color: '#9CA3AF' }} />
              {bandElements('v')}
              <ReferenceLine yAxisId="v" y={VOLTAGE_DROP_THRESHOLD_V} stroke={EVENT_TYPES.line_drop.color} strokeDasharray="6 4"
                label={{ value: '207 V', position: 'insideBottomRight', fill: EVENT_TYPES.line_drop.color, fontSize: 10 }} />
              <Line yAxisId="v" type="monotone" dataKey="avgVoltage" name="Avg V" stroke="#A78BFA" strokeWidth={2} dot={false} />
              <Line yAxisId="v" type="monotone" dataKey="minVoltage" name="Min V" stroke={EVENT_TYPES.line_drop.color} strokeWidth={1.5} dot={false} strokeOpacity={0.8} />
            </ComposedChart>
          </ResponsiveContainer>
        </div>
      </div>

      {bands.length > 0 && (
        <p className="xl:col-span-2 text-[11px] text-gray-500">
          Shaded bands are this transformer's alert windows in the period
          ({Array.from(new Set(bands.map((b) => b.type))).map((t) => (isEventType(t) ? EVENT_TYPES[t].label : t)).join(', ')}).
        </p>
      )}
    </div>
  );
}
