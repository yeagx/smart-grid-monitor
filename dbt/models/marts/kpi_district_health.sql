{{
    config(
        materialized='table'
    )
}}

-- kWh and anomalies are counted separately, then joined,
-- so a reading is never counted once per anomaly.

WITH consumption AS (
    SELECT
        d.district,
        d.governorate,
        COUNT(DISTINCT f.transformer_id) AS active_transformers,
        SUM(f.kwh)                       AS total_kwh_consumed
    FROM {{ ref('fact_transformer_readings') }} f
    JOIN {{ ref('dim_transformers') }} d
        ON f.transformer_id = d.transformer_id
    GROUP BY d.district, d.governorate
),
anomalies AS (
    SELECT
        d.district,
        COUNT(DISTINCT a.anomaly_id)                                   AS total_anomalies,
        COUNT(DISTINCT CASE WHEN a.event_type = 'theft'     THEN a.transformer_id END) AS theft_transformers,
        COUNT(DISTINCT CASE WHEN a.event_type = 'overload'  THEN a.transformer_id END) AS overload_transformers,
        COUNT(DISTINCT CASE WHEN a.event_type = 'line_drop' THEN a.transformer_id END) AS line_drop_transformers
    FROM {{ ref('anomaly_flags') }} a
    JOIN {{ ref('dim_transformers') }} d
        ON a.transformer_id = d.transformer_id
    GROUP BY d.district
)
SELECT
    c.district,
    c.governorate,
    c.active_transformers,
    c.total_kwh_consumed,
    COALESCE(a.total_anomalies, 0)        AS total_anomalies,
    COALESCE(a.theft_transformers, 0)     AS theft_transformers,
    COALESCE(a.overload_transformers, 0)  AS overload_transformers,
    COALESCE(a.line_drop_transformers, 0) AS line_drop_transformers
FROM consumption c
LEFT JOIN anomalies a
    ON c.district = a.district
