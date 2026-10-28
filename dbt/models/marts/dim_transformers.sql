-- Latest version of each transformer (transformers don't change between days).
SELECT
    transformer_id,
    feeder_id,
    substation_id,
    district,
    governorate,
    capacity_kw,
    lat,
    lon
FROM {{ source('raw', 'grid_transformers') }}
QUALIFY ROW_NUMBER() OVER (PARTITION BY transformer_id ORDER BY file_date DESC) = 1
