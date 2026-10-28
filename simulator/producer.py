#!/usr/bin/env python3
"""Publish a selected day's JSONL files to Kafka, optionally rate-limited.

Raw lines are forwarded byte-for-byte (except line separators). Intentional malformed
records and duplicates are preserved for downstream QA/DLQ tests. The producer does
not publish sim_truth.jsonl or grid CSV files.
"""
import argparse
import re
import os
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from threading import Lock

from kafka import KafkaProducer
from kafka.errors import KafkaError

class AckCounter:
    def __init__(self):
        self.ok = 0
        self.failed = 0
        self.lock = Lock()
    def success(self, _metadata):
        with self.lock:
            self.ok += 1
    def failure(self, _exc):
        with self.lock:
            self.failed += 1

def wait_for_broker(bootstrap_servers: str, timeout_seconds: int) -> None:
    deadline = time.monotonic() + timeout_seconds
    last_error = None
    while time.monotonic() < deadline:
        probe = None
        try:
            probe = KafkaProducer(
                bootstrap_servers=bootstrap_servers, acks="all", max_block_ms=5000,
                request_timeout_ms=10000, api_version=(3, 9, 1),
            )
            if probe.bootstrap_connected():
                probe.close(timeout=5)
                print(f"Kafka is reachable at {bootstrap_servers}", flush=True)
                return
            probe.close(timeout=2)
            last_error = RuntimeError("Producer has not connected to a broker")
        except Exception as exc:
            last_error = exc
            if probe is not None:
                try: probe.close(timeout=1)
                except Exception: pass
            time.sleep(2)
    raise RuntimeError(f"Could not connect to Kafka at {bootstrap_servers} within {timeout_seconds}s: {last_error}")

def publish_file(producer, topic: str, path: Path, batch_size: int, rate: float, max_messages: int | None,
                 ts_filter=None):
    counter = AckCounter()
    queued = immediate_errors = 0
    started = time.monotonic()
    with path.open("rb") as stream:
        for raw_line in stream:
            if max_messages is not None and queued >= max_messages:
                break
            # Demo mode: only send readings whose time starts with the prefix (e.g. one hour).
            if ts_filter is not None and not ts_filter.search(raw_line):
                continue
            # Rate is records/sec. Sleep before sending so the observed rate is bounded.
            if rate > 0:
                target = started + (queued / rate)
                delay = target - time.monotonic()
                if delay > 0:
                    time.sleep(delay)
            value = raw_line.rstrip(b"\r\n")
            try:
                future = producer.send(topic, value=value)
                future.add_callback(counter.success)
                future.add_errback(counter.failure)
                queued += 1
            except KafkaError as exc:
                immediate_errors += 1
                print(f"Immediate Kafka send error in {path.name}: {exc}", file=sys.stderr, flush=True)
            if queued and queued % batch_size == 0:
                producer.flush()
                elapsed = max(time.monotonic() - started, 0.001)
                print(f"{path.name}: queued {queued:,} messages ({queued / elapsed:.1f} msgs/sec current average)", flush=True)
    producer.flush()
    failed = immediate_errors + counter.failed
    elapsed = max(time.monotonic() - started, 0.001)
    print(f"{path.name} -> topic '{topic}': queued={queued:,}, acknowledged={counter.ok:,}, failed={failed:,}, elapsed={elapsed:.1f}s, average_rate={queued/elapsed:.1f}/s", flush=True)
    return counter.ok, failed, queued

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input-dir", default="/app/output", help="Directory with daily JSONL files")
    parser.add_argument("--date", help="Day to publish as YYYYMMDD, e.g. 20260802. If omitted, legacy single-file names are used.")
    parser.add_argument("--bootstrap-servers", default=os.getenv("KAFKA_BOOTSTRAP_SERVERS", "kafka:29092"))
    parser.add_argument("--meter-topic", default="meter.readings")
    parser.add_argument("--transformer-topic", default="transformer.readings")
    parser.add_argument("--wait-seconds", type=int, default=90)
    parser.add_argument("--batch-size", type=int, default=5000)
    parser.add_argument("--meter-rate", type=float, default=0, help="Max meter messages/sec; 0 means unlimited")
    parser.add_argument("--transformer-rate", type=float, default=0, help="Max transformer messages/sec; 0 means unlimited")
    parser.add_argument("--max-meter-messages", type=int, help="Optional cap for a golden streaming run")
    parser.add_argument("--max-transformer-messages", type=int, help="Optional cap for a golden streaming run")
    parser.add_argument("--only-ts", help='Only send readings whose ts starts with this, e.g. "2026-08-02T01" (one hour)')
    args = parser.parse_args()

    if args.batch_size < 1: parser.error("--batch-size must be >= 1")
    if args.meter_rate < 0 or args.transformer_rate < 0: parser.error("rates must be >= 0")
    if args.max_meter_messages is not None and args.max_meter_messages < 1: parser.error("--max-meter-messages must be >= 1")
    if args.max_transformer_messages is not None and args.max_transformer_messages < 1: parser.error("--max-transformer-messages must be >= 1")

    input_dir = Path(args.input_dir)
    if args.date:
        if len(args.date) != 8 or not args.date.isdigit(): parser.error("--date must be YYYYMMDD")
        meter_path = input_dir / f"meter_readings_{args.date}.jsonl"
        transformer_path = input_dir / f"transformer_readings_{args.date}.jsonl"
    else:
        meter_path = input_dir / "meter_readings.jsonl"
        transformer_path = input_dir / "transformer_readings.jsonl"
    missing = [str(path) for path in (meter_path, transformer_path) if not path.is_file()]
    if missing:
        print("Missing input file(s): " + ", ".join(missing), file=sys.stderr)
        return 2

    try:
        wait_for_broker(args.bootstrap_servers, args.wait_seconds)
        producer = KafkaProducer(
            bootstrap_servers=args.bootstrap_servers, acks="all", retries=5,
            max_in_flight_requests_per_connection=1, linger_ms=10,
            request_timeout_ms=40000, max_block_ms=30000,
            api_version=(3, 9, 1),
        )
    except Exception as exc:
        print(f"Kafka initialization failed: {exc}", file=sys.stderr)
        return 2

    # Matches both "ts":"..." (meter files) and "ts": "..." (transformer files)
    ts_prefix = re.compile(rb'"ts":\s*"' + re.escape(args.only_ts.encode())) if args.only_ts else None
    if ts_prefix:
        print(f"Only sending readings with ts starting {args.only_ts}", flush=True)
    total_ok = total_failed = total_queued = 0
    try:
        # Publish both datasets concurrently so meter and transformer event streams overlap.
        with ThreadPoolExecutor(max_workers=2) as pool:
            meter_future = pool.submit(publish_file, producer, args.meter_topic, meter_path,
                                       args.batch_size, args.meter_rate, args.max_meter_messages, ts_prefix)
            transformer_future = pool.submit(publish_file, producer, args.transformer_topic, transformer_path,
                                             args.batch_size, args.transformer_rate, args.max_transformer_messages,
                                             ts_prefix)
            results = [meter_future.result(), transformer_future.result()]
        for ok, failed, queued in results:
            total_ok += ok
            total_failed += failed
            total_queued += queued
    finally:
        producer.close(timeout=60)

    print(f"Publish finished: queued={total_queued:,}; acknowledged={total_ok:,}; failed={total_failed:,}", flush=True)
    if total_failed:
        print("Some messages failed. Do not assume complete delivery.", file=sys.stderr)
        return 1
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
