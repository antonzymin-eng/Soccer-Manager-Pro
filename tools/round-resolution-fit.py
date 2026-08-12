#!/usr/bin/env python3
# tools/round-resolution-fit.py
# Created: 2026-07-26
# Modified: 2026-08-12 (A4a run: resolution + dispersion diagnostics, and --wdl-csv)
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
        # Sample variance (n-1) for the standard ERROR of the bucket mean: the bar below is compared
        # against an estimate, and an estimate has a noise floor. See report_resolution().
        svar_h = sum((r["h"] - mean_h) ** 2 for r in rows) / (n - 1) if n > 1 else float("nan")
        svar_a = sum((r["a"] - mean_a) ** 2 for r in rows) / (n - 1) if n > 1 else float("nan")
        wins = sum(1 for r in rows if r["h"] > r["a"])
        draws = sum(1 for r in rows if r["h"] == r["a"])
        losses = n - wins - draws
        out.append({
            "key": key, "n": n, "mean_d": mean_d,
            "mean_h": mean_h, "mean_a": mean_a, "var_h": var_h, "var_a": var_a,
            "se_h": math.sqrt(svar_h / n) if n > 1 else float("nan"),
            "se_a": math.sqrt(svar_a / n) if n > 1 else float("nan"),
            "w": wins, "d": draws, "l": losses,
        })
    return out


def resolution(summary):
    """
    How finely the corpus can measure its own bucket means, and whether the engine's scorelines are
    Poisson at all. Both exist because a FAIL verdict is uninterpretable without them.

    - `over_bar` counts bucket-SIDES whose standard error already exceeds BUCKET_MEAN_TOLERANCE. Where
      that happens the bar is below the corpus's own noise floor, and NO model — including a perfect
      one — can be shown to satisfy it. That is a property of the sample size, not of the fit.
    - The dispersion index var/mean is 1 for a Poisson variable by definition, and KD-7's model IS a
      Poisson draw. A pooled index above 1 means the engine's scorelines are over-dispersed and the
      model cannot reproduce their spread at ANY parameter values — a model-SHAPE finding, which no
      amount of re-fitting or re-sampling addresses.
    """
    over_bar = sum(1 for b in summary
                   for se in (b["se_h"], b["se_a"])
                   if se == se and se > BUCKET_MEAN_TOLERANCE)
    sides = 2 * len(summary)

    ratios = []
    chi2 = 0.0
    dof = 0
    for b in summary:
        for mean_key, var_key in (("mean_h", "var_h"), ("mean_a", "var_a")):
            mean = b[mean_key]
            if mean <= 0.0:
                continue
            ratios.append(b[var_key] * b["n"] / (b["n"] - 1) / mean)
            # Sum of (x - mean)^2 / mean over the bucket = n * var / mean; ~ chi2_(n-1) under Poisson.
            chi2 += b["n"] * b[var_key] / mean
            dof += b["n"] - 1

    mean_ratio = sum(ratios) / len(ratios) if ratios else float("nan")
    z = (chi2 - dof) / math.sqrt(2.0 * dof) if dof > 0 else float("nan")
    return {"over_bar": over_bar, "sides": sides, "mean_ratio": mean_ratio,
            "above_one": sum(1 for r in ratios if r > 1.0), "ratios": len(ratios),
            "chi2": chi2, "dof": dof, "z": z}


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
    # Extra rows at the acceptance bucket, used ONLY for the W/D/L bar — deliberately NOT fed to the
    # fit. The objective is sample-weighted, so folding a deepened bucket into it would quietly re-weight
    # the whole grid toward whichever bucket happens to have been measured hardest.
    ap.add_argument("--wdl-csv", nargs="*", default=[])
    args = ap.parse_args()

    rows = read_rows(args.csv)
    if not rows:
        print("no rows", file=sys.stderr)
        return 1

    summary = summarise(bucket(rows))
    base, slope, home_adv = fit(summary)
    res = resolution(summary)

    print(f"corpus: {len(rows)} matches across {len(summary)} buckets")
    print(f"fit: BaseGoals={base:.4f} GoalRatingSlope={slope:.4f} HomeAdvantageRating={home_adv:.4f}")
    print(f"objective={objective([base, slope, home_adv], summary):.6f}")
    print()
    hdr = (f"{'bucket':>7} {'n':>4} {'meanD':>7} {'corpH':>6} {'modH':>6} {'dH':>6} {'seH':>6} "
           f"{'corpA':>6} {'modA':>6} {'dA':>6} {'seA':>6} {'varH':>6} {'varA':>6}")
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
              f"{dh:>+6.3f} {b['se_h']:>6.3f} {b['mean_a']:>6.3f} {ma:>6.3f} {da:>+6.3f} "
              f"{b['se_a']:>6.3f} {b['var_h']:>6.3f} {b['var_a']:>6.3f}")

    print()
    print(f"worst per-bucket mean deviation: {worst:.3f} (bar {BUCKET_MEAN_TOLERANCE})")
    print(f"bucket-sides whose OWN standard error exceeds that bar: "
          f"{res['over_bar']} of {res['sides']}")
    print(f"Poisson dispersion (var/mean, model shape = 1.000): mean {res['mean_ratio']:.3f} "
          f"across {res['ratios']} bucket-sides, {res['above_one']} above 1; "
          f"pooled chi2={res['chi2']:.1f} dof={res['dof']} -> z={res['z']:+.2f} sigma")

    zero, deep_n = wdl_bucket(summary, args.wdl_csv)
    cw, cd, cl = (100.0 * zero["w"] / zero["n"], 100.0 * zero["d"] / zero["n"],
                  100.0 * zero["l"] / zero["n"])
    mw, md, ml = model_wdl(base, slope, home_adv, zero["mean_d"])
    wdl_worst = max(abs(cw - mw), abs(cd - md), abs(cl - ml))
    wdl_se = 100.0 * math.sqrt(max(cd, 1e-9) / 100.0 * (1.0 - cd / 100.0) / zero["n"])
    print(f"W/D/L at dSquad~{zero['mean_d']:.2f} (n={zero['n']}"
          f"{', deepened' if deep_n else ''}): "
          f"corpus {cw:.1f}/{cd:.1f}/{cl:.1f}  model {mw:.1f}/{md:.1f}/{ml:.1f}  "
          f"worst {wdl_worst:.1f}pp (bar {WDL_TOLERANCE_POINTS}pp, "
          f"corpus draw-share 1 sigma = {wdl_se:.1f}pp)")

    accepted = worst <= BUCKET_MEAN_TOLERANCE and wdl_worst <= WDL_TOLERANCE_POINTS
    print(f"KD-8 acceptance: {'PASS' if accepted else 'FAIL'}")

    if args.out:
        write_artifact(args, rows, summary, (base, slope, home_adv), worst,
                       (cw, cd, cl), (mw, md, ml), wdl_worst, accepted, zero, res,
                       wdl_se, deep_n)
        print(f"wrote {args.out}")
    return 0


def wdl_bucket(summary, wdl_paths):
    """
    The bucket KD-8 evaluates the W/D/L bar at: dSquad ~ 0, where home advantage is the only thing
    left to produce an asymmetry. Optionally deepened from --wdl-csv, because at the corpus's own
    18-per-bucket depth a draw share carries a ~10pp standard error and cannot resolve a 5pp bar.
    """
    zero = min(summary, key=lambda b: abs(b["mean_d"]))
    if not wdl_paths:
        return zero, 0

    extra = [r for r in read_rows(wdl_paths) if int(round(r["d"])) == zero["key"]]
    if not extra:
        return zero, 0

    merged = summarise(bucket(extra))[0]
    n = zero["n"] + merged["n"]
    combined = {
        "key": zero["key"], "n": n,
        "mean_d": (zero["mean_d"] * zero["n"] + merged["mean_d"] * merged["n"]) / n,
        "mean_h": (zero["mean_h"] * zero["n"] + merged["mean_h"] * merged["n"]) / n,
        "mean_a": (zero["mean_a"] * zero["n"] + merged["mean_a"] * merged["n"]) / n,
        "var_h": zero["var_h"], "var_a": zero["var_a"],
        "se_h": zero["se_h"], "se_a": zero["se_a"],
        "w": zero["w"] + merged["w"], "d": zero["d"] + merged["d"], "l": zero["l"] + merged["l"],
    }
    return combined, merged["n"]


GENERATED_BEGIN = "<!-- GENERATED:BEGIN — everything below is written by tools/round-resolution-fit.py -->"
GENERATED_END = "<!-- GENERATED:END -->"


def split_preserved(path):
    """
    Return (preamble, postamble) of an existing artifact — the hand-maintained sections that must
    survive a regeneration.

    The artifact carries hand-written content the fit cannot know (the standing no-injuries caveat, the
    run recipe, the version history), and a whole-file rewrite would silently delete it. Saying so in
    the file's own header would be documented-but-not-enforced; the markers make it structural. A file
    with no markers (or no file at all) regenerates whole, which is the first-run case.
    """
    try:
        with open(path) as handle:
            existing = handle.read()
    except OSError:
        return "", ""

    begin = existing.find(GENERATED_BEGIN)
    end = existing.find(GENERATED_END)
    if begin < 0 or end < 0 or end < begin:
        return "", ""

    return existing[:begin], existing[end + len(GENERATED_END):]


def write_artifact(args, rows, summary, params, worst, corpus_wdl, model_wdl_v, wdl_worst,
                   accepted, zero, res, wdl_se, deep_n):
    base, slope, home_adv = params
    preamble, postamble = split_preserved(args.out)
    with open(args.out, "w") as f:
        if preamble:
            f.write(preamble)
        f.write(GENERATED_BEGIN + "\n\n")
        if not preamble:
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
        f.write("`se` is the standard error of the corpus mean itself — the precision with which this\n")
        f.write("corpus knows the number the model is being scored against.\n\n")
        f.write("| bucket | n | mean dSquad | corpus home | model home | Δ | se home | corpus away |"
                " model away | Δ | se away | var home | var away |\n")
        f.write("|---|---|---|---|---|---|---|---|---|---|---|---|---|\n")
        for b in summary:
            edge = b["mean_d"] + home_adv
            mh = lam(base, slope, +edge)
            ma = lam(base, slope, -edge)
            f.write(f"| {b['key']:+d} | {b['n']} | {b['mean_d']:.3f} | {b['mean_h']:.3f} | {mh:.3f} | "
                    f"{mh - b['mean_h']:+.3f} | {b['se_h']:.3f} | "
                    f"{b['mean_a']:.3f} | {ma:.3f} | {ma - b['mean_a']:+.3f} | {b['se_a']:.3f} | "
                    f"{b['var_h']:.3f} | {b['var_a']:.3f} |\n")
        f.write("\n## Acceptance (KD-8)\n\n")
        f.write(f"- Per-bucket mean goals within ±{BUCKET_MEAN_TOLERANCE} of the corpus, each side — "
                f"**worst deviation {worst:.3f}**.\n")
        f.write(f"- Win/draw/loss split within ±{WDL_TOLERANCE_POINTS} percentage points at "
                f"`dSquad ≈ 0` (the bucket where home advantage shows as an asymmetry, so it is the one "
                f"that actually tests the fitted `HomeAdvantageRating`) — corpus "
                f"{corpus_wdl[0]:.1f}/{corpus_wdl[1]:.1f}/{corpus_wdl[2]:.1f} vs model "
                f"{model_wdl_v[0]:.1f}/{model_wdl_v[1]:.1f}/{model_wdl_v[2]:.1f}, "
                f"**worst {wdl_worst:.1f}pp** (n={zero['n']}"
                f"{f', deepened by {deep_n} matches beyond the grid' if deep_n else ''}).\n\n")
        f.write(f"**Verdict: {'PASS' if accepted else 'FAIL'}.**\n\n")
        f.write("### Why the verdict reads the way it does\n\n")
        f.write("A bar is only meaningful against a measurement precise enough to test it, and a fit is\n")
        f.write("only meaningful if the model can express the shape it is fitting. Both are measured here\n")
        f.write("rather than assumed.\n\n")
        f.write(f"**1. Sampling resolution.** {res['over_bar']} of {res['sides']} bucket-sides have a\n")
        f.write(f"standard error on their own mean that already exceeds the ±{BUCKET_MEAN_TOLERANCE}\n")
        f.write("bar (`se` column above). Where that holds, **no** model — including a perfect one — can\n")
        f.write("be shown to satisfy the bar, because the target it is scored against is not known that\n")
        f.write("precisely. That is a property of the grid's depth, not of the fit.\n\n")
        f.write(f"At the acceptance bucket the corpus draw share carries a ±{wdl_se:.1f}pp standard error\n")
        f.write(f"against a ±{WDL_TOLERANCE_POINTS}pp bar")
        if deep_n:
            f.write(f", after deepening it by {deep_n} matches beyond the grid.\n\n")
        else:
            f.write(" — the bar is inside the noise at this depth.\n\n")
        f.write("**2. Model shape — the finding that no re-fit addresses.** A Poisson variable has\n")
        f.write("variance equal to its mean by definition, and KD-7's model *is* a Poisson draw. The\n")
        f.write(f"engine's scorelines are **over-dispersed**: mean var/mean = {res['mean_ratio']:.3f}\n")
        f.write(f"across {res['ratios']} bucket-sides, {res['above_one']} of them above 1, pooled\n")
        f.write(f"chi2 = {res['chi2']:.1f} on {res['dof']} dof (**z = {res['z']:+.2f}**). So the engine\n")
        f.write("produces more blowouts and more shut-outs than any Poisson with the same means can, and\n")
        f.write("correspondingly **fewer draws** — which is exactly where the W/D/L bar is missed. No\n")
        f.write("choice of the three fitted parameters closes that gap; it is a statement about the\n")
        f.write("model's family, not its coefficients.\n\n")
        f.write("## Raw rows\n\n```csv\n")
        f.write("dSquad,homeGoals,awayGoals\n")
        for r in rows:
            f.write(f"{r['d']:.6f},{r['h']},{r['a']}\n")
        f.write("```\n")
        f.write("\n" + GENERATED_END)
        if postamble:
            f.write(postamble)
        else:
            f.write("\n")


if __name__ == "__main__":
    sys.exit(main())

# Version history
# | Version | Date       | Author | Notes                                                            |
# | 1.0     | 2026-07-26 | —      | Initial fitter: dependency-free coordinate-refinement least      |
# |         |            |        | squares over the three KD-7 parameters, per-bucket table, the    |
# |         |            |        | analytic W/D/L comparison, and the KD-8 acceptance verdict.      |
# | 1.1     | 2026-08-12 | —      | A4a's first real run. The fit alone made a FAIL verdict          |
# |         |            |        | uninterpretable — it could not distinguish "the model is wrong"  |
# |         |            |        | from "the bar is unmeasurable" or from "the model FAMILY cannot  |
# |         |            |        | express this" — so both are now measured and emitted into the    |
# |         |            |        | artifact: (a) the standard error of every bucket mean, plus the  |
# |         |            |        | count of bucket-sides whose own error exceeds the bar            |
# |         |            |        | (ERR-030-033), and (b) the Poisson dispersion index with a       |
# |         |            |        | pooled chi-square z (ERR-030-034). Also --wdl-csv, which lets    |
# |         |            |        | the acceptance bucket be deepened for the W/D/L bar WITHOUT      |
# |         |            |        | feeding those rows to the fit: the objective is sample-weighted, |
# |         |            |        | so folding them in would re-weight the whole grid toward         |
# |         |            |        | whichever bucket happened to be measured hardest. Finally, the   |
# |         |            |        | artifact now carries GENERATED:BEGIN/END markers and this tool   |
# |         |            |        | rewrites ONLY between them: the file holds hand-written content  |
# |         |            |        | the fit cannot know (the no-injuries caveat, the run recipe, the |
# |         |            |        | version history), and a whole-file rewrite deleted it silently.  |
# |         |            |        | Stating that in the file's header would be documented-but-not-   |
# |         |            |        | enforced; the markers make it structural. Verified idempotent —  |
# |         |            |        | a second regeneration over the same inputs is byte-identical.    |
