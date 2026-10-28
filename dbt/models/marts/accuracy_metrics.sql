{{
    config(
        materialized='table'
    )
}}

-- Compares what we planted (sim_truth) with what the checks found (anomaly_flags),
-- one row per transformer + problem type.
-- silent_meter is left out: it's a data gap, not one of the 3 checks.

WITH truth AS (
    SELECT
        transformer_id,
        event_type,
        MIN(start_ts) AS truth_start_ts,
        MAX(end_ts)   AS truth_end_ts
    FROM {{ source('raw', 'sim_truth') }}
    WHERE event_type IN ('overload', 'line_drop', 'theft')
    GROUP BY transformer_id, event_type
),
detected AS (
    SELECT
        transformer_id,
        event_type,
        MIN(start_ts)     AS detected_start_ts,
        MAX(end_ts)       AS detected_end_ts,
        COUNT(anomaly_id) AS flag_rows
    FROM {{ ref('anomaly_flags') }}
    GROUP BY transformer_id, event_type
)
SELECT
    COALESCE(t.transformer_id, d.transformer_id) AS transformer_id,
    COALESCE(t.event_type, d.event_type)         AS event_type,
    t.truth_start_ts,
    t.truth_end_ts,
    d.detected_start_ts,
    d.detected_end_ts,
    COALESCE(d.flag_rows, 0)                     AS flag_rows,
    CASE
        WHEN t.transformer_id IS NOT NULL AND d.transformer_id IS NOT NULL THEN 'True Positive'
        WHEN t.transformer_id IS NOT NULL                                  THEN 'False Negative'
        ELSE 'False Positive'
    END                                          AS detection_status
FROM truth t
FULL OUTER JOIN detected d
    ON  t.transformer_id = d.transformer_id
    AND t.event_type     = d.event_type
