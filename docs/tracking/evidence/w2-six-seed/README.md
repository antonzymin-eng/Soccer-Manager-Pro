# W2 post-#416 six-seed evidence

**Captured:** September 18, 2026  
**Production base:** `e8207f4c6f4d9d301872da869e3796163b8b26ad` (merged PR #416)  
**Baseline run:** `35389373818` at `a1f105c9baf2205877fc6f9852331576daa97b6e`  
**Result run:** `35389986678` at `dbd3053ad191e06f587ce84fce280ebe74e4bec7`  
**Status:** Step 3.2 complete as a six-seed **stall/non-vacuity revalidation**. This is not W2 efficacy or tackle-outcome calibration evidence.

## What the measured predicate actually is

The driver reuses `MatchEngineInPossGateScenarios`:

`IsPossessionPhase(phase) => phase == InPoss || phase == OutOfPoss`.

`PhaseClassifier` returns those two values when the positioning snapshot says
`HasTeamPossession`; it uses transition phases when no team possession is present. The reported
"homeShare"/"awayShare" therefore measure the share of sampled final-third ticks for which the
positioning snapshot says **some team has possession**, seen through the two mirrored team snapshots.

They are not per-team possession shares and are not two independent estimates. Their equality is
expected when mirrored snapshot construction is coherent. They also are not a direct
`BallStateType.Controlled` measure because team possession can remain defined across an in-flight
pass.

## Governing results

All six production legs retained the preregistered final-third population and stayed above the
unchanged strict `> 0.70` settled-possession predicate. The old post-W6 / pre-#416 collapse did not
recur.

| Seed | Samples / floor | Production share | Resolved | Won | Loose | Foul | Missed |
|---|---:|---:|---:|---:|---:|---:|---:|
| `0x0F1E2D3C4B5A6978` | 15,830 / 12,664 | 0.970815 | 27 | 0 | 9 | 1 | 17 |
| `0x00000000D1A6D05E` | 18,909 / 15,127 | 0.950341 | 37 | 3 | 4 | 2 | 28 |
| `0x5EED000000000003` | 15,671 / 12,536 | 0.971412 | 29 | 2 | 3 | 0 | 24 |
| `0x5EED000000000004` | 15,550 / 12,440 | 0.962894 | 37 | 3 | 3 | 2 | 29 |
| `0x00000000D1A6D05F` | 18,162 / 14,529 | 0.963165 | 25 | 1 | 3 | 0 | 21 |
| `0x1A2B3C4D5E6F7081` | 16,423 / 13,138 | 0.964379 | 33 | 2 | 7 | 0 | 24 |

Across six full matches: **188 resolved challenges = 31.3/match**, comprising **11 won,
29 loose, 5 foul, 143 missed**. Per match, resolved volume is 25–37, clean wins 0–3 and
tackle-outcome fouls 0–2. Those numbers are observations of the current uncalibrated `[GT]` set;
they are not acceptance targets.

The disarmed arm genuinely disables W2: all six runs record
`won=loose=foul=missed=0`. Yet the same settled-possession predicate remains healthy in every
disarmed run:

| Seed | Disarmed share | Production − disarmed |
|---|---:|---:|
| `0x0F1E2D3C4B5A6978` | 0.964825 | +0.599 pp |
| `0x00000000D1A6D05E` | 0.945848 | +0.449 pp |
| `0x5EED000000000003` | 0.969489 | +0.192 pp |
| `0x5EED000000000004` | 0.955075 | +0.782 pp |
| `0x00000000D1A6D05F` | 0.952334 | +1.083 pp |
| `0x1A2B3C4D5E6F7081` | 0.951025 | +1.335 pp |

So this corpus has little discriminating power for W2 efficacy. It establishes that the historical
stall/deadlock is absent on the post-#416 engine while W2 is active; it does **not** establish that
W2 causes the healthy phase occupancy or that its outcome distribution is realistic.

## Floor interpretation

The six Phase-1 floors were preregistered as
`floor(0.80 × corrected_baseline_samples(seed))` before any result share was observed.

On the unchanged deterministic production arm, Phase 2 reproduces the Phase-1 sample counts exactly.
For this same-head revalidation the floor therefore functions primarily as a starvation/config/
determinism tripwire, not as a statistical-power calculation. It remains useful for future
trajectory-moving code because one seed cannot be starved and hidden by another.

## Workflow-success interpretation

The production matrix tests themselves assert the floor and `> 0.70` predicates, so their green
status is meaningful for those preregistered checks. The aggregate job deliberately records
`production_all_pass` rather than failing on that summary flag. The disarmed arm returns after
asserting zero tackle outcomes; its reported share is diagnostic and is not enforced.

Therefore workflow success is supporting execution evidence, not a substitute for reading the
retained rows.

## Warning channels observed

The evidence driver temporarily set `LogAssert.ignoreFailingMessages = true`. Detailed logs contain
substantial pre-existing warning traffic in both production and disarmed arms, including
`FM-DT-09`, `TargetResolver` pitch-bound clamps, and executor possession/state warnings. This
Step-3.2 record does not disposition or close those warning channels, and its green result must not be
cited as evidence that the composed match ran warning-clean.

## Sequencing consequence

Step 3.2's historical stall prerequisite is satisfied. The **ten tackle-outcome `[GT]` values and
`TackleCooldownStrides` remain uncalibrated**, exactly as the existing W2 governance record says.

This corpus does **not** authorize fitting foul/card or tackle-outcome constants from the five observed
tackle fouls. A later foul/card pass must begin with a sample-bearing measurement of the complete
post-W2 foul/card stream; tackle-outcome calibration remains a separate governed task and needs an
instrument sized to its own outcome rates.

## Durable files

- `baseline-counts-and-floors.tsv` — exact aggregate from run `35389373818`
- `w2-six-seed-results.tsv` — exact aggregate from run `35389986678`
- `provenance.txt` — governing run/head/artifact ids
- `artifact-SHA256SUMS` — hashes of the two archived aggregate TSV files

The original Actions artifacts retain per-arm TRX, detailed logs, exit status, parsed rows and
per-artifact SHA-256 manifests.
