SELECT 
    meter_id,
    transformer_id,
    tariff,
    customer_name,
    address,
    lat,
    lon,
    dbt_valid_from,
    dbt_valid_to,
    CASE WHEN dbt_valid_to IS NULL THEN TRUE ELSE FALSE END AS is_current
FROM {{ ref('meters_snapshot') }}