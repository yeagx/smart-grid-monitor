# Smart Grid Monitor — API & Data Contract

## 1. Application Purpose

The Smart Grid Monitor web application exposes analytical results from an existing
data engineering pipeline that simulates electricity consumption for **100,000 meters**
connected to **500 transformers**.

The backend is a read-only analytics API. It does **not** re-implement detection
algorithms. All anomaly detection is performed by the dbt models in Snowflake.

---

## 2. Backend Project Structure

```
apps/backend/
├── SmartGrid.sln                           # Solution file
├── SmartGrid.slnx                          # .NET 10 solution (XML format)
│
├── src/
│   ├── SmartGrid.Api/                      # ASP.NET Core Web API
│   │   ├── Common/
│   │   │   └── DateRangeQueryParser.cs     # Query validation & parsing
│   │   ├── Controllers/
│   │   │   ├── HealthController.cs         # GET /api/health
│   │   │   └── DashboardController.cs      # GET /api/dashboard/summary & demand
│   │   ├── Program.cs                      # Dependency injection & pipeline
│   │   ├── appsettings.json
│   │   └── SmartGrid.Api.csproj
│   │
│   ├── SmartGrid.Application/             # DTOs, services, repository interfaces
│   │   ├── Abstractions/
│   │   │   ├── Persistence/               # Repository interfaces
│   │   │   └── Time/                      # IDateTimeProvider
│   │   ├── Common/
│   │   │   └── Models/                    # DateRange (with 366 max days), PageRequest, PagedResult<T>
│   │   ├── Dashboard/
│   │   │   ├── IDashboardService.cs       # Application service interface
│   │   │   ├── DashboardService.cs        # Application service implementation
│   │   │   └── Models/                    # DashboardOverviewDto, DailyDemandPointDto
│   │   ├── Alerts/
│   │   │   ├── Models/                    # AlertDto, AlertSummaryByTypeDto, EventTypes
│   │   │   └── Queries/                   # AlertSearchCriteria
│   │   ├── Transformers/
│   │   │   ├── Models/                    # TransformerListItemDto, TransformerDetailsDto, etc.
│   │   │   └── Queries/                   # Search, Peak, Reading criteria
│   │   ├── Accuracy/
│   │   │   └── Models/                    # AccuracyByTypeDto, AccuracyReportDto
│   │   └── SmartGrid.Application.csproj
│   │
│   └── SmartGrid.Infrastructure/          # Will contain mock & Snowflake implementations
│       └── SmartGrid.Infrastructure.csproj
│
├── tests/
│   ├── SmartGrid.Api.Tests/               # Controller & endpoint validation tests
│   │   └── SmartGrid.Api.Tests.csproj
│   └── SmartGrid.Application.Tests/       # Unit tests for shared models & services
│       └── SmartGrid.Application.Tests.csproj
│
└── API_CONTRACT.md                        # This document
```

---

## 3. Project Reference Rules

```
SmartGrid.Api
    ├── SmartGrid.Application
    └── SmartGrid.Infrastructure

SmartGrid.Infrastructure
    └── SmartGrid.Application

SmartGrid.Application
    └── (no project dependencies)

SmartGrid.Application.Tests
    └── SmartGrid.Application
```

**The Application project must never reference:**

- Snowflake drivers or SDKs
- AWS SDKs
- Entity Framework Core
- ASP.NET Core controller types
- Infrastructure implementation classes

---

## 4. Repository Interfaces

All interfaces live in `SmartGrid.Application.Abstractions.Persistence`.

| Interface                    | Responsibility                                     |
|------------------------------|---------------------------------------------------|
| `IDashboardReadRepository`   | Dashboard overview, daily demand series            |
| `IGridMapReadRepository`     | Geographic transformer markers, peak load & alerts |
| `IAlertReadRepository`       | Alert search, get-by-id, summary by type           |
| `ITransformerReadRepository` | Transformer search, details, peaks, readings       |
| `IAccuracyReadRepository`    | Detection accuracy report                          |

### Conventions

- All methods are asynchronous (`Task<T>`) and end with `Async`.
- All accept `CancellationToken`.
- Return Application DTOs — never raw database readers or SQL.
- Use nullable return types when a record may not exist (e.g. `GetByIdAsync`).
- No HTTP-specific types. No Snowflake-specific types.
- These are read-only repositories. No save/update/delete methods.

---

## 5. DTOs and Key Fields

### 5.1 Common Models

| Type             | Fields                               | Notes                               |
|------------------|--------------------------------------|-------------------------------------|
| `DateRange`      | `Start`, `End` (both `DateOnly`)     | Both inclusive. Start ≤ End enforced.|
| `PageRequest`    | `Page` (1-indexed), `PageSize`       | Max page size = 100. Validated.     |
| `PagedResult<T>` | `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages` | `Empty()` factory for empty results.|

### 5.2 Dashboard

| DTO                    | Key Fields                                                    | Source Model(s)                       |
|------------------------|---------------------------------------------------------------|---------------------------------------|
| `DailyDemandPointDto`  | `DemandDate`, `TotalDailyKwh`, `PeakNetworkKw`               | `kpi_daily_demand`                    |
| `DashboardOverviewDto` | `PeriodStart`, `PeriodEnd`, `TotalEnergyKwh`, `PeakNetworkKw`, `TotalAlerts`, `TheftTransformers`, `OverloadTransformers`, `LinedropTransformers`, `AffectedTransformers`, `LatestReadingTimestamp` | `kpi_daily_demand` + `anomaly_flags` + `dim_transformers` + `fact_transformer_readings` |

**Important:** `PeakNetworkKw` is the **maximum** recorded combined network power
across all time slots in the period — not the sum of daily peaks.

All values on `DashboardOverviewDto` are nullable because the warehouse may have no data.

### 5.3 Alerts

| DTO                     | Key Fields                                                    | Source Model(s)                    |
|-------------------------|---------------------------------------------------------------|------------------------------------|
| `AlertDto`              | `AnomalyId`, `EventType`, `EntityId`, `TransformerId`, `District`, `Governorate`, `StartTimestamp`, `EndTimestamp` | `anomaly_flags` JOIN `dim_transformers` |
| `AlertSummaryByTypeDto` | `EventType`, `TotalAlerts`, `AffectedTransformers`            | Aggregation over `anomaly_flags`   |

**Alert event types:** `theft`, `overload`, `line_drop` (case-sensitive, defined in `EventTypes`).

**Query:** `AlertSearchCriteria` — optional `Period`, `EventType`, `District`, `TransformerId` + required `Pagination`.

### 5.4 Transformers

| DTO                       | Key Fields                                                  | Source Model(s)                 |
|---------------------------|-------------------------------------------------------------|---------------------------------|
| `TransformerListItemDto`  | `TransformerId`, `District`, `Governorate`, `FeederId`, `SubstationId`, `CapacityKw`, `Latitude`, `Longitude` | `dim_transformers` |
| `TransformerDetailsDto`   | Same as list + extensible for detail page                   | `dim_transformers`              |
| `TransformerPeakDto`      | `TransformerId`, `District`, `CapacityKw`, `PeakKwRecorded`, `PeakLoadPercentage` | `kpi_transformer_peak` |
| `TransformerReadingDto`   | `TransformerId`, `ReadingTimestamp`, `ActivePowerKw`, `EnergyKwh`, `Voltage` | `fact_transformer_readings` |

**Queries:**

- `TransformerSearchCriteria` — optional `District`, `Governorate`, `FeederId`, `SubstationId` + required `Pagination`.
- `TransformerPeakCriteria` — optional `District` + required `Pagination`.
- `TransformerReadingCriteria` — required `TransformerId`, `Period`, `Pagination`.

### 5.5 Accuracy

| DTO                  | Key Fields                                                              | Source Model(s)      |
|----------------------|-------------------------------------------------------------------------|----------------------|
| `AccuracyByTypeDto`  | `EventType`, `TruePositives`, `FalsePositives`, `FalseNegatives`, `Precision`, `Recall` | `accuracy_summary` |
| `AccuracyReportDto`  | `ByType`, `TotalTruePositives`, `TotalFalsePositives`, `TotalFalseNegatives`, `OverallPrecision`, `OverallRecall` | Computed from `accuracy_summary` |

---

## 6. Date Filtering Behavior

| Repository Method           | Date Filter Behavior                                          |
|-----------------------------|---------------------------------------------------------------|
| Dashboard overview          | Required `DateRange`. Filters `kpi_daily_demand.demand_date` and `anomaly_flags.end_ts`. |
| Daily demand                | Required `DateRange`. Filters `kpi_daily_demand.demand_date`. |
| Alert search                | Optional `DateRange`. Filters on `anomaly_flags.end_ts`.      |
| Alert summary by type       | Required `DateRange`. Filters on `anomaly_flags.end_ts`.      |
| Transformer search          | No date filter (dimension table).                             |
| Transformer peaks           | **No date filter.** The source model has no date column.      |
| Transformer readings        | Required `DateRange`. Filters on `fact_transformer_readings.reading_ts`. |
| Accuracy report             | No date filter (global report).                               |

---

## 7. Pagination Conventions

- Pages are **1-indexed**.
- Minimum page size: 1. Maximum page size: **100**.
- Invalid values are rejected with `ArgumentOutOfRangeException` in `PageRequest`.
- `PagedResult<T>.TotalPages` is computed as ⌈TotalCount / PageSize⌉.
- `PagedResult<T>.Empty()` creates an empty result with zero items and zero total count.

---

## 8. Supported Alert Types

Exactly three event types, as defined in the dbt schema:

| Event Type   | dbt Source           | Description                                            |
|--------------|----------------------|--------------------------------------------------------|
| `theft`      | `check_theft.sql`    | >11% energy loss in rolling 4h window, ≥95% meter readings present |
| `overload`   | `check_overload.sql` | Load ratio ≥ 85% for 2 consecutive readings (30 min)  |
| `line_drop`  | `check_line_drop.sql`| Voltage < 207V for 4 consecutive readings (1 hour)    |

Event type values are **case-sensitive** and match the lowercase strings in the warehouse.

---

## 9. Units

| Quantity     | Unit    | Used In                                            |
|--------------|---------|----------------------------------------------------|
| Power        | kW      | `DailyDemandPointDto.PeakNetworkKw`, `TransformerPeakDto.PeakKwRecorded`, `TransformerReadingDto.ActivePowerKw`, `TransformerListItemDto.CapacityKw` |
| Energy       | kWh     | `DailyDemandPointDto.TotalDailyKwh`, `TransformerReadingDto.EnergyKwh`, `DashboardOverviewDto.TotalEnergyKwh` |
| Voltage      | V       | `TransformerReadingDto.Voltage`                     |
| Load         | %       | `TransformerPeakDto.PeakLoadPercentage`             |
| Precision    | ratio   | `AccuracyByTypeDto.Precision` (0.0–1.0)            |
| Recall       | ratio   | `AccuracyByTypeDto.Recall` (0.0–1.0)               |

No unit conversions are performed in the Application layer.

---

## 10. Warehouse Model → DTO Mapping

| Warehouse Model               | Snowflake Columns                                          | C# DTO                     |
|-------------------------------|------------------------------------------------------------|-----------------------------|
| `kpi_daily_demand`            | `demand_date`, `total_daily_kwh`, `peak_network_kw`        | `DailyDemandPointDto`       |
| `kpi_transformer_peak`        | `transformer_id`, `district`, `capacity_kw`, `peak_kw_recorded`, `peak_load_percentage` | `TransformerPeakDto` |
| `dim_transformers`            | `transformer_id`, `feeder_id`, `substation_id`, `district`, `governorate`, `capacity_kw`, `lat`, `lon` | `TransformerListItemDto`, `TransformerDetailsDto` |
| `fact_transformer_readings`   | `transformer_id`, `reading_ts`, `kw`, `kwh`, `voltage`    | `TransformerReadingDto`     |
| `anomaly_flags`               | `anomaly_id`, `event_type`, `entity_id`, `transformer_id`, `start_ts`, `end_ts` | `AlertDto` (partial)   |
| `accuracy_summary`            | `event_type`, `true_positives`, `false_positives`, `false_negatives`, `precision`, `recall` | `AccuracyByTypeDto` |

---

## 11. Required Joins

### Alerts ↔ Transformer Metadata

The `anomaly_flags` model does **not** contain `district` or `governorate`.
These fields must be obtained by joining:

```sql
anomaly_flags.transformer_id → dim_transformers.transformer_id
```

The Snowflake/mock repository implementation is responsible for performing this join
and populating `AlertDto.District` and `AlertDto.Governorate`.

### Dashboard Overview

The dashboard overview aggregates from multiple tables:

- Energy/power: `kpi_daily_demand` filtered by `demand_date`
- Alert counts: `anomaly_flags` filtered by `end_ts`, with district via `dim_transformers`
- Freshness: `MAX(reading_ts)` from `fact_transformer_readings`

---

## 12. Transformer Peaks vs. Date-Filtered Demand

These are two fundamentally different metrics:

| Metric                    | Source                    | Date Filter | Meaning                              |
|---------------------------|---------------------------|-------------|--------------------------------------|
| Daily demand peak         | `kpi_daily_demand`        | ✅ Yes      | Network-wide peak kW on a given day  |
| Transformer historical peak | `kpi_transformer_peak`  | ❌ No       | Highest kW ever recorded for a transformer across **all** available data |

The `kpi_transformer_peak` model has no date column. Querying "peak for the last 7 days"
is not supported by this model. A date-filtered peak would require querying
`fact_transformer_readings` directly.

---

## 13. Accuracy Metric Calculations

Overall precision and recall are computed from **aggregate counts**, never by averaging
per-type percentages.

```
Overall Precision = Total TP / (Total TP + Total FP)
Overall Recall    = Total TP / (Total TP + Total FN)
```

Both are **nullable** — `null` when the denominator is zero (e.g., no flags raised at all).

Per-type precision and recall come directly from `accuracy_summary` and are also nullable
for zero-denominator cases.

---

## 14. Mappings Requiring Live Snowflake Verification

The following have been derived from dbt SQL but need confirmation against the actual
Snowflake schema:

| Item | What to Verify |
|------|---------------|
| Column types | `demand_date` — is it `DATE` or `TIMESTAMP`? C# maps to `DateOnly`. |
| Column types | `capacity_kw` — is it `NUMBER(x,y)` or `FLOAT`? C# maps to `decimal`. |
| Column types | `lat`, `lon` — are they `FLOAT` or `NUMBER`? C# maps to `double`. |
| Column types | `peak_load_percentage` — verify `ROUND(..., 2)` returns `NUMBER(x,2)`. |
| Column types | `kw`, `kwh`, `voltage` — verify numeric types in `transformer_readings` raw source. |
| Column types | `precision`, `recall` in `accuracy_summary` — verify `ROUND(..., 3)` produces `NUMBER(x,3)`. |
| Null handling | Can `kpi_daily_demand` rows have NULL `peak_network_kw`? |
| Timestamp TZ | `reading_ts`, `start_ts`, `end_ts` — `TIMESTAMP_NTZ` or `TIMESTAMP_TZ`? |
| Row counts | `dim_transformers` — confirm exactly 500 rows in current data. |
| Anomaly IDs | Verify format matches pattern `{transformer_id}-{type_code}-{timestamp}`. |

Verified against the live warehouse: see [SNOWFLAKE_SETUP.md](SNOWFLAKE_SETUP.md#3-verified-schema-answers-to-api_contract-section-14).

---

## 15. Mock & Snowflake Provider Strategy

Both providers implement the same repository interfaces from `SmartGrid.Application.Abstractions.Persistence`.

### 15.1 Provider Selection Configuration

Provider selection is configured centrally in `appsettings.json` or environment variables:

```json
{
  "DataSource": {
    "Provider": "Mock"
  },
  "ConnectionStrings": {
    "Snowflake": ""
  }
}
```

- Supported `DataSource:Provider` values: `"Mock"` (default) and `"Snowflake"`.
- When set to `"Mock"`: registers `MockDashboardRepository`.
- When set to `"Snowflake"`: reads `ConnectionStrings:Snowflake`. If missing or empty, throws a clear `InvalidOperationException`. Registers `SnowflakeDashboardRepository`.
- Rejects any unknown provider name during startup with an informative configuration error.
- Registered in `Program.cs` via:
  ```csharp
  builder.Services.AddSmartGridInfrastructure(builder.Configuration);
  ```

### 15.2 Protecting Snowflake Credentials

- The repository does **not** commit real connection strings to source control.
- In local development, configure user secrets for `SmartGrid.Api`:
  ```bash
  cd apps/backend/src/SmartGrid.Api
  dotnet user-secrets set "ConnectionStrings:Snowflake" "<private-connection-string>"
  ```
- In production/CI, supply through environment variable `ConnectionStrings__Snowflake`.

### 15.3 Mock Dashboard Provider

- Located in `SmartGrid.Infrastructure.Repositories.MockDashboardRepository`.
- Uses deterministic, internally consistent fixtures in `SmartGrid.Infrastructure.Data.MockDashboardData`.
- Simulation dates: August 1 and August 2, 2026.
- Dynamic aggregation ensures totals and distinct counts agree across summary and demand endpoints.
- When an empty range is queried, returns `null` for overview metrics and empty collection `[]` for daily demand.

### 15.4 Snowflake Dashboard Provider

- Located in `SmartGrid.Infrastructure.Repositories.SnowflakeDashboardRepository`.
- Uses official driver `Snowflake.Data` (v6.4.0).
- Uses parameterized queries (`:startDate`, `:endDate`) against:
  - `KPI_DAILY_DEMAND`
  - `ANOMALY_FLAGS`
  - `FACT_TRANSFORMER_READINGS`
- Does not open a database connection during service provider construction.
- Timestamps are explicitly treated as UTC.
- Cancellation tokens are propagated to driver operations.
- Errors are not swallowed into fake successful results.

---

## 16. Timestamp Convention

All `DateTimeOffset` values at the API boundary represent **UTC** timestamps.

The `reading_ts` column in the warehouse stores the start of each 15-minute slot in UTC.

---

## 17. Health Endpoint

```
GET /api/health
```

Returns:

```json
{
  "status": "healthy",
  "timestamp": "2024-05-15T12:00:00+00:00",
  "service": "SmartGrid.Api",
  "dataProvider": "Snowflake"
}
```

This confirms the API process is running and which provider is active (`Mock` or `Snowflake`).
It does **not** open a Snowflake connection.

---

## 18. Dashboard Endpoints (Milestone 2)

### 18.1 GET /api/dashboard/summary

Retrieves overview metrics for the specified inclusive date range.

#### Query Parameters

| Parameter | Type   | Required | Format       | Description                     |
|-----------|--------|----------|--------------|---------------------------------|
| `from`    | string | Yes      | `YYYY-MM-DD` | Inclusive start of query window |
| `to`      | string | Yes      | `YYYY-MM-DD` | Inclusive end of query window   |

#### Success Response (HTTP 200 OK)

```json
{
  "periodStart": "2026-08-01",
  "periodEnd": "2026-08-02",
  "totalEnergyKwh": 125430.75,
  "peakNetworkKw": 8450.20,
  "totalAlerts": 14,
  "theftTransformers": 3,
  "overloadTransformers": 5,
  "linedropTransformers": 2,
  "affectedTransformers": 8,
  "latestReadingTimestamp": "2026-08-02T23:45:00+00:00"
}
```

*Note: All metric fields are nullable when warehouse records are not present for the requested period.*

---

### 18.2 GET /api/dashboard/demand

Retrieves daily energy delivery and peak network power for each day within the requested inclusive date range, ordered by `demandDate` ascending.

#### Query Parameters

| Parameter | Type   | Required | Format       | Description                     |
|-----------|--------|----------|--------------|---------------------------------|
| `from`    | string | Yes      | `YYYY-MM-DD` | Inclusive start of query window |
| `to`      | string | Yes      | `YYYY-MM-DD` | Inclusive end of query window   |

#### Success Response (HTTP 200 OK)

```json
[
  {
    "demandDate": "2026-08-01",
    "totalDailyKwh": 61200.50,
    "peakNetworkKw": 8320.10
  },
  {
    "demandDate": "2026-08-02",
    "totalDailyKwh": 64230.25,
    "peakNetworkKw": 8450.20
  }
]
```

---

### 18.3 Validation and Error Responses

Both endpoints validate query parameters strictly. Any violation returns **HTTP 400 Bad Request** with an RFC 7807 `ProblemDetails` object:

```json
{
  "type": null,
  "title": "Invalid Query Parameters",
  "status": 400,
  "detail": "The 'from' query parameter is required.",
  "instance": "/api/dashboard/summary"
}
```

#### Enforced Validation Rules:
1. **Missing parameters**: Both `from` and `to` are required.
2. **Date format**: Both dates must strictly follow the ISO 8601 `YYYY-MM-DD` format.
3. **Ordering**: `from` must be earlier than or equal to `to`.
4. **Range limit**: The maximum supported query range is **366 days** (inclusive).
5. **No 500s on bad input**: All bad user inputs are caught and mapped to HTTP 400.
6. **No fabricated data**: In the event of a repository exception (e.g. database error), the exception propagates rather than returning fabricated defaults.

---

### 18.4 Provider Integration & Testing Guide

The dashboard data integration is fully wired across the Application, Infrastructure, and API layers:

1. **Mock Provider**: Fully implemented in `MockDashboardRepository` with deterministic August 1–2, 2026 fixtures.
2. **Snowflake Provider**: Fully implemented in `SnowflakeDashboardRepository` with parameterized SQL for `KPI_DAILY_DEMAND`, `ANOMALY_FLAGS`, and `FACT_TRANSFORMER_READINGS`.
3. **DI Registration**: Managed centrally via `builder.Services.AddSmartGridInfrastructure(builder.Configuration)`.

#### Running Tests

From the repository root or backend solution directory:

```bash
# Run all unit and integration tests (uses Mock provider, no Snowflake credentials required)
dotnet test apps/backend/SmartGrid.sln
```

#### Optional Live Snowflake Smoke Test

Live queries against a real Snowflake account are opt-in to avoid blocking CI or local environments without credentials. To run:

```bash
export RUN_SNOWFLAKE_INTEGRATION_TESTS=true
export SNOWFLAKE_CONNECTION_STRING="account=<org-account>;user=<user>;password=<password>;db=SMART_GRID;schema=MARTS;warehouse=COMPUTE_WH"

dotnet test apps/backend/tests/SmartGrid.Api.Tests/ --filter "FullyQualifiedName~SnowflakeLiveSmokeTests"
```

#### Verified Facts vs Live Warehouse Assumptions

- **Verified Facts**:
  - `KPI_DAILY_DEMAND` provides pre-aggregated `demand_date`, `total_daily_kwh`, `peak_network_kw`.
  - `ANOMALY_FLAGS` contains `event_type`, `transformer_id`, and `end_ts`.
  - Distinct affected transformers across multiple dates cannot be computed by summing daily counts; `COUNT(DISTINCT transformer_id)` in SQL or in-memory sets is required.
  - Energy is in kWh and power is in kW across both SQL models and C# DTOs (no conversion needed).
  - All timestamps at the API boundary are formatted in UTC (`DateTimeOffset`).
- **Live Warehouse Assumptions to Confirm with Credentials**:
  - The default database and schema in the Snowflake connection string should point to `SMART_GRID.MARTS`.
  - If table names are case-sensitive in Snowflake (unquoted identifiers default to uppercase), ensure Snowflake session settings resolve `KPI_DAILY_DEMAND`, `ANOMALY_FLAGS`, and `FACT_TRANSFORMER_READINGS`.

---

## 19. Grid Map Markers API & Data Contract

### 19.1 Endpoint Specification

```http
GET /api/grid/map-markers
```

Retrieves geographic transformer markers enriched with administrative hierarchy, nameplate capacity, historical peak loading, and period-specific anomaly alert window aggregations.

#### Query Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `from` | `YYYY-MM-DD` | **Yes** | Inclusive calendar start date. |
| `to` | `YYYY-MM-DD` | **Yes** | Inclusive calendar end date. |
| `district` | `string` | No | Case-insensitive filter matching `DIM_TRANSFORMERS.DISTRICT` (e.g. `Dokki`, `Nasr City`, `Maadi`, `Heliopolis`, `Shubra`). |
| `type` | `string` | No | Threat type filter. Must be exactly `theft`, `overload`, or `line_drop`. Unsupported values return HTTP 400. |
| `onlyWithAlerts` | `boolean` | No | When `true`, returns only transformers with qualifying alerts in the period. Default is `false`. |

#### Validation Rules

- Both `from` and `to` are required and must conform to `YYYY-MM-DD`.
- `from` must be earlier than or equal to `to`.
- Maximum query range is **366 calendar days** (inclusive).
- `type` must belong to the supported event types (`theft`, `overload`, `line_drop`). Invalid values return HTTP 400 with `ProblemDetails`.

---

### 19.2 MapMarkerDto Response Structure

Each item in the returned array represents one distinct transformer:

```json
[
  {
    "transformerId": "TR-0012",
    "district": "Dokki",
    "governorate": "Giza",
    "feederId": "F-01",
    "substationId": "SS-DOKKI",
    "latitude": 30.0384,
    "longitude": 31.2109,
    "capacityKw": 500.0,
    "peakKwRecorded": 420.50,
    "peakLoadPercentage": 84.10,
    "theftAlertCount": 2,
    "overloadAlertCount": 0,
    "lineDropAlertCount": 0,
    "totalAlertCount": 2,
    "latestAlertAt": "2026-08-02T07:00:00+00:00"
  }
]
```

#### Field Specifications

| Field | Type | Description |
|---|---|---|
| `transformerId` | `string` | Unique transformer identifier (from `DIM_TRANSFORMERS`). |
| `district` | `string` | Administrative district. |
| `governorate` | `string` | Administrative governorate. |
| `feederId` | `string` | Connected feeder ID. |
| `substationId` | `string` | Connected substation ID. |
| `latitude` | `double` | WGS 84 latitude coordinate. |
| `longitude` | `double` | WGS 84 longitude coordinate. |
| `capacityKw` | `decimal` | Nameplate capacity in kilowatts (kW). |
| `peakKwRecorded` | `decimal?` | Historical maximum active power recorded across all available readings (kW). Null if unrecorded. |
| `peakLoadPercentage` | `decimal?` | Historical maximum load percentage relative to capacity (%). Null if unrecorded. |
| `theftAlertCount` | `int` | Number of theft alert windows overlapping the requested period. |
| `overloadAlertCount` | `int` | Number of overload alert windows overlapping the requested period. |
| `lineDropAlertCount` | `int` | Number of line drop (voltage anomaly) alert windows overlapping the requested period. |
| `totalAlertCount` | `int` | Sum of all alert windows in the period (`theft` + `overload` + `line_drop`). |
| `latestAlertAt` | `DateTimeOffset?` | UTC end timestamp of the latest qualifying alert in the period. Null if 0 alerts. |

---

### 19.3 Date & Timestamp Semantics

Date parameters are interpreted as UTC boundary intervals:
- Start boundary: `from` date at `00:00:00 UTC` (inclusive).
- End boundary: Day after `to` date at `00:00:00 UTC` (exclusive).

An alert window overlaps the period if:
```sql
alert.start_ts < periodEndExclusive AND alert.end_ts >= periodStartInclusive
```
This ensures multi-hour alerts or alerts crossing midnight boundaries are accurately accounted for.

---

### 19.4 Coordinate Validation Policy

Transformers without valid geographic coordinates are never returned as usable map markers:
- Latitude must be between `-90.0` and `+90.0`.
- Longitude must be between `-180.0` and `+180.0`.
- Null Island coordinates (`lat = 0.0` and `lon = 0.0`), `NaN`, and infinities are rejected.

---

### 19.5 Filtering and Aggregation Semantics

1. **Distinct Transformers**: Each qualifying transformer appears at most once in the response.
2. **Alert Windows vs Transformers**: Alert counts represent the number of detected anomaly alert windows, not distinct transformers.
3. **Preserved Complete Profile**: Even when a specific threat `type` filter is active, the per-type counts (`theftAlertCount`, `overloadAlertCount`, `lineDropAlertCount`) on each returned marker reflect the full alert profile in the period.
4. **`onlyWithAlerts` Behavior**:
   - When `onlyWithAlerts=true`: only transformers with at least one matching alert are returned. If a `type` filter is active, the transformer must have alerts of that specific type.
   - When `onlyWithAlerts=false`: transformers without alerts in the period still appear (with counts = 0 and `latestAlertAt = null`). If a `type` filter is active, non-alert transformers appear alongside transformers matching the specified threat type.
5. **Historical Peak Metrics**: `peakKwRecorded` and `peakLoadPercentage` are sourced from `KPI_TRANSFORMER_PEAK` and represent historical maxima over all available warehouse records. They are explicitly not real-time loads.

---

## 20. Alert Log, Transformers & Accuracy APIs

All endpoints return camelCase JSON; timestamps are ISO 8601 UTC. Paged endpoints return
`{ items, page, pageSize, totalCount, totalPages }` (`page` ≥ 1, `pageSize` 1–100, default 25).
Invalid query values return `400` ProblemDetails; unknown IDs return `404`.

### Alerts (`MARTS.ANOMALY_FLAGS` ⋈ `DIM_TRANSFORMERS`)

| Endpoint | Query | Notes |
|---|---|---|
| `GET /api/alerts` | `from`, `to` (optional, both or neither), `type`, `district`, `transformerId` (partial match), `page`, `pageSize` | Newest first. An alert is in the period when its window overlaps it (same rule as the map). |
| `GET /api/alerts/summary` | `from`, `to` (required) | Always 3 rows: `{ eventType, totalAlerts, affectedTransformers }`. |
| `GET /api/alerts/{anomalyId}` | — | One `AlertDto`. |

### Transformers (`DIM_TRANSFORMERS`, `KPI_TRANSFORMER_PEAK`, `FACT_TRANSFORMER_READINGS`)

| Endpoint | Query | Notes |
|---|---|---|
| `GET /api/transformers` | `search`, `district`, `governorate`, `feederId`, `substationId`, `onlyWithAlerts`, `sort` = `id` \| `alerts` \| `load`, `page`, `pageSize` | `alertCount` is all-time; invalid coordinates are excluded (same rule as the map). |
| `GET /api/transformers/peaks` | `district`, `page`, `pageSize` | Highest `peakLoadPercentage` first. |
| `GET /api/transformers/{id}` | — | `TransformerDetailsDto`. |
| `GET /api/transformers/{id}/load-profile` | `from`, `to` (max 31 days) | Hourly buckets: `{ hourTimestamp, avgKw, maxKw, energyKwh, avgVoltage, minVoltage, readingCount }`. |
| `GET /api/transformers/{id}/readings` | `from`, `to`, `page`, `pageSize` | Raw 15-minute readings, oldest first. |

### Accuracy (`ACCURACY_SUMMARY`, `ACCURACY_METRICS`)

| Endpoint | Query | Notes |
|---|---|---|
| `GET /api/accuracy` | — | Per type + overall TP/FP/FN. `precision` / `recall` are **0–1 ratios recomputed from the counts** (null when undefined). |
| `GET /api/accuracy/details` | `type`, `status` (`True Positive` \| `False Positive` \| `False Negative`), `transformerId`, `page`, `pageSize` | Truth window vs detected window per row. |
