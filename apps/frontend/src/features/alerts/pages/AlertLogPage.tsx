import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Bell, Download, RotateCcw, Search } from 'lucide-react';
import { EmptyState, ErrorCard, Field, PageHeader, Pagination, Skeleton, fieldClass } from '@/components/ui';
import { DEFAULT_DATE_RANGE, rangeFromSearchParams, validateDateRange } from '@/lib/date-range';
import { DISTRICTS, EVENT_TYPE_LIST, isEventType, type EventType } from '@/lib/event-types';
import { formatDate } from '@/lib/formatters';
import { pageFromParam } from '@/lib/paging';
import { downloadCsv, toCsv } from '@/lib/csv';
import { AlertTypeCards } from '../components/AlertTypeCards';
import { AlertTable } from '../components/AlertTable';
import { fetchAllAlerts } from '../api';
import { useAlertSummary, useAlerts, useRefreshAlerts } from '../hooks';
import type { AlertSearchParams } from '../types';

const PAGE_SIZE = 25;

/**
 * Alert Log — every detected anomaly window, filterable and paged.
 * All filters live in the URL, so links from the map / transformer pages
 * (e.g. ?transformerId=TR-0089&from=…&to=…) open the log already filtered.
 */
export function AlertLogPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const active = useMemo(() => {
    const range = rangeFromSearchParams(searchParams);
    const type = searchParams.get('type');
    return {
      ...range,
      type: (isEventType(type) ? type : '') as EventType | '',
      district: searchParams.get('district') ?? '',
      transformerId: searchParams.get('transformerId') ?? '',
      page: pageFromParam(searchParams.get('page')),
    };
  }, [searchParams]);

  // Staged inputs (applied with the Apply button), kept in sync when the URL changes.
  const [inputFrom, setInputFrom] = useState(active.from);
  const [inputTo, setInputTo] = useState(active.to);
  const [inputDistrict, setInputDistrict] = useState(active.district);
  const [inputTransformer, setInputTransformer] = useState(active.transformerId);
  const [dateError, setDateError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    setInputFrom(active.from);
    setInputTo(active.to);
    setInputDistrict(active.district);
    setInputTransformer(active.transformerId);
  }, [active.from, active.to, active.district, active.transformerId]);

  const query: AlertSearchParams = {
    from: active.from,
    to: active.to,
    type: active.type,
    district: active.district,
    transformerId: active.transformerId,
    page: active.page,
    pageSize: PAGE_SIZE,
  };

  const alertsQuery = useAlerts(query);
  const summaryQuery = useAlertSummary(active.from, active.to);
  const refresh = useRefreshAlerts();

  const update = useCallback(
    (changes: Record<string, string | number | null>) => {
      const next = new URLSearchParams(searchParams);
      for (const [key, value] of Object.entries(changes)) {
        if (value === null || value === '') next.delete(key);
        else next.set(key, String(value));
      }
      // Any filter change goes back to page 1.
      if (!('page' in changes)) next.delete('page');
      setSearchParams(next);
    },
    [searchParams, setSearchParams],
  );

  const handleApply = () => {
    const error = validateDateRange(inputFrom, inputTo);
    if (error) {
      setDateError(error);
      return;
    }
    setDateError(null);
    update({
      from: inputFrom,
      to: inputTo,
      district: inputDistrict,
      transformerId: inputTransformer.trim(),
    });
  };

  const handleReset = () => {
    setDateError(null);
    setSearchParams(new URLSearchParams({ from: DEFAULT_DATE_RANGE.from, to: DEFAULT_DATE_RANGE.to }));
  };

  const handleExport = async () => {
    setExporting(true);
    try {
      const rows = await fetchAllAlerts({ ...query, page: undefined, pageSize: undefined });
      const csv = toCsv(
        ['alert_id', 'event_type', 'transformer_id', 'entity_id', 'district', 'governorate', 'start_utc', 'end_utc'],
        rows.map((a) => [a.anomalyId, a.eventType, a.transformerId, a.entityId, a.district, a.governorate, a.startTimestamp, a.endTimestamp]),
      );
      downloadCsv(`alerts_${active.from}_${active.to}.csv`, csv);
    } finally {
      setExporting(false);
    }
  };

  const data = alertsQuery.data;
  const hasFilters = Boolean(active.type || active.district || active.transformerId);

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Bell}
        title="Alert Log"
        description="Every detected anomaly window — theft, overload and voltage drop — with time, place and duration."
        isFetching={alertsQuery.isFetching || summaryQuery.isFetching}
        onRefresh={refresh}
      >
        <span className="text-xs text-gray-400">
          Period: <span className="font-medium text-gray-200">{formatDate(active.from)} — {formatDate(active.to)}</span>
        </span>
      </PageHeader>

      {/* Filters */}
      <form
        className="card p-4 flex flex-wrap items-end gap-3"
        onSubmit={(e) => {
          e.preventDefault();
          handleApply();
        }}
      >
        <Field label="From" htmlFor="alert-from">
          <input id="alert-from" type="date" value={inputFrom} onChange={(e) => setInputFrom(e.target.value)} className={fieldClass} />
        </Field>
        <Field label="To" htmlFor="alert-to">
          <input id="alert-to" type="date" value={inputTo} onChange={(e) => setInputTo(e.target.value)} className={fieldClass} />
        </Field>
        <Field label="District" htmlFor="alert-district">
          <select id="alert-district" value={inputDistrict} onChange={(e) => setInputDistrict(e.target.value)} className={fieldClass}>
            <option value="">All districts</option>
            {DISTRICTS.map((d) => (
              <option key={d} value={d}>{d}</option>
            ))}
          </select>
        </Field>
        <Field label="Transformer" htmlFor="alert-transformer">
          <div className="relative">
            <Search className="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-gray-500" aria-hidden="true" />
            <input
              id="alert-transformer"
              type="search"
              placeholder="e.g. TR-0089"
              value={inputTransformer}
              onChange={(e) => setInputTransformer(e.target.value)}
              className={`${fieldClass} pl-8 w-40`}
            />
          </div>
        </Field>
        <button type="submit" className="btn-primary h-9">Apply</button>
        <button type="button" onClick={handleReset} className="btn-ghost h-9">
          <RotateCcw className="h-3.5 w-3.5" aria-hidden="true" />
          Reset
        </button>
        <button
          type="button"
          onClick={handleExport}
          disabled={exporting || !data || data.totalCount === 0}
          className="btn-ghost h-9 ml-auto disabled:opacity-40 disabled:cursor-not-allowed"
          title="Download all alerts matching the filters as CSV"
        >
          <Download className="h-3.5 w-3.5" aria-hidden="true" />
          {exporting ? 'Exporting…' : 'Export CSV'}
        </button>
        {dateError && <p className="w-full text-xs text-status-theft" role="alert">{dateError}</p>}
      </form>

      <AlertTypeCards
        data={summaryQuery.data}
        isLoading={summaryQuery.isLoading}
        activeType={active.type}
        onSelect={(type) => update({ type })}
      />

      {alertsQuery.isError ? (
        <ErrorCard
          title="Failed to load alerts"
          message={alertsQuery.error?.message ?? 'An unexpected error occurred while fetching alerts.'}
          onRetry={() => alertsQuery.refetch()}
        />
      ) : (
        <div className="card">
          <div className="flex items-center justify-between border-b border-grid-border px-4 py-3">
            <h2 className="text-sm font-semibold text-gray-200">
              Alerts
              {active.type && <span className="ml-2 text-xs font-normal text-gray-500">type: {EVENT_TYPE_LIST.find((t) => t.value === active.type)?.label}</span>}
              {active.district && <span className="ml-2 text-xs font-normal text-gray-500">district: {active.district}</span>}
              {active.transformerId && <span className="ml-2 text-xs font-normal text-gray-500">transformer: {active.transformerId}</span>}
            </h2>
            {hasFilters && (
              <button type="button" className="text-xs text-sky-400 hover:text-sky-300 hover:underline" onClick={() => update({ type: null, district: null, transformerId: null })}>
                Clear filters
              </button>
            )}
          </div>

          {alertsQuery.isLoading ? (
            <div className="space-y-2 p-4">
              {Array.from({ length: 6 }).map((_, i) => (
                <Skeleton key={i} className="h-8 w-full" />
              ))}
            </div>
          ) : data && data.items.length > 0 ? (
            <>
              <AlertTable alerts={data.items} from={active.from} to={active.to} />
              <Pagination
                className="px-4 py-3"
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                onPageChange={(page) => update({ page })}
              />
            </>
          ) : (
            <EmptyState
              icon={<Bell className="h-8 w-8" />}
              title="No alerts match these filters"
              description="Try a wider date range or clear the type, district and transformer filters."
              className="h-64"
            />
          )}
        </div>
      )}
    </div>
  );
}
