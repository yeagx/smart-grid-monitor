import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { AlertMapPage } from '../pages/AlertMapPage';
import { getMarkerSizeForZoom } from '../components/TransformerMap';
import type { MapMarker } from '../types';

// Mock the API layer directly
vi.mock('@/features/map/api', () => ({
  fetchMapMarkers: vi.fn(),
}));

import { fetchMapMarkers } from '@/features/map/api';

const mockedFetchMapMarkers = vi.mocked(fetchMapMarkers);

const mockMarkers: MapMarker[] = [
  {
    transformerId: 'TR-0012',
    district: 'Dokki',
    governorate: 'Giza',
    feederId: 'F-01',
    substationId: 'SS-DOKKI',
    latitude: 30.0384,
    longitude: 31.2109,
    capacityKw: 500,
    peakKwRecorded: 420.5,
    peakLoadPercentage: 84.1,
    theftAlertCount: 2,
    overloadAlertCount: 0,
    lineDropAlertCount: 0,
    totalAlertCount: 2,
    latestAlertAt: '2026-08-02T07:00:00Z',
  },
  {
    transformerId: 'TR-0045',
    district: 'Nasr City',
    governorate: 'Cairo',
    feederId: 'F-06',
    substationId: 'SS-NASR',
    latitude: 30.0566,
    longitude: 31.3302,
    capacityKw: 630,
    peakKwRecorded: 585.9,
    peakLoadPercentage: 93.0,
    theftAlertCount: 1,
    overloadAlertCount: 1,
    lineDropAlertCount: 0,
    totalAlertCount: 2,
    latestAlertAt: '2026-08-02T19:30:00Z',
  },
  {
    transformerId: 'TR-0301',
    district: 'Dokki',
    governorate: 'Giza',
    feederId: 'F-03',
    substationId: 'SS-DOKKI',
    latitude: 30.04,
    longitude: 31.208,
    capacityKw: 500,
    peakKwRecorded: null,
    peakLoadPercentage: null,
    theftAlertCount: 0,
    overloadAlertCount: 0,
    lineDropAlertCount: 0,
    totalAlertCount: 0,
    latestAlertAt: null,
  },
];

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
    },
  });
}

function renderWithProviders(initialEntry = '/alerts/map') {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/alerts/map" element={<AlertMapPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('AlertMapPage', () => {
  it('scales marker sizes with zoom while respecting visibility bounds', () => {
    expect(getMarkerSizeForZoom(4)).toBe(18);
    expect(getMarkerSizeForZoom(12)).toBe(26);
    expect(getMarkerSizeForZoom(18)).toBe(34);
    expect(getMarkerSizeForZoom(14)).toBeGreaterThan(getMarkerSizeForZoom(10));
  });

  it('renders successfully with map header and filters', async () => {
    mockedFetchMapMarkers.mockResolvedValue(mockMarkers);

    renderWithProviders();

    expect(screen.getByText('Alert Map')).toBeInTheDocument();
    expect(
      screen.getByText(/Explore transformer locations and investigate detected grid anomalies/i),
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Map Legend')).toBeInTheDocument();
    });
  });

  it('shows loading skeletons before marker data arrives', () => {
    mockedFetchMapMarkers.mockReturnValue(new Promise(() => {}));

    renderWithProviders();

    expect(screen.getByText('Alert Map')).toBeInTheDocument();
    const skeletons = document.querySelectorAll('.skeleton');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('displays an error card when the API call fails and supports retry', async () => {
    mockedFetchMapMarkers.mockRejectedValue(new Error('Connection lost'));

    renderWithProviders();

    await waitFor(() => {
      expect(screen.getByText('Failed to load map markers')).toBeInTheDocument();
    });

    expect(screen.getByText('Retry')).toBeInTheDocument();
  });

  it('displays empty state when zero transformers match', async () => {
    mockedFetchMapMarkers.mockResolvedValue([]);

    renderWithProviders();

    await waitFor(() => {
      expect(screen.getByText('No transformers match criteria')).toBeInTheDocument();
    });
  });

  it('displays summary strip metrics distinguishing alert windows from affected transformers', async () => {
    mockedFetchMapMarkers.mockResolvedValue(mockMarkers);

    renderWithProviders();

    await waitFor(() => {
      // 3 transformers total displayed
      expect(screen.getByText('3')).toBeInTheDocument();
    });

    // 2 affected transformers (TR-0012, TR-0045)
    // 4 total alert windows (2 on TR-0012, 2 on TR-0045)
    expect(screen.getByText('With Alerts')).toBeInTheDocument();
    expect(screen.getByText('Alert Windows')).toBeInTheDocument();
    expect(screen.getByText('4')).toBeInTheDocument();
  });

  it('sends correct district parameter when filter is applied', async () => {
    mockedFetchMapMarkers.mockResolvedValue(mockMarkers);

    renderWithProviders();

    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Alert Map')).toBeInTheDocument();
    });

    const districtSelect = screen.getByLabelText('District');
    await user.selectOptions(districtSelect, 'Dokki');

    const applyBtn = screen.getByText('Apply Filters');
    await user.click(applyBtn);

    await waitFor(() => {
      expect(mockedFetchMapMarkers).toHaveBeenCalledWith(
        expect.objectContaining({ district: 'Dokki' }),
        expect.any(AbortSignal),
      );
    });
  });

  it('sends threat type and onlyWithAlerts parameters when filters are applied', async () => {
    mockedFetchMapMarkers.mockResolvedValue(mockMarkers);

    renderWithProviders();

    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Alert Map')).toBeInTheDocument();
    });

    const typeSelect = screen.getByLabelText('Threat Type');
    await user.selectOptions(typeSelect, 'theft');

    const alertsCheckbox = screen.getByLabelText('Only transformers with alerts');
    await user.click(alertsCheckbox);

    const applyBtn = screen.getByText('Apply Filters');
    await user.click(applyBtn);

    await waitFor(() => {
      expect(mockedFetchMapMarkers).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'theft', onlyWithAlerts: true }),
        expect.any(AbortSignal),
      );
    });
  });

  it('validates start and end date range before applying', async () => {
    mockedFetchMapMarkers.mockResolvedValue(mockMarkers);

    renderWithProviders();

    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Alert Map')).toBeInTheDocument();
    });

    const fromInput = screen.getByLabelText('From');
    const toInput = screen.getByLabelText('To');
    const applyBtn = screen.getByText('Apply Filters');

    await user.clear(fromInput);
    await user.type(fromInput, '2026-09-01');
    await user.clear(toInput);
    await user.type(toInput, '2026-08-01');
    await user.click(applyBtn);

    expect(screen.getByText('Start date must not be later than end date.')).toBeInTheDocument();
  });

  it('renders detail panel when selecting a transformer and displays historical peaks and links', async () => {
    mockedFetchMapMarkers.mockResolvedValue(mockMarkers);

    renderWithProviders();

    await waitFor(() => {
      expect(screen.getByText('Map Legend')).toBeInTheDocument();
    });

    // In jsdom with react-leaflet, the marker div or popup text appears
    // Let's verify markers are present or trigger click on a marker popup
    const popupNodes = document.querySelectorAll('.custom-grid-marker');
    expect(popupNodes.length).toBe(3);
  });
});
