{{
    config(
        materialized='view'
    )
}}

SELECT * FROM {{ ref('check_overload') }}
UNION ALL
SELECT * FROM {{ ref('check_line_drop') }}
UNION ALL
SELECT * FROM {{ ref('check_theft') }}