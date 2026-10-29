import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { EVENT_TYPES, eventTypeLabel, isEventType } from '@/lib/event-types';
import { formatInt, formatRatio } from '@/lib/formatters';
import { f1Score, type AccuracyReport } from '../types';

const AXIS = { fill: '#9CA3AF', fontSize: 11 };
const TP = '#10B981';
const FP = '#F59E0B';
const FN = '#EF4444';

function Meter({ label, value, color }: { label: string; value: number | null; color: string }) {
  return (
    <div>
      <div className="flex items-center justify-between text-[11px]">
        <span className="text-gray-500">{label}</span>
        <span className="font-mono text-gray-200">{formatRatio(value)}</span>
      </div>
      <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-grid-border" aria-hidden="true">
        <div className="h-full rounded-full" style={{ width: `${Math.round((value ?? 0) * 100)}%`, backgroundColor: color }} />
      </div>
    </div>
  );
}

function Kpi({ label, value, hint, color }: { label: string; value: string; hint: string; color?: string }) {
  return (
    <div className="card p-4">
      <span className="text-xs font-medium uppercase tracking-wider text-gray-500">{label}</span>
      <p className="mt-1 text-2xl font-semibold font-mono" style={{ color: color ?? '#F3F4F6' }}>{value}</p>
      <p className="text-[11px] text-gray-500">{hint}</p>
    </div>
  );
}

/** Overall KPIs, per-type precision/recall cards and a TP/FP/FN bar chart. */
export function AccuracyOverview({ report }: { report: AccuracyReport }) {
  const overallF1 = f1Score(report.overallPrecision, report.overallRecall);
  const chartData = report.byType.map((t) => ({
    type: eventTypeLabel(t.eventType),
    'True positive': t.truePositives,
    'False positive': t.falsePositives,
    'False negative': t.falseNegatives,
  }));

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kpi label="Precision" value={formatRatio(report.overallPrecision)} hint="Of the alerts raised, how many were real" color="#60A5FA" />
        <Kpi label="Recall" value={formatRatio(report.overallRecall)} hint="Of the real anomalies, how many were caught" color="#A78BFA" />
        <Kpi label="F1 score" value={formatRatio(overallF1)} hint="Balance of precision and recall" color={TP} />
        <Kpi
          label="Detections"
          value={formatInt(report.totalTruePositives + report.totalFalsePositives)}
          hint={`${formatInt(report.totalTruePositives)} TP · ${formatInt(report.totalFalsePositives)} FP · ${formatInt(report.totalFalseNegatives)} missed`}
        />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-5">
        <div className="grid grid-cols-1 gap-3 self-start sm:grid-cols-3 xl:col-span-3">
          {report.byType.map((t) => {
            const meta = isEventType(t.eventType) ? EVENT_TYPES[t.eventType] : null;
            const color = meta?.color ?? '#9CA3AF';
            return (
              <div key={t.eventType} className="card p-4 space-y-3" style={{ borderTop: `2px solid ${color}` }}>
                <div className="flex items-center justify-between">
                  <span className="text-sm font-semibold" style={{ color }}>{eventTypeLabel(t.eventType)}</span>
                  <span className="text-[11px] font-mono text-gray-500">F1 {formatRatio(f1Score(t.precision, t.recall))}</span>
                </div>
                <Meter label="Precision" value={t.precision} color="#60A5FA" />
                <Meter label="Recall" value={t.recall} color="#A78BFA" />
                <div className="grid grid-cols-3 gap-1 text-center text-[11px]">
                  <div className="rounded bg-grid-surface/60 py-1"><p className="font-mono text-sm" style={{ color: TP }}>{t.truePositives}</p><p className="text-gray-500">TP</p></div>
                  <div className="rounded bg-grid-surface/60 py-1"><p className="font-mono text-sm" style={{ color: FP }}>{t.falsePositives}</p><p className="text-gray-500">FP</p></div>
                  <div className="rounded bg-grid-surface/60 py-1"><p className="font-mono text-sm" style={{ color: FN }}>{t.falseNegatives}</p><p className="text-gray-500">FN</p></div>
                </div>
              </div>
            );
          })}
        </div>

        <div className="card p-4 xl:col-span-2">
          <h2 className="mb-2 text-sm font-semibold text-gray-200">Outcomes by type</h2>
          <div className="h-56">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData} margin={{ top: 4, right: 8, left: -16, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#1E293B" vertical={false} />
                <XAxis dataKey="type" tick={AXIS} tickLine={false} axisLine={{ stroke: '#1E293B' }} />
                <YAxis tick={AXIS} tickLine={false} axisLine={false} allowDecimals={false} />
                <Tooltip
                  cursor={{ fill: 'rgba(255,255,255,0.04)' }}
                  contentStyle={{ background: '#1A2035', border: '1px solid #334155', borderRadius: 8, fontSize: 12 }}
                  labelStyle={{ color: '#E5E7EB' }}
                />
                <Legend wrapperStyle={{ fontSize: 11, color: '#9CA3AF' }} />
                <Bar dataKey="True positive" fill={TP} radius={[3, 3, 0, 0]} />
                <Bar dataKey="False positive" fill={FP} radius={[3, 3, 0, 0]} />
                <Bar dataKey="False negative" fill={FN} radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>
    </div>
  );
}
