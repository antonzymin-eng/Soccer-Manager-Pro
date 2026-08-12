#!/usr/bin/env python3
# tools/round-resolution-fit.py
# Created: 2026-07-26
# Modified: 2026-08-12 (A4a run: resolution + dispersion diagnostics, --wdl-csv)
# Modified: 2026-08-12 (ERR-030-033: KD-8 mean bar re-specified; KD-7a successor diagnostics)
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

# KD-8 acceptance bars, as re-specified August 12, 2026 (ERR-030-033).
#
# The original bar was a flat +/-0.25 on every per-bucket mean. It was unmeetable by construction: a
# bucket mean is an ESTIMATE, and at the 18 samples/bucket KD-8 itself sizes, 15 of 22 bucket-sides
# carried a standard error larger than the whole bar — so a perfectly correct model would fail it too.
# The fix is not to widen the number (a bar moved to fit its own result is not a bar) but to state it
# against the precision the corpus actually has, a priori, for any corpus:
#
#   1. Per-bucket-side screen: |model - corpus| <= max(BUCKET_MEAN_TOLERANCE, 2*se). The floor is the
#      ORIGINAL 0.25, so once a corpus is deep enough that 2*se < 0.25 the original bar automatically
#      becomes binding again. The aspiration is preserved, not abandoned.
#   2. A bounded number of exceedances, because a 2-sigma screen over many cells expects some by
#      chance, and no single cell may exceed the 3-sigma / 0.40 hard screen.
#   3. A pooled chi-square, which is where the statistical POWER lives: it catches systematic misfit
#      that every individual cell passes.
#
# A corpus shallower than MIN_SAMPLES_PER_BUCKET may not be scored at all — otherwise the se-relative
# form is gameable by shrinking n, which widens every tolerance.
BUCKET_MEAN_TOLERANCE = 0.25          # per-bucket mean goals, each side — now a FLOOR, not the bar
BUCKET_MEAN_SIGMA = 2.0               # se multiplier for the per-bucket-side screen
BUCKET_HARD_TOLERANCE = 0.40          # no single bucket-side may exceed max(this, 3*se)
BUCKET_HARD_SIGMA = 3.0
MIN_SAMPLES_PER_BUCKET = 18           # below this the corpus is not scoreable at all
WDL_TOLERANCE_POINTS = 5.0            # percentage points, at dSquad = 0
WDL_MIN_SAMPLES = 250                 # depth at which 2*se <= the 5pp bar at a ~20% draw share

# Mirrored from LeagueBootstrapConstants / LeagueBootstrap.StrengthDelta — used ONLY to re-weight the
# grid into a league-representative goal rate (see goal_rates). Not part of the fit.
LEAGUE_STRENGTH_SPREAD = 3
LEAGUE_CLUB_COUNT = 20

# Football reference for the goal rate, for the realism line only (invariants: ~2.7 goals/match).
FOOTBALL_GOALS_PER_MATCH = 2.7


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


def chi2_critical(dof, p=0.95):
    """
    Upper-tail chi-square critical value by the Wilson-Hilferty transform.

    No third-party dependency on purpose (see the module header): the fit must run on any host that
    can generate the corpus. Wilson-Hilferty is accurate to well under 1% for dof >= 10, which is
    every corpus this bar can legally score — MIN_SAMPLES_PER_BUCKET forces at least 11 buckets'
    worth of cells before a verdict is issued.
    """
    z = {0.95: 1.6448536269514722, 0.99: 2.3263478740408408}[p]
    t = 1.0 - (2.0 / (9.0 * dof)) + z * math.sqrt(2.0 / (9.0 * dof))
    return dof * t * t * t


def mean_bar(summary, params):
    """
    The per-bucket mean bar, scored cell by cell against the corpus's OWN measured precision.

    Returns the per-cell detail plus the pooled chi-square, so the artifact can show why a verdict
    reads the way it does rather than only that it does.
    """
    base, slope, home_adv = params
    cells = []
    for b in summary:
        edge = b["mean_d"] + home_adv
        for model_lam, corpus_key, se_key, side in (
                (lam(base, slope, +edge), "mean_h", "se_h", "home"),
                (lam(base, slope, -edge), "mean_a", "se_a", "away")):
            delta = model_lam - b[corpus_key]
            se = b[se_key]
            allowed = max(BUCKET_MEAN_TOLERANCE, BUCKET_MEAN_SIGMA * se)
            hard = max(BUCKET_HARD_TOLERANCE, BUCKET_HARD_SIGMA * se)
            cells.append({
                "key": b["key"], "side": side, "delta": delta, "se": se,
                "z": delta / se if se > 0 else float("inf"),
                "allowed": allowed, "over": abs(delta) > allowed, "over_hard": abs(delta) > hard,
            })

    chi2 = sum(c["z"] ** 2 for c in cells)
    dof = max(1, len(cells) - 3)              # three fitted parameters
    crit = chi2_critical(dof)
    # A 2-sigma screen over many cells expects ~4.55% of them to exceed by chance; allow that plus one.
    allowance = 1 + int(round(0.0455 * len(cells)))
    exceed = sum(1 for c in cells if c["over"])
    hard_exceed = sum(1 for c in cells if c["over_hard"])
    shallow = min(b["n"] for b in summary)

    passed = (shallow >= MIN_SAMPLES_PER_BUCKET and exceed <= allowance
              and hard_exceed == 0 and chi2 <= crit)
    return {"cells": cells, "chi2": chi2, "dof": dof, "crit": crit, "exceed": exceed,
            "allowance": allowance, "hard_exceed": hard_exceed, "shallow": shallow,
            "scoreable": shallow >= MIN_SAMPLES_PER_BUCKET, "passed": passed,
            "worst": max((abs(c["delta"]) for c in cells), default=0.0),
            "worst_z": max((abs(c["z"]) for c in cells), default=0.0)}


def wdl_verdict(worst_pp, se_pp, n):
    """
    The W/D/L bar, with the same discipline as the mean bar: a bar failure must also be
    distinguishable from noise, or the honest answer is that the corpus cannot yet say.
    """
    if worst_pp <= WDL_TOLERANCE_POINTS:
        return "PASS"
    return "FAIL" if worst_pp > BUCKET_MEAN_SIGMA * se_pp else "INCONCLUSIVE"


def successor_diagnostics(summary):
    """
    KD-7a's dispersion parameter, reported with its own stability — because a single number here
    would be a false precision.

    `alpha` is fitted as `var = mu*(1 + alpha*mu)`, by least squares through the origin on
    `(var - mu)` vs `mu^2`. Specified in that form, NOT as a constant var/mean ratio, because
    dispersion rises with the mean and one ratio misfits both ends.

    Two estimators and a leverage figure are emitted rather than one alpha, because at this corpus's
    depth the weighted estimate is dominated by whichever cell happens to have drawn a low sample
    variance: a variance estimate at n=18 carries ~33% relative error, and the inverse-variance
    weights go as 1/var^2, so one unlucky cell can carry a third of the fit. When `max_leverage` is
    large or the two estimators disagree by more than a factor of ~1.5, **alpha is not determined by
    this corpus** and adopting a successor on it would be fitting noise.
    """
    pts = []
    for b in summary:
        for mean_key, var_key in (("mean_h", "var_h"), ("mean_a", "var_a")):
            mu = b[mean_key]
            if mu <= 0.0:
                continue
            var = b[var_key] * b["n"] / (b["n"] - 1)
            if var <= 0.0:
                continue
            pts.append((mu, var, (b["n"] - 1) / (2.0 * var * var)))

    def solve(weighted):
        num = sum((w if weighted else 1.0) * (mu * mu) * (var - mu) for mu, var, w in pts)
        den = sum((w if weighted else 1.0) * (mu * mu) ** 2 for mu, var, w in pts)
        return num / den if den else float("nan")

    alpha_w = solve(True)
    alpha_u = solve(False)
    leverage = [w * (mu ** 4) for mu, var, w in pts]
    total = sum(leverage)
    max_leverage = max(leverage) / total if total else float("nan")
    ratio = (max(alpha_w, alpha_u) / min(alpha_w, alpha_u)
             if min(alpha_w, alpha_u) > 0 else float("inf"))
    determined = max_leverage < 0.25 and ratio < 1.5

    return {"alpha": alpha_w, "alpha_unweighted": alpha_u, "points": len(pts),
            "max_leverage": max_leverage, "ratio": ratio, "determined": determined}


def pooled_correlation(rows):
    """Pooled WITHIN-bucket home/away correlation — see successor_diagnostics for why it matters."""
    buckets = bucket(rows)
    num = den_h = den_a = 0.0
    n_total = 0
    for _, group in buckets.items():
        n = len(group)
        if n < 2:
            continue
        mh = sum(r["h"] for r in group) / n
        ma = sum(r["a"] for r in group) / n
        num += sum((r["h"] - mh) * (r["a"] - ma) for r in group)
        den_h += sum((r["h"] - mh) ** 2 for r in group)
        den_a += sum((r["a"] - ma) ** 2 for r in group)
        n_total += n
    if den_h <= 0 or den_a <= 0:
        return float("nan"), float("nan"), n_total
    r = num / math.sqrt(den_h * den_a)
    se = 1.0 / math.sqrt(max(1, n_total - len(buckets)))
    return r, se, n_total


def nb2_pmf(k, mu, alpha):
    """NB2 pmf by the same forward recurrence KD-7a pins for its inverse CDF."""
    if alpha <= 0.0:
        return math.exp(-mu) * mu ** k / math.factorial(k)
    r = 1.0 / alpha
    q = alpha * mu / (1.0 + alpha * mu)
    p = (1.0 + alpha * mu) ** (-r)
    for i in range(k):
        p *= q * (r + i) / (i + 1)
    return p


def draw_share(lam_h, lam_a, alpha):
    return sum(nb2_pmf(k, lam_h, alpha) * nb2_pmf(k, lam_a, alpha)
               for k in range(MAX_GOALS_PER_SIDE + 1))


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
    bar = mean_bar(summary, (base, slope, home_adv))
    succ = successor_diagnostics(summary)
    corr, corr_se, corr_n = pooled_correlation(rows)

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
    print(f"MEAN BAR (ERR-030-033 re-spec): per-cell |delta| <= max({BUCKET_MEAN_TOLERANCE}, "
          f"{BUCKET_MEAN_SIGMA:.0f}*se), <= {bar['allowance']} exceedances, none over "
          f"max({BUCKET_HARD_TOLERANCE}, {BUCKET_HARD_SIGMA:.0f}*se); pooled chi2 <= crit")
    print(f"  worst |delta| {bar['worst']:.3f} (worst |z| {bar['worst_z']:.2f}); "
          f"exceedances {bar['exceed']}/{bar['allowance']}, hard {bar['hard_exceed']}; "
          f"chi2 {bar['chi2']:.1f} on {bar['dof']} dof vs crit {bar['crit']:.1f}; "
          f"shallowest bucket n={bar['shallow']} (min {MIN_SAMPLES_PER_BUCKET})")
    print(f"  -> MEAN: {'PASS' if bar['passed'] else 'FAIL'}")
    print(f"NOTE the original flat {BUCKET_MEAN_TOLERANCE} bar was unmeetable here: "
          f"{res['over_bar']} of {res['sides']} bucket-sides have a standard error exceeding it.")
    print(f"Poisson dispersion (var/mean, model shape = 1.000): mean {res['mean_ratio']:.3f} "
          f"across {res['ratios']} bucket-sides, {res['above_one']} above 1; "
          f"pooled chi2={res['chi2']:.1f} dof={res['dof']} -> z={res['z']:+.2f} sigma")

    zero, deep_n = wdl_bucket(summary, args.wdl_csv)

    rates = goal_rates(summary, rows, zero)
    print(f"goals/match — grid-weighted {rates['grid']:.2f} (over-weights blowouts, NOT a football "
          f"figure) | balanced dSquad~0 {rates['balanced']:.2f} (n={rates['n_balanced']}) | "
          f"league-weighted {rates['league']:.2f} ({rates['covered']:.0f}% of a season covered) | "
          f"football ~{FOOTBALL_GOALS_PER_MATCH}")
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

    wdl = wdl_verdict(wdl_worst, wdl_se, zero["n"])
    if zero["n"] < WDL_MIN_SAMPLES:
        print(f"  (acceptance bucket n={zero['n']} is below the pinned minimum {WDL_MIN_SAMPLES}; "
              f"a PASS here would not be resolvable, a significant FAIL still is)")
    print(f"  -> SHAPE (W/D/L): {wdl}")

    zero_lam_h = lam(base, slope, +(zero["mean_d"] + home_adv))
    zero_lam_a = lam(base, slope, -(zero["mean_d"] + home_adv))
    nb_draws = 100.0 * draw_share(zero_lam_h, zero_lam_a, succ["alpha"])
    print(f"KD-7a successor diagnostics: NB2 alpha (var = mu(1+alpha*mu)) = {succ['alpha']:.4f} "
          f"weighted / {succ['alpha_unweighted']:.4f} unweighted — "
          f"{'DETERMINED' if succ['determined'] else 'NOT DETERMINED by this corpus'} "
          f"(max single-cell leverage {100*succ['max_leverage']:.0f}%, estimator ratio "
          f"{succ['ratio']:.2f}); "
          f"pooled within-bucket home/away corr = {corr:+.3f} +/- {corr_se:.3f} (n={corr_n}); "
          f"NB2 draw share at the acceptance bucket = {nb_draws:.1f}% vs corpus {cd:.1f}% "
          f"(NB2 fixes dispersion, NOT the draw deficit)")

    accepted = bar["passed"] and wdl == "PASS"
    print(f"KD-8 acceptance: {'PASS' if accepted else 'FAIL'} "
          f"(mean {'PASS' if bar['passed'] else 'FAIL'}, shape {wdl})")

    if args.out:
        write_artifact(args, rows, summary, (base, slope, home_adv), worst,
                       (cw, cd, cl), (mw, md, ml), wdl_worst, accepted, zero, res,
                       wdl_se, deep_n, rates, bar, wdl,
                       (succ, corr, corr_se, corr_n, nb_draws))
        print(f"wrote {args.out}")
    return 0


def goal_rates(summary, rows, zero):
    """
    Goals per match, reported three ways — because the corpus mean is NOT a football-comparable
    figure and reading it as one is a mistake this project has already made once.

    The grid samples dSquad -5..+5 UNIFORMLY. A real 20-club season does not: its fixtures cluster
    near dSquad 0 (|dSquad| <= 1 is ~39% of a 380-fixture season against the grid's 27%), and
    mismatched fixtures score more. So the corpus mean over-weights blowouts and reads high against
    football's ~2.7 even when the engine is exactly right at the strengths a league actually plays.

    - `grid`: the raw corpus mean. Useful for the fit, misleading as a realism figure.
    - `balanced`: the dSquad ~ 0 bucket alone — the football-comparable population, and the one the
      acceptance bucket is deepened at, so it is also the best-measured.
    - `league`: per-bucket rates re-weighted by the dSquad distribution of an actual season under the
      shipped LeagueBootstrap.StrengthDelta ramp. The honest single number for "what will a league
      table's goal rate look like".
    """
    grid = sum(r["h"] + r["a"] for r in rows) / len(rows)

    balanced = zero["mean_h"] + zero["mean_a"]

    # The shipped ramp: StrengthDelta(rank, clubCount) with halves-away-from-zero rounding.
    def rounded_divide(num, den):
        return (num + den // 2) // den if num >= 0 else -((-num + den // 2) // den)

    spread, clubs = LEAGUE_STRENGTH_SPREAD, LEAGUE_CLUB_COUNT
    ramp = [rounded_divide((2 * spread * rank) - (spread * (clubs - 1)), clubs - 1)
            for rank in range(clubs)]
    fixtures = defaultdict(int)
    for home in range(clubs):
        for away in range(clubs):
            if home != away:
                fixtures[ramp[home] - ramp[away]] += 1

    # The acceptance bucket's DEEPENED mean, where it exists — it is the best-measured rate in the
    # corpus and the one a league weights most heavily, so using the shallow grid value would throw
    # away the very samples that were run to measure it.
    by_key = {b["key"]: b["mean_h"] + b["mean_a"] for b in summary}
    by_key[zero["key"]] = balanced
    weight = sum(n for k, n in fixtures.items() if k in by_key)
    league = (sum(n * by_key[k] for k, n in fixtures.items() if k in by_key) / weight
              if weight else float("nan"))
    covered = 100.0 * weight / sum(fixtures.values())

    return {"grid": grid, "balanced": balanced, "league": league, "covered": covered,
            "n_balanced": zero["n"]}


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
                   accepted, zero, res, wdl_se, deep_n, rates, bar, wdl, successor):
    base, slope, home_adv = params
    preamble, postamble = split_preserved(args.out)
    with open(args.out, "w") as f:
        if preamble:
            f.write(preamble)
        f.write(GENERATED_BEGIN + "\n\n")
        if not preamble:
            # First run only: the file has no hand-maintained head yet, so emit the title and the
            # header blockquote. On every later run BOTH already live in the preserved preamble, and
            # re-emitting them here duplicates the header inside the generated region.
            f.write("# Round-Resolution Calibration Corpus\n\n")
            f.write("> **Created:** July 26, 2026\n")
            f.write("> **Status:** ARTIFACT — the measured ground truth the #30 round-resolution model's\n")
            f.write("> three `[GT]` parameters are fitted against. Governed by\n")
            f.write("> `docs/tracking/league-bootstrap-design.md` KD-7 (model shape) + KD-8 (methodology);\n")
            f.write("> `docs/tracking/path-to-playable-roadmap.md` item **A4a**.\n")
            f.write("> **Regenerate with:** the env-gated `Corpus_GeneratesTheRequestedSlice` driver in\n")
            f.write("> `src/season-save/tests/RoundResolutionCalibrationHarnessTests.cs`, then\n")
            f.write("> `python3 tools/round-resolution-fit.py <csv...> --out <this file>`.\n\n")
        # The `---` closes the header blockquote, so it belongs to the header: with a preserved
        # preamble the separator is already up there and repeating it opens the generated region
        # with a stray rule.
        if not preamble:
            f.write("---\n\n")
        f.write("## Capture provenance\n\n")
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
        f.write("\n## Goal rate — and why the corpus mean is not the football number\n\n")
        f.write("The grid samples `dSquad` −5…+5 **uniformly**; a real season does not. Its fixtures\n")
        f.write("cluster near 0, and mismatched fixtures score more, so the corpus mean over-weights\n")
        f.write("blowouts and reads high against football even when the engine is right at the strengths\n")
        f.write("a league actually plays. Three figures, because quoting the first one alone has already\n")
        f.write("caused one false alarm:\n\n")
        f.write("| Population | Goals/match | Notes |\n|---|---|---|\n")
        f.write(f"| Grid-weighted (raw corpus mean) | {rates['grid']:.2f} | Correct for the fit; "
                f"**not** a realism figure. |\n")
        f.write(f"| **Balanced — `dSquad ≈ 0`** | **{rates['balanced']:.2f}** | n={rates['n_balanced']}. "
                f"The football-comparable population, and the best-measured bucket. |\n")
        f.write(f"| League-weighted | {rates['league']:.2f} | Per-bucket rates re-weighted by the "
                f"`dSquad` distribution of a real {LEAGUE_CLUB_COUNT}-club season under the shipped "
                f"`StrengthDelta` ramp ({rates['covered']:.0f}% of fixtures covered). |\n")
        f.write(f"| Football reference | ~{FOOTBALL_GOALS_PER_MATCH} | |\n\n")
        f.write("\n## Acceptance (KD-8, bars re-specified August 12, 2026 — ERR-030-033)\n\n")
        f.write("The original bar was a flat ±0.25 on every per-bucket mean. It could not be met by any\n")
        f.write("model at the depth KD-8 sizes, because a bucket mean is an **estimate**: "
                f"{res['over_bar']} of {res['sides']} bucket-sides\n")
        f.write("carry a standard error larger than the whole bar. The bar is now stated against the\n")
        f.write("precision the corpus actually has — with ±0.25 kept as a **floor**, so a deeper corpus\n")
        f.write("automatically restores the original requirement rather than abandoning it.\n\n")
        f.write("**Mean agreement.** Per cell (bucket × side): "
                f"`|model − corpus| ≤ max({BUCKET_MEAN_TOLERANCE}, {BUCKET_MEAN_SIGMA:.0f}·se)`, at most\n")
        f.write(f"{bar['allowance']} exceedances (a {BUCKET_MEAN_SIGMA:.0f}σ screen over "
                f"{len(bar['cells'])} cells expects ~1 by chance), none over\n")
        f.write(f"`max({BUCKET_HARD_TOLERANCE}, {BUCKET_HARD_SIGMA:.0f}·se)`, and a pooled "
                f"`χ² ≤ χ²₀.₉₅(dof)` — which is where the statistical\n")
        f.write("power lives, since it catches systematic misfit that every individual cell passes.\n")
        f.write(f"A corpus shallower than {MIN_SAMPLES_PER_BUCKET}/bucket is **not scoreable at all**, "
                f"so the se-relative form cannot be\ngamed by shrinking n.\n\n")
        f.write(f"| Check | Measured | Bar | |\n|---|---|---|---|\n")
        f.write(f"| Worst cell deviation | {bar['worst']:.3f} (|z| = {bar['worst_z']:.2f}) | "
                f"per-cell, se-relative | |\n")
        f.write(f"| Exceedances | {bar['exceed']} | ≤ {bar['allowance']} | "
                f"{'✅' if bar['exceed'] <= bar['allowance'] else '❌'} |\n")
        f.write(f"| Hard exceedances | {bar['hard_exceed']} | 0 | "
                f"{'✅' if bar['hard_exceed'] == 0 else '❌'} |\n")
        f.write(f"| Pooled χ² | {bar['chi2']:.1f} on {bar['dof']} dof | ≤ {bar['crit']:.1f} | "
                f"{'✅' if bar['chi2'] <= bar['crit'] else '❌'} |\n")
        f.write(f"| Shallowest bucket | n = {bar['shallow']} | ≥ {MIN_SAMPLES_PER_BUCKET} | "
                f"{'✅' if bar['scoreable'] else '❌'} |\n\n")
        f.write(f"**Mean agreement: {'PASS' if bar['passed'] else 'FAIL'}.**\n\n")
        f.write("**Distribution shape.** ")
        f.write(f"Win/draw/loss split within ±{WDL_TOLERANCE_POINTS} percentage points at "
                f"`dSquad ≈ 0` (the bucket where home advantage shows as an asymmetry, so it is the one "
                f"that actually tests the fitted `HomeAdvantageRating`) — corpus "
                f"{corpus_wdl[0]:.1f}/{corpus_wdl[1]:.1f}/{corpus_wdl[2]:.1f} vs model "
                f"{model_wdl_v[0]:.1f}/{model_wdl_v[1]:.1f}/{model_wdl_v[2]:.1f}, "
                f"**worst {wdl_worst:.1f}pp** (n={zero['n']}"
                f"{f', deepened by {deep_n} matches beyond the grid' if deep_n else ''}). "
                f"A bar failure must also be distinguishable from noise, or the honest answer is that "
                f"the corpus cannot yet say: the corpus draw share carries a ±{wdl_se:.1f}pp standard "
                f"error here")
        if zero["n"] < WDL_MIN_SAMPLES:
            f.write(f", and n is below the pinned minimum of {WDL_MIN_SAMPLES} at which a *pass* "
                    f"would be resolvable (a significant *fail* still is)")
        f.write(f".\n\n**Distribution shape: {wdl}.**\n\n")
        f.write(f"**Overall verdict: {'PASS' if accepted else 'FAIL'} — mean agreement "
                f"{'PASS' if bar['passed'] else 'FAIL'}, distribution shape {wdl}.**\n\n")
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
        succ_d, corr_v, corr_se_v, corr_n_v, nb_draws_v = successor
        f.write("## KD-7a successor diagnostics\n\n")
        f.write("The three statistics KD-7a's adoption tripwire turns on, emitted every run so the\n")
        f.write("decision is made against measurements rather than re-derived by hand at the next capture.\n\n")
        f.write("| Statistic | Measured | What it decides |\n|---|---|---|\n")
        f.write(f"| NB2 dispersion `α` (`var = μ(1+αμ)`) | **{succ_d['alpha']:.4f}** weighted / "
                f"{succ_d['alpha_unweighted']:.4f} unweighted — "
                f"**{'determined' if succ_d['determined'] else 'NOT DETERMINED by this corpus'}** "
                f"(one cell carries {100*succ_d['max_leverage']:.0f}% of the weighted fit; estimators "
                f"differ by {succ_d['ratio']:.2f}×) | The successor's one new parameter, and whether "
                f"this corpus can yet fix it. A variance estimate at 18 samples carries ~33% relative "
                f"error and the weights go as `1/var²`, so one unlucky cell dominates. Adopting on this "
                f"would be fitting noise. |\n")
        f.write(f"| Pooled within-bucket home/away correlation | **{corr_v:+.3f} ± {corr_se_v:.3f}** "
                f"(n={corr_n_v}) | **Discriminates the candidate families.** Any shared-swing mechanism "
                f"that would cut the draw share implies a clearly negative value; measured ≈ 0, such a "
                f"family is refuted however well it fits the draw count. |\n")
        f.write(f"| NB2 draw share at the acceptance bucket | **{nb_draws_v:.1f}%** vs corpus "
                f"{corpus_wdl[1]:.1f}% | **The number that stops NB2 being mistaken for a fix to the draw "
                f"deficit.** It closes the dispersion gap and barely moves draws. |\n\n")
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
# | 1.2     | 2026-08-12 | —      | The goal-rate match-realism pass. The corpus mean was quoted as  |
# |         |            |        | a football figure (3.09 vs ~2.7, reported as an overshoot) and   |
# |         |            |        | it is not one: the grid samples dSquad -5..+5 UNIFORMLY, while a |
# |         |            |        | real season clusters near 0 and mismatches score far more, so    |
# |         |            |        | the mean over-weights blowouts. Measured properly the engine is  |
# |         |            |        | on football's rate — balanced fixtures 2.70 (n=198, 0.02 sigma), |
# |         |            |        | league-weighted 2.93 (+1.47 sigma, not significant). goal_rates  |
# |         |            |        | now reports all three side by side, re-weighting the per-bucket  |
# |         |            |        | rates by the real StrengthDelta fixture distribution and using   |
# |         |            |        | the DEEPENED acceptance bucket, and the artifact carries them as |
# |         |            |        | a table. The fix belongs in the tool, not in prose: a warning    |
# |         |            |        | not to misread the mean is advice; emitting the right number     |
# |         |            |        | beside it is structural.                                         |
# | 1.3     | 2026-08-12 | —      | ERR-030-033: KD-8's mean bar re-specified and now SCORED here    |
# |         |            |        | rather than asserted. mean_bar() applies the per-cell            |
# |         |            |        | max(0.25, 2*se) screen, the bounded-exceedance rule and the      |
# |         |            |        | pooled chi-square (chi2_critical by Wilson-Hilferty — no         |
# |         |            |        | third-party dependency, verified against exact values at dof 10  |
# |         |            |        | and 19); wdl_verdict() returns INCONCLUSIVE when a miss is not   |
# |         |            |        | distinguishable from noise. The verdict is reported in two parts |
# |         |            |        | because the halves fail for unrelated reasons, and the flat      |
# |         |            |        | verdict had been making ERR-030-034 read as a fit failure.       |
# | 1.4     | 2026-08-12 | —      | KD-7a: the three statistics the successor-adoption tripwire      |
# |         |            |        | turns on, so it is evaluable rather than merely written. The     |
# |         |            |        | one that changed the deliverable is alpha's STABILITY: the       |
# |         |            |        | weighted and unweighted estimators differ by 2.01x and one       |
# |         |            |        | 18-sample cell carries 36% of the weighted fit, so alpha is      |
# |         |            |        | NOT determined by this corpus and recording a single value       |
# |         |            |        | would have been false precision. Also the pooled within-bucket   |
# |         |            |        | home/away correlation (the statistic that DISCRIMINATES          |
# |         |            |        | candidate families — a shared-swing family needs it clearly      |
# |         |            |        | negative) and NB2's draw share at the acceptance bucket (the     |
# |         |            |        | number that stops NB2 being mistaken for a fix to the draws).    |
