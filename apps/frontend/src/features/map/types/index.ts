export type ThreatType = 'theft' | 'overload' | 'line_drop';

export interface MapMarker {
  transformerId: string;
  district: string;
  governorate: string;
  feederId: string;
  substationId: string;
  latitude: number;
  longitude: number;
  capacityKw: number;
  peakKwRecorded: number | null;
  peakLoadPercentage: number | null;
  theftAlertCount: number;
  overloadAlertCount: number;
  lineDropAlertCount: number;
  totalAlertCount: number;
  latestAlertAt: string | null;
}

export interface MapFilterParams {
  from: string;
  to: string;
  district?: string;
  type?: ThreatType | '';
  onlyWithAlerts?: boolean;
}

export const DISTRICT_OPTIONS = [
  'Dokki',
  'Nasr City',
  'Maadi',
  'Heliopolis',
  'Shubra',
] as const;

export const THREAT_TYPE_OPTIONS: Array<{ label: string; value: ThreatType | '' }> = [
  { label: 'All Threats', value: '' },
  { label: 'Theft', value: 'theft' },
  { label: 'Overload', value: 'overload' },
  { label: 'Voltage Drop', value: 'line_drop' },
];
