# Smart Grid Monitor — Frontend

The web dashboard for the Smart Grid Monitor application. Provides real-time operational awareness across 100,000 smart meters and 500 distribution transformers in Cairo and Giza.

## Tech Stack

- **Framework**: React 18 with TypeScript and Vite
- **Data Fetching & State**: TanStack Query (React Query) v5
- **Routing**: React Router v7
- **Styling**: Tailwind CSS with custom dark industrial control-room theme
- **Visualization**: Recharts (charts), Leaflet + OpenStreetMap (map)
- **Icons**: Lucide React
- **Testing**: Vitest + React Testing Library + Jest-DOM

## Pages

| Page | Route | What it shows |
|---|---|---|
| Overview | `/` | Demand, network peak, events by threat, alert breakdown |
| Alert Map | `/alerts/map` | Every transformer on the Cairo/Giza map, coloured by threat, with a details panel |
| Alert Log | `/alert-log` | Every event with start, end and duration; filters and CSV export |
| Transformers | `/transformers` | Search and sort the 500 transformers; hourly load and voltage charts with alert periods shaded |
| Accuracy | `/accuracy` | Precision, recall and F1 per threat; detected vs planted windows |
| Decision Center | `/decisions` | **Closed feature.** Recommended actions from the readings, configured for the company that operates the grid |

All filters live in the URL, so links between pages (e.g. map → alert log for one transformer) open already filtered.
Times are shown in UTC, the same time zone as the warehouse.

## Architecture

```
apps/frontend/src/
├── app/                 App shell, routes (pages load on demand), React Query provider
├── components/
│   ├── layout/          Sidebar and AppLayout
│   └── ui/              Shared pieces: Skeleton, ErrorCard, EmptyState, StatusBadge, Pagination, PageHeader
├── features/            One folder per page: api/, hooks/, types/, components/, pages/, __tests__/
│   ├── dashboard/       Overview
│   ├── map/             Alert Map
│   ├── alerts/          Alert Log
│   ├── transformers/    Transformers
│   ├── accuracy/        Accuracy
│   └── decisions/       Decision Center (locked)
├── lib/                 API client, formatters (UTC), event types and colours, date ranges, paging, CSV
├── styles/              Tailwind layers and the dark map tiles
└── test/                Test setup and render helpers
```

## Getting Started

### 1. Environment Setup

Copy `.env.example` to `.env`:

```bash
cp .env.example .env
```

Default configuration:
```ini
VITE_API_BASE_URL=http://localhost:5091
```

### 2. Install Dependencies

```bash
npm install
```

### 3. Run Development Server

```bash
npm run dev
```

The application will be accessible at `http://localhost:5173`.

### 4. Run Tests & Validation

```bash
# Run unit and integration tests
npm test

# Type checking
npx tsc --noEmit

# Production build
npm run build
```

## Backend Integration

The frontend talks only to the ASP.NET Core API at `VITE_API_BASE_URL`:

| Endpoint | Used by |
|---|---|
| `GET /api/dashboard/summary`, `/api/dashboard/demand` | Overview |
| `GET /api/grid/map-markers` | Alert Map |
| `GET /api/alerts`, `/api/alerts/summary` | Alert Log, Transformers |
| `GET /api/transformers`, `/api/transformers/{id}`, `/api/transformers/{id}/load-profile` | Transformers |
| `GET /api/accuracy`, `/api/accuracy/details` | Accuracy |

The full contract is in [`../backend/API_CONTRACT.md`](../backend/API_CONTRACT.md).

The default date range is `2026-08-01` to `2026-08-02`, the period covered by the data.
