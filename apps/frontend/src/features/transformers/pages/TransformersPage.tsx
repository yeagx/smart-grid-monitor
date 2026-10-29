import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Cpu, Search } from 'lucide-react';
import { EmptyState, ErrorCard, Field, PageHeader, Pagination, Skeleton, fieldClass } from '@/components/ui';
import { DEFAULT_DATE_RANGE, dayCount, rangeFromSearchParams, validateDateRange } from '@/lib/date-range';
import { DISTRICTS } from '@/lib/event-types';
import { pageFromParam } from '@/lib/paging';
import { TransformerList } from '../components/TransformerList';
import { TransformerDetail } from '../components/TransformerDetail';
import { useRefreshTransformers, useTransformers } from '../hooks';
import type { TransformerSort } from '../types';

const PAGE_SIZE = 10;
const MAX_PROFILE_DAYS = 31;

const SORT_OPTIONS: Array<{ value: TransformerSort; label: string }> = [
  { value: 'alerts', label: 'Most alerts' },
  { value: 'load', label: 'Highest peak load' },
  { value: 'id', label: 'Transformer ID' },
];

function isSort(value: string | null): value is TransformerSort {
  return value === 'id' || value === 'alerts' || value === 'load';
}

/**
 * Transformers — searchable directory plus a detail view (charts + alerts) for the selected one.
 * ?id=TR-0089 selects a transformer (used by links from the map and the alert log).
 * With no id, the first row of the list is shown so the page is never empty.
 */
export function TransformersPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const active = useMemo(() => {
    const sort = searchParams.get('sort');
    const range = rangeFromSearchParams(searchParams);
    return {
      // Charts allow at most 31 days; a wider range from another page falls back to the default.
      ...(dayCount(range.from, range.to) > MAX_PROFILE_DAYS ? DEFAULT_DATE_RANGE : range),
      id: searchParams.get('id')?.trim() || null,
      search: searchParams.get('search') ?? '',
      district: searchParams.get('district') ?? '',
      sort: isSort(sort) ? sort : ('alerts' as TransformerSort),
      onlyWithAlerts: searchParams.get('onlyWithAlerts') === 'true',
      page: pageFromParam(searchParams.get('page')),
    };
  }, [searchParams]);

  const [inputSearch, setInputSearch] = useState(active.search);
  const [inputFrom, setInputFrom] = useState(active.from);
  const [inputTo, setInputTo] = useState(active.to);
  const [dateError, setDateError] = useState<string | null>(null);

  useEffect(() => setInputSearch(active.search), [active.search]);
  useEffect(() => {
    setInputFrom(active.from);
    setInputTo(active.to);
  }, [active.from, active.to]);

  const listQuery = useTransformers({
    search: active.search,
    district: active.district,
    sort: active.sort,
    onlyWithAlerts: active.onlyWithAlerts,
    page: active.page,
    pageSize: PAGE_SIZE,
  });
  const refresh = useRefreshTransformers();

  const update = useCallback(
    (changes: Record<string, string | number | boolean | null>, resetPage = true) => {
      const next = new URLSearchParams(searchParams);
      for (const [key, value] of Object.entries(changes)) {
        if (value === null || value === '' || value === false) next.delete(key);
        else next.set(key, String(value));
      }
      if (resetPage && !('page' in changes)) next.delete('page');
      setSearchParams(next, { replace: 'id' in changes && Object.keys(changes).length === 1 });
    },
    [searchParams, setSearchParams],
  );

  const items = listQuery.data?.items ?? [];
  const selectedId = active.id ?? items[0]?.transformerId ?? null;

  const applyPeriod = () => {
    const error = validateDateRange(inputFrom, inputTo, MAX_PROFILE_DAYS);
    if (error) {
      setDateError(error);
      return;
    }
    setDateError(null);
    update({ from: inputFrom, to: inputTo }, false);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Cpu}
        title="Transformers"
        description="Find any transformer, check its peak loading and alerts, and inspect its hourly load and voltage."
        isFetching={listQuery.isFetching}
        onRefresh={refresh}
      />

      {/* Directory */}
      <div className="card">
        <form
          className="flex flex-wrap items-end gap-3 border-b border-grid-border p-4"
          onSubmit={(e) => {
            e.preventDefault();
            update({ search: inputSearch.trim() });
          }}
        >
          <Field label="Search" htmlFor="tr-search">
            <div className="relative">
              <Search className="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-gray-500" aria-hidden="true" />
              <input
                id="tr-search"
                type="search"
                placeholder="Transformer ID"
                value={inputSearch}
                onChange={(e) => setInputSearch(e.target.value)}
                onBlur={() => inputSearch.trim() !== active.search && update({ search: inputSearch.trim() })}
                className={`${fieldClass} pl-8 w-44`}
              />
            </div>
          </Field>
          <Field label="District" htmlFor="tr-district">
            <select id="tr-district" value={active.district} onChange={(e) => update({ district: e.target.value })} className={fieldClass}>
              <option value="">All districts</option>
              {DISTRICTS.map((d) => (
                <option key={d} value={d}>{d}</option>
              ))}
            </select>
          </Field>
          <Field label="Sort by" htmlFor="tr-sort">
            <select id="tr-sort" value={active.sort} onChange={(e) => update({ sort: e.target.value })} className={fieldClass}>
              {SORT_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </Field>
          <label className="flex h-9 items-center gap-2 text-sm text-gray-300 cursor-pointer select-none">
            <input
              type="checkbox"
              checked={active.onlyWithAlerts}
              onChange={(e) => update({ onlyWithAlerts: e.target.checked })}
              className="h-4 w-4 rounded border-grid-border bg-grid-panel accent-[#1328A0]"
            />
            Only with alerts
          </label>
          <button type="submit" className="btn-primary h-9">Search</button>
        </form>

        {listQuery.isError ? (
          <ErrorCard
            className="m-4"
            title="Failed to load transformers"
            message={listQuery.error?.message ?? 'Unexpected error.'}
            onRetry={() => listQuery.refetch()}
          />
        ) : listQuery.isLoading ? (
          <div className="space-y-2 p-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-8 w-full" />
            ))}
          </div>
        ) : items.length === 0 ? (
          <EmptyState
            icon={<Cpu className="h-8 w-8" />}
            title="No transformers found"
            description="Try another ID, district, or turn off “Only with alerts”."
            className="h-48"
          />
        ) : (
          <>
            <TransformerList items={items} selectedId={selectedId} onSelect={(id) => update({ id }, false)} />
            <Pagination
              className="px-4 py-3"
              page={listQuery.data!.page}
              pageSize={listQuery.data!.pageSize}
              totalCount={listQuery.data!.totalCount}
              onPageChange={(page) => update({ page })}
            />
          </>
        )}
      </div>

      {/* Period for charts */}
      {selectedId && (
        <form
          className="flex flex-wrap items-end gap-3"
          onSubmit={(e) => {
            e.preventDefault();
            applyPeriod();
          }}
        >
          <Field label="Chart from" htmlFor="tr-from">
            <input id="tr-from" type="date" value={inputFrom} onChange={(e) => setInputFrom(e.target.value)} className={fieldClass} />
          </Field>
          <Field label="Chart to" htmlFor="tr-to">
            <input id="tr-to" type="date" value={inputTo} onChange={(e) => setInputTo(e.target.value)} className={fieldClass} />
          </Field>
          <button type="submit" className="btn-primary h-9">Apply</button>
          <span className="text-[11px] text-gray-500">Up to {MAX_PROFILE_DAYS} days of hourly readings.</span>
          {dateError && <p className="w-full text-xs text-status-theft" role="alert">{dateError}</p>}
        </form>
      )}

      {selectedId && <TransformerDetail key={selectedId} transformerId={selectedId} from={active.from} to={active.to} />}
    </div>
  );
}
