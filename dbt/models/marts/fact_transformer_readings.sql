SELECT 
    transformer_id,
    ts AS reading_ts,
    kw,
    kwh,
    voltage
FROM {{ source('raw', 'transformer_readings') }}