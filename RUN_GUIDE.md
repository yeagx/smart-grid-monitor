# Run guide

This guide takes you from an empty laptop to the full pipeline running, with the dashboard showing the results. Every step says **why** it is needed.

**Time:** about 1 hour of setup, then about 1.5 hours for the full run (the stream itself takes about 80 minutes).

| Part | What you do | When |
|---|---|---|
| 1 | Install the tools | once |
| 2 | Get the project | once |
| 3 | AWS: bucket, access key, role for Snowflake | once |
| 4 | Snowflake: warehouse, tables, users | once |
| 5 | dbt: install and connect | once |
| 6 | Build the Docker images | once |
| 7 | Smoke test | once |
| 8 | Clean start | before every full run |
| 9 | The full run (stream one day) | the main run |
| 10 | Load into Snowflake and build the models | after the run |
| 11 | Airflow (every 15 minutes) | after Part 10 |
| 12 | Power BI dashboard | after Part 10 |
| 13 | Send new data (incremental runs and live demo) | any time |
| 14 | Web app (API + browser front end) | after Part 10 |

> **Rule for the whole guide:** run every command inside Ubuntu, in your home folder (`~/smart-grid-monitor`), never in `/mnt/c/...`.
> *Why:* the Windows drive is very slow from Ubuntu and breaks Linux file permissions.

---

## Part 1 · Install the tools (once)

1. **Ubuntu on Windows.** In PowerShell as administrator: `wsl --install -d Ubuntu-24.04`, restart, create a username.
   *Why:* the scripts are Linux (bash) scripts. On Linux or macOS, skip this.
2. **Docker Desktop.** Install it, then *Settings → Resources → WSL integration* → turn on **Ubuntu-24.04**.
   *Why:* Kafka, both Spark jobs and the simulator run in Docker containers.
3. **Command-line tools,** inside Ubuntu:
   ```bash
   cd ~
   sudo apt update && sudo apt install -y git unzip python3 python3-venv python3-pip
   curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o awscliv2.zip
   unzip -q -o awscliv2.zip && sudo ./aws/install --update && rm -rf aws awscliv2.zip
   docker --version && aws --version
   ```
   *Why:* `aws` uploads to and checks S3; Python runs dbt and Airflow.
4. **Power BI Desktop** (Windows, from the Microsoft Store), for the dashboard.

## Part 2 · Get the project (once)

```bash
cd ~
git clone <this-repo-url> smart-grid-monitor
cd smart-grid-monitor
```

## Part 3 · AWS (once)

**3.1 The bucket.** S3 → *Create bucket* → name `sic-grad-project`, region **Europe (Stockholm) eu-north-1**.
If you choose another name or region, use it everywhere below and in `.env`, and replace `sic-grad-project` in `warehouse/01_setup_snowflake.sql`.
*Why:* this bucket is the data lake: bronze, silver, dlq, export, grid and truth all live in it.

**3.2 A policy for the pipeline.** IAM → *Policies* → *Create policy* → JSON → paste, then name it `smartgrid-bucket-access`:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Action": ["s3:ListBucket", "s3:GetBucketLocation"],
      "Resource": "arn:aws:s3:::sic-grad-project" },
    { "Effect": "Allow", "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
      "Resource": "arn:aws:s3:::sic-grad-project/*" }
  ]
}
```
*Why:* the key can only touch this one bucket, nothing else in your AWS account.

**3.3 A user and its key.** IAM → *Users* → *Create user* → name `smart-grid-pipeline`, no console access → attach `smartgrid-bucket-access` → create. Open it → *Security credentials* → *Create access key* → *Application running outside AWS*. Then:
```bash
cp .env.example .env
nano .env        # paste the two key values; save with Ctrl+O, Enter, Ctrl+X
set -a; source .env; set +a
aws sts get-caller-identity      # shows user/smart-grid-pipeline
aws s3 ls s3://sic-grad-project/ # no error = the key works
```
*Why:* the scripts and Spark read the key from `.env`. The file is ignored by git, so the key never ends up in the repository.

**3.4 A role for Snowflake.** IAM → *Roles* → *Create role* → *Custom trust policy* (leave the default for now) → attach a new policy with **read-only** access:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Action": ["s3:GetObject", "s3:GetObjectVersion"],
      "Resource": "arn:aws:s3:::sic-grad-project/*" },
    { "Effect": "Allow", "Action": ["s3:ListBucket", "s3:GetBucketLocation"],
      "Resource": "arn:aws:s3:::sic-grad-project" }
  ]
}
```
Name the role `snowflake-s3-role`. You'll finish its trust policy in Part 4.
*Why:* Snowflake reads the bucket through this role, so no AWS key is ever stored in Snowflake.

## Part 4 · Snowflake (once)

**4.1 Account.** Sign up for a Snowflake trial (edition *Enterprise*, cloud **AWS**, ideally the same region as the bucket). Open *Projects → Worksheets → + SQL Worksheet*.

**4.2 Step A.** Open `warehouse/01_setup_snowflake.sql`, replace `<your-aws-account-id>` with your 12-digit AWS account number, paste it into the worksheet, select from the top to `END OF STEP A` and run it.
The last result (`DESC INTEGRATION s3_int`) shows two values. Copy `STORAGE_AWS_IAM_USER_ARN` and `STORAGE_AWS_EXTERNAL_ID`.

**4.3 Trust policy.** AWS → IAM → *Roles* → `snowflake-s3-role` → *Trust relationships* → *Edit trust policy* → replace everything with this (your two values), then wait about 1 minute:
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": { "AWS": "<STORAGE_AWS_IAM_USER_ARN>" },
    "Action": "sts:AssumeRole",
    "Condition": { "StringEquals": { "sts:ExternalId": "<STORAGE_AWS_EXTERNAL_ID>" } }
  }]
}
```
*Why:* only your Snowflake account can use the role.

**4.4 Step B.** Select from `STEP B` to `END OF STEP B` and run. The three `LIST` lines must run without errors (0 files is fine). An error means 4.3 isn't right yet.

**4.5 Step C.** Replace the two passwords in the `CREATE USER` lines, then select `STEP C` to the end and run. The last query prints your **account identifier** (like `ABCDEFG-XY12345`). Keep it for dbt and Power BI.
*Why:* dbt and Airflow use `DBT_USER`; Power BI uses `PBI_USER`, which can only read the final tables.

## Part 5 · dbt (once)

```bash
cd ~/smart-grid-monitor/dbt
python3 -m venv .venv && source .venv/bin/activate
pip install dbt-snowflake
mkdir -p ~/.dbt && cp profiles.example.yml ~/.dbt/profiles.yml
nano ~/.dbt/profiles.yml          # your account identifier + DBT_USER's password
dbt deps
dbt debug                         # must end with: All checks passed!
deactivate
```
*Why:* the virtual environment must be at `dbt/.venv`, because Airflow calls dbt from there. The real `profiles.yml` stays outside the repository.

## Part 6 · Build the Docker images (once, about 10 minutes)

```bash
cd ~/smart-grid-monitor
docker compose --profile tools --profile silver build
```
*Why:* downloads Kafka and Spark and builds the three images (simulator, Spark job 1, Spark job 2). Rebuild after you change any code in `simulator/` or `spark/`.

## Part 7 · Smoke test (about 5 minutes)

```bash
./scripts/run_stream.sh smoke
```
Wait for **`SMOKE PASS`** with `actual=100, expected=100` and `actual=5, expected=5`.
*Why:* proves simulator → Kafka → Spark → S3 works before the long run. It uses its own test topics and folders.

## Part 8 · Clean start (before every full run)

```bash
CONFIRM_RESET=YES ./scripts/reset_lake.sh
```
Then in Snowflake run `warehouse/02_reset_warehouse.sql`.
*Why:* leftovers from tests would mix with the real run and give wrong numbers.

## Part 9 · The full run (about 1.5 hours)

**Terminal 1: generate, upload the grid and answer key, stream day 1**
```bash
cd ~/smart-grid-monitor
CONFIRM_PRODUCTION=YES ALLOW_ASSET_OVERWRITE=YES ./scripts/run_stream.sh fullday
```
- It generates 100,000 meters on 500 transformers for 2 days (a few minutes), into `output/full_<run id>/`.
- It uploads `grid/` and `truth/`, then prints **`GRID AND TRUTH UPLOADED`** → start terminal 2.
- It streams the whole of day 1 at 2,000 readings/second (about 80 minutes) and ends with **`FULLDAY RUN PASS`**.
- Slower, longer run: put `METER_RATE=1000` in front (about 2 h 40 min).

*Why the whole day:* every planted problem is in day 1. Theft runs all day, line drop is 06:00–10:00 and overload is 18:00–21:00.

**Terminal 2: Spark job 2 (start right after `GRID AND TRUTH UPLOADED`)**
```bash
cd ~/smart-grid-monitor
docker compose --profile silver up spark-silver
```
Every minute it prints a batch, for example:
```
=== Processing micro-batch 12 ===
Batch 12: meter valid+grid matched=99,421; new export=99,421; DLQ=104
Batch 12: transformer valid=500; new export=500; DLQ=0
```
*Why after the upload:* the job reads the grid files once, when it starts.

When terminal 1 says `FULLDAY RUN PASS`, wait until terminal 2 prints `no source rows` twice (it can lag 10–20 minutes behind), then press **Ctrl+C** in terminal 2.

**What one reading looks like at each stop**

| Stop | Example |
|---|---|
| Kafka message | `{"meter_id":"M-000865","ts":"2026-08-01T00:00:00Z","kw":0.552,"kvar":0.253,"voltage":223.34,"kwh":0.1379}` |
| `bronze/` | topic `meter.readings` · partition 0 · offset 10452 · arrival time · `raw_value` = the JSON above, untouched |
| `dlq/` (if broken) | reason `negative_kw` · `raw_value` = the original message |
| `silver/` and `export/` | M-000865 · T-0004 · 2026-08-01 00:00 · 0.552 kW · 0.253 kvar · 223.34 V · 0.1379 kWh |

## Part 10 · Load into Snowflake and build the models

1. In Snowflake run `warehouse/03_load_raw.sql`. Expected counts:

   | Table | Rows |
   |---|---|
   | METER_READINGS | about 9.6 million |
   | TRANSFORMER_READINGS | 48,000 (500 × 96 slots) |
   | GRID_METERS | 200,000 (100,000 meters × 2 daily files) |
   | GRID_TRANSFORMERS | 1,000 (500 × 2 daily files) |
   | SIM_TRUTH | 815 planted problems |

2. Build the models:
   ```bash
   cd ~/smart-grid-monitor/dbt && source .venv/bin/activate
   dbt snapshot --vars "{grid_date: 2026-08-01}"
   dbt snapshot --vars "{grid_date: 2026-08-02}"
   dbt run --full-refresh
   dbt test --exclude tag:demo
   ```
   *Why two snapshots:* day 1 creates the meter history; day 2 adds a new version for every meter that moved or changed tariff (SCD Type 2).

3. Check the result:
   ```sql
   SELECT * FROM SMART_GRID_DB.MARTS.ACCURACY_SUMMARY;
   -- theft 10, overload 10, line_drop 5 true positives; precision and recall 1.0
   ```

4. Optional, the quality-gate demo (fails on purpose): `dbt test --select tag:demo`

5. Optional, the lineage graph: `dbt docs generate && dbt docs serve --port 8081`, then open http://localhost:8081.

## Part 11 · Airflow (runs the batch part every 15 minutes)

The DAG `smart_grid_pipeline` runs, in order:
```
clean_bronze → load_to_snowflake → dbt_snapshot → dbt_run → dbt_test
```
| Task | What it does |
|---|---|
| clean_bronze | Spark job 2 in run-once mode: cleans whatever is new in bronze, then stops. Skips itself if the live job from Part 9 is running |
| load_to_snowflake | Runs `warehouse/03_load_raw.sql` with the dbt login. Only new files load |
| dbt_snapshot / dbt_run / dbt_test | History, models and checks, tests. A failed test turns the run red: the quality gate |

**Install (once), in a new Ubuntu terminal:**
```bash
python3 -m venv ~/airflow_venv && source ~/airflow_venv/bin/activate
PY=$(python3 -c 'import sys; print(f"{sys.version_info.major}.{sys.version_info.minor}")')
pip install "apache-airflow==2.10.5" snowflake-connector-python pyyaml \
  --constraint "https://raw.githubusercontent.com/apache/airflow/constraints-2.10.5/constraints-${PY}.txt"
```
*Why a separate environment:* Airflow pins many libraries; keeping it apart stops it from clashing with dbt.

**Start it (every time):**
```bash
source ~/airflow_venv/bin/activate
export AIRFLOW_HOME=~/smart-grid-monitor/airflow
export SMART_GRID_HOME=~/smart-grid-monitor
export AIRFLOW__CORE__LOAD_EXAMPLES=False
airflow standalone
```
- The first start prints `Login with username: admin  password: ...` (also saved in `airflow/standalone_admin_password.txt`).
- Open **http://localhost:8080**, switch `smart_grid_pipeline` on and click **Trigger DAG**. The five boxes turn green in about 2–4 minutes.
- Docker Desktop must be running, because `clean_bronze` starts a container. Keep this terminal open.

## Part 12 · Power BI dashboard

1. Open `powerbi/smart_grid_monitor.pbix` in Power BI Desktop.
2. *Transform data → Data source settings → Change source*: server `<account-identifier>.snowflakecomputing.com`, warehouse `SMART_GRID_WH`.
3. *Edit permissions → Credentials*: user `PBI_USER` and its password.
4. Click **Refresh**.

The report has 4 pages: **Overview** (alerts, demand, peak), **Alert Map** (where the flagged transformers are), **Alert Log** (every alert with time and place) and **Accuracy** (found vs planted).
Refresh only after an Airflow run is green; refreshing while dbt is rebuilding can fail.

## Part 13 · Send new data (incremental runs and the live demo)

Day 2 is generated but not streamed in Part 9, so it can be sent later as new data:
```bash
docker compose up -d kafka spark-bronze
docker compose --profile silver up spark-silver        # terminal 2: live batches
./scripts/run_stream.sh demo 00:00                     # one 15-minute slot of day 2 (~100k readings, ~1 minute)
```
Then trigger the DAG (or wait for the schedule) and refresh Power BI: Aug 2 appears and the totals go up.
- Each slot can be sent **once**; a repeated slot is dropped as duplicates. Used slots are kept in `demo_history.txt`.
- Use slots in order: `00:00`, `00:15`, `00:30`, … or a whole hour: `demo 01`.

**The five batch runs from the handbook**

| # | Run | How |
|---|---|---|
| 1 | Initial load | Part 10 |
| 2 | Incremental | `./scripts/run_stream.sh demo 00:00`, then Trigger DAG: only the new slot loads |
| 3 | Same run again | Trigger DAG again: the load finds 0 new files and the counts don't change |
| 4 | Failure and retry | While `dbt_run` runs, *Mark Failed*, then *Clear*: it runs again and turns green |
| 5 | Backfill | `dbt/.venv/bin/dbt run --full-refresh --select check_theft check_overload check_line_drop` (from `dbt/`) |

## Part 14 · Web app (API and browser front end)

The web app reads the same Gold tables as Power BI and shows them in a browser: Overview, Alert Map, Alert Log, Transformers, Accuracy, and the closed Decision Center. This part can run on Windows (PowerShell) or in Ubuntu.

**14.1 Install the tools (once).** [.NET 10 SDK](https://dotnet.microsoft.com/download) for the API and [Node.js 20 or newer](https://nodejs.org) for the front end. Check with `dotnet --version` and `node --version`.

**14.2 Try it on mock data first (no Snowflake needed).**
```bash
cd apps/backend
dotnet run --project src/SmartGrid.Api --launch-profile http      # API on http://localhost:5091
```
In a second terminal:
```bash
cd apps/frontend
cp .env.example .env            # PowerShell: copy .env.example .env
npm install
npm run dev                     # open http://localhost:5173
```
*Why:* the API starts on built-in sample data by default, so you can check that both parts run before connecting the warehouse.

**14.3 Create a key pair for the web app user (once, in Ubuntu).**
```bash
openssl genrsa 2048 | openssl pkcs8 -topk8 -inform PEM -out rsa_key.p8 -nocrypt
openssl rsa -in rsa_key.p8 -pubout -out rsa_key.pub
cat rsa_key.pub
```
Copy the text between the `BEGIN` and `END` lines into `RSA_PUBLIC_KEY` in **Step D** of `warehouse/01_setup_snowflake.sql`, then select Step D and run it.
*Why:* the web app gets its own read-only user (`WEBAPP_USER`) that signs in with a key instead of a password. Keep `rsa_key.p8` private: git ignores it and it never leaves your machine.

**14.4 Point the API at Snowflake.** In `apps/backend/src/SmartGrid.Api`:
```bash
dotnet user-secrets set "DataSource:Provider" "Snowflake"
dotnet user-secrets set "ConnectionStrings:Snowflake" "account=<account-identifier>;user=WEBAPP_USER;authenticator=snowflake_jwt;private_key_file=<full path to rsa_key.p8>;db=SMART_GRID_DB;schema=MARTS;warehouse=SMART_GRID_WH;role=WEBAPP_ROLE"
```
*Why:* user secrets are stored on your machine, outside the repo, so the connection details are never committed.

Run the API again and open `http://localhost:5091/api/health`: it should say `"dataProvider": "Snowflake"`. Refresh the browser and the pages show the real data (25 events: 10 theft, 10 overload, 5 voltage drop).
More detail and a connection test: [apps/backend/SNOWFLAKE_SETUP.md](apps/backend/SNOWFLAKE_SETUP.md).

**14.5 Tests (optional).**
```bash
dotnet test apps/backend/SmartGrid.sln      # 149 tests
cd apps/frontend && npm test                # 63 tests
```

> The warehouse stores one row for every flagged 15-minute reading. The API merges back-to-back rows into one **event**, so one theft is counted once. That is why the web app shows 25 events, the same number as the answer key.

## Run script modes

| Command | What it does |
|---|---|
| `./scripts/run_stream.sh smoke` | 100 + 5 messages on separate test topics; checks they reach S3 |
| `./scripts/run_stream.sh fullday` | Generates the full data and streams all of day 1 (needs `CONFIRM_PRODUCTION=YES ALLOW_ASSET_OVERWRITE=YES`) |
| `./scripts/run_stream.sh good` | Same, but only the first 2 million readings |
| `./scripts/run_stream.sh demo HH[:MM]` | Sends one hour or one 15-minute slot of day 2 |
| `./scripts/run_stream.sh status` | Lists the Bronze files on S3 and the running containers |
| `./scripts/run_stream.sh clean-bronze` | Empties `bronze/` and the Spark checkpoint (needs `CONFIRM_CLEAN_BRONZE=YES`) |

The size of the data, the days and the planted problems are set in `config.yaml`. The file holds a small development setup (1,000 meters, 5 transformers); the `fullday` and `good` modes switch it to the full size (100,000 meters, 500 transformers) automatically. The same seed always gives exactly the same data.

---

## If something goes wrong

| Problem | Fix |
|---|---|
| `AccessDenied` / `Unable to locate credentials` | Check `.env` and that the policy names your bucket |
| `run_stream.sh` says bronze is not empty | Run Part 8 again |
| Silver: `No usable meter grid rows` | Spark job 2 started before the grid upload. Stop it (Ctrl+C) and start it again |
| Silver falls far behind | Normal on a laptop with slow upload; it catches up after the stream ends. Or use `METER_RATE=1000` |
| A command runs for minutes with no output | You're probably inside `/mnt/c/...` (`pwd` shows it). Move to `~` and run it again |
| `reset_lake.sh`: permission denied on `checkpoints/` | `sudo rm -rf checkpoints/*`, then continue |
| COPY INTO loads 0 files | `export/` is empty (Spark job 2 not run yet), or those files were already loaded |
| `LIST` fails: `not authorized to perform: sts:AssumeRole` | Recheck 4.3: both values copied exactly, policy saved, wait a minute |
| dbt: `Could not find profile named 'smart_grid'` | The first line of `~/.dbt/profiles.yml` must be `smart_grid:` |
| Airflow `clean_bronze` fails with a Docker error | Docker Desktop must be running; test with `docker ps` |
| Airflow doesn't show the DAG | Check `AIRFLOW_HOME` and `SMART_GRID_HOME` point to `~/smart-grid-monitor` |
| Accuracy shows overload as "False Negative" | The stream stopped before 18:00–21:00 of day 1; let `fullday` finish |
| Power BI refresh fails with a "cyclic reference" | dbt was rebuilding the tables; wait for the Airflow run to finish and refresh again |
| Web app pages say "Unable to reach the backend" | Start the API first (14.2) and check `VITE_API_BASE_URL` in `apps/frontend/.env` |
| Web app shows sample numbers, not yours | The API is still on mock data; check `/api/health` and redo 14.4 |
| API: `JWT token is invalid` | The public key in Snowflake doesn't match `rsa_key.p8`; run `ALTER USER WEBAPP_USER SET RSA_PUBLIC_KEY='...'` with the matching key |
