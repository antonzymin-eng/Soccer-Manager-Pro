# Round-Resolution Calibration Corpus

> **Created:** July 26, 2026
> **Status:** ARTIFACT — the measured ground truth the #30 round-resolution model's
> three `[GT]` parameters are fitted against. Governed by
> `docs/tracking/league-bootstrap-design.md` KD-7 (model shape) + KD-8 (methodology);
> `docs/tracking/path-to-playable-roadmap.md` item **A4a**.
> **Regenerate with:** the env-gated `Corpus_GeneratesTheRequestedSlice` driver in
> `src/season-save/tests/RoundResolutionCalibrationHarnessTests.cs`, then
> `python3 tools/round-resolution-fit.py <csv...> --out <this file>`.
>
> **This file is machine-generated below the header.** Everything from *Capture provenance*
> to *Raw rows* is written by `tools/round-resolution-fit.py`; edit the tool, never this
> file, or the next regeneration silently discards the correction. The two hand-maintained
> sections — the standing caveat and the version history — bracket the generated body.

---

## 0.a How this run was actually executed (August 12, 2026)

Reproducing the numbers below needs three things KD-8 does not state, all learned by running it:

```bash
# 0. The SDK. The Ubuntu archive carries it; every dot.net host is 403 at the proxy.
apt-get update && apt-get install -y dotnet-sdk-8.0     # 8.0.129
python3 tools/dotnet-ci/generate_projects.py
dotnet build src/season-save/tests/season-save-tests.gen.csproj -c Release

# 1. Step 0 FIRST — it gates the corpus, and it is the whole reason the July-26 run
#    did not fit three parameters to a table of zeros.
TD_CALIBRATION_PILOT=1 dotnet test src/season-save/tests/season-save-tests.gen.csproj \
    -c Release --no-build --filter "FullyQualifiedName~Pilot_Extreme"

# 2. The corpus, four processes over disjoint bucket ranges (~90 s/match here).
#    The engine's EventBus is process-static, so parallelism is across PROCESSES only.
TD_CALIBRATION_SAMPLES=18 TD_CALIBRATION_DELTA_FROM=-5 TD_CALIBRATION_DELTA_TO=-3 \
TD_CALIBRATION_OUT=/tmp/s1.csv dotnet test ... --filter "FullyQualifiedName~Corpus_Generates"
#    ... and -2..0, 1..3, 4..5 in three more processes.

# 3. The acceptance bucket, deepened across four processes via the sample window.
TD_CALIBRATION_SAMPLES=45 TD_CALIBRATION_DELTA_FROM=0 TD_CALIBRATION_DELTA_TO=0 \
TD_CALIBRATION_SAMPLE_FROM=18 TD_CALIBRATION_OUT=/tmp/z1.csv dotnet test ...
#    ... and SAMPLE_FROM=63, 108, 153.

python3 tools/round-resolution-fit.py /tmp/s{1,2,3,4}.csv \
    --wdl-csv /tmp/z{1,2,3,4}.csv \
    --engine-sha "$(git rev-parse --short HEAD)" --schema-version 20 \
    --platform "Linux x64, .NET 8, Release — non-certifying" \
    --out docs/tracking/round-resolution-corpus.md
```

**Do not run a build while slices are in flight.** A rebuild swaps assemblies under live
test processes; this run hit exactly that and discarded the affected slices rather than
reasoning about whether it mattered (the gate-invalidation class, `spec-error-log.md`
v1.90).

**Two properties were verified rather than assumed, both before the hours were spent:**

1. **A slice reproduces in isolation.** One process ran buckets −1 then 0; another ran
   bucket 0 alone. Bucket 0's rows came back byte-identical. Every split in this run —
   and the whole "parallelisable" claim in KD-8 — rests on that, and nothing had checked it.
2. **A split reproduces the sanctioned driver.** The ±6 buckets run as two separate
   processes produced **all 20** of `Pilot_Extreme`'s own rows, exactly.

## 0.b Standing caveat — the corpus is captured with no injuries (carried forward)

The #29/#41 balance pass armed the occurrence dial (FR-MD-027): a career-wired season removes
injured players from selection, so the squad-strength distribution `RoundResolutionModel`
resolves against is not the all-fit distribution this corpus is captured on. The harness boots
a bare `MatchEngine` over two shifted squads with no career state, so **every row below is an
all-fit row**. The effect is bounded — ~9% of players unavailable at a matchday, starting XIs
re-selected from the remainder — but systematic. **Recorded, deliberately not folded in** (the
evidence advisor's call at the balance pass): re-fitting against the armed-career distribution
is its own pass. Treat this fit as conditioned on "no injuries" and re-check it when it is next
used with a career wired.

---

<!-- GENERATED:BEGIN — everything below is written by tools/round-resolution-fit.py -->

---

## Capture provenance

This corpus measures what the match engine does **at the commit below**. A later engine
change invalidates the fit rather than merely aging it (KD-8's re-capture trigger): goal
detection landed July 11 2026 with a deliberately minimal restart model, so anything that
moves scoring moves this table.

| Field | Value |
|---|---|
| Engine commit SHA | `95ffc31` |
| `SNAPSHOT_SCHEMA_VERSION` | 20 |
| Capture platform | Linux x64 (Ubuntu 24.04), .NET SDK 8.0.129, Release — non-certifying |
| Matches | 198 |
| Buckets | 11 |
| Base-roster seed | `0x0CA11B8A7E5EED01` |

**Non-certifying.** These are engine *results*, not timings or determinism proofs, so the
Linux gate host is a legitimate capture platform — unlike `FR-PO-052` perf baselines or the
`FR-DS-009-GATE` determinism KAT, which require the pinned Windows/Unity tuple.

## Fitted parameters

Least squares over the three KD-7 parameters against the per-bucket means below.
`LambdaMin` / `LambdaMax` are safety clamps and are deliberately **not** fitted.

| Constant | Fitted value |
|---|---|
| `QuickSimBaseGoals` | 1.2325 |
| `QuickSimGoalRatingSlope` | 0.2162 |
| `QuickSimHomeAdvantageRating` | 0.4996 |

## Per-bucket corpus vs model

Buckets are on the **measured** `dSquad = Rating(home) − Rating(away)` in unit steps —
never on `edge`, which contains the fitted home advantage and does not exist at capture
time (KD-8 / AR-7 H-1).

`se` is the standard error of the corpus mean itself — the precision with which this
corpus knows the number the model is being scored against.

| bucket | n | mean dSquad | corpus home | model home | Δ | se home | corpus away | model away | Δ | se away | var home | var away |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| -5 | 18 | -4.999 | 0.389 | 0.466 | +0.077 | 0.143 | 2.889 | 3.261 | +0.372 | 0.498 | 0.349 | 4.210 |
| -4 | 18 | -4.000 | 0.556 | 0.578 | +0.023 | 0.202 | 2.889 | 2.627 | -0.262 | 0.332 | 0.691 | 1.877 |
| -3 | 18 | -3.000 | 0.444 | 0.718 | +0.273 | 0.166 | 2.111 | 2.116 | +0.005 | 0.361 | 0.469 | 2.210 |
| -2 | 18 | -2.000 | 0.778 | 0.891 | +0.113 | 0.263 | 1.833 | 1.705 | -0.129 | 0.364 | 1.173 | 2.250 |
| -1 | 18 | -1.000 | 0.889 | 1.106 | +0.217 | 0.254 | 1.389 | 1.373 | -0.016 | 0.344 | 1.099 | 2.015 |
| +0 | 18 | 0.000 | 1.444 | 1.373 | -0.071 | 0.354 | 1.056 | 1.106 | +0.051 | 0.297 | 2.136 | 1.497 |
| +1 | 18 | 1.000 | 1.500 | 1.705 | +0.205 | 0.364 | 1.056 | 0.891 | -0.164 | 0.286 | 2.250 | 1.386 |
| +2 | 18 | 2.000 | 2.944 | 2.116 | -0.828 | 0.501 | 1.167 | 0.718 | -0.449 | 0.218 | 4.275 | 0.806 |
| +3 | 18 | 3.000 | 2.389 | 2.627 | +0.238 | 0.537 | 0.500 | 0.578 | +0.078 | 0.218 | 4.904 | 0.806 |
| +4 | 18 | 4.000 | 2.611 | 3.261 | +0.650 | 0.537 | 0.389 | 0.466 | +0.077 | 0.164 | 4.904 | 0.460 |
| +5 | 18 | 5.000 | 4.500 | 4.048 | -0.452 | 0.633 | 0.278 | 0.375 | +0.097 | 0.135 | 6.806 | 0.312 |

## Goal rate — and why the corpus mean is not the football number

The grid samples `dSquad` −5…+5 **uniformly**; a real season does not. Its fixtures
cluster near 0, and mismatched fixtures score more, so the corpus mean over-weights
blowouts and reads high against football even when the engine is right at the strengths
a league actually plays. Three figures, because quoting the first one alone has already
caused one false alarm:

| Population | Goals/match | Notes |
|---|---|---|
| Grid-weighted (raw corpus mean) | 3.09 | Correct for the fit; **not** a realism figure. |
| **Balanced — `dSquad ≈ 0`** | **2.70** | n=198. The football-comparable population, and the best-measured bucket. |
| League-weighted | 2.93 | Per-bucket rates re-weighted by the `dSquad` distribution of a real 20-club season under the shipped `StrengthDelta` ramp (98% of fixtures covered). |
| Football reference | ~2.7 | |


## Acceptance (KD-8)

- Per-bucket mean goals within ±0.25 of the corpus, each side — **worst deviation 0.828**.
- Win/draw/loss split within ±5.0 percentage points at `dSquad ≈ 0` (the bucket where home advantage shows as an asymmetry, so it is the one that actually tests the fitted `HomeAdvantageRating`) — corpus 44.4/19.2/36.4 vs model 42.9/26.8/30.2, **worst 7.6pp** (n=198, deepened by 180 matches beyond the grid).

**Verdict: FAIL.**

### Why the verdict reads the way it does

A bar is only meaningful against a measurement precise enough to test it, and a fit is
only meaningful if the model can express the shape it is fitting. Both are measured here
rather than assumed.

**1. Sampling resolution.** 15 of 22 bucket-sides have a
standard error on their own mean that already exceeds the ±0.25
bar (`se` column above). Where that holds, **no** model — including a perfect one — can
be shown to satisfy the bar, because the target it is scored against is not known that
precisely. That is a property of the grid's depth, not of the fit.

At the acceptance bucket the corpus draw share carries a ±2.8pp standard error
against a ±5.0pp bar, after deepening it by 180 matches beyond the grid.

**2. Model shape — the finding that no re-fit addresses.** A Poisson variable has
variance equal to its mean by definition, and KD-7's model *is* a Poisson draw. The
engine's scorelines are **over-dispersed**: mean var/mean = 1.395
across 22 bucket-sides, 19 of them above 1, pooled
chi2 = 521.7 on 374 dof (**z = +5.40**). So the engine
produces more blowouts and more shut-outs than any Poisson with the same means can, and
correspondingly **fewer draws** — which is exactly where the W/D/L bar is missed. No
choice of the three fitted parameters closes that gap; it is a statement about the
model's family, not its coefficients.

## Raw rows

```csv
dSquad,homeGoals,awayGoals
-4.818182,0,2
-5.041056,1,3
-5.231672,0,3
-4.697947,0,3
-5.082112,1,3
-5.120234,1,6
-4.862171,0,0
-5.272727,1,9
-4.929619,0,2
-4.782992,1,3
-5.205279,0,2
-4.938415,0,1
-5.093843,0,1
-4.970674,0,4
-5.014664,0,5
-4.906159,2,1
-5.023460,0,2
-4.982405,0,2
-3.821115,0,3
-4.041056,0,3
-4.231671,1,2
-3.700882,3,1
-4.085045,2,3
-4.120234,1,3
-3.862171,0,2
-4.272726,0,2
-3.932553,1,3
-3.785925,0,2
-4.205279,1,7
-3.941349,0,3
-4.093842,0,4
-3.973608,0,3
-4.017596,1,3
-3.906159,0,2
-4.026394,0,1
-3.982405,0,5
-2.821115,0,2
-3.041056,1,1
-3.231671,0,4
-2.700882,0,1
-3.085045,0,4
-3.120235,0,5
-2.862171,0,1
-3.272726,0,4
-2.932553,1,4
-2.785925,0,0
-3.205279,1,2
-2.941349,0,1
-3.093842,0,1
-2.973608,1,3
-3.017596,2,2
-2.906159,2,1
-3.026394,0,2
-2.982405,0,0
-1.821116,0,1
-2.041056,1,1
-2.231671,0,0
-1.700881,4,2
-2.085045,0,4
-2.120235,0,6
-1.862171,0,1
-2.272726,0,0
-1.932552,1,2
-1.785925,1,3
-2.205279,2,2
-1.941349,0,2
-2.093842,0,3
-1.973607,0,1
-2.017596,2,1
-1.906159,2,0
-2.026394,1,1
-1.982405,0,3
-0.821115,1,2
-1.041056,0,1
-1.231670,2,1
-0.700881,0,1
-1.085044,2,2
-1.120235,1,0
-0.862171,0,3
-1.272726,0,1
-0.932551,3,6
-0.785925,3,0
-1.205278,0,2
-0.941349,0,0
-1.093842,0,2
-0.973607,0,0
-1.017595,0,1
-0.906159,1,2
-1.026393,1,0
-0.982405,2,1
0.178885,0,1
-0.041056,1,1
-0.231670,1,3
0.299119,1,4
-0.085044,2,0
-0.120234,1,0
0.137829,1,2
-0.272726,0,0
0.067449,6,0
0.214075,2,0
-0.205277,2,3
0.058651,1,0
-0.093842,0,1
0.026393,2,1
-0.017595,4,1
0.093842,1,0
-0.026393,1,0
0.017595,0,2
1.178884,1,1
0.958945,1,1
0.768330,0,1
1.299120,0,0
0.914956,2,5
0.879766,3,1
1.137830,0,2
0.727274,0,1
1.067450,3,1
1.214075,4,1
0.794723,1,0
1.058651,3,2
0.906158,1,1
1.026394,2,0
0.982405,1,0
1.093842,5,0
0.973607,0,0
1.017596,0,2
2.178885,1,2
1.958945,3,0
1.768330,3,2
2.299120,5,2
1.914956,1,0
1.879766,1,1
2.137831,4,1
1.727274,1,2
2.067450,3,1
2.214075,3,1
1.794723,2,2
2.058651,1,0
1.906159,5,3
2.026394,0,0
1.982405,7,1
2.093842,6,0
1.973607,6,1
2.017596,1,2
3.178885,1,0
2.958945,0,0
2.768330,0,0
3.299120,7,0
2.914956,5,1
2.879767,0,0
3.137831,3,2
2.727274,0,0
3.067450,4,1
3.214075,5,0
2.794723,5,0
3.058651,2,0
2.906159,0,0
3.026394,4,0
2.982405,4,2
3.093842,2,0
2.973607,0,0
3.017596,1,3
4.178885,1,0
3.958945,1,0
3.768330,2,2
4.299120,3,0
3.914957,3,0
3.879767,5,0
4.137831,6,2
3.727274,1,1
4.067450,1,0
4.214075,1,0
3.794723,9,1
4.058651,1,0
3.906159,0,0
4.026394,2,0
3.982405,2,0
4.093842,2,0
3.973608,5,0
4.017596,2,1
5.178885,4,0
4.958946,3,0
4.768329,4,0
5.299119,9,0
4.914957,2,1
4.879766,6,0
5.137831,5,0
4.727273,4,0
5.067449,2,0
5.214075,1,0
4.794723,4,1
5.058651,4,0
4.906159,2,1
5.026393,10,0
4.982404,2,0
5.093841,3,0
4.973608,9,0
5.017597,7,2
```

<!-- GENERATED:END -->
---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-26 | — | Initial record. A4a's KD-8 Step 0 pilot executed and REFUSED to proceed: 20/20 engine matches 0–0 at ±6 measured `dSquad`. Characterised to root cause — the ball starts at rest, only a moving ball can be received, and only a possessing agent can kick, so a production match was a closed deadlock (ERR-030-014). |
| 0.2 | 2026-07-26 | — | Post-Phase-H / §5.Z.11 status updates: Step 0 re-runnable, then re-ran onto 25–0 scorelines — passing while unfittable (the home/away asymmetry). |
| 0.3 | 2026-07-28 | — | **Step 0 PASSED** on the post-§5.Z.21 tree: margins +7.100 / −4.700, both directions separate, upsets present, venue asymmetry down to ~1.5× on margin. Instrument fix recorded (the `LogAssert` wrapper — a playing match emits FM-08/FM-03 as ordinary events). |
| 0.4 | 2026-08-03 | — | Status amended: the goal rate moved again after §5.Z.22 and §5.Z.23 (4.7 → 5.0 → 3.7/match). Step 0 must be re-run before the corpus, or the fit calibrates the quick-sim to a rate the engine no longer produces. |
| **1.0** | **2026-08-12** | **—** | **THE RUN. This file stops being an evidence record and becomes the KD-8 artifact.** Step 0 re-run PASSED (+4.000 / −3.500; the extremes have converged since July 28 as the engine's goal rate fell). Corpus captured: **198 real 90-minute `MatchEngine` matches**, 11 `dSquad` buckets × 18, ~90 s/match over four processes, ~1.4 h. Fitted: `QuickSimBaseGoals` **1.2325**, `QuickSimGoalRatingSlope` **0.2162**, `QuickSimHomeAdvantageRating` **0.4996** (was 1.35 / 0.35 / 0.30, provisional since #30 T2). **Verdict FAIL on both bars, for two measured reasons that are not fit failures** — `ERR-030-033` (the ±0.25 bar is below this corpus's own noise floor: 15 of 22 bucket-sides have a larger standard error than the whole bar) and `ERR-030-034` (the engine is Poisson-over-dispersed at z = +5.40, a model-FAMILY gap). The acceptance bucket was **deepened to n = 198** so the W/D/L bar could be resolved at all; that deepening moved the measured draw share **11.1% → 19.2%**, so the grid-depth reading would have overstated the draw defect twofold while still detecting it — the clearest single demonstration of why `ERR-030-033` matters. Body below is now tool-generated; §0.a records how the run was executed and the two methodology properties verified rather than assumed; §0.b carries the no-injuries caveat forward. |
