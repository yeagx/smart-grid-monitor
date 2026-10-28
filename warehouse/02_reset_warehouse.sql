-- =====================================================================
-- RUN (as ACCOUNTADMIN) right after scripts/reset_lake.sh.
-- Empties the RAW tables and the SCD2 history so the full run starts clean.
-- TRUNCATE also clears Snowflake's "already loaded" memory, so COPY INTO
-- will load the new files even if they have the same names as old ones.
-- =====================================================================
USE ROLE ACCOUNTADMIN;
USE WAREHOUSE SMART_GRID_WH;
USE DATABASE SMART_GRID_DB;

TRUNCATE TABLE RAW.METER_READINGS;
TRUNCATE TABLE RAW.TRANSFORMER_READINGS;
TRUNCATE TABLE RAW.GRID_METERS;
TRUNCATE TABLE RAW.GRID_TRANSFORMERS;
TRUNCATE TABLE RAW.SIM_TRUTH;

-- Old SCD2 history was built from mixed days; dbt rebuilds it.
DROP TABLE IF EXISTS CORE.METERS_SNAPSHOT;
