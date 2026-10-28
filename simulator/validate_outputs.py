#!/usr/bin/env python3
"""Validate either daily JSONL files (current guide) or legacy single JSONL files."""
import csv
import glob
import json
import math
import os
import sys
from collections import Counter

out = sys.argv[1] if len(sys.argv) > 1 else "output"
errors = []

def is_number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)

def source_files(pattern, legacy_name):
    found = sorted(glob.glob(os.path.join(out, pattern)))
    if found:
        return found
    legacy = os.path.join(out, legacy_name)
    return [legacy] if os.path.isfile(legacy) else []

def validate_readings(paths, kind):
    if not paths:
        errors.append(f"Missing {kind} reading file(s) in {out}")
        return
    total = valid = invalid = 0
    ids, timestamps = set(), set()
    energy_error = 0.0
    required = (("meter_id", "ts", "kw", "kvar", "voltage", "kwh") if kind == "Meter"
                else ("transformer_id", "ts", "kw", "kwh", "voltage"))
    numeric = (("kw", "kvar", "voltage", "kwh") if kind == "Meter" else ("kw", "kwh", "voltage"))
    id_col = "meter_id" if kind == "Meter" else "transformer_id"
    for path in paths:
        with open(path, encoding="utf-8") as stream:
            for line in stream:
                total += 1
                try:
                    row = json.loads(line)
                    if not isinstance(row, dict) or not all(k in row for k in required):
                        invalid += 1
                        continue
                    if not isinstance(row[id_col], str) or not row[id_col].strip():
                        invalid += 1
                        continue
                    if not isinstance(row["ts"], str) or not row["ts"].strip():
                        invalid += 1
                        continue
                    if not all(is_number(row[k]) for k in numeric):
                        invalid += 1
                        continue
                    if row["kw"] < 0 or row["kwh"] < 0 or row["voltage"] <= 0 or (kind == "Meter" and row["kvar"] < 0):
                        invalid += 1
                        continue
                    valid += 1
                    ids.add(row[id_col])
                    timestamps.add(row["ts"])
                    if kind == "Meter":
                        energy_error = max(energy_error, abs(float(row["kwh"]) - float(row["kw"]) * 0.25))
                except (json.JSONDecodeError, TypeError, ValueError):
                    invalid += 1
    print(f"{kind} files: {len(paths)}; lines: {total:,}; valid: {valid:,}; malformed/invalid: {invalid:,}")
    print(f"Distinct valid {kind.lower()} IDs: {len(ids):,}; timestamps: {len(timestamps):,}")
    if kind == "Meter":
        print(f"Max |kWh - kW*0.25| on valid meter readings: {energy_error:.6f}")
        if energy_error > 0.00018:
            errors.append(f"Meter kWh arithmetic deviation too large: {energy_error:.6f} (allowed 0.00018)")
    return total, valid, invalid

meter_paths = source_files("meter_readings_*.jsonl", "meter_readings.jsonl")
transformer_paths = source_files("transformer_readings_*.jsonl", "transformer_readings.jsonl")
validate_readings(meter_paths, "Meter")
validate_readings(transformer_paths, "Transformer")

truth_path = os.path.join(out, "sim_truth.jsonl")
if not os.path.isfile(truth_path):
    errors.append(f"Missing file: {truth_path}")
else:
    count = missing_id = malformed = 0
    types = Counter()
    with open(truth_path, encoding="utf-8") as stream:
        for line in stream:
            try:
                row = json.loads(line)
                count += 1
                types[row.get("event_type", "unknown") if isinstance(row, dict) else "unknown"] += 1
                if not isinstance(row, dict) or not row.get("anomaly_id"):
                    missing_id += 1
            except (json.JSONDecodeError, TypeError):
                malformed += 1
    print(f"Ground-truth events: {count}; event types: {dict(types)}; missing IDs: {missing_id}; malformed: {malformed}")
    if missing_id or malformed:
        errors.append("sim_truth has missing anomaly IDs or malformed rows")

grid_dir = os.path.join(out, "grid")
meter_grids = sorted(glob.glob(os.path.join(grid_dir, "meters_*.csv")))
transformer_grids = sorted(glob.glob(os.path.join(grid_dir, "transformers_*.csv")))
print(f"Daily grid files: {len(meter_grids)} meter-list; {len(transformer_grids)} transformer-list")
if not meter_grids or not transformer_grids:
    errors.append("Missing daily grid CSV files")
for path, required_columns in (
    [(p, ["meter_id", "transformer_id", "tariff", "customer_name", "address", "lat", "lon"]) for p in meter_grids]
    + [(p, ["transformer_id", "feeder_id", "substation_id", "district", "governorate", "capacity_kw", "lat", "lon"]) for p in transformer_grids]
):
    try:
        with open(path, newline="", encoding="utf-8") as stream:
            header = next(csv.reader(stream))
        if not set(required_columns).issubset(header):
            errors.append(f"Unexpected/missing columns in {path}")
    except Exception as exc:
        errors.append(f"Cannot read {path}: {exc}")

if errors:
    print("VALIDATION FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)
print("VALIDATION PASSED: daily/legacy file structure and clean-row arithmetic checked; intentional dirty rows are excluded.")
print("Note: this does not test anomaly detection, Kafka delivery, Spark watermarking, or S3 upload.")
