import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { OverviewPage } from '@/features/dashboard/pages/OverviewPage';
import type { DashboardOverview, DailyDemandPoint } from '@/features/dashboard/types';

/* ------------------------------------------------------------------ */
/*  Test helpers                                                      */
/* ------------------------------------------------------------------ */

const mockSummary: DashboardOverview = {
  periodStart: '2026-08-01',
  periodEnd: '2026-08-02',
  totalEnergyKwh: 2905251.25,
  peakNetworkKw: 91200.80,
  totalAlerts: 11,
  theftTransformers: 3,
  overloadTransformers: 3,
  linedropTransformers: 2,
  affectedTransformers: 6,
  latestReadingTimestamp: '2026-08-02T23:45:00+00:00',
};

const mockDemand: DailyDemandPoint[] = [
  { demandDate: '2026-08-01', totalDailyKwh: 1425000.5, peakNetworkKw: 88400.25 },
  { demandDate: '2026-08-02', totalDailyKwh: 1480250.75, peakNetworkKw: 91200.80 },
];

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
    },
  });
}

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/']}>
        {ui}
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

/* ------------------------------------------------------------------ */
/*  Mocks                                                             */
/* ------------------------------------------------------------------ */

// We mock the API functions directly rather than the HTTP layer
vi.mock('@/features/dashboard/api', () => ({
  fetchDashboardSummary: vi.fn(),
  fetchDailyDemand: vi.fn(),
}));

import { fetchDashboardSummary, fetchDailyDemand } from '@/features/dashboard/api';

const mockedFetchSummary = vi.mocked(fetchDashboardSummary);
const mockedFetchDemand = vi.mocked(fetchDailyDemand);

beforeEach(() => {
  vi.clearAllMocks();
});

afterEach(() => {
  vi.restoreAllMocks();
});

/* ------------------------------------------------------------------ */
/*  Tests                                                             */
/* ------------------------------------------------------------------ */

describe('OverviewPage', () => {
  it('displays KPI values from a successful summary response', async () => {
    mockedFetchSummary.mockResolvedValue(mockSummary);
    mockedFetchDemand.mockResolvedValue(mockDemand);

    renderWithProviders(<OverviewPage />);

    // Wait for summary data to appear — energy in GWh
    await waitFor(() => {
      expect(screen.getByText('2.91 GWh')).toBeInTheDocument();
    });

    // Peak power in MW
    expect(screen.getByText('91.20 MW')).toBeInTheDocument();

    // Alert counts — both theft and overload show "3", so use getAllByText
    const threes = screen.getAllByText('3');
    expect(threes.length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('6')).toBeInTheDocument(); // affected
  });

  it('shows loading skeletons before data arrives', () => {
    // Never resolve
    mockedFetchSummary.mockReturnValue(new Promise(() => {}));
    mockedFetchDemand.mockReturnValue(new Promise(() => {}));

    renderWithProviders(<OverviewPage />);

    // Title should be visible immediately
    expect(screen.getByText('Overview Dashboard')).toBeInTheDocument();

    // Skeleton elements should exist (via class)
    const skeletons = document.querySelectorAll('.skeleton');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('displays an error card when the summary request fails', async () => {
    mockedFetchSummary.mockRejectedValue(new Error('Network Error'));
    mockedFetchDemand.mockResolvedValue(mockDemand);

    renderWithProviders(<OverviewPage />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load summary')).toBeInTheDocument();
    });

    // Retry button should be present
    expect(screen.getByText('Retry')).toBeInTheDocument();
  });

  it('handles partial failure — chart error with successful summary', async () => {
    mockedFetchSummary.mockResolvedValue(mockSummary);
    mockedFetchDemand.mockRejectedValue(new Error('Server Error'));

    renderWithProviders(<OverviewPage />);

    // Summary KPIs should still render
    await waitFor(() => {
      expect(screen.getByText('2.91 GWh')).toBeInTheDocument();
    });

    // Chart should show error
    await waitFor(() => {
      expect(screen.getByText('Failed to load demand data')).toBeInTheDocument();
    });
  });

  it('shows empty state when demand data is an empty array', async () => {
    mockedFetchSummary.mockResolvedValue({
      ...mockSummary,
      totalEnergyKwh: null,
      peakNetworkKw: null,
      totalAlerts: null,
      theftTransformers: null,
      overloadTransformers: null,
      linedropTransformers: null,
      affectedTransformers: null,
      latestReadingTimestamp: null,
    });
    mockedFetchDemand.mockResolvedValue([]);

    renderWithProviders(<OverviewPage />);

    await waitFor(() => {
      expect(screen.getByText('No demand data')).toBeInTheDocument();
    });

    // KPI values should show dashes for null
    const dashes = screen.getAllByText('—');
    expect(dashes.length).toBeGreaterThanOrEqual(2);
  });

  it('validates date range before applying', async () => {
    mockedFetchSummary.mockResolvedValue(mockSummary);
    mockedFetchDemand.mockResolvedValue(mockDemand);

    renderWithProviders(<OverviewPage />);

    const user = userEvent.setup();

    // Wait for initial load
    await waitFor(() => {
      expect(screen.getByText('2.91 GWh')).toBeInTheDocument();
    });

    // Set invalid range (from > to)
    const fromInput = screen.getByLabelText('From');
    const toInput = screen.getByLabelText('To');
    const applyBtn = screen.getByText('Apply');

    await user.clear(fromInput);
    await user.type(fromInput, '2026-09-01');
    await user.clear(toInput);
    await user.type(toInput, '2026-08-01');
    await user.click(applyBtn);

    expect(screen.getByText('Start date must not be later than end date.')).toBeInTheDocument();
  });

  it('sends correct query parameters to the API', async () => {
    mockedFetchSummary.mockResolvedValue(mockSummary);
    mockedFetchDemand.mockResolvedValue(mockDemand);

    renderWithProviders(<OverviewPage />);

    await waitFor(() => {
      expect(mockedFetchSummary).toHaveBeenCalledWith(
        { from: '2026-08-01', to: '2026-08-02' },
        expect.any(AbortSignal),
      );
    });

    expect(mockedFetchDemand).toHaveBeenCalledWith(
      { from: '2026-08-01', to: '2026-08-02' },
      expect.any(AbortSignal),
    );
  });

  it('updates both KPIs and chart when date range changes', async () => {
    mockedFetchSummary.mockResolvedValue(mockSummary);
    mockedFetchDemand.mockResolvedValue(mockDemand);

    renderWithProviders(<OverviewPage />);

    const user = userEvent.setup();

    // Wait for initial load
    await waitFor(() => {
      expect(screen.getByText('2.91 GWh')).toBeInTheDocument();
    });

    // Update the summary mock to return different data for the new range
    const newSummary: DashboardOverview = {
      ...mockSummary,
      periodStart: '2026-08-01',
      periodEnd: '2026-08-01',
      totalEnergyKwh: 1425000.5,
    };
    mockedFetchSummary.mockResolvedValue(newSummary);
    mockedFetchDemand.mockResolvedValue([mockDemand[0]]);

    // Change the date range
    const toInput = screen.getByLabelText('To');
    const applyBtn = screen.getByText('Apply');

    await user.clear(toInput);
    await user.type(toInput, '2026-08-01');
    await user.click(applyBtn);

    // Verify the new query was made
    await waitFor(() => {
      expect(mockedFetchSummary).toHaveBeenCalledWith(
        { from: '2026-08-01', to: '2026-08-01' },
        expect.any(AbortSignal),
      );
    });
  });

  it('calls refresh to invalidate queries when refresh button is clicked', async () => {
    mockedFetchSummary.mockResolvedValue(mockSummary);
    mockedFetchDemand.mockResolvedValue(mockDemand);

    renderWithProviders(<OverviewPage />);

    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('2.91 GWh')).toBeInTheDocument();
    });

    const initialCallCount = mockedFetchSummary.mock.calls.length;

    const refreshBtn = screen.getByLabelText('Refresh dashboard data');
    await user.click(refreshBtn);

    // Should trigger a refetch
    await waitFor(() => {
      expect(mockedFetchSummary.mock.calls.length).toBeGreaterThan(initialCallCount);
    });
  });
});
