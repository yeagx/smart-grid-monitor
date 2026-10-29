import { Filter, RotateCcw } from 'lucide-react';
import { DISTRICT_OPTIONS, THREAT_TYPE_OPTIONS, type ThreatType } from '../types';

interface MapFilterPanelProps {
  inputFrom: string;
  inputTo: string;
  inputDistrict: string;
  inputType: ThreatType | '';
  inputOnlyWithAlerts: boolean;
  dateError: string | null;
  onFromChange: (val: string) => void;
  onToChange: (val: string) => void;
  onDistrictChange: (val: string) => void;
  onTypeChange: (val: ThreatType | '') => void;
  onOnlyWithAlertsChange: (val: boolean) => void;
  onApply: () => void;
  onReset: () => void;
}

export function MapFilterPanel({
  inputFrom,
  inputTo,
  inputDistrict,
  inputType,
  inputOnlyWithAlerts,
  dateError,
  onFromChange,
  onToChange,
  onDistrictChange,
  onTypeChange,
  onOnlyWithAlertsChange,
  onApply,
  onReset,
}: MapFilterPanelProps) {
  return (
    <div className="card p-4 space-y-3">
      <div className="flex flex-wrap items-end gap-3">
        {/* From Date */}
        <div className="flex flex-col gap-1">
          <label htmlFor="map-date-from" className="text-xs font-medium text-gray-400 uppercase tracking-wider">
            From
          </label>
          <input
            id="map-date-from"
            type="date"
            value={inputFrom}
            onChange={(e) => onFromChange(e.target.value)}
            className="h-9 rounded-lg border border-grid-border bg-grid-panel px-3 text-sm text-gray-200 outline-none focus:border-brand focus:ring-1 focus:ring-brand transition-colors"
          />
        </div>

        {/* To Date */}
        <div className="flex flex-col gap-1">
          <label htmlFor="map-date-to" className="text-xs font-medium text-gray-400 uppercase tracking-wider">
            To
          </label>
          <input
            id="map-date-to"
            type="date"
            value={inputTo}
            onChange={(e) => onToChange(e.target.value)}
            className="h-9 rounded-lg border border-grid-border bg-grid-panel px-3 text-sm text-gray-200 outline-none focus:border-brand focus:ring-1 focus:ring-brand transition-colors"
          />
        </div>

        {/* District Selector */}
        <div className="flex flex-col gap-1">
          <label htmlFor="map-district" className="text-xs font-medium text-gray-400 uppercase tracking-wider">
            District
          </label>
          <select
            id="map-district"
            value={inputDistrict}
            onChange={(e) => onDistrictChange(e.target.value)}
            className="h-9 rounded-lg border border-grid-border bg-grid-panel px-3 text-sm text-gray-200 outline-none focus:border-brand focus:ring-1 focus:ring-brand transition-colors min-w-[130px]"
          >
            <option value="">All Districts</option>
            {DISTRICT_OPTIONS.map((d) => (
              <option key={d} value={d}>
                {d}
              </option>
            ))}
          </select>
        </div>

        {/* Threat Type Selector */}
        <div className="flex flex-col gap-1">
          <label htmlFor="map-threat-type" className="text-xs font-medium text-gray-400 uppercase tracking-wider">
            Threat Type
          </label>
          <select
            id="map-threat-type"
            value={inputType}
            onChange={(e) => onTypeChange(e.target.value as ThreatType | '')}
            className="h-9 rounded-lg border border-grid-border bg-grid-panel px-3 text-sm text-gray-200 outline-none focus:border-brand focus:ring-1 focus:ring-brand transition-colors min-w-[140px]"
          >
            {THREAT_TYPE_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        {/* Only With Alerts Toggle */}
        <div className="flex items-center gap-2 pb-2">
          <label className="flex items-center gap-2 cursor-pointer text-xs font-medium text-gray-300">
            <input
              id="map-only-alerts"
              type="checkbox"
              checked={inputOnlyWithAlerts}
              onChange={(e) => onOnlyWithAlertsChange(e.target.checked)}
              className="h-4 w-4 rounded border-grid-border bg-grid-panel text-brand focus:ring-brand focus:ring-offset-grid-bg"
            />
            <span>Only transformers with alerts</span>
          </label>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2 ml-auto">
          <button
            type="button"
            onClick={onApply}
            className="btn-primary h-9"
          >
            <Filter className="h-4 w-4" aria-hidden="true" />
            Apply Filters
          </button>
          <button
            type="button"
            onClick={onReset}
            className="btn-ghost h-9 text-xs"
            title="Reset filters to default"
          >
            <RotateCcw className="h-3.5 w-3.5" aria-hidden="true" />
            Reset
          </button>
        </div>
      </div>

      {dateError && (
        <div
          role="alert"
          className="rounded-lg border border-status-theft/30 bg-status-theft/10 px-3 py-2 text-xs text-status-theft"
        >
          {dateError}
        </div>
      )}
    </div>
  );
}
