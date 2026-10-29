import { lazy, Suspense } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@/components/layout/AppLayout';
import { Skeleton } from '@/components/ui';
import { OverviewPage } from '@/features/dashboard/pages/OverviewPage';

// Pages other than the overview load on demand, so the first screen downloads less JavaScript.
const AlertMapPage = lazy(() => import('@/features/map/pages/AlertMapPage').then((m) => ({ default: m.AlertMapPage })));
const AlertLogPage = lazy(() => import('@/features/alerts/pages/AlertLogPage').then((m) => ({ default: m.AlertLogPage })));
const TransformersPage = lazy(() => import('@/features/transformers/pages/TransformersPage').then((m) => ({ default: m.TransformersPage })));
const DecisionCenterPage = lazy(() => import('@/features/decisions/pages/DecisionCenterPage').then((m) => ({ default: m.DecisionCenterPage })));
const AccuracyPage = lazy(() => import('@/features/accuracy/pages/AccuracyPage').then((m) => ({ default: m.AccuracyPage })));

function PageFallback() {
  return (
    <div className="space-y-4">
      <Skeleton className="h-10 w-64" />
      <Skeleton className="h-96 w-full" />
    </div>
  );
}

export function AppRouter() {
  return (
    <AppLayout>
      <Suspense fallback={<PageFallback />}>
        <Routes>
          <Route path="/" element={<OverviewPage />} />
          <Route path="/alerts/map" element={<AlertMapPage />} />
          <Route path="/alert-log" element={<AlertLogPage />} />
          <Route path="/transformers" element={<TransformersPage />} />
          <Route path="/accuracy" element={<AccuracyPage />} />
          <Route path="/decisions" element={<DecisionCenterPage />} />
          {/* Aliases */}
          <Route path="/alert-map" element={<Navigate to="/alerts/map" replace />} />
          <Route path="/alerts" element={<Navigate to="/alert-log" replace />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Suspense>
    </AppLayout>
  );
}
