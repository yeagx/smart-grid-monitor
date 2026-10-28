{{
    config(
        materialized='table'
    )
}}

-- Precision = of everything we flagged, how much was real.
-- Recall    = of everything we planted, how much we caught.
SELECT
    event_type,
    COUNT_IF(detection_status = 'True Positive')  AS true_positives,
    COUNT_IF(detection_status = 'False Positive') AS false_positives,
    COUNT_IF(detection_status = 'False Negative') AS false_negatives,
    ROUND(true_positives / NULLIF(true_positives + false_positives, 0), 3) AS precision,
    ROUND(true_positives / NULLIF(true_positives + false_negatives, 0), 3) AS recall
FROM {{ ref('accuracy_metrics') }}
GROUP BY event_type
