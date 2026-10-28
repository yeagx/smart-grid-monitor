{{
    config(
        materialized='table'
    )
}}

-- First add up all transformers per 15-min slot (the whole network),
-- then take the daily total and the daily peak of that network sum.
WITH per_slot AS (
    SELECT
        reading_ts,
        SUM(kw)  AS network_kw,
        SUM(kwh) AS network_kwh
    FROM {{ ref('fact_transformer_readings') }}
    GROUP BY reading_ts
)
SELECT
    DATE(reading_ts) AS demand_date,
    SUM(network_kwh) AS total_daily_kwh,
    MAX(network_kw)  AS peak_network_kw
FROM per_slot
GROUP BY DATE(reading_ts)
