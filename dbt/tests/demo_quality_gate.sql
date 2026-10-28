{{ config(tags=['demo'], severity='error') }}

-- FAILS ON PURPOSE, only for the demo of the quality gate.
-- Rule: no transformer may go above 100% of its capacity.
-- The planted overload breaks this rule, so the test fails and the pipeline stops.
-- Normal runs skip it:   dbt test --exclude tag:demo
-- Demo:                  dbt test --select tag:demo

SELECT
    f.transformer_id,
    f.reading_ts,
    f.kw,
    d.capacity_kw,
    ROUND(f.kw / d.capacity_kw * 100, 1) AS load_pct
FROM {{ ref('fact_transformer_readings') }} f
JOIN {{ ref('dim_transformers') }} d
    ON f.transformer_id = d.transformer_id
WHERE f.kw > d.capacity_kw
