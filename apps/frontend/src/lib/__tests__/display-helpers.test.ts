import { describe, it, expect } from 'vitest';
import { formatDuration, formatRatio, formatShortTimestamp, formatTimestamp } from '@/lib/formatters';
import { dayCount, rangeFromSearchParams, validateDateRange } from '@/lib/date-range';
import { toQueryString } from '@/lib/paging';
import { toCsv } from '@/lib/csv';

describe('UTC timestamp formatting', () => {
  it('keeps warehouse (UTC) times on their own calendar day', () => {
    expect(formatTimestamp('2026-08-02T23:45:00+00:00')).toBe('Aug 2, 2026, 23:45 UTC');
    expect(formatShortTimestamp('2026-08-01T18:00:00Z')).toBe('Aug 1, 18:00');
  });

  it('formats durations', () => {
    expect(formatDuration('2026-08-01T13:00:00Z', '2026-08-01T13:30:00Z')).toBe('30 min');
    expect(formatDuration('2026-08-01T02:00:00Z', '2026-08-01T06:00:00Z')).toBe('4 h');
    expect(formatDuration('2026-08-01T02:00:00Z', '2026-08-01T03:15:00Z')).toBe('1 h 15 min');
    expect(formatDuration(null, '2026-08-01T03:15:00Z')).toBe('—');
  });

  it('formats 0–1 ratios as percentages', () => {
    expect(formatRatio(0.8571)).toBe('85.7%');
    expect(formatRatio(null)).toBe('—');
  });
});

describe('date range helpers', () => {
  it('counts inclusive days', () => {
    expect(dayCount('2026-08-01', '2026-08-02')).toBe(2);
  });

  it('validates ranges', () => {
    expect(validateDateRange('2026-08-02', '2026-08-01')).toMatch(/Start date/);
    expect(validateDateRange('2026-08-01', '2026-09-15', 31)).toMatch(/31 days/);
    expect(validateDateRange('2026-08-01', '2026-08-02')).toBeNull();
  });

  it('falls back to the default window for bad URL values', () => {
    expect(rangeFromSearchParams(new URLSearchParams('from=bad&to=2026-08-02'))).toEqual({ from: '2026-08-01', to: '2026-08-02' });
    expect(rangeFromSearchParams(new URLSearchParams('from=2026-08-02&to=2026-08-02'))).toEqual({ from: '2026-08-02', to: '2026-08-02' });
  });
});

describe('query string and CSV helpers', () => {
  it('skips empty values', () => {
    expect(toQueryString({ from: '2026-08-01', type: '', district: undefined, onlyWithAlerts: false, page: 2 })).toBe('?from=2026-08-01&page=2');
  });

  it('escapes CSV cells', () => {
    expect(toCsv(['a', 'b'], [['x,y', 'say "hi"']])).toBe('a,b\r\n"x,y","say ""hi"""');
  });
});
