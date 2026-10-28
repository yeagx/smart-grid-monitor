{{
    config(
        materialized='table'
    )
}}

SELECT 
    f.transformer_id,
    d.district,
    d.capacity_kw,
    MAX(f.kw) AS peak_kw_recorded,
    ROUND((MAX(f.kw) / NULLIF(d.capacity_kw, 0)) * 100, 2) AS peak_load_percentage
FROM {{ ref('fact_transformer_readings') }} f
JOIN {{ ref('dim_transformers') }} d 
  ON f.transformer_id = d.transformer_id
GROUP BY f.transformer_id, d.district, d.capacity_kw