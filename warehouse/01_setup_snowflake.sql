-- =====================================================================
-- SNOWFLAKE SETUP: everything the pipeline needs, from zero (run once).
-- Run it in 3 steps (A, B, C). Between A and B you update the AWS role.
-- Before you start, replace in this file:
--   <your-aws-account-id>   your 12-digit AWS account number
--   sic-grad-project        your bucket name, if you use a different one
-- =====================================================================


-- =====================================================================
-- STEP A: warehouse, database, schemas, and the link to S3
-- Select from here down to "END OF STEP A" and click Run.
-- =====================================================================
USE ROLE ACCOUNTADMIN;

CREATE WAREHOUSE IF NOT EXISTS SMART_GRID_WH
    WAREHOUSE_SIZE = 'XSMALL'
    AUTO_SUSPEND = 60            -- sleeps after 60 s of no work, so it stops using credits
    AUTO_RESUME = TRUE
    INITIALLY_SUSPENDED = TRUE;

CREATE DATABASE IF NOT EXISTS SMART_GRID_DB;
CREATE SCHEMA IF NOT EXISTS SMART_GRID_DB.RAW   COMMENT = 'Raw data loaded from S3';
CREATE SCHEMA IF NOT EXISTS SMART_GRID_DB.CORE  COMMENT = 'SCD2 history (dbt snapshot)';
CREATE SCHEMA IF NOT EXISTS SMART_GRID_DB.MARTS COMMENT = 'Gold: star schema, checks, KPIs';

-- The link to the bucket (only these 3 folders)
CREATE STORAGE INTEGRATION IF NOT EXISTS s3_int
    TYPE = EXTERNAL_STAGE
    STORAGE_PROVIDER = 'S3'
    ENABLED = TRUE
    STORAGE_AWS_ROLE_ARN = 'arn:aws:iam::<your-aws-account-id>:role/snowflake-s3-role'
    STORAGE_ALLOWED_LOCATIONS = ('s3://sic-grad-project/export/',
                                 's3://sic-grad-project/grid/',
                                 's3://sic-grad-project/truth/');

-- Copy 2 values from the result into the AWS role's trust policy:
--   STORAGE_AWS_IAM_USER_ARN  and  STORAGE_AWS_EXTERNAL_ID
DESC INTEGRATION s3_int;
-- ========================= END OF STEP A =============================
-- Now update the AWS trust policy (RUN_GUIDE.md, step 4.3), wait 1 minute,
-- then run STEP B. Never run CREATE OR REPLACE on s3_int: it changes the values.


-- =====================================================================
-- STEP B: file formats, stages, raw tables
-- =====================================================================
USE ROLE ACCOUNTADMIN;
USE WAREHOUSE SMART_GRID_WH;
USE DATABASE SMART_GRID_DB;
USE SCHEMA RAW;

CREATE FILE FORMAT IF NOT EXISTS RAW.PARQUET_FMT    TYPE = PARQUET;
CREATE FILE FORMAT IF NOT EXISTS RAW.GRID_CSV_FMT   TYPE = CSV SKIP_HEADER = 1
    FIELD_OPTIONALLY_ENCLOSED_BY = '"' EMPTY_FIELD_AS_NULL = TRUE;
CREATE FILE FORMAT IF NOT EXISTS RAW.TRUTH_JSON_FMT TYPE = JSON;

CREATE STAGE IF NOT EXISTS RAW.STG_EXPORT URL = 's3://sic-grad-project/export/'
    STORAGE_INTEGRATION = s3_int FILE_FORMAT = RAW.PARQUET_FMT;
CREATE STAGE IF NOT EXISTS RAW.STG_GRID   URL = 's3://sic-grad-project/grid/'
    STORAGE_INTEGRATION = s3_int FILE_FORMAT = RAW.GRID_CSV_FMT;
CREATE STAGE IF NOT EXISTS RAW.STG_TRUTH  URL = 's3://sic-grad-project/truth/'
    STORAGE_INTEGRATION = s3_int FILE_FORMAT = RAW.TRUTH_JSON_FMT;

-- These 3 must run WITHOUT errors (0 files is fine before the first run).
-- An error here = the AWS trust policy step is not right yet.
LIST @RAW.STG_EXPORT;
LIST @RAW.STG_GRID;
LIST @RAW.STG_TRUTH;

CREATE TABLE IF NOT EXISTS RAW.METER_READINGS (
    meter_id VARCHAR, transformer_id VARCHAR, ts TIMESTAMP_NTZ,
    kw FLOAT, kvar FLOAT, voltage FLOAT, kwh FLOAT
);
CREATE TABLE IF NOT EXISTS RAW.TRANSFORMER_READINGS (
    transformer_id VARCHAR, ts TIMESTAMP_NTZ, kw FLOAT, kwh FLOAT, voltage FLOAT
);
CREATE TABLE IF NOT EXISTS RAW.GRID_METERS (
    meter_id VARCHAR, transformer_id VARCHAR, tariff VARCHAR, customer_name VARCHAR,
    address VARCHAR, lat FLOAT, lon FLOAT,
    file_date DATE                       -- which day's file the row came from (needed for SCD2)
);
CREATE TABLE IF NOT EXISTS RAW.GRID_TRANSFORMERS (
    transformer_id VARCHAR, feeder_id VARCHAR, substation_id VARCHAR, district VARCHAR,
    governorate VARCHAR, capacity_kw FLOAT, lat FLOAT, lon FLOAT,
    file_date DATE
);
CREATE TABLE IF NOT EXISTS RAW.SIM_TRUTH (
    anomaly_id VARCHAR, event_type VARCHAR, entity_id VARCHAR, transformer_id VARCHAR,
    start_ts TIMESTAMP_NTZ, end_ts TIMESTAMP_NTZ,
    recorded_share FLOAT, factor FLOAT, voltage_factor FLOAT
);
-- ========================= END OF STEP B =============================


-- =====================================================================
-- STEP C: users and roles for dbt and Power BI
-- Put your own passwords in the 2 CREATE USER lines first.
-- =====================================================================
USE ROLE ACCOUNTADMIN;

-- dbt: reads RAW, builds CORE + MARTS
CREATE ROLE IF NOT EXISTS DBT_ROLE;
GRANT USAGE ON WAREHOUSE SMART_GRID_WH TO ROLE DBT_ROLE;
GRANT USAGE ON DATABASE SMART_GRID_DB TO ROLE DBT_ROLE;
GRANT USAGE ON SCHEMA SMART_GRID_DB.RAW TO ROLE DBT_ROLE;
GRANT SELECT ON ALL TABLES IN SCHEMA SMART_GRID_DB.RAW TO ROLE DBT_ROLE;
GRANT SELECT ON FUTURE TABLES IN SCHEMA SMART_GRID_DB.RAW TO ROLE DBT_ROLE;
GRANT ALL PRIVILEGES ON SCHEMA SMART_GRID_DB.CORE TO ROLE DBT_ROLE;
GRANT ALL PRIVILEGES ON SCHEMA SMART_GRID_DB.MARTS TO ROLE DBT_ROLE;
-- Airflow also loads S3 files with this role
GRANT USAGE ON ALL STAGES       IN SCHEMA SMART_GRID_DB.RAW TO ROLE DBT_ROLE;
GRANT USAGE ON ALL FILE FORMATS IN SCHEMA SMART_GRID_DB.RAW TO ROLE DBT_ROLE;
GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA SMART_GRID_DB.RAW TO ROLE DBT_ROLE;

CREATE USER IF NOT EXISTS DBT_USER
    PASSWORD = '<choose-a-dbt-password>'
    DEFAULT_ROLE = DBT_ROLE
    DEFAULT_WAREHOUSE = SMART_GRID_WH;
GRANT ROLE DBT_ROLE TO USER DBT_USER;

-- Power BI: read-only on MARTS, so the dashboard can never change data
CREATE ROLE IF NOT EXISTS PBI_ROLE;
GRANT USAGE ON WAREHOUSE SMART_GRID_WH TO ROLE PBI_ROLE;
GRANT USAGE ON DATABASE SMART_GRID_DB TO ROLE PBI_ROLE;
GRANT USAGE ON SCHEMA SMART_GRID_DB.MARTS TO ROLE PBI_ROLE;
GRANT SELECT ON FUTURE TABLES IN SCHEMA SMART_GRID_DB.MARTS TO ROLE PBI_ROLE;
GRANT SELECT ON FUTURE VIEWS  IN SCHEMA SMART_GRID_DB.MARTS TO ROLE PBI_ROLE;

CREATE USER IF NOT EXISTS PBI_USER
    PASSWORD = '<choose-a-powerbi-password>'
    DEFAULT_ROLE = PBI_ROLE
    DEFAULT_WAREHOUSE = SMART_GRID_WH;
GRANT ROLE PBI_ROLE TO USER PBI_USER;

-- Let ACCOUNTADMIN (through SYSADMIN) see everything dbt builds.
-- Without this, querying MARTS as ACCOUNTADMIN says "Insufficient privileges".
GRANT ROLE DBT_ROLE TO ROLE SYSADMIN;
GRANT ROLE PBI_ROLE TO ROLE SYSADMIN;

-- Your account identifier for dbt and Power BI:
SELECT CURRENT_ORGANIZATION_NAME() || '-' || CURRENT_ACCOUNT_NAME() AS account_identifier;
-- ========================= END OF STEP C =============================

-- ========================= STEP D (optional: the web app) ============
-- Web app: read-only on MARTS, like Power BI. A service user that logs in with a key pair
-- (no password), because Snowflake is retiring password-only sign-ins.
-- Create the key pair first (RUN_GUIDE Part 14) and paste the public key below.
CREATE ROLE IF NOT EXISTS WEBAPP_ROLE;
GRANT USAGE ON WAREHOUSE SMART_GRID_WH TO ROLE WEBAPP_ROLE;
GRANT USAGE ON DATABASE SMART_GRID_DB TO ROLE WEBAPP_ROLE;
GRANT USAGE ON SCHEMA SMART_GRID_DB.MARTS TO ROLE WEBAPP_ROLE;
GRANT SELECT ON ALL TABLES    IN SCHEMA SMART_GRID_DB.MARTS TO ROLE WEBAPP_ROLE;
GRANT SELECT ON ALL VIEWS     IN SCHEMA SMART_GRID_DB.MARTS TO ROLE WEBAPP_ROLE;
GRANT SELECT ON FUTURE TABLES IN SCHEMA SMART_GRID_DB.MARTS TO ROLE WEBAPP_ROLE;
GRANT SELECT ON FUTURE VIEWS  IN SCHEMA SMART_GRID_DB.MARTS TO ROLE WEBAPP_ROLE;

CREATE USER IF NOT EXISTS WEBAPP_USER
    TYPE = SERVICE
    RSA_PUBLIC_KEY = '<paste rsa_key.pub here, without the BEGIN/END lines>'
    DEFAULT_ROLE = WEBAPP_ROLE
    DEFAULT_WAREHOUSE = SMART_GRID_WH;
GRANT ROLE WEBAPP_ROLE TO USER WEBAPP_USER;
GRANT ROLE WEBAPP_ROLE TO ROLE SYSADMIN;
-- ========================= END OF STEP D =============================
