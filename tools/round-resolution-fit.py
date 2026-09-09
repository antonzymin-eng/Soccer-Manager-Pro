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


class CorpusError(ValueError):
    """A deterministic, user-actionable corpus validation failure."""


def lam(base, slope, signed_edge):
    """The model's expected goals for one side — SeasonLoopConstants shape, clamps included."""
    raw = base * math.exp(slope * signed_edge)
    return min(max(raw, LAMBDA_MIN), LAMBDA_MAX)


def read_rows(paths):
    rows = []
    for path in paths:
        try:
            handle = open(path, newline="", encoding="utf-8-sig")
        except OSError as error:
            raise CorpusError(f"{path}: cannot read corpus: {error}") from error

        with handle:
            reader = csv.DictReader(handle)
            if reader.fieldnames is None:
                raise CorpusError(f"{path}: corpus is empty or has no CSV header")

            required = ("dSquad", "homeGoals", "awayGoals")
            missing = [name for name in required if name not in reader.fieldnames]
            if missing:
                raise CorpusError(f"{path}: missing required column(s): {', '.join(missing)}")

            for line_number, record in enumerate(reader, start=2):
                location = f"{path}:{line_number}"
                if None in record:
                    raise CorpusError(f"{location}: row has more values than the CSV header")

                raw_d = record["dSquad"]
                raw_h = record["homeGoals"]
                raw_a = record["awayGoals"]
                if raw_d is None or not raw_d.strip():
                    raise CorpusError(f"{location}: dSquad must not be blank")
                if raw_h is None or not raw_h.strip():
                    raise CorpusError(f"{location}: homeGoals must not be blank")
                if raw_a is None or not raw_a.strip():
                    raise CorpusError(f"{location}: awayGoals must not be blank")

                try:
                    d_squad = float(raw_d)
                except ValueError as error:
                    raise CorpusError(f"{location}: invalid dSquad {raw_d!r}") from error
                if not math.isfinite(d_squad):
                    raise CorpusError(f"{location}: dSquad must be finite")

                home_goals = _parse_goal_count(raw_h, "homeGoals", location)
                away_goals = _parse_goal_count(raw_a, "awayGoals", location)
                rows.append({"d": d_squad, "h": home_goals, "a": away_goals})
    return rows


def _parse_goal_count(raw, field, location):
    """Parse one canonical non-negative integer score without accepting float spellings."""
    try:
        value = int(raw, 10)
    except ValueError as error:
        raise CorpusError(f"{location}: {field} must be an integer, got {raw!r}") from error
    if value < 0:
        raise CorpusError(f"{location}: {field} must be >= 0, got {value}")
    return value


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
