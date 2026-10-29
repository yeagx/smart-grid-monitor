import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderPage } from '@/test/render';
import { AccuracyPage } from '@/features/accuracy/pages/AccuracyPage';
import { f1Score } from '@/features/accuracy/types';

vi.mock('@/features/accuracy/api', () => ({
  fetchAccuracyReport: vi.fn(),
  fetchAccuracyDetails: vi.fn(),
}));

import { fetchAccuracyDetails, fetchAccuracyReport } from '@/features/accuracy/api';

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(fetchAccuracyReport).mockResolvedValue({
    byType: [
      { eventType: 'theft', truePositives: 8, falsePositives: 2, falseNegatives: 2, precision: 0.8, recall: 0.8 },
      { eventType: 'overload', truePositives: 5, falsePositives: 0, falseNegatives: 0, precision: 1, recall: 1 },
      { eventType: 'line_drop', truePositives: 3, falsePositives: 1, falseNegatives: 1, precision: 0.75, recall: 0.75 },
    ],
    totalTruePositives: 16,
    totalFalsePositives: 3,
    totalFalseNegatives: 3,
    overallPrecision: 0.8421,
    overallRecall: 0.8421,
  });
  vi.mocked(fetchAccuracyDetails).mockResolvedValue({
    items: [
      {
        transformerId: 'TR-0303',
        eventType: 'theft',
        detectionStatus: 'False Negative',
        truthStartTimestamp: '2026-08-02T01:00:00+00:00',
        truthEndTimestamp: '2026-08-02T04:00:00+00:00',
        detectedStartTimestamp: null,
        detectedEndTimestamp: null,
        flagRows: null,
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
  });
});

describe('AccuracyPage', () => {
  it('shows overall precision, recall and per-type results', async () => {
    renderPage(<AccuracyPage />, '/accuracy', '/accuracy');

    expect(await screen.findAllByText('84.2%')).toHaveLength(3); // precision, recall, F1
    expect(screen.getAllByText('Voltage Drop').length).toBeGreaterThan(0);
    expect(await screen.findByRole('link', { name: 'TR-0303' })).toBeInTheDocument();
  });

  it('filters details by outcome', async () => {
    const user = userEvent.setup();
    renderPage(<AccuracyPage />, '/accuracy', '/accuracy');

    await user.selectOptions(await screen.findByLabelText('Outcome'), 'False Positive');

    await waitFor(() =>
      expect(fetchAccuracyDetails).toHaveBeenLastCalledWith(
        expect.objectContaining({ status: 'False Positive', page: 1 }),
        expect.anything(),
      ),
    );
  });

  it('computes F1 as the harmonic mean', () => {
    expect(f1Score(0.8, 0.5)).toBeCloseTo(0.6154, 4);
    expect(f1Score(null, 0.5)).toBeNull();
    expect(f1Score(0, 0)).toBeNull();
  });
});
