SELECT 
    meter_id,
    transformer_id,
    ts AS reading_ts,
    kw,
    kvar,
    voltage,
    kwh
FROM {{ source('raw', 'meter_readings') }}