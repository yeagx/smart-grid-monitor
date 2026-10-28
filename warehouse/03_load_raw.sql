-- =====================================================================
-- Loads the pipeline's S3 files into Snowflake. Airflow runs this every 15 minutes
-- (with DBT_ROLE); you can also run it by hand as ACCOUNTADMIN.
-- COPY INTO remembers which files it already loaded, so running it again
-- only loads NEW files (this is the "incremental batch" run).
-- =====================================================================
USE ROLE ACCOUNTADMIN;
USE WAREHOUSE SMART_GRID_WH;
USE DATABASE SMART_GRID_DB;

-- 1. Clean meter readings (from export/, written by Spark job 2)
COPY INTO RAW.METER_READINGS
FROM @RAW.STG_EXPORT
PATTERN = '.*meter_readings/.*[.]parquet'
FILE_FORMAT = (TYPE = PARQUET)
MATCH_BY_COLUMN_NAME = CASE_INSENSITIVE;

-- 2. Clean transformer readings (from export/, written by Spark job 2)
COPY INTO RAW.TRANSFORMER_READINGS
FROM @RAW.STG_EXPORT
PATTERN = '.*transformer_readings/.*[.]parquet'
FILE_FORMAT = (TYPE = PARQUET)
MATCH_BY_COLUMN_NAME = CASE_INSENSITIVE;

-- 3. Daily meter list, with the day taken from the file name
COPY INTO RAW.GRID_METERS
    (meter_id, transformer_id, tariff, customer_name, address, lat, lon, file_date)
FROM (
    SELECT $1, $2, $3, $4, $5, $6, $7,
           TO_DATE(REGEXP_SUBSTR(METADATA$FILENAME, '[0-9]{8}'), 'YYYYMMDD')
    FROM @RAW.STG_GRID
)
PATTERN = '.*meters_[0-9]{8}[.]csv'
FILE_FORMAT = (FORMAT_NAME = 'RAW.GRID_CSV_FMT');

-- 4. Daily transformer list, with the day taken from the file name
COPY INTO RAW.GRID_TRANSFORMERS
    (transformer_id, feeder_id, substation_id, district, governorate, capacity_kw, lat, lon, file_date)
FROM (
    SELECT $1, $2, $3, $4, $5, $6, $7, $8,
           TO_DATE(REGEXP_SUBSTR(METADATA$FILENAME, '[0-9]{8}'), 'YYYYMMDD')
    FROM @RAW.STG_GRID
)
PATTERN = '.*transformers_[0-9]{8}[.]csv'
FILE_FORMAT = (FORMAT_NAME = 'RAW.GRID_CSV_FMT');

-- 5. The answer key
COPY INTO RAW.SIM_TRUTH
FROM (
    SELECT
        $1:anomaly_id::VARCHAR,
        $1:event_type::VARCHAR,
        $1:entity_id::VARCHAR,
        $1:transformer_id::VARCHAR,
        $1:start_ts::TIMESTAMP_NTZ,
        $1:end_ts::TIMESTAMP_NTZ,
        $1:recorded_share::FLOAT,
        $1:factor::FLOAT,
        $1:voltage_factor::FLOAT
    FROM @RAW.STG_TRUTH
)
FILE_FORMAT = (FORMAT_NAME = 'RAW.TRUTH_JSON_FMT');

-- 6. Check what got loaded
SELECT 'meter_readings' AS table_name, COUNT(*) AS row_count FROM RAW.METER_READINGS
UNION ALL SELECT 'transformer_readings', COUNT(*) FROM RAW.TRANSFORMER_READINGS
UNION ALL SELECT 'grid_meters',          COUNT(*) FROM RAW.GRID_METERS
UNION ALL SELECT 'grid_transformers',    COUNT(*) FROM RAW.GRID_TRANSFORMERS
UNION ALL SELECT 'sim_truth',            COUNT(*) FROM RAW.SIM_TRUTH;

SELECT file_date, COUNT(*) AS meters FROM RAW.GRID_METERS GROUP BY file_date ORDER BY file_date;
