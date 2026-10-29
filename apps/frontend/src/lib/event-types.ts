/**
 * Single source of truth for the three anomaly types:
 * API value, display label, badge variant and hex colours (for Leaflet / Recharts,
 * which need raw colours instead of Tailwind classes).
 *
 * Hex values match tailwind.config.js `status.*`.
 */

export type EventType = 'theft' | 'overload' | 'line_drop';

export type StatusVariant = 'theft' | 'overload' | 'linedrop';

export interface EventTypeMeta {
  value: EventType;
  label: string;
  description: string;
  variant: StatusVariant;
  color: string;
  ring: string;
}

export const EVENT_TYPES: Record<EventType, EventTypeMeta> = {
  theft: {
    value: 'theft',
    label: 'Theft',
    description: 'Energy sent by the transformer is missing from customer meters',
    variant: 'theft',
    color: '#EF4444',
    ring: '#F87171',
  },
  overload: {
    value: 'overload',
    label: 'Overload',
    description: 'Load ≥ 85% of transformer capacity, sustained',
    variant: 'overload',
    color: '#F59E0B',
    ring: '#FBBF24',
  },
  line_drop: {
    value: 'line_drop',
    label: 'Voltage Drop',
    description: 'Voltage < 207 V (−10% of 230 V), sustained',
    variant: 'linedrop',
    color: '#14B8A6',
    ring: '#2DD4BF',
  },
};

export const EVENT_TYPE_LIST: EventTypeMeta[] = [EVENT_TYPES.theft, EVENT_TYPES.overload, EVENT_TYPES.line_drop];

export const HEALTHY_COLOR = { color: '#10B981', ring: '#34D399' };

export function isEventType(value: string | null | undefined): value is EventType {
  return value === 'theft' || value === 'overload' || value === 'line_drop';
}

/** Label for any event type string; unknown values are shown as-is. */
export function eventTypeLabel(value: string): string {
  return isEventType(value) ? EVENT_TYPES[value].label : value;
}

export const DISTRICTS = ['Dokki', 'Nasr City', 'Maadi', 'Heliopolis', 'Shubra'] as const;
