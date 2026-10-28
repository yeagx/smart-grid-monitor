{{
    config(
        materialized='incremental',
        unique_key='anomaly_id'
    )
}}

WITH deduped_transformers AS (
    SELECT * FROM {{ ref('fact_transformer_readings') }}
    QUALIFY ROW_NUMBER() OVER (PARTITION BY transformer_id, reading_ts ORDER BY reading_ts) = 1
),
deduped_meters AS (
    SELECT * FROM {{ ref('fact_meter_readings') }}
    QUALIFY ROW_NUMBER() OVER (PARTITION BY meter_id, reading_ts ORDER BY reading_ts) = 1
),
meter_sums AS (
    SELECT 
        transformer_id,
        reading_ts,
        SUM(kwh) AS homes_kwh,
        COUNT(meter_id) AS actual_readings_count
    FROM deduped_meters
    GROUP BY transformer_id, reading_ts
),
joined_readings AS (
    SELECT 
        t.transformer_id,
        t.reading_ts,
        t.kwh AS transformer_kwh,
        m.homes_kwh,
        m.actual_readings_count,
        (SELECT COUNT(meter_id) FROM {{ ref('dim_meters') }} dm WHERE dm.transformer_id = t.transformer_id AND dm.is_current = TRUE) AS expected_meters
    FROM deduped_transformers t
    JOIN meter_sums m 
      ON t.transformer_id = m.transformer_id AND t.reading_ts = m.reading_ts
),
rolling_4h AS (
    SELECT 
        transformer_id,
        reading_ts AS end_ts,
        LAG(reading_ts, 15) OVER (PARTITION BY transformer_id ORDER BY reading_ts) AS start_ts,
        SUM(transformer_kwh) OVER (PARTITION BY transformer_id ORDER BY reading_ts ROWS BETWEEN 15 PRECEDING AND CURRENT ROW) AS total_sent,
        SUM(homes_kwh) OVER (PARTITION BY transformer_id ORDER BY reading_ts ROWS BETWEEN 15 PRECEDING AND CURRENT ROW) AS total_recorded,
        SUM(actual_readings_count) OVER (PARTITION BY transformer_id ORDER BY reading_ts ROWS BETWEEN 15 PRECEDING AND CURRENT ROW) AS total_actual_readings,
        SUM(expected_meters) OVER (PARTITION BY transformer_id ORDER BY reading_ts ROWS BETWEEN 15 PRECEDING AND CURRENT ROW) AS total_expected_readings
    FROM joined_readings
)
SELECT 
    transformer_id || '-THF-' || TO_CHAR(end_ts, 'YYYYMMDDHH24MISS') AS anomaly_id,
    'theft' AS event_type,
    transformer_id AS entity_id,
    transformer_id,
    start_ts,
    end_ts
FROM rolling_4h
WHERE start_ts IS NOT NULL
  AND (total_sent - total_recorded) / NULLIF(total_sent, 0) > 0.11
  AND total_actual_readings / NULLIF(total_expected_readings, 0) >= 0.95

{% if is_incremental() %}
  AND end_ts > (SELECT MAX(end_ts) FROM {{ this }})
{% endif %}