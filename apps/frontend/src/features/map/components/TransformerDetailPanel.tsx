import { X, ExternalLink, ShieldAlert, AlertTriangle, Activity, Zap, Clock } from 'lucide-react';
import { Link } from 'react-router-dom';
import { formatPowerMw, formatInt, formatDate, formatTimestamp } from '@/lib/formatters';
import type { MapMarker } from '../types';

interface TransformerDetailPanelProps {
  marker: MapMarker;
  dateFrom: string;
  dateTo: string;
  onClose: () => void;
}

export function TransformerDetailPanel({
  marker,
  dateFrom,
  dateTo,
  onClose,
}: TransformerDetailPanelProps) {
  // Format peak metrics
  const peakKwDisplay = marker.peakKwRecorded != null
    ? `${marker.peakKwRecorded.toLocaleString()} kW (${formatPowerMw(marker.peakKwRecorded)})`
    : '—';

  const peakLoadDisplay = marker.peakLoadPercentage != null
    ? `${marker.peakLoadPercentage.toFixed(1)}%`
    : '—';

  const latestAlertDisplay = marker.latestAlertAt
    ? formatTimestamp(marker.latestAlertAt)
    : 'None';

  return (
    <div className="card p-5 space-y-4">
      {/* Header */}
      <div className="flex items-start justify-between border-b border-grid-border pb-3">
        <div>
          <div className="flex items-center gap-2">
            <span className="text-base font-semibold text-gray-100 font-mono">
              {marker.transformerId}
            </span>
            <span className="rounded bg-grid-surface px-2 py-0.5 text-xs text-gray-400">
              {marker.district}, {marker.governorate}
            </span>
          </div>
          <p className="text-xs text-gray-500 mt-0.5">
            Feeder: <span className="text-gray-300 font-mono">{marker.feederId}</span> • Substation:{' '}
            <span className="text-gray-300 font-mono">{marker.substationId}</span>
          </p>
        </div>

        <button
          type="button"
          onClick={onClose}
          className="rounded p-1 text-gray-400 hover:text-gray-200 hover:bg-grid-surface"
          aria-label="Close detail panel"
        >
          <X className="h-4 w-4" aria-hidden="true" />
        </button>
      </div>

      {/* Grid Specs & Historical Peak Metrics */}
      <div className="grid grid-cols-2 gap-3 text-xs">
        <div className="rounded-lg border border-grid-border bg-grid-surface/50 p-2.5">
          <span className="text-gray-500 uppercase tracking-wider text-[10px]">Nameplate Capacity</span>
          <p className="mt-0.5 font-semibold text-gray-200 font-mono">
            {marker.capacityKw.toLocaleString()} kW
          </p>
        </div>

        <div className="rounded-lg border border-grid-border bg-grid-surface/50 p-2.5">
          <span className="text-gray-500 uppercase tracking-wider text-[10px]" title="Historical peak recorded load">
            Historical Peak Load
          </span>
          <p className="mt-0.5 font-semibold text-gray-200 font-mono">
            {peakLoadDisplay}
          </p>
          <span className="text-[10px] text-gray-500">{peakKwDisplay}</span>
        </div>
      </div>

      <div className="rounded border border-amber-500/20 bg-amber-500/5 px-2.5 py-1.5 text-[11px] text-amber-400/90 flex items-center gap-1.5">
        <Clock className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
        <span>Peak metrics represent historical recorded maxima over available readings, not real-time load.</span>
      </div>

      {/* Period Alert Profile */}
      <div className="space-y-2">
        <div className="flex items-center justify-between text-xs">
          <span className="font-medium text-gray-300">Period Alerts ({formatDate(dateFrom)} – {formatDate(dateTo)})</span>
          <span className="font-mono font-semibold text-gray-200">
            {formatInt(marker.totalAlertCount)} alert window{marker.totalAlertCount !== 1 ? 's' : ''}
          </span>
        </div>

        <div className="grid grid-cols-3 gap-2 text-center text-xs">
          <div className="rounded-lg border border-status-theft/30 bg-status-theft/10 p-2">
            <div className="flex items-center justify-center gap-1 text-status-theft text-[11px] font-medium">
              <ShieldAlert className="h-3 w-3" aria-hidden="true" />
              <span>Theft</span>
            </div>
            <p className="mt-1 text-base font-semibold text-status-theft font-mono">
              {formatInt(marker.theftAlertCount)}
            </p>
          </div>

          <div className="rounded-lg border border-status-overload/30 bg-status-overload/10 p-2">
            <div className="flex items-center justify-center gap-1 text-status-overload text-[11px] font-medium">
              <AlertTriangle className="h-3 w-3" aria-hidden="true" />
              <span>Overload</span>
            </div>
            <p className="mt-1 text-base font-semibold text-status-overload font-mono">
              {formatInt(marker.overloadAlertCount)}
            </p>
          </div>

          <div className="rounded-lg border border-status-linedrop/30 bg-status-linedrop/10 p-2">
            <div className="flex items-center justify-center gap-1 text-status-linedrop text-[11px] font-medium">
              <Activity className="h-3 w-3" aria-hidden="true" />
              <span>Voltage Drop</span>
            </div>
            <p className="mt-1 text-base font-semibold text-status-linedrop font-mono">
              {formatInt(marker.lineDropAlertCount)}
            </p>
          </div>
        </div>

        {marker.latestAlertAt && (
          <p className="text-[11px] text-gray-400">
            Latest alert: <span className="text-gray-200">{latestAlertDisplay}</span>
          </p>
        )}
      </div>

      {/* Navigation Links */}
      <div className="flex items-center gap-2 border-t border-grid-border pt-3">
        <Link
          to={`/transformers?id=${encodeURIComponent(marker.transformerId)}`}
          className="btn-ghost flex-1 text-xs justify-center"
        >
          <Zap className="h-3.5 w-3.5" aria-hidden="true" />
          <span>View Transformer</span>
        </Link>

        <Link
          to={`/alert-log?transformerId=${encodeURIComponent(marker.transformerId)}&from=${dateFrom}&to=${dateTo}`}
          className="btn-primary flex-1 text-xs justify-center"
        >
          <ExternalLink className="h-3.5 w-3.5" aria-hidden="true" />
          <span>View Alerts</span>
        </Link>
      </div>
    </div>
  );
}
