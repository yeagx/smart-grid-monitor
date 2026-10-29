/**
 * Types matching the backend alert DTOs (camelCase JSON).
 * Timestamps are ISO 8601 UTC.
 */
import type { EventType } from '@/lib/event-types';

/** GET /api/alerts item (AlertDto) */
export interface Alert {
  anomalyId: string;
  eventType: EventType | string;
  entityId: string;
  transformerId: string;
  district: string;
  governorate: string;
  startTimestamp: string;
  endTimestamp: string;
}

/** GET /api/alerts/summary item (AlertSummaryByTypeDto) */
export interface AlertSummaryByType {
  eventType: EventType | string;
  totalAlerts: number;
  affectedTransformers: number;
}

export interface AlertSearchParams {
  from?: string;
  to?: string;
  type?: EventType | '';
  district?: string;
  transformerId?: string;
  page?: number;
  pageSize?: number;
}
