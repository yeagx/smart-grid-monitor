"""Smart grid data simulator.

Writes these files (one set per simulated day):
  meter_readings_YYYYMMDD.jsonl        home readings every 15 min, one file per day (dirty + late data mixed in)
  transformer_readings_YYYYMMDD.jsonl  transformer readings every 15 min, one file per day
  grid/meters_YYYYMMDD.csv    daily meter list (who is on which transformer)
  grid/transformers_YYYYMMDD.csv  daily transformer list
  sim_truth.jsonl             answer key of every injected problem

Install:  pip install numpy pyyaml
Usage:    python simulator/generator.py --config config.yaml --out output/
"""
import argparse
import csv
import json
import os
import time
from datetime import datetime, timedelta, timezone

import numpy as np
import yaml


# 5 districts, one substation each: (district, governorate, lat, lon)
DISTRICTS = [
    ("Dokki", "Giza", 30.0384, 31.2109),
    ("Nasr City", "Cairo", 30.0566, 31.3302),
    ("Maadi", "Cairo", 29.9602, 31.2569),
    ("Heliopolis", "Cairo", 30.0911, 31.3227),
    ("Shubra", "Cairo", 30.0866, 31.2444),
]
FEEDERS_PER_SUBSTATION = 5

# Daily usage shape, one value per hour 0..24 (smooth, no sudden jumps).
HOME_SHAPE = [0.35, 0.32, 0.30, 0.30, 0.32, 0.38, 0.50, 0.62, 0.60, 0.50, 0.45, 0.45,
              0.48, 0.50, 0.50, 0.52, 0.58, 0.70, 0.85, 0.97, 1.00, 0.95, 0.80, 0.55, 0.35]
SHOP_SHAPE = [0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.20, 0.30, 0.55, 0.85, 1.00,
              1.00, 1.00, 0.95, 0.95, 1.00, 1.00, 1.00, 1.00, 0.95, 0.80, 0.45, 0.25, 0.15]
HOME_PEAK_KW = 2.6


def iso(t):
    return t.strftime("%Y-%m-%dT%H:%M:%SZ")


def hhmm_to_slot(hhmm, interval):
    h, m = map(int, hhmm.split(":"))
    return (h * 60 + m) // interval


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--config", default="config.yaml")
    ap.add_argument("--out", default="output")
    args = ap.parse_args()
    cfg = yaml.safe_load(open(args.config))
    generation_started = time.perf_counter()

    rng = np.random.default_rng(cfg["seed"])
    N, M = cfg["meters"], cfg["transformers"]
    interval = cfg["interval_minutes"]
    slots = 24 * 60 // interval
    days = cfg["days"]
    start = datetime.fromisoformat(cfg["start_date"]).replace(tzinfo=timezone.utc)
    per_tr = N // M
    assert per_tr * M == N, "meters must split evenly across transformers"

    os.makedirs(os.path.join(args.out, "grid"), exist_ok=True)

    # ---------- the world ----------
    t_ids = [f"T-{i + 1:04d}" for i in range(M)]
    m_ids = [f"M-{i + 1:06d}" for i in range(N)]
    # Spread transformers over districts, at least 2 per district, so a meter can always
    # be moved to a neighbouring transformer (dev mode: 5 transformers -> 2 districts).
    n_districts = max(1, min(len(DISTRICTS), M // 2))
    t_district = np.arange(M) * n_districts // M
    t_feeder = []
    for t in range(M):
        d = t_district[t]
        same = np.where(t_district == d)[0]
        pos = int(np.where(same == t)[0][0])
        t_feeder.append(d * FEEDERS_PER_SUBSTATION + pos * FEEDERS_PER_SUBSTATION // len(same))
    t_lat = np.array([DISTRICTS[d][2] for d in t_district]) + rng.uniform(-0.012, 0.012, M)
    t_lon = np.array([DISTRICTS[d][3] for d in t_district]) + rng.uniform(-0.012, 0.012, M)

    meter_tr = np.repeat(np.arange(M), per_tr)                    # 200 homes per transformer
    r = 0.0018 * np.sqrt(rng.uniform(0, 1, N))               # homes within ~200 m
    a = rng.uniform(0, 2 * np.pi, N)
    m_lat = t_lat[meter_tr] + r * np.cos(a)
    m_lon = t_lon[meter_tr] + r * np.sin(a) / np.cos(np.radians(30))
    is_shop = rng.uniform(0, 1, N) < cfg["commercial_share"]
    scale = np.where(is_shop, rng.uniform(3, 8, N), rng.lognormal(0, 0.35, N) * HOME_PEAK_KW)
    kvar_ratio = rng.uniform(0.30, 0.50, N)
    v_drop = rng.uniform(0, 4, N)                             # farther homes get slightly lower voltage

    # Fake Egyptian names and streets (all invented, no real people)
    first = ["Ahmed", "Mohamed", "Mahmoud", "Mostafa", "Omar", "Youssef", "Karim", "Tarek", "Hassan", "Amr",
             "Nour", "Mariam", "Salma", "Hana", "Yasmin", "Aya", "Rana", "Dina", "Laila", "Farida"]
    last = ["Hassan", "Ali", "Ibrahim", "Saad", "Fathy", "Nabil", "Adel", "Samir", "Kamal", "Fouad",
            "Mansour", "Shawky", "Rashad", "Zaki", "Hamdy", "Lotfy", "Sabry", "Ezzat", "Galal", "Mounir"]
    street_list = ["Tahrir St", "Mesaha Sq", "Abbas El Akkad St", "Makram Ebeid St", "Road 9", "Road 233",
                   "El Merghany St", "El Nozha St", "Shubra St", "Khalafawy Sq", "Nile Corniche", "Gameat El Dowal St"]
    names = [f"{first[rng.integers(20)]} {last[rng.integers(20)]}" for _ in range(N)]
    streets = [street_list[rng.integers(len(street_list))] for _ in range(N)]
    addresses = [f"{rng.integers(1, 120)} {streets[i]}, {DISTRICTS[t_district[meter_tr[i]]][0]}" for i in range(N)]

    hours = np.arange(slots) * interval / 60
    home_shape = np.interp(hours, np.arange(25), HOME_SHAPE)
    shop_shape = np.interp(hours, np.arange(25), SHOP_SHAPE)

    # Capacity: sized so a normal evening peak is ~75% of capacity.
    expected = np.zeros((M, slots))
    for s in range(slots):
        load = scale * np.where(is_shop, shop_shape[s], home_shape[s])
        expected[:, s] = np.bincount(meter_tr, weights=load, minlength=M) * (1 + cfg["cable_loss"])
    capacity = np.round(expected.max(axis=1) / cfg["normal_peak_load"], -1)

    # ---------- pick where problems happen (different transformers for each) ----------
    an = cfg["anomalies"]
    order = rng.permutation(M)
    n_ov, n_ld, n_th = an["overload"]["transformers"], an["line_drop"]["transformers"], an["theft"]["transformers"]
    assert n_ov + n_ld + n_th <= M, "not enough transformers for the anomalies"
    ov_t = order[:n_ov]
    ld_t = order[n_ov:n_ov + n_ld]
    th_t = order[n_ov + n_ld:n_ov + n_ld + n_th]

    truth = []

    def add_truth(kind, entity, tr, t0, t1, **details):
        truth.append({"anomaly_id": f"A-{len(truth) + 1:04d}", "event_type": kind, "entity_id": entity,
                      "transformer_id": tr, "start_ts": iso(t0), "end_ts": iso(t1), **details})

    share = np.ones(N)                                        # share of real use the meter records
    lo, hi = an["theft"]["recorded_share"]
    thieves = []
    for t in th_t:
        homes = np.where(meter_tr == t)[0]
        pick = rng.choice(homes, an["theft"]["thieves_per_transformer"], replace=False)
        share[pick] = rng.uniform(lo, hi, len(pick))
        thieves.extend(pick.tolist())
        for m in pick:
            add_truth("theft", m_ids[m], t_ids[t], start, start + timedelta(days=days),
                      recorded_share=round(float(share[m]), 2))
    thieves = set(thieves)

    def window(spec):
        d0 = start + timedelta(days=spec["day"] - 1)
        s0 = hhmm_to_slot(spec["start"], interval) + (spec["day"] - 1) * slots
        return d0, s0, s0 + spec["hours"] * 60 // interval

    ov_day, ov_s0, ov_s1 = window(an["overload"])
    for t in ov_t:
        t0 = ov_day + timedelta(minutes=(ov_s0 % slots) * interval)
        add_truth("overload", t_ids[t], t_ids[t], t0, t0 + timedelta(hours=an["overload"]["hours"]),
                  factor=an["overload"]["factor"])
    ld_day, ld_s0, ld_s1 = window(an["line_drop"])
    for t in ld_t:
        t0 = ld_day + timedelta(minutes=(ld_s0 % slots) * interval)
        add_truth("line_drop", t_ids[t], t_ids[t], t0, t0 + timedelta(hours=an["line_drop"]["hours"]),
                  voltage_factor=an["line_drop"]["voltage_factor"])

    silent = {}
    candidates = np.array([m for m in range(N) if m not in thieves])
    for m in rng.choice(candidates, an["silent_meters"]["meters"], replace=False):
        h = int(rng.integers(an["silent_meters"]["hours"][0], an["silent_meters"]["hours"][1] + 1))
        s0 = int(rng.integers(0, slots - h * 60 // interval))
        silent[int(m)] = (s0, s0 + h * 60 // interval)
        t0 = start + timedelta(minutes=s0 * interval)
        add_truth("silent_meter", m_ids[m], t_ids[meter_tr[m]], t0, t0 + timedelta(hours=h))

    # ---------- open the output files ----------
    dup_p = cfg["dirty_data"]["duplicate_pct"] / 100
    bad_p = cfg["dirty_data"]["malformed_pct"] / 100
    counts = {"clean_meter_rows": 0, "duplicates": 0, "malformed": 0, "late": 0, "transformer_rows": 0}
    late_cfg = cfg.get("late_data", {"late_pct": 0, "delay_slots": [1, 1]})
    late_p = late_cfg["late_pct"] / 100
    d_lo, d_hi = late_cfg["delay_slots"]
    waiting = {}                                              # slot number -> late readings sent in that slot

    tariff = np.where(is_shop, "commercial", "residential").astype(object)
    names_today = list(names)
    typo_meters = rng.choice(N, cfg["grid_changes_per_day"]["name_fixes"], replace=False)
    for m in typo_meters:                                     # day 1 names have a typo, fixed on day 2
        names_today[m] = names[m][:-1]
    gc = cfg["grid_changes_per_day"]
    movable = np.array([m for m in range(N) if m not in thieves and m not in silent])

    for d in range(days):
        day = start + timedelta(days=d)
        if d > 0:                                             # small daily grid changes (SCD)
            for m in rng.choice(movable, max(1, round(N * gc["moved_meters_pct"] / 100)), replace=False):
                # Rewired to the NEAREST other transformer in the same district.
                # The home itself does not move: its lat/lon stay the same.
                same = [t for t in range(M) if t != meter_tr[m] and t_district[t] == t_district[meter_tr[m]]]
                dist = [(t_lat[t] - m_lat[m]) ** 2 + (t_lon[t] - m_lon[m]) ** 2 for t in same]
                meter_tr[m] = same[int(np.argmin(dist))]
            for m in rng.choice(movable, max(2, round(N * gc["tariff_changes_pct"] / 100)), replace=False):
                tariff[m] = "commercial" if tariff[m] == "residential" else "residential"
            if d == 1:
                for m in typo_meters:
                    names_today[m] = names[m]

        tag = day.strftime("%Y%m%d")
        # One file per day: day 1 = the batch load, day 2 = the stream (full mode)
        f_m = open(os.path.join(args.out, f"meter_readings_{tag}.jsonl"), "w")
        f_t = open(os.path.join(args.out, f"transformer_readings_{tag}.jsonl"), "w")
        with open(os.path.join(args.out, "grid", f"meters_{tag}.csv"), "w", newline="") as g:
            w = csv.writer(g)
            w.writerow(["meter_id", "transformer_id", "tariff", "customer_name", "address", "lat", "lon"])
            for i in range(N):
                w.writerow([m_ids[i], t_ids[meter_tr[i]], tariff[i], names_today[i], addresses[i],
                            f"{m_lat[i]:.6f}", f"{m_lon[i]:.6f}"])
        with open(os.path.join(args.out, "grid", f"transformers_{tag}.csv"), "w", newline="") as g:
            w = csv.writer(g)
            w.writerow(["transformer_id", "feeder_id", "substation_id", "district", "governorate",
                        "capacity_kw", "lat", "lon"])
            for t in range(M):
                dd = t_district[t]
                w.writerow([t_ids[t], f"F-{t_feeder[t] + 1:02d}", f"S-{dd + 1:02d}", DISTRICTS[dd][0],
                            DISTRICTS[dd][1], int(capacity[t]), f"{t_lat[t]:.6f}", f"{t_lon[t]:.6f}"])

        heat = 1.0 if d == 0 else rng.uniform(0.95, 1.05)
        for s in range(slots):
            gs = d * slots + s                                # global slot number
            ts = iso(day + timedelta(minutes=s * interval))
            shape = np.where(tariff == "commercial", shop_shape[s], home_shape[s]).astype(float)
            real_kw = scale * shape * heat * rng.lognormal(0, 0.08, N)
            if ov_s0 <= gs < ov_s1:                           # overload: the HOMES use more
                real_kw[np.isin(meter_tr, ov_t)] *= an["overload"]["factor"]
            rec_kw = real_kw * share                          # thieves' meters record less

            t_real = np.bincount(meter_tr, weights=real_kw, minlength=M) * (1 + cfg["cable_loss"])
            t_kw = t_real * rng.normal(1, 0.003, M)
            load = t_kw / capacity
            t_volt = 228 - 10 * load + rng.normal(0, 0.3, M)  # voltage drops a little as load rises
            if ld_s0 <= gs < ld_s1:
                t_volt[ld_t] *= an["line_drop"]["voltage_factor"]
            m_volt = t_volt[meter_tr] - v_drop + rng.normal(0, 0.8, N)
            kvar = rec_kw * kvar_ratio * rng.normal(1, 0.03, N)

            t_kwh = t_kw * 0.25
            for t in range(M):
                f_t.write(json.dumps({"transformer_id": t_ids[t], "ts": ts, "kw": round(float(t_kw[t]), 3),
                                      "kwh": round(float(t_kwh[t]), 4),
                                      "voltage": round(float(t_volt[t]), 2)}) + "\n")
            counts["transformer_rows"] += M

            lines = []
            for i in range(N):
                if i in silent and d == 0 and silent[i][0] <= s < silent[i][1]:
                    continue                                  # silent meter: no reading at all
                lines.append(f'{{"meter_id":"{m_ids[i]}","ts":"{ts}","kw":{rec_kw[i]:.3f},'
                             f'"kvar":{kvar[i]:.3f},"voltage":{m_volt[i]:.2f},"kwh":{rec_kw[i] * 0.25:.4f}}}')
            counts["clean_meter_rows"] += len(lines)
            extra = []
            for idx in np.where(rng.uniform(0, 1, len(lines)) < dup_p)[0]:
                extra.append(lines[idx])                      # exact copy = duplicate
            counts["duplicates"] += len(extra)
            n_bad = rng.binomial(len(lines), bad_p)
            for _ in range(n_bad):
                kind = rng.integers(5)
                src = lines[rng.integers(len(lines))]
                if kind == 0:
                    bad = src[: rng.integers(5, len(src) - 5)]            # cut off mid-message
                elif kind == 1:
                    bad = src.replace(src[13:21], "")                     # empty meter_id
                elif kind == 2:
                    bad = src.replace('"kw":', '"kw":-', 1)                # negative power
                elif kind == 3:
                    bad = src.replace('"kw":', '"kw":"abc","old_kw":', 1)  # text instead of a number
                else:
                    bad = src.replace(f'"ts":"{ts}",', "")                 # missing time
                extra.append(bad)
            counts["malformed"] += n_bad
            # Late readings: keep their real time, but send them 1 to 8 slots later (15 min to 2 h).
            on_time = []
            for line in lines:
                if rng.uniform() < late_p:
                    arrive = gs + int(rng.integers(d_lo, d_hi + 1))
                    waiting.setdefault(arrive, []).append(line)
                    counts["late"] += 1
                else:
                    on_time.append(line)
            batch = on_time + extra + waiting.pop(gs, [])
            for j in rng.permutation(len(batch)):             # dirty rows land at random positions
                f_m.write(batch[j] + "\n")

        if d < days - 1:                                      # last day stays open for the leftovers below
            f_m.close()
            f_t.close()

    for arrive in sorted(waiting):                            # late readings from the last slots
        for line in waiting[arrive]:
            f_m.write(line + "\n")
    f_m.close()
    f_t.close()

    with open(os.path.join(args.out, "sim_truth.jsonl"), "w") as f:
        for row in truth:
            f.write(json.dumps(row) + "\n")

    # Persist measured generation metrics so the README can distinguish measurements from estimates.
    elapsed = time.perf_counter() - generation_started
    files = {}
    for root, _, filenames in os.walk(args.out):
        for filename in sorted(filenames):
            path = os.path.join(root, filename)
            rel = os.path.relpath(path, args.out)
            if rel == "run_metrics.json":
                continue
            item = {"size_bytes": os.path.getsize(path)}
            if filename.endswith(".jsonl"):
                with open(path, "rb") as source:
                    item["lines"] = sum(1 for _ in source)
            files[rel] = item
    metrics = {
        "seed": cfg["seed"],
        "start_date": cfg["start_date"],
        "days": days,
        "meters": N,
        "transformers": M,
        "interval_minutes": interval,
        "generation_elapsed_seconds": round(elapsed, 3),
        "counts": counts,
        "truth_events": len(truth),
        "files": files,
    }
    with open(os.path.join(args.out, "run_metrics.json"), "w", encoding="utf-8") as f:
        json.dump(metrics, f, indent=2)
        f.write("\n")

    print(f"Done: {counts['clean_meter_rows']:,} meter readings "
          f"(of them {counts['late']:,} sent late; +{counts['duplicates']:,} duplicates, "
          f"+{counts['malformed']:,} broken), "
          f"{counts['transformer_rows']:,} transformer readings, {len(truth)} planted problems "
          f"-> {args.out}")
    print(f"Generation elapsed: {elapsed:.3f}s; metrics: {os.path.join(args.out, 'run_metrics.json')}")


if __name__ == "__main__":
    main()
