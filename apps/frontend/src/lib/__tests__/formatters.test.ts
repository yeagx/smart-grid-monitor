import { describe, it, expect } from 'vitest';
import {
  formatEnergyGwh,
  formatPowerMw,
  formatInt,
  formatDate,
  formatRelativeTime,
  toGwh,
  toMw,
} from '@/lib/formatters';

describe('formatEnergyGwh', () => {
  it('converts kWh to GWh with 2 decimal places', () => {
    expect(formatEnergyGwh(1_425_000.5)).toBe('1.43 GWh');
  });

  it('handles small values', () => {
    expect(formatEnergyGwh(500_000)).toBe('0.50 GWh');
  });

  it('handles large values with thousands separator', () => {
    expect(formatEnergyGwh(2_500_000_000)).toBe('2,500.00 GWh');
  });

  it('returns dash for null', () => {
    expect(formatEnergyGwh(null)).toBe('—');
  });

  it('returns dash for undefined', () => {
    expect(formatEnergyGwh(undefined)).toBe('—');
  });

  it('handles zero', () => {
    expect(formatEnergyGwh(0)).toBe('0.00 GWh');
  });
});

describe('formatPowerMw', () => {
  it('converts kW to MW with 2 decimal places', () => {
    expect(formatPowerMw(88_400.25)).toBe('88.40 MW');
  });

  it('handles small values', () => {
    expect(formatPowerMw(500)).toBe('0.50 MW');
  });

  it('returns dash for null', () => {
    expect(formatPowerMw(null)).toBe('—');
  });

  it('returns dash for undefined', () => {
    expect(formatPowerMw(undefined)).toBe('—');
  });

  it('handles zero', () => {
    expect(formatPowerMw(0)).toBe('0.00 MW');
  });
});

describe('formatInt', () => {
  it('formats integer with no decimals', () => {
    expect(formatInt(42)).toBe('42');
  });

  it('adds thousands separator', () => {
    expect(formatInt(1234)).toBe('1,234');
  });

  it('returns dash for null', () => {
    expect(formatInt(null)).toBe('—');
  });

  it('returns dash for undefined', () => {
    expect(formatInt(undefined)).toBe('—');
  });

  it('handles zero', () => {
    expect(formatInt(0)).toBe('0');
  });
});

describe('formatDate', () => {
  it('formats a YYYY-MM-DD date string', () => {
    const result = formatDate('2026-08-01');
    // The exact format depends on locale but should contain Aug and 2026
    expect(result).toContain('Aug');
    expect(result).toContain('2026');
  });

  it('returns dash for null', () => {
    expect(formatDate(null)).toBe('—');
  });

  it('returns dash for undefined', () => {
    expect(formatDate(undefined)).toBe('—');
  });

  it('returns dash for empty string', () => {
    expect(formatDate('')).toBe('—');
  });
});

describe('formatRelativeTime', () => {
  it('returns "Unknown" for null', () => {
    expect(formatRelativeTime(null)).toBe('Unknown');
  });

  it('returns "Unknown" for undefined', () => {
    expect(formatRelativeTime(undefined)).toBe('Unknown');
  });

  it('returns a time description for a valid timestamp', () => {
    // Use a timestamp far in the past
    const result = formatRelativeTime('2020-01-01T00:00:00Z');
    expect(result).toMatch(/\d+d ago/);
  });
});

describe('toGwh', () => {
  it('converts kWh to GWh', () => {
    expect(toGwh(1_000_000)).toBe(1);
    expect(toGwh(1_425_000.5)).toBeCloseTo(1.4250005);
  });
});

describe('toMw', () => {
  it('converts kW to MW', () => {
    expect(toMw(1_000)).toBe(1);
    expect(toMw(88_400.25)).toBeCloseTo(88.40025);
  });
});
