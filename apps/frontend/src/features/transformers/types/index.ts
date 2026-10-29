/** Types matching the backend transformer DTOs (camelCase JSON). */

export type TransformerSort = 'id' | 'alerts' | 'load';

/** GET /api/transformers item (TransformerListItemDto) */
export interface TransformerListItem {
  transformerId: string;
  district: string;
  governorate: string;
  feederId: string;
  substationId: string;
  capacityKw: number;
  latitude: number;
  longitude: number;
  peakKwRecorded: number | null;
  peakLoadPercentage: number | null;
  alertCount: number;
}

/** GET /api/transformers/{id} (TransformerDetailsDto) */
export interface TransformerDetails {
  transformerId: string;
  district: string;
  governorate: string;
  feederId: string;
  substationId: string;
  capacityKw: number;
  latitude: number;
  longitude: number;
}

/** GET /api/transformers/{id}/load-profile item — one hour (TransformerLoadPointDto) */
export interface TransformerLoadPoint {
  hourTimestamp: string;
  avgKw: number;
  maxKw: number;
  energyKwh: number;
  avgVoltage: number;
  minVoltage: number;
  readingCount: number;
}

export interface TransformerSearchParams {
  search?: string;
  district?: string;
  sort?: TransformerSort;
  onlyWithAlerts?: boolean;
  page?: number;
  pageSize?: number;
}

/** Detection thresholds used by the dbt models (shown as reference lines on charts). */
export const OVERLOAD_THRESHOLD_RATIO = 0.85;
export const VOLTAGE_DROP_THRESHOLD_V = 207;
export const NOMINAL_VOLTAGE_V = 230;
