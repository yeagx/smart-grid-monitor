#!/usr/bin/env bash
# Streaming side of the pipeline: generate the data, upload the grid + answer key,
# start Kafka and the Bronze Spark job, and send the readings through Kafka.
# Modes:
#   smoke       isolated end-to-end test (100 meter + 5 transformer messages)
#   good        full generator config; publishes first 2M meter readings + aligned transformer rows
#   fullday     full generator config; publishes the WHOLE first day (~9.6M meter readings).
#               Use this one: overload (18:00) and line drop (06:00) only happen later in day 1.
#   demo HH     live demo: sends ONE hour of day 2 (e.g. "demo 01" = 01:00-02:00, ~400k readings)
#               "demo 01:15" sends one 15-minute slot (~100k readings, about 1 minute)
#   clean-bronze explicitly clears s3://$S3_BUCKET/bronze/ and production Spark checkpoint
#   status      shows the production Bronze prefixes and service state
set -Eeuo pipefail

# Always run from the repository root (this script lives in <repo>/scripts/).
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

MODE="${1:-}"
if [[ -z "$MODE" ]]; then
  echo "Usage: $0 {smoke|good|fullday|demo HH[:MM]|clean-bronze|status}" >&2
  exit 2
fi

# Load local Compose/AWS settings without printing them. The .env file must be trusted.
if [[ -f .env ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi
BUCKET="${S3_BUCKET:-sic-grad-project}"
REGION="${AWS_REGION:-eu-north-1}"
AWS_PROFILE_NAME="${AWS_PROFILE:-default}"
RUN_ID="${RUN_ID:-$(date +%Y%m%d_%H%M%S)}"

if [[ -n "${AWS_ACCESS_KEY_ID:-}" && -n "${AWS_SECRET_ACCESS_KEY:-}" ]]; then
  awsx() { aws "$@"; }
else
  awsx() { aws --profile "$AWS_PROFILE_NAME" "$@"; }
fi

need_tools() {
  command -v docker >/dev/null || { echo "ERROR: docker not found" >&2; exit 2; }
  docker compose version >/dev/null
  command -v python3 >/dev/null || { echo "ERROR: python3 not found" >&2; exit 2; }
  awsx sts get-caller-identity --region "$REGION" >/dev/null
  docker compose config --quiet
}

ensure_kafka() {
  echo "Ensuring Kafka is running and healthy..."
  docker compose up -d kafka >/dev/null
  local container status n
  container="$(docker compose ps -q kafka)"
  if [[ -z "$container" ]]; then
    echo "ERROR: could not find the Kafka container after startup." >&2
    return 1
  fi
  for ((n=1; n<=60; n++)); do
    status="$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$container" 2>/dev/null || true)"
    if [[ "$status" == "healthy" ]]; then
      echo "Kafka is healthy."
      return 0
    fi
    if [[ "$status" == "exited" || "$status" == "dead" ]]; then
      echo "ERROR: Kafka container is $status." >&2
      docker logs --tail=60 "$container" >&2 || true
      return 1
    fi
    sleep 2
  done
  echo "ERROR: Kafka did not become healthy within 120 seconds." >&2
  docker logs --tail=60 "$container" >&2 || true
  return 1
}

write_verifier() {
  mkdir -p bronze
  cat > bronze/verify_s3_counts.py <<'PY'
import sys
from pyspark.sql import SparkSession

if len(sys.argv) != 5:
    raise SystemExit("Usage: verify_s3_counts.py METER_PATH EXPECTED_METER TRANSFORMER_PATH EXPECTED_TRANSFORMER")

meter_path, expected_meter_s, transformer_path, expected_transformer_s = sys.argv[1:]
checks = [
    ("meter_readings", meter_path, int(expected_meter_s)),
    ("transformer_readings", transformer_path, int(expected_transformer_s)),
]
spark = SparkSession.builder.appName("Bronze-Count-Verification").getOrCreate()
failed = False
try:
    for name, path, expected in checks:
        actual = spark.read.parquet(path).count()
        status = "PASS" if actual == expected else "FAIL"
        print(f"{status}: {name}: actual={actual:,}, expected={expected:,}, path={path}", flush=True)
        failed = failed or actual != expected
finally:
    spark.stop()
if failed:
    raise SystemExit(1)
PY
}

make_full_config() {
  local output="$1"
  python3 - config.yaml "$output" <<'PY'
import re, sys
src, dst = sys.argv[1:]
lines = open(src, encoding="utf-8").readlines()
section = None
anomaly = None
out = []
for line in lines:
    if re.match(r"^[A-Za-z_][A-Za-z0-9_-]*:", line):
        section = line.split(":", 1)[0]
        anomaly = None
    elif section == "anomalies" and re.match(r"^  [A-Za-z_][A-Za-z0-9_-]*:", line):
        anomaly = line.strip().split(":", 1)[0]
    if re.match(r"^days:\s*", line):
        line = 'days: 2                    # full run: two days\n'
    elif re.match(r"^meters:\s*", line):
        line = 'meters: 100000             # full mode\n'
    elif re.match(r"^transformers:\s*", line) and section != "anomalies":
        line = 'transformers: 500           # full mode\n'
    elif section == "anomalies" and anomaly == "overload" and re.match(r"^    transformers:\s*", line):
        line = '    transformers: 10        # full mode\n'
    elif section == "anomalies" and anomaly == "line_drop" and re.match(r"^    transformers:\s*", line):
        line = '    transformers: 5         # full mode\n'
    elif section == "anomalies" and anomaly == "theft" and re.match(r"^    transformers:\s*", line):
        line = '    transformers: 10        # full mode\n'
    elif section == "anomalies" and anomaly == "silent_meters" and re.match(r"^    meters:\s*", line):
        line = '    meters: 500             # full mode\n'
    out.append(line)
open(dst, "w", encoding="utf-8").writelines(out)
PY
  echo "Prepared full-mode config: $output"
  grep -E '^(days|meters|transformers):|^  (overload|line_drop|theft|silent_meters):|^    (transformers|meters):' "$output"
}

run_generator() {
  local config_file="$1" output_dir="$2"
  mkdir -p "$output_dir"
  docker compose --profile tools run --rm --no-deps \
    -v "$PWD/$output_dir:/app/output" \
    -v "$PWD/$config_file:/app/run-config.yaml:ro" \
    simulator sh -lc \
    'python simulator/generator.py --config /app/run-config.yaml --out /app/output && python simulator/validate_outputs.py /app/output'
}

upload_isolated_assets() {
  local output_dir="$1" run_id="$2"
  local grid_prefix="s3://$BUCKET/smoke/$run_id/grid/"
  local truth_key="s3://$BUCKET/smoke/$run_id/truth/sim_truth.jsonl"
  shopt -s nullglob
  local f
  for f in "$output_dir"/grid/*.csv; do
    awsx s3 cp "$f" "$grid_prefix$(basename "$f")" --region "$REGION"
  done
  awsx s3 cp "$output_dir/sim_truth.jsonl" "$truth_key" --region "$REGION"
  echo "Isolated grid assets: $grid_prefix"
  echo "Isolated answer key:  $truth_key"
}

wait_for_log() {
  local container="$1" pattern="$2" tries="${3:-60}"
  local n
  for ((n=1; n<=tries; n++)); do
    if ! docker inspect -f '{{.State.Running}}' "$container" 2>/dev/null | grep -q true; then
      echo "ERROR: Spark container $container stopped before becoming ready." >&2
      docker logs --tail=80 "$container" >&2 || true
      return 1
    fi
    if docker logs "$container" 2>&1 | grep -Fq "$pattern"; then
      return 0
    fi
    sleep 2
  done
  echo "ERROR: Spark did not report readiness within $((tries*2)) seconds." >&2
  docker logs --tail=80 "$container" >&2 || true
  return 1
}

wait_for_kafka_start() {
  # Spark writes <checkpoint>/<group>/sources/0/0 once it has fixed its Kafka start position.
  # Sending before that file exists is how the first ~20 seconds got skipped in the first full run.
  local base="$1" n
  for ((n=1; n<=90; n++)); do
    if [[ -f "$base/meter_readings/sources/0/0" && -f "$base/transformer_readings/sources/0/0" ]]; then
      echo "Spark is reading Kafka; safe to start sending."
      return 0
    fi
    sleep 2
  done
  echo "ERROR: Spark did not fix its Kafka start position within 180 seconds." >&2
  return 1
}

verify_counts() {
  local spark_target="$1" meter_path="$2" meter_expected="$3" transformer_path="$4" transformer_expected="$5"
  write_verifier
  local cmd=(/opt/spark/bin/spark-submit --master 'local[2]' --packages org.apache.hadoop:hadoop-aws:3.3.4 --conf spark.jars.ivy=/tmp/spark-ivy /app/bronze/verify_s3_counts.py "$meter_path" "$meter_expected" "$transformer_path" "$transformer_expected")
  if [[ "$spark_target" == "compose-service" ]]; then
    docker compose exec -T spark-bronze "${cmd[@]}"
  else
    docker exec "$spark_target" "${cmd[@]}"
  fi
}

ensure_production_bronze_empty() {
  local listing
  listing="$(awsx s3 ls "s3://$BUCKET/bronze/" --recursive --region "$REGION" || true)"
  if [[ -n "$listing" ]]; then
    echo "ERROR: s3://$BUCKET/bronze/ is not empty. Run clean-bronze explicitly before a production run." >&2
    echo "$listing" | head -25 >&2
    return 1
  fi
  if [[ -d checkpoints/bronze ]] && find checkpoints/bronze -type f -print -quit | grep -q .; then
    echo "ERROR: production Spark checkpoint still exists. Run clean-bronze first." >&2
    return 1
  fi
}

case "$MODE" in
  status)
    need_tools
    echo "===== Bronze objects ====="
    awsx s3 ls "s3://$BUCKET/bronze/" --recursive --region "$REGION" || true
    echo
    echo "===== Compose status ====="
    docker compose ps
    ;;

  clean-bronze)
    need_tools
    if [[ "${CONFIRM_CLEAN_BRONZE:-}" != "YES" ]]; then
      echo "This deletes EVERYTHING under s3://$BUCKET/bronze/ plus the production Bronze checkpoint." >&2
      echo "To confirm, rerun with CONFIRM_CLEAN_BRONZE=YES." >&2
      exit 2
    fi
    echo "Stopping production Spark before cleanup..."
    docker compose stop spark-bronze >/dev/null 2>&1 || true
    echo "Current Bronze objects to be deleted:"
    awsx s3 ls "s3://$BUCKET/bronze/" --recursive --region "$REGION" || true
    awsx s3 rm "s3://$BUCKET/bronze/" --recursive --region "$REGION"
    if [[ -d checkpoints/bronze ]]; then
      rm -rf -- checkpoints/bronze
    fi
    echo "Bronze prefix emptied and production Bronze checkpoint removed. grid/ and truth/ were not touched."
    ;;

  smoke)
    need_tools
    ensure_kafka
    OUT_DIR="output/smoke_${RUN_ID}"
    CONFIG_REL="$OUT_DIR/run_config.yaml"
    SPARK_CONTAINER="smartgrid-smoke-spark-${RUN_ID//_/-}"
    TOPIC_TAG="smoke-${RUN_ID//_/-}"
    METER_TOPIC="meter.readings.${TOPIC_TAG}"
    TRANSFORMER_TOPIC="transformer.readings.${TOPIC_TAG}"
    BASE="s3a://$BUCKET/bronze/_smoke_${RUN_ID}"
    CHECKPOINT="/app/checkpoints/smoke_${RUN_ID}"
    trap 'docker rm -f "$SPARK_CONTAINER" >/dev/null 2>&1 || true' EXIT
    mkdir -p "$OUT_DIR"
    cp config.yaml "$OUT_DIR/run_config.yaml"
    run_generator "$CONFIG_REL" "$OUT_DIR"
    upload_isolated_assets "$OUT_DIR" "$RUN_ID"

    echo "Creating isolated Kafka topics: $METER_TOPIC and $TRANSFORMER_TOPIC"
    docker compose exec -T kafka /opt/kafka/bin/kafka-topics.sh \
      --bootstrap-server kafka:29092 --create --if-not-exists \
      --topic "$METER_TOPIC" --partitions 1 --replication-factor 1 >/dev/null
    docker compose exec -T kafka /opt/kafka/bin/kafka-topics.sh \
      --bootstrap-server kafka:29092 --create --if-not-exists \
      --topic "$TRANSFORMER_TOPIC" --partitions 1 --replication-factor 1 >/dev/null

    echo "Starting isolated Spark consumer: $METER_TOPIC and $TRANSFORMER_TOPIC"
    docker compose run -d --no-deps --name "$SPARK_CONTAINER" \
      -e KAFKA_TOPICS="$METER_TOPIC,$TRANSFORMER_TOPIC" \
      -e KAFKA_STARTING_OFFSETS=latest \
      -e BRONZE_OUTPUT_PATH="$BASE/" \
      -e SPARK_CHECKPOINT_PATH="$CHECKPOINT" \
      spark-bronze >/dev/null
    wait_for_log "$SPARK_CONTAINER" "Parquet output: $BASE/meter_readings"
    wait_for_kafka_start "checkpoints/smoke_${RUN_ID}"

    echo "Publishing a small sample (100 meter + 5 transformer messages)..."
    docker compose --profile tools run --rm --no-deps \
      -v "$PWD/$OUT_DIR:/app/run-output:ro" simulator \
      python simulator/producer.py --input-dir /app/run-output --date 20260801 \
        --meter-topic "$METER_TOPIC" --transformer-topic "$TRANSFORMER_TOPIC" \
        --meter-rate 1000 --transformer-rate 5 \
        --max-meter-messages 100 --max-transformer-messages 5
    echo "Waiting for Spark's 60-second micro-batch to commit..."
    sleep 70
    verify_counts "$SPARK_CONTAINER" \
      "$BASE/meter_readings" 100 "$BASE/transformer_readings" 5
    echo
    echo "SMOKE PASS. Run ID: $RUN_ID"
    echo "Bronze test data kept at: s3://$BUCKET/bronze/_smoke_${RUN_ID}/"
    echo "Only isolated test topics were used; production Bronze was not changed by this smoke run."
    ;;

  good)
    need_tools
    if [[ "${CONFIRM_PRODUCTION:-}" != "YES" ]]; then
      echo "Production mode sends messages to meter.readings / transformer.readings." >&2
      echo "Set CONFIRM_PRODUCTION=YES to explicitly confirm the production run." >&2
      exit 2
    fi
    if [[ "${ALLOW_ASSET_OVERWRITE:-}" != "YES" ]]; then
      echo "Production mode overwrites the shared grid/*.csv and truth/sim_truth.jsonl assets." >&2
      echo "Set ALLOW_ASSET_OVERWRITE=YES only after the team agrees to publish this generated dataset." >&2
      exit 2
    fi
    ensure_production_bronze_empty
    OUT_DIR="output/full_${RUN_ID}"
    CONFIG_REL="$OUT_DIR/run_config.yaml"
    mkdir -p "$OUT_DIR"
    make_full_config "$PWD/$CONFIG_REL"
    run_generator "$CONFIG_REL" "$OUT_DIR"

    echo "Uploading daily grid files and sim_truth.jsonl to their agreed shared paths (overwrite explicitly authorized)..."
    shopt -s nullglob
    for f in "$OUT_DIR"/grid/*.csv; do
      awsx s3 cp "$f" "s3://$BUCKET/grid/$(basename "$f")" --region "$REGION"
    done
    awsx s3 cp "$OUT_DIR/sim_truth.jsonl" "s3://$BUCKET/truth/sim_truth.jsonl" --region "$REGION"

    docker compose up -d kafka spark-bronze
    SPARK_CONTAINER="$(docker compose ps -q spark-bronze)"
    if [[ -z "$SPARK_CONTAINER" ]]; then
      echo "ERROR: could not resolve the running spark-bronze container." >&2
      exit 1
    fi
    wait_for_log "$SPARK_CONTAINER" "Parquet output: s3a://$BUCKET/bronze/meter_readings"
    wait_for_kafka_start "checkpoints/bronze"
    echo "Good-level stream: 2,000,000 meter messages + 10,000 aligned transformer messages."
    docker compose --profile tools run --rm --no-deps \
      -v "$PWD/$OUT_DIR:/app/run-output:ro" simulator \
      python simulator/producer.py --input-dir /app/run-output --date 20260801 \
        --meter-topic meter.readings --transformer-topic transformer.readings \
        --meter-rate 1000 --transformer-rate 5 \
        --max-meter-messages 2000000 --max-transformer-messages 10000
    echo "Waiting for Spark's last micro-batch to commit..."
    sleep 70
    verify_counts compose-service \
      "s3a://$BUCKET/bronze/meter_readings" 2000000 \
      "s3a://$BUCKET/bronze/transformer_readings" 10000
    echo "GOOD RUN PASS. Run ID: $RUN_ID. Spark remains running."
    ;;

  fullday)
    # Same as "good", but streams the whole first day so every planted problem is included:
    # theft (all day), line drop (06:00-10:00) and overload (18:00-21:00).
    # Speed: METER_RATE meter messages/sec (default 2000 -> about 80 minutes).
    # Transformers are sent at 1/200 of that rate, so both topics move through the day together.
    need_tools
    if [[ "${CONFIRM_PRODUCTION:-}" != "YES" ]]; then
      echo "Production mode sends messages to meter.readings / transformer.readings." >&2
      echo "Set CONFIRM_PRODUCTION=YES to explicitly confirm the production run." >&2
      exit 2
    fi
    if [[ "${ALLOW_ASSET_OVERWRITE:-}" != "YES" ]]; then
      echo "Production mode overwrites the shared grid/*.csv and truth/sim_truth.jsonl assets." >&2
      echo "Set ALLOW_ASSET_OVERWRITE=YES only after the team agrees to publish this generated dataset." >&2
      exit 2
    fi
    ensure_production_bronze_empty
    METER_RATE="${METER_RATE:-2000}"
    TRANSFORMER_RATE="$(python3 -c "print(max(1, int(${METER_RATE}) // 200))")"
    OUT_DIR="output/full_${RUN_ID}"
    CONFIG_REL="$OUT_DIR/run_config.yaml"
    mkdir -p "$OUT_DIR"
    make_full_config "$PWD/$CONFIG_REL"
    run_generator "$CONFIG_REL" "$OUT_DIR"

    METER_LINES="$(wc -l < "$OUT_DIR/meter_readings_20260801.jsonl" | tr -d ' ')"
    TRANSFORMER_LINES="$(wc -l < "$OUT_DIR/transformer_readings_20260801.jsonl" | tr -d ' ')"
    echo "Day 1 files: ${METER_LINES} meter lines, ${TRANSFORMER_LINES} transformer lines"

    echo "Uploading daily grid files and sim_truth.jsonl to their agreed shared paths..."
    shopt -s nullglob
    for f in "$OUT_DIR"/grid/*.csv; do
      awsx s3 cp "$f" "s3://$BUCKET/grid/$(basename "$f")" --region "$REGION"
    done
    awsx s3 cp "$OUT_DIR/sim_truth.jsonl" "s3://$BUCKET/truth/sim_truth.jsonl" --region "$REGION"
    echo "GRID AND TRUTH UPLOADED - you can start the silver job now (docker compose --profile silver up -d spark-silver)"

    docker compose up -d kafka spark-bronze
    SPARK_CONTAINER="$(docker compose ps -q spark-bronze)"
    if [[ -z "$SPARK_CONTAINER" ]]; then
      echo "ERROR: could not resolve the running spark-bronze container." >&2
      exit 1
    fi
    wait_for_log "$SPARK_CONTAINER" "Parquet output: s3a://$BUCKET/bronze/meter_readings"
    wait_for_kafka_start "checkpoints/bronze"
    echo "Full-day stream: ${METER_LINES} meter messages at ${METER_RATE}/s + ${TRANSFORMER_LINES} transformer messages at ${TRANSFORMER_RATE}/s."
    docker compose --profile tools run --rm --no-deps \
      -v "$PWD/$OUT_DIR:/app/run-output:ro" simulator \
      python simulator/producer.py --input-dir /app/run-output --date 20260801 \
        --meter-topic meter.readings --transformer-topic transformer.readings \
        --meter-rate "$METER_RATE" --transformer-rate "$TRANSFORMER_RATE"
    echo "Waiting for Spark's last micro-batch to commit..."
    sleep 70
    verify_counts compose-service \
      "s3a://$BUCKET/bronze/meter_readings" "$METER_LINES" \
      "s3a://$BUCKET/bronze/transformer_readings" "$TRANSFORMER_LINES"
    echo "FULLDAY RUN PASS. Run ID: $RUN_ID. Spark remains running."
    ;;

  demo)
    # Sends ONE hour (or one 15-min slot) of day 2 through the running pipeline.
    # Day 2 is new to the pipeline, so the dashboard changes. Use a NEW hour every time:
    # an hour that was already sent is dropped as duplicates and nothing changes.
    need_tools
    SLOT="${2:-}"
    if [[ ! "$SLOT" =~ ^[0-9]{2}(:[0-9]{2})?$ ]]; then
      echo "Usage: $0 demo HH   (e.g. demo 01)   or   $0 demo HH:MM   (e.g. demo 01:15)" >&2
      exit 2
    fi
    OUT_DIR="$(ls -dt output/full_*/ 2>/dev/null | head -1)"
    OUT_DIR="${OUT_DIR%/}"
    if [[ -z "$OUT_DIR" || ! -f "$OUT_DIR/meter_readings_20260802.jsonl" ]]; then
      echo "ERROR: no full-run output with day 2 found (output/full_*/meter_readings_20260802.jsonl)." >&2
      exit 2
    fi
    touch demo_history.txt
    if grep -qx "$SLOT" demo_history.txt; then
      echo "WARNING: $SLOT was already sent before; it will be dropped as duplicates." >&2
      echo "Hours already used: $(tr '\n' ' ' < demo_history.txt)" >&2
      echo "Pick another one, or rerun with DEMO_FORCE=YES to send it anyway." >&2
      [[ "${DEMO_FORCE:-}" == "YES" ]] || exit 2
    fi
    METER_RATE="${METER_RATE:-2000}"
    TRANSFORMER_RATE="$(python3 -c "print(max(1, int(${METER_RATE}) // 200))")"
    docker compose up -d kafka spark-bronze >/dev/null
    wait_for_kafka_start "checkpoints/bronze"
    echo "Demo: sending day 2, ts starting 2026-08-02T${SLOT}, at ${METER_RATE} readings/sec."
    echo "Make sure the silver job is running (docker compose --profile silver up spark-silver)."
    docker compose --profile tools run --rm --no-deps \
      -v "$PWD/$OUT_DIR:/app/run-output:ro" simulator \
      python simulator/producer.py --input-dir /app/run-output --date 20260802 \
        --meter-topic meter.readings --transformer-topic transformer.readings \
        --meter-rate "$METER_RATE" --transformer-rate "$TRANSFORMER_RATE" \
        --only-ts "2026-08-02T${SLOT}"
    echo "$SLOT" >> demo_history.txt
    echo "DEMO SENT: 2026-08-02T${SLOT}. Next: wait for the silver batch, then load (03_load_raw.sql) + dbt run, then refresh Power BI."
    ;;

  *)
    echo "Unknown mode: $MODE" >&2
    echo "Usage: $0 {smoke|good|fullday|demo HH[:MM]|clean-bronze|status}" >&2
    exit 2
    ;;
esac
