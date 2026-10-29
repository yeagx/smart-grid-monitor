import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderPage } from '@/test/render';
import { TransformersPage } from '@/features/transformers/pages/TransformersPage';
import type { TransformerListItem } from '@/features/transformers/types';

vi.mock('@/features/transformers/api', () => ({
  fetchTransformers: vi.fn(),
  fetchTransformer: vi.fn(),
  fetchLoadProfile: vi.fn(),
}));
vi.mock('@/features/alerts/api', () => ({
  fetchAlerts: vi.fn(),
  fetchAlertSummary: vi.fn(),
  fetchAllAlerts: vi.fn(),
}));

import { fetchLoadProfile, fetchTransformer, fetchTransformers } from '@/features/transformers/api';
import { fetchAlerts } from '@/features/alerts/api';

function item(id: string, alertCount: number): TransformerListItem {
  return {
    transformerId: id,
    district: 'Heliopolis',
    governorate: 'Cairo',
    feederId: 'F-16',
    substationId: 'SS-HELIO',
    capacityKw: 500,
    latitude: 30.09,
    longitude: 31.32,
    peakKwRecorded: 475,
    peakLoadPercentage: 95,
    alertCount,
  };
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(fetchTransformers).mockResolvedValue({
    items: [item('TR-0089', 2), item('TR-0304', 0)],
    page: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  });
  vi.mocked(fetchTransformer).mockImplementation(async (id) => ({ ...item(id, 0) }));
  vi.mocked(fetchLoadProfile).mockResolvedValue([
    { hourTimestamp: '2026-08-01T13:00:00+00:00', avgKw: 480, maxKw: 540, energyKwh: 480, avgVoltage: 225, minVoltage: 224, readingCount: 4 },
    { hourTimestamp: '2026-08-01T14:00:00+00:00', avgKw: 300, maxKw: 320, energyKwh: 300, avgVoltage: 226, minVoltage: 225, readingCount: 4 },
  ]);
  vi.mocked(fetchAlerts).mockResolvedValue({ items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 });
});

describe('TransformersPage', () => {
  it('selects the first transformer when no id is given and computes period stats', async () => {
    renderPage(<TransformersPage />, '/transformers', '/transformers');

    await waitFor(() => expect(fetchLoadProfile).toHaveBeenCalledWith('TR-0089', '2026-08-01', '2026-08-02', expect.anything()));
    expect(await screen.findByText('540 kW')).toBeInTheDocument(); // peak in period
    expect(screen.getByText('108.0% of capacity')).toBeInTheDocument();
    expect(fetchTransformers).toHaveBeenCalledWith(expect.objectContaining({ sort: 'alerts', page: 1 }), expect.anything());
  });

  it('opens the transformer from ?id= (links from map and alert log)', async () => {
    renderPage(<TransformersPage />, '/transformers?id=TR-0304', '/transformers');

    await waitFor(() => expect(fetchTransformer).toHaveBeenCalledWith('TR-0304', expect.anything()));
    expect(await screen.findByRole('link', { name: /Alert log/ })).toHaveAttribute(
      'href',
      '/alert-log?transformerId=TR-0304&from=2026-08-01&to=2026-08-02',
    );
  });

  it('selects a row on click and keeps it in the URL', async () => {
    const user = userEvent.setup();
    renderPage(<TransformersPage />, '/transformers', '/transformers');

    await user.click(await screen.findByText('TR-0304'));

    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('id=TR-0304'));
  });
});
