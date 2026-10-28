"""Smart Grid pipeline, every 15 minutes:

    clean_bronze  ->  load_to_snowflake  ->  dbt_snapshot  ->  dbt_run  ->  dbt_test

1. clean_bronze       Spark job 2 in "available-now" mode: cleans whatever is new in
                      bronze/, writes silver/ dlq/ export/, then stops. Skipped when the
                      continuous silver job is already running (live demo), so the two
                      never use the same checkpoint at the same time.
2. load_to_snowflake  COPY INTO from S3 (warehouse/03_load_raw.sql). Only NEW files load.
3. dbt_snapshot       SCD2 history of the meter list (latest grid day).
4. dbt_run            star schema, the 3 checks (incremental), KPIs, accuracy.
5. dbt_test           data quality tests (the quality gate). A failure turns the run red.

Snowflake login is read from ~/.dbt/profiles.yml, so the password lives in one place.
The repository location comes from SMART_GRID_HOME (default: ~/smart-grid-monitor).
"""
from __future__ import annotations

import os
from datetime import datetime, timedelta
from pathlib import Path

from airflow import DAG
from airflow.operators.bash import BashOperator
from airflow.operators.python import PythonOperator

PROJECT = Path(os.environ.get("SMART_GRID_HOME", Path.home() / "smart-grid-monitor"))
DBT_DIR = PROJECT / "dbt"
DBT = DBT_DIR / ".venv" / "bin" / "dbt"
LOAD_SQL = PROJECT / "warehouse" / "03_load_raw.sql"
PROFILES = Path.home() / ".dbt" / "profiles.yml"

SILVER_CMD = (
    "/opt/spark/bin/spark-submit --master 'local[3]' --driver-memory 3g "
    "--conf spark.jars.ivy=/tmp/spark-ivy "
    "--packages io.delta:delta-spark_2.12:3.2.0,org.apache.hadoop:hadoop-aws:3.3.4 "
    "/app/spark/silver_processing.py --mode s3 --trigger-mode available-now"
)


def load_to_snowflake() -> None:
    """Run warehouse/03_load_raw.sql with the dbt login (role DBT_ROLE)."""
    import snowflake.connector
    import yaml

    cfg = yaml.safe_load(PROFILES.read_text())["smart_grid"]["outputs"]["dev"]
    # The file starts with USE ROLE ACCOUNTADMIN for manual runs; Airflow uses DBT_ROLE instead.
    sql = "\n".join(
        line for line in LOAD_SQL.read_text().splitlines()
        if not line.strip().upper().startswith("USE ROLE")
    )
    conn = snowflake.connector.connect(
        account=cfg["account"], user=cfg["user"], password=cfg["password"],
        role=cfg["role"], warehouse=cfg["warehouse"], database=cfg["database"],
    )
    try:
        for cursor in conn.execute_string(sql):
            rows = cursor.fetchall() if cursor.description else []
            for row in rows[:10]:
                print(row)
    finally:
        conn.close()


default_args = {
    "owner": "smart_grid",
    "retries": 1,
    "retry_delay": timedelta(minutes=2),
}

with DAG(
    dag_id="smart_grid_pipeline",
    description="bronze -> silver -> Snowflake -> dbt, every 15 minutes",
    start_date=datetime(2026, 10, 1),
    schedule="*/15 * * * *",
    catchup=False,
    max_active_runs=1,
    default_args=default_args,
    tags=["smart-grid", "sic"],
) as dag:

    clean_bronze = BashOperator(
        task_id="clean_bronze",
        cwd=str(PROJECT),
        bash_command=(
            'if [ -n "$(docker compose --profile silver ps -q --status running spark-silver)" ]; then '
            'echo "Continuous silver job is running (live mode); skipping this step."; '
            "else "
            f"docker compose --profile silver run --rm -T spark-silver {SILVER_CMD}; "
            "fi"
        ),
        execution_timeout=timedelta(minutes=60),
    )

    load = PythonOperator(
        task_id="load_to_snowflake",
        python_callable=load_to_snowflake,
        execution_timeout=timedelta(minutes=30),
    )

    dbt_snapshot = BashOperator(
        task_id="dbt_snapshot",
        cwd=str(DBT_DIR),
        bash_command=f"{DBT} snapshot",
    )

    dbt_run = BashOperator(
        task_id="dbt_run",
        cwd=str(DBT_DIR),
        bash_command=f"{DBT} run",
    )

    dbt_test = BashOperator(
        task_id="dbt_test",
        cwd=str(DBT_DIR),
        bash_command=f"{DBT} test --exclude tag:demo",
    )

    clean_bronze >> load >> dbt_snapshot >> dbt_run >> dbt_test
