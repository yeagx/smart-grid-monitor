import { Link } from 'react-router-dom';
import { EventTypeBadge, StatusBadge } from '@/components/ui';
import { formatShortTimestamp } from '@/lib/formatters';
import type { AccuracyDetail } from '../types';

function statusVariant(status: string) {
  if (status === 'True Positive') return 'healthy' as const;
  if (status === 'False Positive') return 'overload' as const;
  if (status === 'False Negative') return 'theft' as const;
  return 'neutral' as const;
}

function Window({ start, end }: { start: string | null; end: string | null }) {
  if (!start) return <span className="text-gray-600">—</span>;
  return (
    <span className="font-mono text-gray-300">
      {formatShortTimestamp(start)} → {formatShortTimestamp(end)}
    </span>
  );
}

/** Truth window vs detected window for each injected or detected anomaly. */
export function AccuracyDetailsTable({ rows }: { rows: AccuracyDetail[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-grid-border text-left text-[11px] uppercase tracking-wider text-gray-500">
            <th scope="col" className="px-4 py-2.5 font-medium">Outcome</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Type</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Transformer</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Injected (truth, UTC)</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Detected (UTC)</th>
            <th scope="col" className="px-4 py-2.5 font-medium text-right">Flagged rows</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r, i) => (
            <tr key={`${r.transformerId}-${r.eventType}-${r.truthStartTimestamp ?? r.detectedStartTimestamp ?? i}`} className="border-b border-grid-border/60 hover:bg-grid-surface/60">
              <td className="px-4 py-2.5 whitespace-nowrap"><StatusBadge variant={statusVariant(r.detectionStatus)}>{r.detectionStatus}</StatusBadge></td>
              <td className="px-4 py-2.5 whitespace-nowrap"><EventTypeBadge type={r.eventType} /></td>
              <td className="px-4 py-2.5 whitespace-nowrap">
                <Link to={`/transformers?id=${encodeURIComponent(r.transformerId)}`} className="font-mono text-sky-400 hover:text-sky-300 hover:underline">
                  {r.transformerId}
                </Link>
              </td>
              <td className="px-4 py-2.5 whitespace-nowrap text-xs"><Window start={r.truthStartTimestamp} end={r.truthEndTimestamp} /></td>
              <td className="px-4 py-2.5 whitespace-nowrap text-xs"><Window start={r.detectedStartTimestamp} end={r.detectedEndTimestamp} /></td>
              <td className="px-4 py-2.5 whitespace-nowrap text-right font-mono text-gray-400">{r.flagRows ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
