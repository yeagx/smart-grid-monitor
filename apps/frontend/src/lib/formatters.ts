/**
 * Display formatting functions for engineering units.
 *
 * The API returns energy in kWh and power in kW.
 * These functions convert to human-friendly GWh / MW for display.
 *
 * IMPORTANT: Only use these for display. Raw API values should be
 * kept unchanged in the data layer.
 */

const numberFormatter = new Intl.NumberFormat('en-US', {
  maximumFractionDigits: 2,
  minimumFractionDigits: 2,
});

const intFormatter = new Intl.NumberFormat('en-US', {
  maximumFractionDigits: 0,
});

/**
 * Convert kWh to GWh and format for display.
 * 1 GWh = 1,000,000 kWh
 */
export function formatEnergyGwh(kwh: number | null | undefined): string {
  if (kwh == null) return '—';
  const gwh = kwh / 1_000_000;
  return `${numberFormatter.format(gwh)} GWh`;
}

/**
 * Convert kW to MW and format for display.
 * 1 MW = 1,000 kW
 */
export function formatPowerMw(kw: number | null | undefined): string {
  if (kw == null) return '—';
  const mw = kw / 1_000;
  return `${numberFormatter.format(mw)} MW`;
}

/** Format a plain integer with thousands separator. */
export function formatInt(value: number | null | undefined): string {
  if (value == null) return '—';
  return intFormatter.format(value);
}

/** Format a date string (YYYY-MM-DD) or DateOnly for display. */
export function formatDate(date: string | null | undefined): string {
  if (!date) return '—';
  try {
    const d = new Date(date + 'T00:00:00');
    return d.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  } catch {
    return date;
  }
}

/**
 * Format an ISO timestamp for display.
 *
 * Warehouse timestamps are UTC (TIMESTAMP_NTZ), so they are always shown in UTC.
 * Converting to the viewer's local zone would move late-evening readings onto the
 * next calendar day and no longer match the selected date range.
 */
export function formatTimestamp(ts: string | null | undefined): string {
  if (!ts) return '—';
  const d = new Date(ts);
  if (Number.isNaN(d.getTime())) return ts;
  return `${d.toLocaleString('en-US', {
    timeZone: 'UTC',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })} UTC`;
}

/** Compact UTC timestamp for tables, e.g. "Aug 1, 13:30". */
export function formatShortTimestamp(ts: string | null | undefined): string {
  if (!ts) return '—';
  const d = new Date(ts);
  if (Number.isNaN(d.getTime())) return ts;
  return d.toLocaleString('en-US', {
    timeZone: 'UTC',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  });
}

/** Hour label for chart axes, e.g. "Aug 1 13:00" (UTC). */
export function formatHourLabel(ts: string): string {
  const d = new Date(ts);
  if (Number.isNaN(d.getTime())) return ts;
  const day = d.toLocaleString('en-US', { timeZone: 'UTC', month: 'short', day: 'numeric' });
  const hh = String(d.getUTCHours()).padStart(2, '0');
  return `${day} ${hh}:00`;
}

/** Duration between two ISO timestamps, e.g. "45 min", "2 h", "1 h 30 min". */
export function formatDuration(start: string | null | undefined, end: string | null | undefined): string {
  if (!start || !end) return '—';
  const minutes = Math.round((Date.parse(end) - Date.parse(start)) / 60_000);
  if (!Number.isFinite(minutes) || minutes < 0) return '—';
  if (minutes < 60) return `${minutes} min`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m === 0 ? `${h} h` : `${h} h ${m} min`;
}

/** Format a 0–1 ratio as a percentage, e.g. 0.8571 → "85.7%". */
export function formatRatio(ratio: number | null | undefined, digits = 1): string {
  if (ratio == null || !Number.isFinite(ratio)) return '—';
  return `${(ratio * 100).toFixed(digits)}%`;
}

/** Format a value that is already a percentage, e.g. 93 → "93.0%". */
export function formatPercent(value: number | null | undefined, digits = 1): string {
  if (value == null || !Number.isFinite(value)) return '—';
  return `${value.toFixed(digits)}%`;
}

/** Format kW with thousands separator, e.g. 1234.5 → "1,234.5 kW". */
export function formatKw(kw: number | null | undefined): string {
  if (kw == null || !Number.isFinite(kw)) return '—';
  return `${kw.toLocaleString('en-US', { maximumFractionDigits: 1 })} kW`;
}

/**
 * Return a relative time description for data freshness.
 * E.g. "5 minutes ago", "2 hours ago", "3 days ago".
 */
export function formatRelativeTime(ts: string | null | undefined): string {
  if (!ts) return 'Unknown';
  try {
    const then = new Date(ts).getTime();
    const now = Date.now();
    const diffMs = now - then;

    if (diffMs < 0) return 'Just now';

    const minutes = Math.floor(diffMs / 60_000);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;

    const days = Math.floor(hours / 24);
    return `${days}d ago`;
  } catch {
    return 'Unknown';
  }
}

/** Format kWh to GWh as a raw number (for chart tooltips). */
export function toGwh(kwh: number): number {
  return kwh / 1_000_000;
}

/** Format kW to MW as a raw number (for chart tooltips). */
export function toMw(kw: number): number {
  return kw / 1_000;
}
