import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderPage } from '@/test/render';
import { AlertLogPage } from '@/features/alerts/pages/AlertLogPage';
import type { Alert, AlertSummaryByType } from '@/features/alerts/types';

vi.mock('@/features/alerts/api', () => ({
  fetchAlerts: vi.fn(),
  fetchAlertSummary: vi.fn(),
  fetchAllAlerts: vi.fn(),
}));

import { fetchAlerts, fetchAlertSummary } from '@/features/alerts/api';

const alerts: Alert[] = [
  {
    anomalyId: 'TR-0089-OVL-20260801183000',
    eventType: 'overload',
    entityId: 'TR-0089',
    transformerId: 'TR-0089',
    district: 'Heliopolis',
    governorate: 'Cairo',
    startTimestamp: '2026-08-01T18:00:00+00:00',
    endTimestamp: '2026-08-01T18:30:00+00:00',
  },
];

const summary: AlertSummaryByType[] = [
  { eventType: 'theft', totalAlerts: 4, affectedTransformers: 3 },
  { eventType: 'overload', totalAlerts: 5, affectedTransformers: 3 },
  { eventType: 'line_drop', totalAlerts: 2, affectedTransformers: 2 },
];

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(fetchAlerts).mockResolvedValue({ items: alerts, page: 1, pageSize: 25, totalCount: 1, totalPages: 1 });
  vi.mocked(fetchAlertSummary).mockResolvedValue(summary);
});

describe('AlertLogPage', () => {
  it('shows alerts with UTC times, duration and a transformer link', async () => {
    renderPage(<AlertLogPage />, '/alert-log', '/alert-log');

    expect(await screen.findByText('TR-0089-OVL-20260801183000')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'TR-0089' })).toHaveAttribute(
      'href',
      '/transformers?id=TR-0089&from=2026-08-01&to=2026-08-02',
    );
    expect(screen.getByText('Aug 1, 18:00')).toBeInTheDocument();
    expect(screen.getByText('30 min')).toBeInTheDocument();
  });

  it('applies filters from the URL (links from the map)', async () => {
    renderPage(<AlertLogPage />, '/alert-log?transformerId=TR-0089&from=2026-08-01&to=2026-08-01', '/alert-log');

    await waitFor(() =>
      expect(fetchAlerts).toHaveBeenCalledWith(
        expect.objectContaining({ transformerId: 'TR-0089', from: '2026-08-01', to: '2026-08-01', page: 1 }),
        expect.anything(),
      ),
    );
    expect(screen.getByLabelText('Transformer')).toHaveValue('TR-0089');
  });

  it('filters by type when a summary card is clicked', async () => {
    const user = userEvent.setup();
    renderPage(<AlertLogPage />, '/alert-log', '/alert-log');

    await user.click(await screen.findByRole('button', { name: /Voltage Drop/ }));

    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('type=line_drop'));
    await waitFor(() =>
      expect(fetchAlerts).toHaveBeenLastCalledWith(expect.objectContaining({ type: 'line_drop' }), expect.anything()),
    );
  });

  it('shows an empty state when nothing matches', async () => {
    vi.mocked(fetchAlerts).mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });
    renderPage(<AlertLogPage />, '/alert-log', '/alert-log');

    expect(await screen.findByText('No alerts match these filters')).toBeInTheDocument();
  });
});
