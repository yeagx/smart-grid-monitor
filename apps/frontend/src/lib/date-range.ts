/**
 * Shared date-range defaults and validation for every page.
 * The warehouse data covers 1–2 Aug 2026, so that is the default window.
 */

export interface DateRangeParams {
  from: string; // YYYY-MM-DD
  to: string; // YYYY-MM-DD
}

export const DEFAULT_DATE_RANGE: DateRangeParams = {
  from: '2026-08-01',
  to: '2026-08-02',
};

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

/** Number of calendar days in an inclusive range (both ends counted). */
export function dayCount(from: string, to: string): number {
  const start = Date.parse(`${from}T00:00:00Z`);
  const end = Date.parse(`${to}T00:00:00Z`);
  return Math.round((end - start) / 86_400_000) + 1;
}

/** Returns an error message, or null when the range is valid. */
export function validateDateRange(from: string, to: string, maxDays = 366): string | null {
  if (!from || !to) return 'Both dates are required.';
  if (!ISO_DATE.test(from) || !ISO_DATE.test(to)) return 'Dates must use the YYYY-MM-DD format.';
  if (from > to) return 'Start date must not be later than end date.';
  if (dayCount(from, to) > maxDays) return `Date range cannot exceed ${maxDays} days.`;
  return null;
}

/** Reads from/to from URL params, falling back to the default window when missing or invalid. */
export function rangeFromSearchParams(params: URLSearchParams): DateRangeParams {
  const from = params.get('from') ?? '';
  const to = params.get('to') ?? '';
  return validateDateRange(from, to) === null ? { from, to } : DEFAULT_DATE_RANGE;
}
