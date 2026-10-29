import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Info, Target } from 'lucide-react';
import { EmptyState, ErrorCard, Field, PageHeader, Pagination, Skeleton, fieldClass } from '@/components/ui';
import { EVENT_TYPE_LIST, isEventType, type EventType } from '@/lib/event-types';
import { pageFromParam } from '@/lib/paging';
import { AccuracyOverview } from '../components/AccuracyOverview';
import { AccuracyDetailsTable } from '../components/AccuracyDetailsTable';
import { useAccuracyDetails, useAccuracyReport, useRefreshAccuracy } from '../hooks';
import { DETECTION_STATUSES, type DetectionStatus } from '../types';

const PAGE_SIZE = 20;

/**
 * Detection Accuracy — compares the anomalies we detected with the ones injected
 * into the simulated data (ground truth).
 */
export function AccuracyPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const active = useMemo(() => {
    const type = searchParams.get('type');
    const status = searchParams.get('status');
    return {
      type: (isEventType(type) ? type : '') as EventType | '',
      status: (DETECTION_STATUSES.includes(status as DetectionStatus) ? status : '') as DetectionStatus | '',
      page: pageFromParam(searchParams.get('page')),
    };
  }, [searchParams]);

  const reportQuery = useAccuracyReport();
  const detailsQuery = useAccuracyDetails({ type: active.type, status: active.status, page: active.page, pageSize: PAGE_SIZE });
  const refresh = useRefreshAccuracy();

  const update = useCallback(
    (changes: Record<string, string | number>) => {
      const next = new URLSearchParams(searchParams);
      for (const [key, value] of Object.entries(changes)) {
        if (value === '' || value === null) next.delete(key);
        else next.set(key, String(value));
      }
      if (!('page' in changes)) next.delete('page');
      setSearchParams(next);
    },
    [searchParams, setSearchParams],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Target}
        title="Detection Accuracy"
        description="How well the pipeline's rules catch the anomalies injected into the simulated data."
        isFetching={reportQuery.isFetching || detailsQuery.isFetching}
        onRefresh={refresh}
      />

      <div className="flex items-start gap-2 rounded-lg border border-brand/30 bg-brand/5 px-3 py-2 text-xs text-gray-300">
        <Info className="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand-light" aria-hidden="true" />
        <p>
          <span className="font-semibold text-gray-100">True positive</span>: injected and detected ·{' '}
          <span className="font-semibold text-gray-100">False positive</span>: detected but not injected ·{' '}
          <span className="font-semibold text-gray-100">False negative</span>: injected but missed.
          Precision = TP / (TP + FP); Recall = TP / (TP + FN).
        </p>
      </div>

      {reportQuery.isError ? (
        <ErrorCard title="Failed to load accuracy report" message={reportQuery.error?.message ?? 'Unexpected error.'} onRetry={() => reportQuery.refetch()} />
      ) : reportQuery.isLoading || !reportQuery.data ? (
        <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </div>
      ) : (
        <AccuracyOverview report={reportQuery.data} />
      )}

      <div className="card">
        <div className="flex flex-wrap items-end gap-3 border-b border-grid-border p-4">
          <h2 className="mr-auto self-center text-sm font-semibold text-gray-200">Detection details</h2>
          <Field label="Type" htmlFor="acc-type">
            <select id="acc-type" value={active.type} onChange={(e) => update({ type: e.target.value })} className={fieldClass}>
              <option value="">All types</option>
              {EVENT_TYPE_LIST.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </Field>
          <Field label="Outcome" htmlFor="acc-status">
            <select id="acc-status" value={active.status} onChange={(e) => update({ status: e.target.value })} className={fieldClass}>
              <option value="">All outcomes</option>
              {DETECTION_STATUSES.map((s) => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </Field>
        </div>

        {detailsQuery.isError ? (
          <ErrorCard className="m-4" title="Failed to load details" message={detailsQuery.error?.message ?? 'Unexpected error.'} onRetry={() => detailsQuery.refetch()} />
        ) : detailsQuery.isLoading ? (
          <div className="space-y-2 p-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-8 w-full" />
            ))}
          </div>
        ) : detailsQuery.data && detailsQuery.data.items.length > 0 ? (
          <>
            <AccuracyDetailsTable rows={detailsQuery.data.items} />
            <Pagination
              className="px-4 py-3"
              page={detailsQuery.data.page}
              pageSize={detailsQuery.data.pageSize}
              totalCount={detailsQuery.data.totalCount}
              onPageChange={(page) => update({ page })}
            />
          </>
        ) : (
          <EmptyState icon={<Target className="h-8 w-8" />} title="No rows match these filters" className="h-40" />
        )}
      </div>
    </div>
  );
}
