{% snapshot meters_snapshot %}

{{
    config(
      target_database='SMART_GRID_DB',
      target_schema='CORE',
      unique_key='meter_id',
      strategy='check',
      check_cols=['transformer_id', 'tariff', 'customer_name', 'address']
    )
}}

-- One day of the grid at a time, so each meter appears once per run.
-- Run per day:  dbt snapshot --vars "{grid_date: 2026-08-01}"
-- No var given = the latest day loaded (what Airflow runs every day).
{% set grid_date = var('grid_date', none) %}

SELECT
    meter_id,
    transformer_id,
    tariff,
    customer_name,
    address,
    lat,
    lon,
    file_date
FROM {{ source('raw', 'grid_meters') }}
WHERE file_date =
    {% if grid_date %}
        '{{ grid_date }}'::DATE
    {% else %}
        (SELECT MAX(file_date) FROM {{ source('raw', 'grid_meters') }})
    {% endif %}

{% endsnapshot %}
