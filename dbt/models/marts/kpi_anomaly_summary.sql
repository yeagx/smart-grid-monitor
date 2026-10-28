{{
    config(
        materialized='table'
    )
}}

SELECT 
    DATE(end_ts) AS anomaly_date,
    event_type,
    COUNT(anomaly_id) AS total_events,
    COUNT(DISTINCT transformer_id) AS affected_transformers
FROM {{ ref('anomaly_flags') }}
GROUP BY DATE(end_ts), event_type
ORDER BY anomaly_date DESC, event_type