#!/usr/bin/env bash
# Empties the S3 lake so the full run starts clean.
# Deletes: bronze/ silver/ dlq/ export/ checkpoints/ grid/ truth/ smoke/
# Why: old test files (dev data, smoke tests, earlier runs) would mix with the real run.
set -Eeuo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [[ -f .env ]]; then set -a; source .env; set +a; fi
BUCKET="${S3_BUCKET:-sic-grad-project}"
REGION="${AWS_REGION:-eu-north-1}"

if [[ "${CONFIRM_RESET:-}" != "YES" ]]; then
  echo "This deletes everything in s3://$BUCKET under:"
  echo "  bronze/ silver/ dlq/ export/ checkpoints/ grid/ truth/ smoke/"
  echo "Run again with CONFIRM_RESET=YES to do it."
  exit 2
fi

echo "Stopping the Spark jobs first..."
docker compose stop spark-bronze >/dev/null 2>&1 || true
docker compose --profile silver stop spark-silver >/dev/null 2>&1 || true

for prefix in bronze silver dlq export checkpoints grid truth smoke; do
  echo "Deleting s3://$BUCKET/$prefix/ ..."
  aws s3 rm "s3://$BUCKET/$prefix/" --recursive --region "$REGION" --only-show-errors || true
done

echo "Deleting local Spark checkpoints..."
find checkpoints -mindepth 1 ! -name .gitkeep -exec rm -rf {} + 2>/dev/null || true

echo "Done. What is left in the bucket:"
aws s3 ls "s3://$BUCKET/" --region "$REGION" || true
