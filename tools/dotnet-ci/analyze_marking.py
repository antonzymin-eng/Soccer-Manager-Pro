#!/usr/bin/env python3
"""Decode and check the W13 §10 capture, then report M1–M8 per match and stratum."""

import argparse
import csv
import gzip
import json
from collections import Counter
from pathlib import Path

import numpy as np


SEEDS = tuple(f"{seed:016X}" for seed in (1, 0x5A5A5A5A, 0x99887766, 0xABCDEF12, 0xD1A6D05E, 0x0F1E2D3C4B5A6978))
RADII = (3, 6, 8, 15)
SAMPLE = np.dtype([("frame", "<i4"), ("inactive", "<u4"), ("keeper", "<u4"), ("points", "<f4", (22, 2, 2))])
MARK = np.dtype([("mode", "u1"), ("target", "i1"), ("active", "u1"), ("point", "<f4", (2,))])
WITNESS = np.dtype([("team", "i1"), ("x", "<f4")])


def require(condition, message):
    if not condition:
        raise ValueError(message)


def binary(path, name, dtype):
    with gzip.open(path / (name + ".bin.gz"), "rb") as stream:
        return np.frombuffer(stream.read(), dtype=dtype)


def write_csv(path, rows, columns):
    with path.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, columns)
        writer.writeheader()
        for row in rows:
            writer.writerow({k: json.dumps(v, separators=(",", ":")) if isinstance(v, (dict, list)) else v
                             for k, v in row.items()})


def percentiles(values):
    if not values.size:
        return [None] * 5
    finite = np.sort(values)
    # Nearest rank handles an infinite distance (all defenders inactive).
    ranks = np.ceil(np.array([.10, .25, .50, .75, .90]) * len(finite)).astype(int)
    return [float(v) if np.isfinite(v) else "inf" for v in finite[ranks - 1]]


def share(numerator, denominator):
    return {"n": int(numerator), "d": int(denominator),
            "share": float(numerator / denominator) if denominator else None}


def spell_labels(frames, witness, events, full_time):
    """Label 10 Hz sampled spells using intervening ledger events."""
    owners = witness["team"][frames - 1]
    restart = np.zeros((len(frames), 2), dtype=bool)
    any_goal = np.zeros(len(frames), dtype=bool)
    for event in events:
        index = np.searchsorted(frames, event["frame"])
        if index == len(frames):
            continue
        if event["event"] == "restart":
            restart[index, event["team"]] = True
        elif event["event"] == "goal":
            any_goal[index] = True
    starts = np.zeros(len(frames), dtype=bool)
    origin = np.zeros(len(frames), dtype=bool)
    previous = -1
    restart_origin = False
    for i, team in enumerate(owners):
        if team >= 0 and (team != previous or restart[i, team] or any_goal[i]):
            starts[i] = True
            restart_origin = restart[i, team] or any_goal[i]
        origin[i] = team >= 0 and restart_origin
        previous = team
    censored = len(frames) > 0 and full_time and owners[-1] >= 0 and witness["team"][-1] == owners[-1]
    return owners, starts, origin, censored


def distances(points, active_defender, attackers, defenders, defending_team):
    position_a = points[:, attackers]
    position_d = points[:, defenders]
    separation = np.linalg.norm(position_a[:, :, None, :] - position_d[:, None, :, :], axis=-1)
    separation = np.where(active_defender[:, None, :], separation, np.inf)
    goal = np.array([105.0 * defending_team, 34.0])
    a_goal = np.linalg.norm(position_a - goal, axis=-1)
    d_goal = np.linalg.norm(position_d - goal, axis=-1)
    goal_side = d_goal[:, None, :] < a_goal[:, :, None]
    nearest = separation.argmin(axis=-1)[..., None]
    gap = np.take_along_axis(separation, nearest, axis=-1)[..., 0]
    nearest_goal_side = np.take_along_axis(goal_side, nearest, axis=-1)[..., 0] & np.isfinite(gap)
    goal_side_gap = np.where(goal_side, separation, np.inf).min(axis=-1)
    return gap, nearest_goal_side, goal_side_gap


def geometry_rows(samples, witness, owners, starts, origin, censored, identity):
    """M1–M4 geometry and M7–M8 denominators and territory."""
    frame = samples["frame"]
    ball_x = witness["x"][frame - 1]
    masks = (1 << np.arange(22, dtype=np.uint32))[None, :]
    active = (samples["inactive"][:, None] & masks) == 0
    is_keeper = (samples["keeper"][:, None] & masks) != 0
    after_dismissal = samples["inactive"] != 0
    for team in (0, 1):
        defending_team = 1 - team
        attackers = np.arange(team * 11, (team + 1) * 11)
        defenders = np.arange(defending_team * 11, (defending_team + 1) * 11)
        final_third_ball = ball_x >= 70 if team == 0 else ball_x <= 35
        prior_third = np.r_[False, final_third_ball[:-1]]
        entries = (owners == team) & final_third_ball & (starts | ~prior_third)
        for restart_origin in (False, True):
            for dismissed in (False, True):
                mask = (owners == team) & (origin == restart_origin) & (after_dismissal == dismissed)
                key = dict(identity, defending_team=defending_team, restart_origin=restart_origin,
                           after_dismissal=dismissed)
                yield "strata", dict(key, samples=int(mask.sum()), spells=int((starts & mask).sum()),
                                     final_third_entries=int((entries & mask).sum()),
                                     censored=int(bool(censored and mask[-1])))
                if not mask.any():
                    continue
                for layout, index in (("position", 0), ("slot", 1)):
                    points = samples["points"][mask, :, index]
                    x = points[:, attackers, 0]
                    in_third = x >= 70 if team == 0 else x <= 35
                    for include_gk in (False, True):
                        defenders_active = active[mask][:, defenders]
                        if not include_gk:
                            defenders_active &= ~is_keeper[mask][:, defenders]
                        gap, goal_side, goal_gap = distances(points, defenders_active, attackers, defenders, defending_team)
                        for slot, agent in enumerate(attackers):
                            valid = active[mask, agent]
                            final_third = valid & in_third[:, slot]
                            row = dict(key, layout=layout, include_gk=include_gk, slot=slot,
                                       samples=int(valid.sum()), gap_percentiles=percentiles(gap[valid, slot]),
                                       nearest_goal_side=share(goal_side[valid, slot].sum(), valid.sum()))
                            for radius in RADII:
                                row[f"no_defender_{radius}m"] = share((gap[final_third, slot] > radius).sum(), final_third.sum())
                                row[f"no_goal_side_defender_{radius}m"] = share(
                                    (goal_gap[final_third, slot] > radius).sum(), final_third.sum())
                            yield "attackers", row


def goal_class(goal, shot, witness):
    team, scorer = goal["team"], goal["agent"]
    if scorer < 0 or scorer // 11 != team:
        return "own"
    if shot is not None and shot["team"] == team and shot["agent"] == scorer:
        intervening = witness["team"][shot["frame"]:goal["frame"] - 1]
        if not np.any(intervening == 1 - team):
            return "linked"
    return "uncertain"


def load_match(path, allow_short):
    metadata = dict(line.split("=", 1) for line in (path / "summary.txt").read_text().splitlines())
    events = list(csv.DictReader((path / "events.csv").read_text().splitlines()))
    for event in events:
        for field in ("frame", "team", "agent", "detail"):
            event[field] = int(event[field])
    samples = binary(path, "samples", SAMPLE)
    witness = binary(path, "witness", WITNESS)
    marks = binary(path, "marking", MARK).reshape(-1, 22)
    frames = int(metadata["frames"])
    full_time = metadata["fullTime"] == "True"
    require(allow_short or (frames == 324000 and full_time and metadata["runtime"] == "8.0.32"),
            f"{path}: expected a full match on CLR 8.0.32")
    require(len(samples) > 0 and len(samples) == len(marks) == int(metadata["samples"])
            and len(witness) == frames, f"{path}: sample totals")
    expected_frames = np.arange(6, frames + (0 if full_time else 1), 6)
    require(np.array_equal(samples["frame"], expected_frames), f"{path}: invalid sample frames")
    require(np.isfinite(samples["points"]).all() and np.isfinite(witness["x"]).all()
            and np.isfinite(marks["point"]).all(), f"{path}: non-finite capture")
    require(np.isin(witness["team"], (-1, 0, 1)).all(), f"{path}: invalid possession")
    require(np.isin(marks["mode"], (0, 1, 2, 3)).all() and np.isin(marks["active"], (0, 1)).all(),
            f"{path}: invalid assignment")
    require(np.all((marks["target"] >= -1) & (marks["target"] < 22)), f"{path}: invalid target ID")
    require(all(1 <= event["frame"] <= frames and event["team"] in (0, 1) for event in events),
            f"{path}: invalid ledger metadata")
    for name in ("shot", "goal", "foul", "offside"):
        counts = Counter(event["team"] for event in events if event["event"] == name)
        require([counts[0], counts[1]] == [int(n) for n in metadata[name + "s"].split(",")],
                f"{path}: {name} ledger totals")
    if "groundedEntries" in metadata:
        fields = ("groundedEntries", "collisionKnockdowns", "slidingTackleEntries",
                  "divingHeaderEntries", "otherGroundedEntries")
        counts = [[int(n) for n in metadata[field].split(",")] for field in fields]
        require(all(len(pair) == 2 and min(pair) >= 0 for pair in counts),
                f"{path}: invalid grounded-entry counts")
        require(counts[0] == [sum(pair[team] for pair in counts[1:]) for team in (0, 1)],
                f"{path}: grounded-entry reasons do not reconcile")
    require(metadata["nonCarrierPresses"] == "0", f"{path}: marker pressed non-carrier")
    return metadata, events, samples, witness, marks, full_time


def self_test():
    points = np.array([[[70., 34.], [75., 34.], [69., 34.]]])
    active = np.array([[True, True]])
    home = distances(points, active, np.array([0]), np.array([1, 2]), 1)
    away = distances(np.array([105, 68]) - points, active, np.array([0]), np.array([1, 2]), 0)
    require(all(np.array_equal(left, right) for left, right in zip(home, away)), "mirror geometry")
    require(home[0][0, 0] == 1 and not home[1][0, 0] and home[2][0, 0] == 5, "goal side")
    witness = np.array([(0, 80.), (-1, 60.), (0, 80.)], dtype=WITNESS)
    shot = {"team": 0, "agent": 9, "frame": 1}
    goal = {"team": 0, "agent": 9, "frame": 3}
    require(goal_class(goal, shot, witness) == "linked", "goal link")
    witness["team"][1] = 1
    require(goal_class(goal, shot, witness) == "uncertain", "intervening possession")
    require(goal_class(dict(goal, agent=20), shot, witness) == "own", "own goal")

    witnesses = np.array([(0, 74.), (0, 75.), (0, 76.), (0, 77.)], dtype=WITNESS)
    _, starts, origins, censored = spell_labels(np.array([1, 3]), witnesses,
        [{"event": "restart", "frame": 2, "team": 0}], True)
    require(starts.tolist() == [True, True] and origins.tolist() == [False, True] and censored,
            "restart-origin and full-time censoring")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--descriptive", action="store_true", help="Full-length sensitivity or scheme subset")
    parser.add_argument("--sensitivity", action="store_true", help="Compare distance or scheme variant directories")
    parser.add_argument("--allow-short", action="store_true", help="Smoke-test captures only")
    args = parser.parse_args()
    self_test()
    results = {name: [] for name in ("strata", "attackers", "territory", "shots", "goals", "matches")}
    pattern = "*/*/*/*/summary.txt" if args.sensitivity else "*/*/*/summary.txt"
    paths = sorted(args.capture.glob(pattern))
    require(paths, "No completed captures")
    strict = not (args.allow_short or args.descriptive or args.sensitivity)
    if strict:
        expected = {(arm, f"C{case}", seed) for arm, cases in (("off", range(1)), ("on", range(10)))
                    for case in cases for seed in SEEDS}
        require({tuple(path.parts[-4:-1]) for path in paths} == expected, "Incomplete frozen six-seed matrix")
    build_hashes = set()
    for summary in paths:
        path = summary.parent
        arm, case, seed = summary.parts[-4:-1]
        identity = dict(arm=arm, case=case, seed=seed)
        if args.sensitivity:
            identity["variant"] = summary.parts[-5]
        metadata, events, samples, witness, marks, full_time = load_match(path, args.allow_short)
        if strict:
            build_hashes.add(metadata["buildHash"])
            require(len(build_hashes) == 1, f"{path}: matches came from different builds")
            require(metadata["determinism"] == metadata["restore"] == "passed",
                    f"{path}: missing two-run determinism or restore receipt")
            require(metadata["scheme"] == "Mixed" and metadata["tight"] == "False"
                    and metadata["standoffs"] == "2,1", f"{path}: unexpected tactic or stand-off override")
        owners, starts, origin, censored = spell_labels(samples["frame"], witness, events, full_time)
        for report, row in geometry_rows(samples, witness, owners, starts, origin, censored, identity):
            results[report].append(row)
        frames = np.arange(1, len(witness) + (0 if full_time else 1))
        frame_owners, frame_starts, frame_origin, _ = spell_labels(frames, witness, events, full_time)
        dismissal_frames = np.array([e["frame"] for e in events if e["event"] == "card" and e["detail"] in (1, 2)])
        first_dismissal = dismissal_frames.min() if dismissal_frames.size else len(witness) + 1
        for index in np.flatnonzero(frame_starts & ~frame_origin):
            tick = int(frames[index])
            results["territory"].append(dict(identity, defending_team=1 - int(frame_owners[index]),
                                              after_dismissal=bool(tick >= first_dismissal),
                                              frame=tick, ball_x=float(witness["x"][index])))
        last_shot = None
        for event in events:
            index = event["frame"] - 1
            strata = dict(defending_team=1 - event["team"],
                          restart_origin=bool(frame_origin[index]) if index < len(frames) and
                          frame_owners[index] == event["team"] else None,
                          after_dismissal=bool(event["frame"] >= first_dismissal))
            if event["event"] == "shot":
                last_shot = event
                results["shots"].append(dict(identity, **event, **strata, slot=event["agent"] % 11))
            if event["event"] == "goal":
                classification = goal_class(event, last_shot, witness)
                results["goals"].append(dict(identity, **event, **strata,
                                             classification=classification,
                                             slot=event["agent"] % 11 if classification == "linked" else None))
        census = []
        for team in (0, 1):
            team_marks = marks[:, team * 11:(team + 1) * 11]
            if arm == "off":
                require(not team_marks["active"].any(), f"{path}: disabled arm composed mark targets")
            census.append([{ "mode": mode, "assignments": int((team_marks["mode"] == mode).sum()),
                             "targets": int(((team_marks["mode"] == mode) & (team_marks["active"] != 0)).sum())}
                           for mode in range(4)])
        results["matches"].append(dict(identity, **metadata, census=census,
                                       substitutions=sum(e["event"] == "substitution" for e in events)))
    c0 = {(row.get("variant"), row["arm"], row["seed"]): row
          for row in results["matches"] if row["case"] == "C0"}
    switch_off = {(row["case"], row["seed"]): row for row in results["matches"] if row["arm"] == "off"}
    for row in results["matches"]:
        base = c0.get((row.get("variant"), row["arm"], row["seed"]))
        off = switch_off.get((row["case"], row["seed"])) if row["arm"] == "on" else None
        for event in ("goals", "shots"):
            row["delta_" + event] = ([int(a) - int(b) for a, b in zip(row[event].split(","), base[event].split(","))]
                                     if base else None)
            row[f"delta_{event}_vs_off"] = ([int(a) - int(b) for a, b in zip(row[event].split(","), off[event].split(","))]
                                           if off else None)
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "measurements.json").write_text(json.dumps(results, indent=2, allow_nan=False) + "\n")
    for name, rows in results.items():
        columns = list(dict.fromkeys(key for row in rows for key in row)) if rows else ["arm", "case", "seed"]
        write_csv(args.output / (name + ".csv"), rows, columns)


if __name__ == "__main__":
    main()
