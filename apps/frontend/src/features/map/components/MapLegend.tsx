import { ShieldAlert, AlertTriangle, Activity, CheckCircle2, Info } from 'lucide-react';

export function MapLegend() {
  return (
    <div className="card p-3 space-y-2 text-xs">
      <div className="flex items-center gap-1.5 font-medium text-gray-300">
        <Info className="h-3.5 w-3.5 text-gray-400" aria-hidden="true" />
        <span>Map Legend</span>
      </div>

      <div className="flex flex-wrap items-center gap-4 text-gray-400">
        <div className="flex items-center gap-1.5">
          <span className="flex h-3 w-3 rounded-full bg-status-theft ring-2 ring-status-theft/30" />
          <ShieldAlert className="h-3.5 w-3.5 text-status-theft" aria-hidden="true" />
          <span className="text-gray-200">Theft Alert</span>
        </div>

        <div className="flex items-center gap-1.5">
          <span className="flex h-3 w-3 rounded-full bg-status-overload ring-2 ring-status-overload/30" />
          <AlertTriangle className="h-3.5 w-3.5 text-status-overload" aria-hidden="true" />
          <span className="text-gray-200">Overload Alert</span>
        </div>

        <div className="flex items-center gap-1.5">
          <span className="flex h-3 w-3 rounded-full bg-status-linedrop ring-2 ring-status-linedrop/30" />
          <Activity className="h-3.5 w-3.5 text-status-linedrop" aria-hidden="true" />
          <span className="text-gray-200">Voltage Drop</span>
        </div>

        <div className="flex items-center gap-1.5">
          <span className="flex h-3 w-3 rounded-full bg-status-healthy ring-2 ring-status-healthy/30" />
          <CheckCircle2 className="h-3.5 w-3.5 text-status-healthy" aria-hidden="true" />
          <span className="text-gray-200">No Detected Alerts</span>
        </div>
      </div>

      <p className="text-[11px] text-gray-500 border-t border-grid-border pt-1.5">
        Marker badges show total detected alert windows within the selected date range.
      </p>
    </div>
  );
}
