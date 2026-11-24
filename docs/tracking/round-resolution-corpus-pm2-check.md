# Round-Resolution Corpus — PM-2 Quick-Sim Check

> **Created:** November 24, 2026
> **Purpose:** Roadmap engine-lane item 4 (PM-2 quick-sim check; D-08/D-09). The A4a corpus is
> recaptured on the PM-2 engine and the **shipped** round-resolution constants are scored against
> KD-8's mean bar **without refitting**. Under D-09 the means are refitted only if that bar fails.
> **Status:** ARTIFACT — evidence for one check. It does not replace
> [`round-resolution-corpus.md`](round-resolution-corpus.md), which remains the corpus the shipped
> constants were fitted on (August 12, 2026) and the record of the deepened W/D/L acceptance bucket.
> This is not the depth-phase governed recapture, freeze or lock (D-08).
> **Governed by:** [`league-bootstrap-design.md`](league-bootstrap-design.md) KD-7/KD-8 and the D-09
> note; [`path-to-playable-roadmap.md`](path-to-playable-roadmap.md) engine lane item 4.
> **Regenerate with:** §0.a below. Everything between the `GENERATED` markers is written by
> `tools/round-resolution-fit.py --hold-params`; edit the tool, never that region.

---

## 0.a How this run was executed

```bash
apt-get update && apt-get install -y dotnet-sdk-8.0      # 8.0.134 on this host
python3 tools/dotnet-ci/generate_projects.py
dotnet build src/season-save/tests/season-save-tests.gen.csproj -c Release
P=src/season-save/tests/season-save-tests.gen.csproj

# KD-8 Step 0 (pilot, 20 matches at ±6) — run in its own process alongside the slices below.
TD_CALIBRATION_PILOT=1 dotnet test $P -c Release --no-build \
    --filter "FullyQualifiedName~Pilot_Extreme" --logger "console;verbosity=detailed"

# The 11 × 18 grid in four processes (no build while they run). Same harness, plan and
# base-roster seed as August, so every row is a fresh engine result at an unchanged match seed.
TD_CALIBRATION_SAMPLES=18 TD_CALIBRATION_DELTA_FROM=-5 TD_CALIBRATION_DELTA_TO=-3 \
TD_CALIBRATION_OUT=s1.csv dotnet test $P -c Release --no-build --filter "FullyQualifiedName~Corpus_Generates"
#   ... and -2..0 (s2), 1..3 (s3), 4..5 (s4, after the pilot in the same process).

python3 tools/round-resolution-fit.py s1.csv s2.csv s3.csv s4.csv \
    --hold-params 1.2325,0.2162,0.4996 \
    --engine-sha <sha> --schema-version 31 \
    --platform "Linux x64, .NET SDK 8.0.134, Release — non-certifying" \
    --out docs/tracking/round-resolution-corpus-pm2-check.md
```

**Scope limits, stated rather than implied:**

- **The acceptance bucket was not deepened.** The W/D/L figure below is at grid depth (n = 18),
  far under KD-8 A5's n ≥ 250, so it cannot pass and is not the shape evidence. D-09 records
  `ERR-030-034`'s shape gap as a known PM-2 limitation; its evidence is the August deepened
  bucket in `round-resolution-corpus.md`. The shape verdict printed below is reported, not adopted.
- **No injuries** — the §0.b caveat of `round-resolution-corpus.md` carries forward unchanged: the
  harness boots a bare `MatchEngine` over two shifted all-fit squads.
- **Held, not fitted.** The mean bar keeps `dof = cells − 3` as KD-8 pins it. With nothing fitted
  to this corpus the statistically correct dof would be 22; 19 is the stricter reading.

## 0.b Verdict — mean bar FAILS; a refit cannot pass either; owner decision required

| Check | Result |
|---|---|
| KD-8 Step 0 pilot | **PASS**: strong-at-home mean margin **+11.100** at `dSquad` +6.013, strong-away **−7.700** at −5.985 (August: +4.000 / −3.500). |
| Mean bar, shipped constants held | **FAIL**: χ² **693.9** on 19 dof (bar 30.1); 20 exceedances (≤ 2 allowed), 17 hard (0 allowed); worst cell Δ 9.786 (\|z\| 11.08). |
| Mean bar, refit (authorized by D-09 on failure; scored in scratch, **not adopted**) | **FAIL**: χ² **180.4** on 19 dof; 8 exceedances, 5 hard. Fit lands at `BaseGoals` 4.0000 (the fitter's upper search bound), slope 0.2669, home advantage 0.7855; **8 of 22 cells sit on the unfitted `LambdaMax` = 6.0 clamp**. |
| Engine goal rate | **10.28/match** at balanced `dSquad` (n = 18), 10.07 league-weighted, 10.33 grid-weighted. August's corpus: 2.70 balanced (n = 198). 128 of 396 team-sides scored more than 6; the largest was 20. |

**Why no refit was landed.** D-09 authorizes a refit of the three means *if the mean bar fails*.
It failed, but the refit fails it as well, and it cannot be made to pass inside KD-7. The engine's
per-side means exceed `QuickSimLambdaMax` = 6.0 in a third of all sides, and KD-7 deliberately does
not fit that safety clamp. Landing the refit would move three `[GT]` constants to values that do not
meet acceptance. It would also make the quick-sim reproduce a ~10-goals-per-match league. Raising the
clamp is a KD-7 change. Neither step is within this check's authority, so nothing was refitted and no
`[GT]` moved.

**The cause is on the engine side, and it was already measured elsewhere.** This harness is
independent of those measurements, and its rate agrees with them. The W13 flip measurement
([evidence](evidence/w13-flip-measurement-20261121/README.md), November 21, 60 paired matches per
arm) recorded 11.27 goals/match with marking off and 9.40 with it on. The W8 C record
([w8-c-implementation-results.md](w8-c-implementation-results.md)) has 47 goals in six 90-minute
matches, about 7.8 per match. Both are recorded as descriptive with no approved realism band (KD-W1). This
check does not localize when or why the rate rose from August's 2.70.

**Consequence for PM-2 if nothing changes.** Under PM-2's Career Home round resolution the managed
club's fixture is played by the engine (~10 goals/match) and every other fixture by the quick-sim
(the shipped constants give ~2.7 at balanced strength). The managed club's goals-for, goals-against
and goal difference would then be on a different scale from every other row of the table.

**Also recorded for KD-7a (no adoption; KD-7a stays HELD):** dispersion z = **+10.61**
(var/mean 1.776); pooled within-bucket home/away correlation **−0.209 ± 0.073** (August:
+0.044 ± 0.073), now clearly negative; NB2 α still NOT DETERMINED (estimator ratio 1.65).

**Reading the generated section below.** Its fixed prose was written for the August corpus. Two
phrases do not hold here: the balanced bucket is *not* "the best-measured bucket" (n = 18, no
deepening), and the clause tying over-dispersion to "fewer draws" was refuted in August
(`round-resolution-corpus.md`, `ERR-030-034` correction). The tables and numbers govern.

---

<!-- GENERATED:BEGIN — everything below is written by tools/round-resolution-fit.py -->

## Capture provenance

This corpus measures what the match engine does **at the commit below**. A later engine
change invalidates the fit rather than merely aging it (KD-8's re-capture trigger): goal
detection landed July 11 2026 with a deliberately minimal restart model, so anything that
moves scoring moves this table.

| Field | Value |
|---|---|
| Engine commit SHA | `45d32d1` |
| `SNAPSHOT_SCHEMA_VERSION` | 31 |
| Capture platform | Linux x64 (Ubuntu 24.04), .NET SDK 8.0.134, Release — non-certifying |
| Matches | 198 |
| Buckets | 11 |
| Base-roster seed | `0x0CA11B8A7E5EED01` |

**Non-certifying.** These are engine *results*, not timings or determinism proofs, so the
Linux gate host is a legitimate capture platform — unlike `FR-PO-052` perf baselines or the
`FR-DS-009-GATE` determinism KAT, which require the pinned Windows/Unity tuple.

## Held parameters (scored, not refitted)

These are the shipped constants, scored against this corpus **without** refitting
(`--hold-params`). The mean bar keeps `dof = cells − 3` although nothing is fitted:
that is the bar as KD-8 pins it, and the stricter reading for an independent corpus.

| Constant | Held value |
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
| -5 | 18 | -4.999 | 0.500 | 0.466 | -0.034 | 0.167 | 7.278 | 3.260 | -4.018 | 0.874 | 0.472 | 12.978 |
| -4 | 18 | -4.000 | 2.000 | 0.578 | -1.422 | 0.323 | 8.667 | 2.627 | -6.040 | 0.863 | 1.778 | 12.667 |
| -3 | 18 | -3.000 | 2.111 | 0.718 | -1.393 | 0.577 | 6.389 | 2.116 | -4.273 | 0.519 | 5.654 | 4.571 |
| -2 | 18 | -2.000 | 3.278 | 0.891 | -2.387 | 0.787 | 5.778 | 1.705 | -4.073 | 0.639 | 10.534 | 6.951 |
| -1 | 18 | -1.000 | 4.444 | 1.106 | -3.338 | 1.144 | 4.222 | 1.373 | -2.849 | 0.482 | 22.247 | 3.951 |
| +0 | 18 | 0.000 | 5.778 | 1.373 | -4.405 | 0.965 | 4.500 | 1.106 | -3.394 | 0.579 | 15.840 | 5.694 |
| +1 | 18 | 1.000 | 7.778 | 1.704 | -6.073 | 1.028 | 3.000 | 0.891 | -2.109 | 0.492 | 17.951 | 4.111 |
| +2 | 18 | 2.000 | 8.222 | 2.116 | -6.106 | 0.884 | 2.278 | 0.718 | -1.560 | 0.456 | 13.284 | 3.534 |
| +3 | 18 | 3.000 | 9.944 | 2.627 | -7.318 | 0.802 | 1.389 | 0.578 | -0.811 | 0.257 | 10.941 | 1.127 |
| +4 | 18 | 4.000 | 10.444 | 3.260 | -7.184 | 1.052 | 0.667 | 0.466 | -0.201 | 0.198 | 18.802 | 0.667 |
| +5 | 18 | 5.000 | 13.833 | 4.047 | -9.786 | 0.883 | 1.167 | 0.375 | -0.791 | 0.326 | 13.250 | 1.806 |

## Goal rate — and why the corpus mean is not the football number

The grid samples `dSquad` −5…+5 **uniformly**; a real season does not. Its fixtures
cluster near 0, and mismatched fixtures score more, so the corpus mean over-weights
blowouts and reads high against football even when the engine is right at the strengths
a league actually plays. Three figures, because quoting the first one alone has already
caused one false alarm:

| Population | Goals/match | Notes |
|---|---|---|
| Grid-weighted (raw corpus mean) | 10.33 | Correct for the fit; **not** a realism figure. |
| **Balanced — `dSquad ≈ 0`** | **10.28** | n=18. The football-comparable population, and the best-measured bucket. |
| League-weighted | 10.07 | Per-bucket rates re-weighted by the `dSquad` distribution of a real 20-club season under the shipped `StrengthDelta` ramp (98% of fixtures covered). |
| Football reference | ~2.7 | |


## Acceptance (KD-8, bars re-specified August 12, 2026 — ERR-030-033)

The original bar was a flat ±0.25 on every per-bucket mean. It could not be met by any
model at the depth KD-8 sizes, because a bucket mean is an **estimate**: 20 of 22 bucket-sides
carry a standard error larger than the whole bar. The bar is now stated against the
precision the corpus actually has — with ±0.25 kept as a **floor**, so a deeper corpus
automatically restores the original requirement rather than abandoning it.

**Mean agreement.** Per cell (bucket × side): `|model − corpus| ≤ max(0.25, 2·se)`, at most
2 exceedances (a 2σ screen over 22 cells expects ~1 by chance), none over
`max(0.4, 3·se)`, and a pooled `χ² ≤ χ²₀.₉₅(dof)` — which is where the statistical
power lives, since it catches systematic misfit that every individual cell passes.
A corpus shallower than 18/bucket is **not scoreable at all**, so the se-relative form cannot be
gamed by shrinking n.

| Check | Measured | Bar | |
|---|---|---|---|
| Worst cell deviation | 9.786 (|z| = 11.08) | per-cell, se-relative | |
| Exceedances | 20 | ≤ 2 | ❌ |
| Hard exceedances | 17 | 0 | ❌ |
| Pooled χ² | 693.9 on 19 dof | ≤ 30.1 | ❌ |
| Shallowest bucket | n = 18 | ≥ 18 | ✅ |

**Mean agreement: FAIL.**

**Distribution shape.** Win/draw/loss split within ±5.0 percentage points at `dSquad ≈ 0` (the bucket where home advantage shows as an asymmetry, so it is the one that actually tests the fitted `HomeAdvantageRating`) — corpus 50.0/5.6/44.4 vs model 42.9/26.8/30.2, **worst 21.3pp** (n=18). A bar failure must also be distinguishable from noise, or the honest answer is that the corpus cannot yet say: the corpus draw share carries a ±5.4pp standard error here, and n is below the pinned minimum of 250 at which a *pass* would be resolvable (a significant *fail* still is).

**Distribution shape: FAIL.**

**Overall verdict: FAIL — mean agreement FAIL, distribution shape FAIL.**

### Why the verdict reads the way it does

A bar is only meaningful against a measurement precise enough to test it, and a fit is
only meaningful if the model can express the shape it is fitting. Both are measured here
rather than assumed.

**1. Sampling resolution.** 20 of 22 bucket-sides have a
standard error on their own mean that already exceeds the ±0.25
bar (`se` column above). Where that holds, **no** model — including a perfect one — can
be shown to satisfy the bar, because the target it is scored against is not known that
precisely. That is a property of the grid's depth, not of the fit.

At the acceptance bucket the corpus draw share carries a ±5.4pp standard error
against a ±5.0pp bar — the bar is inside the noise at this depth.

**2. Model shape — the finding that no re-fit addresses.** A Poisson variable has
variance equal to its mean by definition, and KD-7's model *is* a Poisson draw. The
engine's scorelines are **over-dispersed**: mean var/mean = 1.776
across 22 bucket-sides, 17 of them above 1, pooled
chi2 = 664.2 on 374 dof (**z = +10.61**). So the engine
produces more blowouts and more shut-outs than any Poisson with the same means can, and
correspondingly **fewer draws** — which is exactly where the W/D/L bar is missed. No
choice of the three fitted parameters closes that gap; it is a statement about the
model's family, not its coefficients.

## KD-7a successor diagnostics

The three statistics KD-7a's adoption tripwire turns on, emitted every run so the
decision is made against measurements rather than re-derived by hand at the next capture.

| Statistic | Measured | What it decides |
|---|---|---|
| NB2 dispersion `α` (`var = μ(1+αμ)`) | **0.0294** weighted / 0.0485 unweighted — **NOT DETERMINED by this corpus** (one cell carries 36% of the weighted fit; estimators differ by 1.65×) | The successor's one new parameter, and whether this corpus can yet fix it. A variance estimate at 18 samples carries ~33% relative error and the weights go as `1/var²`, so one unlucky cell dominates. Adopting on this would be fitting noise. |
| Pooled within-bucket home/away correlation | **-0.209 ± 0.073** (n=198) | **Discriminates the candidate families.** Any shared-swing mechanism that would cut the draw share implies a clearly negative value; measured ≈ 0, such a family is refuted however well it fits the draw count. |
| NB2 draw share at the acceptance bucket | **26.7%** vs corpus 5.6% | **The number that stops NB2 being mistaken for a fix to the draw deficit.** It closes the dispersion gap and barely moves draws. |

## Raw rows

```csv
dSquad,homeGoals,awayGoals
-4.835776,0,4
-5.067450,0,1
-5.199414,0,5
-4.692083,2,5
-5.096775,0,11
-5.099707,0,12
-4.906159,0,6
-5.266863,0,6
-4.891498,2,11
-4.791789,1,2
-5.199414,1,11
-4.935482,0,11
-5.105573,0,1
-4.958946,1,9
-4.991203,0,8
-4.894428,1,11
-5.035190,0,10
-5.005865,1,7
-3.838710,2,6
-4.067450,1,7
-4.199414,3,4
-3.695015,4,9
-4.099708,3,10
-4.099707,2,10
-3.906159,0,12
-4.266863,1,16
-3.894429,3,3
-3.794722,3,6
-4.199414,2,9
-3.938416,0,5
-4.105573,0,10
-3.961878,3,8
-3.994136,3,5
-3.894428,4,9
-4.038123,0,16
-4.005865,2,11
-2.838710,0,7
-3.067450,0,9
-3.199414,1,6
-2.695015,7,5
-3.099707,2,11
-3.099707,1,3
-2.906159,0,7
-3.266863,0,4
-2.894429,4,4
-2.794722,5,5
-3.199413,2,5
-2.938416,0,8
-3.105573,0,6
-2.961878,7,7
-2.994136,2,4
-2.894428,5,8
-3.038122,0,10
-3.005865,2,6
-1.838710,1,7
-2.067450,0,7
-2.199414,2,3
-1.695015,9,5
-2.099707,3,7
-2.099707,5,8
-1.906159,2,4
-2.266863,1,6
-1.894429,5,7
-1.794722,9,8
-2.199413,2,5
-1.938416,0,8
-2.105573,1,3
-1.961878,3,1
-1.994136,11,1
-1.894428,4,6
-2.038122,1,6
-2.005865,0,12
-0.838710,1,4
-1.067450,3,4
-1.199413,1,4
-0.695015,12,1
-1.099707,3,3
-1.099707,5,4
-0.906159,0,6
-1.266863,1,5
-0.894428,19,3
-0.794722,9,3
-1.199413,4,9
-0.938415,1,7
-1.105573,2,4
-0.961878,4,3
-0.994135,6,3
-0.894428,7,1
-1.038122,1,5
-1.005865,1,7
0.161290,1,6
-0.067450,1,4
-0.199413,5,4
0.304985,11,2
-0.099707,5,4
-0.099706,8,6
0.093841,3,6
-0.266863,4,4
0.105572,16,3
0.205278,13,5
-0.199412,4,7
0.061584,4,5
-0.105573,2,6
0.038122,5,1
0.005865,7,0
0.105573,6,3
-0.038122,2,4
-0.005865,7,11
1.161290,1,2
0.932551,3,6
0.800587,10,2
1.304985,12,0
0.900293,6,5
0.900294,8,2
1.093842,5,7
0.733137,3,3
1.105572,12,0
1.205278,19,2
0.800588,8,4
1.061584,5,3
0.894427,6,5
1.038122,8,3
1.005865,12,1
1.105573,7,1
0.961878,4,2
0.994136,11,6
2.161291,4,3
1.932551,5,3
1.800587,8,2
2.304985,11,0
1.900293,11,3
1.900294,9,2
2.093843,2,2
1.733137,5,1
2.105572,10,1
2.205278,14,2
1.800588,12,2
2.061584,3,0
1.894428,10,7
2.038122,12,2
2.005865,12,1
2.105573,11,1
1.961878,5,2
1.994136,4,7
3.161291,12,3
2.932551,8,1
2.800587,11,1
3.304986,17,1
2.900293,8,2
2.900294,3,2
3.093843,7,4
2.733137,10,2
3.105573,13,1
3.205278,13,0
2.800588,13,2
3.061584,12,1
2.894428,7,2
3.038123,12,0
3.005865,6,0
3.105573,12,1
2.961878,8,0
2.994136,7,2
4.161291,10,1
3.932551,6,0
3.800587,11,0
4.304986,17,1
3.900293,5,3
3.900294,5,0
4.093843,2,0
3.733137,14,0
4.105573,20,1
4.205278,13,0
3.800588,13,0
4.061585,12,1
3.894428,11,2
4.038123,11,0
4.005865,12,0
4.105573,6,1
3.961878,11,1
3.994136,9,1
5.161291,13,0
4.932552,12,1
4.800587,12,1
5.304985,11,1
4.900294,16,0
4.900294,4,3
5.093843,19,0
4.733138,16,0
5.105571,18,0
5.205279,13,0
4.800587,12,1
5.061584,19,1
4.894429,16,3
5.038122,14,0
5.005866,16,1
5.105572,11,2
4.961877,17,5
4.994136,10,2
```

<!-- GENERATED:END -->
---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-11-24 | — | Initial record: PM-2 quick-sim check (roadmap engine lane item 4). |
