{{
    config(
        materialized='incremental',
        unique_key='anomaly_id'
    )
}}

WITH deduped_readings AS (
    SELECT *
    FROM {{ ref('fact_transformer_readings') }}
    QUALIFY ROW_NUMBER() OVER (PARTITION BY transformer_id, reading_ts ORDER BY reading_ts) = 1
),
voltage_drops AS (
    SELECT 
        transformer_id,
        reading_ts AS end_ts,
        LAG(reading_ts, 3) OVER (PARTITION BY transformer_id ORDER BY reading_ts) AS start_ts,
        voltage,
        LAG(voltage, 1) OVER (PARTITION BY transformer_id ORDER BY reading_ts) AS v_minus_1,
        LAG(voltage, 2) OVER (PARTITION BY transformer_id ORDER BY reading_ts) AS v_minus_2,
        LAG(voltage, 3) OVER (PARTITION BY transformer_id ORDER BY reading_ts) AS v_minus_3
    FROM deduped_readings
)
SELECT 
    transformer_id || '-LND-' || TO_CHAR(end_ts, 'YYYYMMDDHH24MISS') AS anomaly_id,
    'line_drop' AS event_type,
    transformer_id AS entity_id,
    transformer_id,
    start_ts,
    end_ts
FROM voltage_drops
WHERE voltage < 207
  AND v_minus_1 < 207
  AND v_minus_2 < 207
  AND v_minus_3 < 207

{% if is_incremental() %}
  AND end_ts > (SELECT MAX(end_ts) FROM {{ this }})
{% endif %}