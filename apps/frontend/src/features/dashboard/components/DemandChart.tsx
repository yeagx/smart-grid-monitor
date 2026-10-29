import {
  ResponsiveContainer,
  ComposedChart,
  Bar,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
  Legend,
} from 'recharts';
import { ErrorCard, EmptyState, Skeleton } from '@/components/ui';
import { toGwh, toMw } from '@/lib/formatters';
import { BarChart3 } from 'lucide-react';
import type { DailyDemandPoint } from '../types';

interface DemandChartProps {
  data: DailyDemandPoint[] | undefined;
  isLoading: boolean;
  isError: boolean;
  error: Error | null;
  onRetry: () => void;
}

interface ChartDataPoint {
  date: string;
  dateLabel: string;
  energyGwh: number;
  peakMw: number;
  rawEnergyKwh: number;
  rawPeakKw: number;
}

function formatDateLabel(dateStr: string): string {
  try {
    const d = new Date(dateStr + 'T00:00:00');
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  } catch {
    return dateStr;
  }
}

function CustomTooltip({ active, payload }: {
  active?: boolean;
  payload?: Array<{ payload: ChartDataPoint }>;
}) {
  if (!active || !payload?.length) return null;

  const point = payload[0].payload;

  return (
    <div className="card p-3 text-xs shadow-lg border-grid-border-light">
      <p className="font-medium text-gray-200 mb-2">{point.dateLabel} ({point.date})</p>
      <div className="space-y-1">
        <div className="flex items-center gap-2">
          <span className="h-2 w-2 rounded-full bg-blue-400" />
          <span className="text-gray-400">Energy:</span>
          <span className="font-mono text-gray-200">{point.energyGwh.toFixed(4)} GWh</span>
          <span className="text-gray-600">({point.rawEnergyKwh.toLocaleString()} kWh)</span>
        </div>
        <div className="flex items-center gap-2">
          <span className="h-2 w-2 rounded-full bg-amber-400" />
          <span className="text-gray-400">Peak:</span>
          <span className="font-mono text-gray-200">{point.peakMw.toFixed(2)} MW</span>
          <span className="text-gray-600">({point.rawPeakKw.toLocaleString()} kW)</span>
        </div>
      </div>
    </div>
  );
}

/**
 * Daily demand chart using Recharts.
 *
 * Displays energy (GWh) as bars on the left Y-axis and
 * peak power (MW) as a line on the right Y-axis.
 */
export function DemandChart({ data, isLoading, isError, error, onRetry }: DemandChartProps) {
  if (isError) {
    return (
      <div className="card p-6">
        <h2 className="text-sm font-semibold text-gray-200 mb-4">Daily Demand</h2>
        <ErrorCard
          title="Failed to load demand data"
          message={error?.message ?? 'An unexpected error occurred.'}
          onRetry={onRetry}
        />
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="card p-6">
        <h2 className="text-sm font-semibold text-gray-200 mb-4">Daily Demand</h2>
        <Skeleton className="h-72 w-full" />
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="card p-6">
        <h2 className="text-sm font-semibold text-gray-200 mb-4">Daily Demand</h2>
        <EmptyState
          icon={<BarChart3 className="h-8 w-8" />}
          title="No demand data"
          description="No daily demand records are available for the selected date range."
          className="h-72"
        />
      </div>
    );
  }

  const chartData: ChartDataPoint[] = data.map((d) => ({
    date: d.demandDate,
    dateLabel: formatDateLabel(d.demandDate),
    energyGwh: toGwh(d.totalDailyKwh),
    peakMw: toMw(d.peakNetworkKw),
    rawEnergyKwh: d.totalDailyKwh,
    rawPeakKw: d.peakNetworkKw,
  }));

  return (
    <div className="card p-6">
      <h2 className="text-sm font-semibold text-gray-200 mb-4">Daily Demand</h2>
      <div className="h-72 sm:h-80">
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={chartData} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#1E293B" vertical={false} />
            <XAxis
              dataKey="dateLabel"
              tick={{ fill: '#9CA3AF', fontSize: 11 }}
              tickLine={false}
              axisLine={{ stroke: '#1E293B' }}
            />
            <YAxis
              yAxisId="energy"
              tick={{ fill: '#9CA3AF', fontSize: 11 }}
              tickLine={false}
              axisLine={false}
              tickFormatter={(v: number) => `${v.toFixed(2)}`}
              label={{
                value: 'GWh',
                angle: -90,
                position: 'insideLeft',
                style: { fill: '#6B7280', fontSize: 10 },
              }}
            />
            <YAxis
              yAxisId="peak"
              orientation="right"
              tick={{ fill: '#9CA3AF', fontSize: 11 }}
              tickLine={false}
              axisLine={false}
              tickFormatter={(v: number) => `${v.toFixed(0)}`}
              label={{
                value: 'MW',
                angle: 90,
                position: 'insideRight',
                style: { fill: '#6B7280', fontSize: 10 },
              }}
            />
            <Tooltip content={<CustomTooltip />} />
            <Legend
              wrapperStyle={{ fontSize: 11, color: '#9CA3AF' }}
            />
            <Bar
              yAxisId="energy"
              dataKey="energyGwh"
              name="Energy (GWh)"
              fill="#60A5FA"
              radius={[3, 3, 0, 0]}
              maxBarSize={48}
            />
            <Line
              yAxisId="peak"
              dataKey="peakMw"
              name="Peak (MW)"
              type="monotone"
              stroke="#FBBF24"
              strokeWidth={2}
              dot={{ fill: '#FBBF24', strokeWidth: 0, r: 4 }}
              activeDot={{ r: 6, strokeWidth: 0 }}
            />
          </ComposedChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
