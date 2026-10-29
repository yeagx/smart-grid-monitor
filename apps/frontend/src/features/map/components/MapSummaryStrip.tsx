import { Cpu, AlertTriangle, ShieldAlert, Activity, Bell } from 'lucide-react';
import { formatInt } from '@/lib/formatters';
import type { MapMarker, ThreatType } from '../types';

interface MapSummaryStripProps {
  markers: MapMarker[] | undefined;
  activeTypeFilter?: ThreatType | '';
}

export function MapSummaryStrip({ markers, activeTypeFilter }: MapSummaryStripProps) {
  if (!markers) return null;

  const totalTransformers = markers.length;
  const affectedTransformers = markers.filter((m) => m.totalAlertCount > 0).length;
  const totalAlertWindows = markers.reduce((sum, m) => sum + m.totalAlertCount, 0);

  const theftTransformers = markers.filter((m) => m.theftAlertCount > 0).length;
  const overloadTransformers = markers.filter((m) => m.overloadAlertCount > 0).length;
  const lineDropTransformers = markers.filter((m) => m.lineDropAlertCount > 0).length;

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
      {/* Transformers Displayed */}
      <div className="card p-3">
        <div className="flex items-center gap-2 text-gray-400">
          <Cpu className="h-4 w-4 text-blue-400" aria-hidden="true" />
          <span className="text-[11px] uppercase tracking-wider font-medium">Displayed</span>
        </div>
        <p className="mt-1 text-lg font-semibold text-gray-100 font-mono">
          {formatInt(totalTransformers)}
        </p>
        <span className="text-[10px] text-gray-500">transformers</span>
      </div>

      {/* Transformers With Alerts */}
      <div className="card p-3">
        <div className="flex items-center gap-2 text-gray-400">
          <AlertTriangle className="h-4 w-4 text-amber-400" aria-hidden="true" />
          <span className="text-[11px] uppercase tracking-wider font-medium">With Alerts</span>
        </div>
        <p className="mt-1 text-lg font-semibold text-gray-100 font-mono">
          {formatInt(affectedTransformers)}
        </p>
        <span className="text-[10px] text-gray-500">transformers</span>
      </div>

      {/* Total Alert Windows */}
      <div className="card p-3">
        <div className="flex items-center gap-2 text-gray-400">
          <Bell className="h-4 w-4 text-purple-400" aria-hidden="true" />
          <span className="text-[11px] uppercase tracking-wider font-medium">Alert Windows</span>
        </div>
        <p className="mt-1 text-lg font-semibold text-gray-100 font-mono">
          {formatInt(totalAlertWindows)}
        </p>
        <span className="text-[10px] text-gray-500">detected events</span>
      </div>

      {/* Theft Affected */}
      <div className={`card p-3 ${activeTypeFilter === 'theft' ? 'ring-1 ring-status-theft/50 bg-status-theft/5' : ''}`}>
        <div className="flex items-center gap-2 text-gray-400">
          <ShieldAlert className="h-4 w-4 text-status-theft" aria-hidden="true" />
          <span className="text-[11px] uppercase tracking-wider font-medium">Theft</span>
        </div>
        <p className="mt-1 text-lg font-semibold text-status-theft font-mono">
          {formatInt(theftTransformers)}
        </p>
        <span className="text-[10px] text-gray-500">transformers</span>
      </div>

      {/* Overload Affected */}
      <div className={`card p-3 ${activeTypeFilter === 'overload' ? 'ring-1 ring-status-overload/50 bg-status-overload/5' : ''}`}>
        <div className="flex items-center gap-2 text-gray-400">
          <AlertTriangle className="h-4 w-4 text-status-overload" aria-hidden="true" />
          <span className="text-[11px] uppercase tracking-wider font-medium">Overload</span>
        </div>
        <p className="mt-1 text-lg font-semibold text-status-overload font-mono">
          {formatInt(overloadTransformers)}
        </p>
        <span className="text-[10px] text-gray-500">transformers</span>
      </div>

      {/* Voltage Drop Affected */}
      <div className={`card p-3 ${activeTypeFilter === 'line_drop' ? 'ring-1 ring-status-linedrop/50 bg-status-linedrop/5' : ''}`}>
        <div className="flex items-center gap-2 text-gray-400">
          <Activity className="h-4 w-4 text-status-linedrop" aria-hidden="true" />
          <span className="text-[11px] uppercase tracking-wider font-medium">Voltage Drop</span>
        </div>
        <p className="mt-1 text-lg font-semibold text-status-linedrop font-mono">
          {formatInt(lineDropTransformers)}
        </p>
        <span className="text-[10px] text-gray-500">transformers</span>
      </div>
    </div>
  );
}
