import { useState, useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { ErrorCard, EmptyState, Skeleton } from '@/components/ui';
import { MapHeader } from '../components/MapHeader';
import { MapFilterPanel } from '../components/MapFilterPanel';
import { MapSummaryStrip } from '../components/MapSummaryStrip';
import { MapLegend } from '../components/MapLegend';
import { TransformerMap } from '../components/TransformerMap';
import { TransformerDetailPanel } from '../components/TransformerDetailPanel';
import { useMapMarkers, useRefreshMap } from '../hooks';
import type { MapFilterParams, MapMarker, ThreatType } from '../types';
import { MapPin } from 'lucide-react';

const DEFAULT_DATE_RANGE = {
  from: '2026-08-01',
  to: '2026-08-02',
};

export function AlertMapPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  // Active query parameters parsed from URL with defaults
  const activeParams = useMemo<MapFilterParams>(() => {
    const from = searchParams.get('from') || DEFAULT_DATE_RANGE.from;
    const to = searchParams.get('to') || DEFAULT_DATE_RANGE.to;
    const district = searchParams.get('district') || '';
    const type = (searchParams.get('type') as ThreatType) || '';
    const onlyWithAlerts = searchParams.get('onlyWithAlerts') === 'true';

    return { from, to, district, type, onlyWithAlerts };
  }, [searchParams]);

  // Form input state (staged before Apply is clicked)
  const [inputFrom, setInputFrom] = useState(activeParams.from);
  const [inputTo, setInputTo] = useState(activeParams.to);
  const [inputDistrict, setInputDistrict] = useState(activeParams.district ?? '');
  const [inputType, setInputType] = useState<ThreatType | ''>(activeParams.type ?? '');
  const [inputOnlyWithAlerts, setInputOnlyWithAlerts] = useState(activeParams.onlyWithAlerts ?? false);
  const [dateError, setDateError] = useState<string | null>(null);

  // Selected transformer marker for detail panel
  const [selectedMarker, setSelectedMarker] = useState<MapMarker | null>(null);

  // Query map markers from API
  const { data: markers, isLoading, isError, error, isFetching, dataUpdatedAt, refetch } =
    useMapMarkers(activeParams);

  const refresh = useRefreshMap();

  const handleApply = useCallback(() => {
    if (!inputFrom || !inputTo) {
      setDateError('Both start and end dates are required.');
      return;
    }
    if (inputFrom > inputTo) {
      setDateError('Start date must not be later than end date.');
      return;
    }

    const fromDate = new Date(inputFrom);
    const toDate = new Date(inputTo);
    const diffDays = Math.ceil((toDate.getTime() - fromDate.getTime()) / (1000 * 60 * 60 * 24)) + 1;
    if (diffDays > 366) {
      setDateError('Date range cannot exceed 366 days.');
      return;
    }

    setDateError(null);

    // Update URL query parameters
    const nextParams = new URLSearchParams();
    nextParams.set('from', inputFrom);
    nextParams.set('to', inputTo);
    if (inputDistrict) nextParams.set('district', inputDistrict);
    if (inputType) nextParams.set('type', inputType);
    if (inputOnlyWithAlerts) nextParams.set('onlyWithAlerts', 'true');

    setSearchParams(nextParams);
    setSelectedMarker(null);
  }, [inputFrom, inputTo, inputDistrict, inputType, inputOnlyWithAlerts, setSearchParams]);

  const handleReset = useCallback(() => {
    setInputFrom(DEFAULT_DATE_RANGE.from);
    setInputTo(DEFAULT_DATE_RANGE.to);
    setInputDistrict('');
    setInputType('');
    setInputOnlyWithAlerts(false);
    setDateError(null);

    const nextParams = new URLSearchParams();
    nextParams.set('from', DEFAULT_DATE_RANGE.from);
    nextParams.set('to', DEFAULT_DATE_RANGE.to);
    setSearchParams(nextParams);
    setSelectedMarker(null);
  }, [setSearchParams]);

  return (
    <div className="space-y-6">
      <MapHeader
        dateFrom={activeParams.from}
        dateTo={activeParams.to}
        lastFetchedAt={dataUpdatedAt || undefined}
        isFetching={isFetching}
        onRefresh={refresh}
      />

      <MapFilterPanel
        inputFrom={inputFrom}
        inputTo={inputTo}
        inputDistrict={inputDistrict}
        inputType={inputType}
        inputOnlyWithAlerts={inputOnlyWithAlerts}
        dateError={dateError}
        onFromChange={setInputFrom}
        onToChange={setInputTo}
        onDistrictChange={setInputDistrict}
        onTypeChange={setInputType}
        onOnlyWithAlertsChange={setInputOnlyWithAlerts}
        onApply={handleApply}
        onReset={handleReset}
      />

      {isError && (
        <ErrorCard
          title="Failed to load map markers"
          message={error?.message ?? 'An unexpected error occurred while fetching transformer markers.'}
          onRetry={() => refetch()}
        />
      )}

      {isLoading && (
        <div className="space-y-4">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-[480px] w-full" />
        </div>
      )}

      {!isLoading && !isError && markers && (
        <>
          <MapSummaryStrip
            markers={markers}
            activeTypeFilter={activeParams.type}
          />

          {markers.length === 0 ? (
            <div className="card p-6">
              <EmptyState
                icon={<MapPin className="h-8 w-8" />}
                title="No transformers match criteria"
                description="No transformers were found matching the selected dates, district, and threat filters. Try clearing or expanding your filters."
                className="h-80"
              />
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
              <div className={`${selectedMarker ? 'lg:col-span-2' : 'lg:col-span-3'}`}>
                <TransformerMap
                  markers={markers}
                  selectedMarker={selectedMarker}
                  activeTypeFilter={activeParams.type}
                  onSelectMarker={setSelectedMarker}
                />
              </div>

              {selectedMarker && (
                <div className="lg:col-span-1">
                  <TransformerDetailPanel
                    marker={selectedMarker}
                    dateFrom={activeParams.from}
                    dateTo={activeParams.to}
                    onClose={() => setSelectedMarker(null)}
                  />
                </div>
              )}
            </div>
          )}

          <MapLegend />
        </>
      )}
    </div>
  );
}
