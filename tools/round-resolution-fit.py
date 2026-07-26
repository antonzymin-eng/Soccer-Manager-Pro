#!/usr/bin/env python3
# tools/round-resolution-fit.py
# Created: 2026-07-26
# Purpose: Fit the Season & Competition Loop #30 round-resolution model's three parameters
#          (BaseGoals, GoalRatingSlope, HomeAdvantageRating) against the engine-simulated
#          corpus produced by RoundResolutionCalibrationHarness, and emit the
#          docs/tracking/round-resolution-corpus.md artifact.
#          Governance: docs/tracking/league-bootstrap-design.md KD-7 (model shape) + KD-8
#          (calibration methodology); path-to-playable roadmap item A4a.
# Usage:   python3 tools/round-resolution-fit.py rows1.csv [rows2.csv ...] \
#              [--out docs/tracking/round-resolution-corpus.md] \
#              [--engine-sha <sha>] [--schema-version <n>]
#
# No third-party dependencies on purpose: the fit must be re-runnable on any host that can
# generate the corpus, including the pinned certification host, without a package install.
# The optimiser is an iterated coordinate grid refinement — the objective is smooth and
# three-dimensional, so this converges to the same answer a library minimiser would and is
# fully deterministic, which matters because the fitted numbers are committed as [GT] constants.

import argparse
import csv
import math
import sys
from collections import defaultdict

# Safety clamps — NOT fitted (KD-7). Mirrored from SeasonLoopConstants; if those change, change
# these in the same commit or the fit optimises against a different model than the one that ships.
LAMBDA_MIN = 0.15
LAMBDA_MAX = 6.0
MAX_GOALS_PER_SIDE = 20

# KD-8 acceptance bars.
BUCKET_MEAN_TOLERANCE = 0.25          # per-bucket mean goals, each side
WDL_TOLERANCE_POINTS = 5.0            # percentage points, at dSquad = 0


def lam(base, slope, signed_edge):
    """The model's expected goals for one side — SeasonLoopConstants shape, clamps included."""
    raw = base * math.exp(slope * signed_edge)
    return min(max(raw, LAMBDA_MIN), LAMBDA_MAX)


def read_rows(paths):
    rows = []
    for path in paths:
        with open(path, newline="") as handle:
            for record in csv.DictReader(handle):
                rows.append({
                    "d": float(record["dSquad"]),
                    "h": int(record["homeGoals"]),
                    "a": int(record["awayGoals"]),
                })
    return rows


def bucket(rows):
    """Bucket on the MEASURED dSquad in unit steps (KD-8: the measured value, never the knob)."""
    buckets = defaultdict(list)
    for row in rows:
        buckets[int(round(row["d"]))].append(row)
    return dict(sorted(buckets.items()))


def summarise(buckets):
    out = []
    for key, rows in buckets.items():
        n = len(rows)
        mean_d = sum(r["d"] for r in rows) / n
        mean_h = sum(r["h"] for r in rows) / n
        mean_a = sum(r["a"] for r in rows) / n
        var_h = sum((r["h"] - mean_h) ** 2 for r in rows) / n
        var_a = sum((r["a"] - mean_a) ** 2 for r in rows) / n
        wins = sum(1 for r in rows if r["h"] > r["a"])
        draws = sum(1 for r in rows if r["h"] == r["a"])
        losses = n - wins - draws
        out.append({
            "key": key, "n": n, "mean_d": mean_d,
            "mean_h": mean_h, "mean_a": mean_a, "var_h": var_h, "var_a": var_a,
            "w": wins, "d": draws, "l": losses,
        })
    return out


def objective(params, summary):
    """Sample-weighted squared error between the model's per-bucket means and the corpus's."""
    base, slope, home_adv = params
    total = 0.0
    for b in summary:
        edge = b["mean_d"] + home_adv
        total += b["n"] * (lam(base, slope, +edge) - b["mean_h"]) ** 2
        total += b["n"] * (lam(base, slope, -edge) - b["mean_a"]) ** 2
    return total


def fit(summary):
    """Iterated coordinate grid refinement. Deterministic; no RNG, no library."""
    params = [1.35, 0.30, 0.30]
    bounds = [(0.20, 4.00), (0.00, 1.50), (-1.50, 1.50)]
    step = [0.40, 0.15, 0.30]

    for _ in range(60):
        improved = False
        for axis in range(3):
            best = params[axis]
            best_cost = objective(params, summary)
            for direction in (-1, +1):
                candidate = params[axis] + direction * step[axis]
                lo, hi = bounds[axis]
                if candidate < lo or candidate > hi:
                    continue
                trial = list(params)
                trial[axis] = candidate
                cost = objective(trial, summary)
                if cost < best_cost - 1e-12:
                    best_cost = cost
                    best = candidate
            if best != params[axis]:
                params[axis] = best
                improved = True
        if not improved:
            step = [s * 0.5 for s in step]
            if max(step) < 1e-5:
                break
    return params


def poisson_pmf(k, lmbda):
    return math.exp(-lmbda) * lmbda ** k / math.factorial(k)


def model_wdl(base, slope, home_adv, mean_d):
    """Analytic W/D/L for the fitted model at a given dSquad — no sampling, so it is exact."""
    edge = mean_d + home_adv
    lh = lam(base, slope, +edge)
    la = lam(base, slope, -edge)
    ph = [poisson_pmf(k, lh) for k in range(MAX_GOALS_PER_SIDE + 1)]
    pa = [poisson_pmf(k, la) for k in range(MAX_GOALS_PER_SIDE + 1)]
    win = draw = loss = 0.0
    for i, phi in enumerate(ph):
        for j, paj in enumerate(pa):
            p = phi * paj
            if i > j:
                win += p
            elif i == j:
                draw += p
            else:
                loss += p
    total = win + draw + loss
    return 100.0 * win / total, 100.0 * draw / total, 100.0 * loss / total


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("csv", nargs="+")
    ap.add_argument("--out", default=None)
    ap.add_argument("--engine-sha", default="UNRECORDED")
    ap.add_argument("--schema-version", default="UNRECORDED")
    ap.add_argument("--platform", default="UNRECORDED")
    args = ap.parse_args()

    rows = read_rows(args.csv)
    if not rows:
        print("no rows", file=sys.stderr)
        return 1

    summary = summarise(bucket(rows))
    base, slope, home_adv = fit(summary)

    print(f"corpus: {len(rows)} matches across {len(summary)} buckets")
    print(f"fit: BaseGoals={base:.4f} GoalRatingSlope={slope:.4f} HomeAdvantageRating={home_adv:.4f}")
    print(f"objective={objective([base, slope, home_adv], summary):.6f}")
    print()
    hdr = (f"{'bucket':>7} {'n':>4} {'meanD':>7} {'corpH':>6} {'modH':>6} {'dH':>6} "
           f"{'corpA':>6} {'modA':>6} {'dA':>6} {'varH':>6} {'varA':>6}")
    print(hdr)

    worst = 0.0
    for b in summary:
        edge = b["mean_d"] + home_adv
        mh = lam(base, slope, +edge)
        ma = lam(base, slope, -edge)
        dh = mh - b["mean_h"]
        da = ma - b["mean_a"]
        worst = max(worst, abs(dh), abs(da))
        print(f"{b['key']:>7} {b['n']:>4} {b['mean_d']:>7.3f} {b['mean_h']:>6.3f} {mh:>6.3f} "
              f"{dh:>+6.3f} {b['mean_a']:>6.3f} {ma:>6.3f} {da:>+6.3f} "
              f"{b['var_h']:>6.3f} {b['var_a']:>6.3f}")

    print()
    print(f"worst per-bucket mean deviation: {worst:.3f} (bar {BUCKET_MEAN_TOLERANCE})")

    zero = min(summary, key=lambda b: abs(b["mean_d"]))
    cw, cd, cl = (100.0 * zero["w"] / zero["n"], 100.0 * zero["d"] / zero["n"],
                  100.0 * zero["l"] / zero["n"])
    mw, md, ml = model_wdl(base, slope, home_adv, zero["mean_d"])
    wdl_worst = max(abs(cw - mw), abs(cd - md), abs(cl - ml))
    print(f"W/D/L at dSquad~{zero['mean_d']:.2f} (n={zero['n']}): "
          f"corpus {cw:.1f}/{cd:.1f}/{cl:.1f}  model {mw:.1f}/{md:.1f}/{ml:.1f}  "
          f"worst {wdl_worst:.1f}pp (bar {WDL_TOLERANCE_POINTS}pp)")

    accepted = worst <= BUCKET_MEAN_TOLERANCE and wdl_worst <= WDL_TOLERANCE_POINTS
    print(f"KD-8 acceptance: {'PASS' if accepted else 'FAIL'}")

    if args.out:
        write_artifact(args, rows, summary, (base, slope, home_adv), worst,
                       (cw, cd, cl), (mw, md, ml), wdl_worst, accepted, zero)
        print(f"wrote {args.out}")
    return 0


def write_artifact(args, rows, summary, params, worst, corpus_wdl, model_wdl_v, wdl_worst,
                   accepted, zero):
    base, slope, home_adv = params
    with open(args.out, "w") as f:
        f.write("# Round-Resolution Calibration Corpus\n\n")
        f.write("> **Created:** July 26, 2026\n")
        f.write("> **Status:** ARTIFACT — the measured ground truth the #30 round-resolution model's\n")
        f.write("> three `[GT]` parameters are fitted against. Governed by\n")
        f.write("> `docs/tracking/league-bootstrap-design.md` KD-7 (model shape) + KD-8 (methodology);\n")
        f.write("> `docs/tracking/path-to-playable-roadmap.md` item **A4a**.\n")
        f.write("> **Regenerate with:** the env-gated `Corpus_GeneratesTheRequestedSlice` driver in\n")
        f.write("> `src/season-save/tests/RoundResolutionCalibrationHarnessTests.cs`, then\n")
        f.write("> `python3 tools/round-resolution-fit.py <csv...> --out <this file>`.\n\n")
        f.write("---\n\n## Capture provenance\n\n")
        f.write("This corpus measures what the match engine does **at the commit below**. A later engine\n")
        f.write("change invalidates the fit rather than merely aging it (KD-8's re-capture trigger): goal\n")
        f.write("detection landed July 11 2026 with a deliberately minimal restart model, so anything that\n")
        f.write("moves scoring moves this table.\n\n")
        f.write("| Field | Value |\n|---|---|\n")
        f.write(f"| Engine commit SHA | `{args.engine_sha}` |\n")
        f.write(f"| `SNAPSHOT_SCHEMA_VERSION` | {args.schema_version} |\n")
        f.write(f"| Capture platform | {args.platform} |\n")
        f.write(f"| Matches | {len(rows)} |\n")
        f.write(f"| Buckets | {len(summary)} |\n")
        f.write(f"| Base-roster seed | `0x0CA11B8A7E5EED01` |\n\n")
        f.write("**Non-certifying.** These are engine *results*, not timings or determinism proofs, so the\n")
        f.write("Linux gate host is a legitimate capture platform — unlike `FR-PO-052` perf baselines or the\n")
        f.write("`FR-DS-009-GATE` determinism KAT, which require the pinned Windows/Unity tuple.\n\n")
        f.write("## Fitted parameters\n\n")
        f.write("Least squares over the three KD-7 parameters against the per-bucket means below.\n")
        f.write("`LambdaMin` / `LambdaMax` are safety clamps and are deliberately **not** fitted.\n\n")
        f.write("| Constant | Fitted value |\n|---|---|\n")
        f.write(f"| `QuickSimBaseGoals` | {base:.4f} |\n")
        f.write(f"| `QuickSimGoalRatingSlope` | {slope:.4f} |\n")
        f.write(f"| `QuickSimHomeAdvantageRating` | {home_adv:.4f} |\n\n")
        f.write("## Per-bucket corpus vs model\n\n")
        f.write("Buckets are on the **measured** `dSquad = Rating(home) − Rating(away)` in unit steps —\n")
        f.write("never on `edge`, which contains the fitted home advantage and does not exist at capture\n")
        f.write("time (KD-8 / AR-7 H-1).\n\n")
        f.write("| bucket | n | mean dSquad | corpus home | model home | Δ | corpus away | model away | Δ |"
                " var home | var away |\n")
        f.write("|---|---|---|---|---|---|---|---|---|---|---|\n")
        for b in summary:
            edge = b["mean_d"] + home_adv
            mh = lam(base, slope, +edge)
            ma = lam(base, slope, -edge)
            f.write(f"| {b['key']:+d} | {b['n']} | {b['mean_d']:.3f} | {b['mean_h']:.3f} | {mh:.3f} | "
                    f"{mh - b['mean_h']:+.3f} | {b['mean_a']:.3f} | {ma:.3f} | {ma - b['mean_a']:+.3f} | "
                    f"{b['var_h']:.3f} | {b['var_a']:.3f} |\n")
        f.write("\n## Acceptance (KD-8)\n\n")
        f.write(f"- Per-bucket mean goals within ±{BUCKET_MEAN_TOLERANCE} of the corpus, each side — "
                f"**worst deviation {worst:.3f}**.\n")
        f.write(f"- Win/draw/loss split within ±{WDL_TOLERANCE_POINTS} percentage points at "
                f"`dSquad ≈ 0` (the bucket where home advantage shows as an asymmetry, so it is the one "
                f"that actually tests the fitted `HomeAdvantageRating`) — corpus "
                f"{corpus_wdl[0]:.1f}/{corpus_wdl[1]:.1f}/{corpus_wdl[2]:.1f} vs model "
                f"{model_wdl_v[0]:.1f}/{model_wdl_v[1]:.1f}/{model_wdl_v[2]:.1f}, "
                f"**worst {wdl_worst:.1f}pp** (n={zero['n']}).\n\n")
        f.write(f"**Verdict: {'PASS' if accepted else 'FAIL'}.**\n\n")
        f.write("## Raw rows\n\n```csv\n")
        f.write("dSquad,homeGoals,awayGoals\n")
        for r in rows:
            f.write(f"{r['d']:.6f},{r['h']},{r['a']}\n")
        f.write("```\n")


if __name__ == "__main__":
    sys.exit(main())
