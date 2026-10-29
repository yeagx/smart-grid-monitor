/** Types matching the backend accuracy DTOs. Precision / recall are 0–1 ratios (null when undefined). */
import type { EventType } from '@/lib/event-types';

export interface AccuracyByType {
  eventType: EventType | string;
  truePositives: number;
  falsePositives: number;
  falseNegatives: number;
  precision: number | null;
  recall: number | null;
}

export interface AccuracyReport {
  byType: AccuracyByType[];
  totalTruePositives: number;
  totalFalsePositives: number;
  totalFalseNegatives: number;
  overallPrecision: number | null;
  overallRecall: number | null;
}

export type DetectionStatus = 'True Positive' | 'False Positive' | 'False Negative';

export const DETECTION_STATUSES: DetectionStatus[] = ['True Positive', 'False Positive', 'False Negative'];

export interface AccuracyDetail {
  transformerId: string;
  eventType: EventType | string;
  detectionStatus: DetectionStatus | string;
  truthStartTimestamp: string | null;
  truthEndTimestamp: string | null;
  detectedStartTimestamp: string | null;
  detectedEndTimestamp: string | null;
  flagRows: number | null;
}

export interface AccuracyDetailParams {
  type?: EventType | '';
  status?: DetectionStatus | '';
  transformerId?: string;
  page?: number;
  pageSize?: number;
}

/** F1 = harmonic mean of precision and recall. */
export function f1Score(precision: number | null, recall: number | null): number | null {
  if (precision == null || recall == null || precision + recall === 0) return null;
  return (2 * precision * recall) / (precision + recall);
}
