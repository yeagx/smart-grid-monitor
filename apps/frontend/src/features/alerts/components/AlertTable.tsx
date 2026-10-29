import { Link } from 'react-router-dom';
import { MapPin } from 'lucide-react';
import { EventTypeBadge } from '@/components/ui';
import { formatDuration, formatShortTimestamp } from '@/lib/formatters';
import type { Alert } from '../types';

interface AlertTableProps {
  alerts: Alert[];
  from?: string;
  to?: string;
}

/** Alert rows, newest first. Transformer IDs link to the transformer page. */
export function AlertTable({ alerts, from, to }: AlertTableProps) {
  const range = from && to ? `&from=${from}&to=${to}` : '';

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-grid-border text-left text-[11px] uppercase tracking-wider text-gray-500">
            <th scope="col" className="px-4 py-2.5 font-medium">Type</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Transformer</th>
            <th scope="col" className="px-4 py-2.5 font-medium">District</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Start (UTC)</th>
            <th scope="col" className="px-4 py-2.5 font-medium">End (UTC)</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Duration</th>
            <th scope="col" className="px-4 py-2.5 font-medium">Alert ID</th>
            <th scope="col" className="px-4 py-2.5 font-medium"><span className="sr-only">Map</span></th>
          </tr>
        </thead>
        <tbody>
          {alerts.map((a) => (
            <tr key={a.anomalyId} className="border-b border-grid-border/60 hover:bg-grid-surface/60 transition-colors">
              <td className="px-4 py-2.5 whitespace-nowrap"><EventTypeBadge type={a.eventType} /></td>
              <td className="px-4 py-2.5 whitespace-nowrap">
                <Link
                  to={`/transformers?id=${encodeURIComponent(a.transformerId)}${range}`}
                  className="font-mono text-sky-400 hover:text-sky-300 hover:underline"
                >
                  {a.transformerId}
                </Link>
                {a.entityId && a.entityId !== a.transformerId && (
                  <span className="block text-[10px] text-gray-500 font-mono">{a.entityId}</span>
                )}
              </td>
              <td className="px-4 py-2.5 whitespace-nowrap text-gray-300">
                {a.district}
                <span className="text-gray-500"> · {a.governorate}</span>
              </td>
              <td className="px-4 py-2.5 whitespace-nowrap font-mono text-gray-300">{formatShortTimestamp(a.startTimestamp)}</td>
              <td className="px-4 py-2.5 whitespace-nowrap font-mono text-gray-300">{formatShortTimestamp(a.endTimestamp)}</td>
              <td className="px-4 py-2.5 whitespace-nowrap text-gray-400">{formatDuration(a.startTimestamp, a.endTimestamp)}</td>
              <td className="px-4 py-2.5 whitespace-nowrap font-mono text-[11px] text-gray-500">{a.anomalyId}</td>
              <td className="px-4 py-2.5 text-right">
                <Link
                  to={`/alerts/map?district=${encodeURIComponent(a.district)}${range}`}
                  className="inline-flex text-gray-500 hover:text-gray-200"
                  title="Show district on the map"
                  aria-label={`Show ${a.district} on the map`}
                >
                  <MapPin className="h-4 w-4" aria-hidden="true" />
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
