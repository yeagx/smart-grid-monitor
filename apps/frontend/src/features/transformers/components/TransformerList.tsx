import { cn } from '@/lib/utils';
import { formatInt, formatKw, formatPercent } from '@/lib/formatters';
import type { TransformerListItem } from '../types';

interface TransformerListProps {
  items: TransformerListItem[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}

/** Colour for the peak-load bar: green < 70%, amber 70–85%, red ≥ 85%. */
function loadColor(pct: number | null): string {
  if (pct == null) return '#4B5563';
  if (pct >= 85) return '#EF4444';
  if (pct >= 70) return '#F59E0B';
  return '#10B981';
}

export function TransformerList({ items, selectedId, onSelect }: TransformerListProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-grid-border text-left text-[11px] uppercase tracking-wider text-gray-500">
            <th scope="col" className="px-4 py-2.5 font-medium">Transformer</th>
            <th scope="col" className="px-4 py-2.5 font-medium">District</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Feeder / Substation</th>
            <th scope="col" className="px-4 py-2.5 font-medium text-right">Capacity</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Peak load</th>
            <th scope="col" className="px-4 py-2.5 font-medium text-right">Alerts</th>
          </tr>
        </thead>
        <tbody>
          {items.map((t) => {
            const selected = t.transformerId === selectedId;
            const pct = t.peakLoadPercentage;
            return (
              <tr
                key={t.transformerId}
                onClick={() => onSelect(t.transformerId)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    onSelect(t.transformerId);
                  }
                }}
                tabIndex={0}
                aria-selected={selected}
                className={cn(
                  'cursor-pointer border-b border-grid-border/60 transition-colors outline-none focus-visible:bg-grid-surface',
                  selected ? 'bg-brand/10' : 'hover:bg-grid-surface/60',
                )}
              >
                <td className="px-4 py-2.5 whitespace-nowrap font-mono text-gray-100">
                  <span className={cn('mr-2 inline-block h-1.5 w-1.5 rounded-full', selected ? 'bg-brand-light' : 'bg-transparent')} />
                  {t.transformerId}
                </td>
                <td className="px-4 py-2.5 whitespace-nowrap text-gray-300">
                  {t.district}
                  <span className="text-gray-500"> · {t.governorate}</span>
                </td>
                <td className="px-4 py-2.5 whitespace-nowrap font-mono text-xs text-gray-400">
                  {t.feederId} / {t.substationId}
                </td>
                <td className="px-4 py-2.5 whitespace-nowrap text-right font-mono text-gray-300">{formatKw(t.capacityKw)}</td>
                <td className="px-4 py-2.5 whitespace-nowrap">
                  <div className="flex items-center gap-2">
                    <div className="h-1.5 w-20 rounded-full bg-grid-border overflow-hidden" aria-hidden="true">
                      <div
                        className="h-full rounded-full"
                        style={{ width: `${Math.min(pct ?? 0, 100)}%`, backgroundColor: loadColor(pct) }}
                      />
                    </div>
                    <span className="font-mono text-xs text-gray-300">{formatPercent(pct)}</span>
                  </div>
                </td>
                <td className="px-4 py-2.5 whitespace-nowrap text-right">
                  <span className={cn('font-mono', t.alertCount > 0 ? 'text-status-overload font-semibold' : 'text-gray-500')}>
                    {formatInt(t.alertCount)}
                  </span>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
