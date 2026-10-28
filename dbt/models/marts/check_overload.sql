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
transformer_loads AS (
    SELECT 
        f.transformer_id,
        f.reading_ts,
        f.kw AS current_kw,
        d.capacity_kw,
        (f.kw / NULLIF(d.capacity_kw, 0)) AS load_ratio
    FROM deduped_readings f
    JOIN {{ ref('dim_transformers') }} d 
      ON f.transformer_id = d.transformer_id
),
lagged_loads AS (
    SELECT 
        transformer_id,
        reading_ts AS end_ts,
        LAG(reading_ts) OVER (PARTITION BY transformer_id ORDER BY reading_ts) AS start_ts,
        load_ratio,
        LAG(load_ratio) OVER (PARTITION BY transformer_id ORDER BY reading_ts) AS prev_load_ratio
    FROM transformer_loads
)
SELECT 
    transformer_id || '-OVL-' || TO_CHAR(end_ts, 'YYYYMMDDHH24MISS') AS anomaly_id,
    'overload' AS event_type,
    transformer_id AS entity_id,
    transformer_id,
    start_ts,
    end_ts
FROM lagged_loads
WHERE load_ratio >= 0.85 
  AND prev_load_ratio >= 0.85

{% if is_incremental() %}
  AND end_ts > (SELECT MAX(end_ts) FROM {{ this }})
{% endif %}