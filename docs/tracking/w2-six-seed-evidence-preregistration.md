# W2 six-seed evidence pre-registration and closeout

**Date:** 2026-09-18  
**Status:** COMPLETE — post-#416 six-seed stall/non-vacuity revalidation; **not** W2 efficacy or calibration evidence  
**Production base:** `e8207f4c6f4d9d301872da869e3796163b8b26ad` (merged PR #416)  
**Permanent gate impact:** none; normal PR CI remains the existing two adversarial seeds.

## Frozen design

Before any six-seed result share was observed, Step 3.2 froze:

- six existing same-population final-third seeds;
- 324,000 ticks per seed (one full 90-minute match);
- one sample every 6 ticks;
- exact `scenario.roster` squad recipe;
- per-seed floor `floor(0.80 × corrected baseline samples)`;
- unchanged strict `> 0.70` check in both mirrored positioning snapshots;
- production W2 versus explicit zero-radius disarmed control;
- no post-result corpus, floor, run-length or threshold changes.

Phase-1 governing run `35389373818` at
`a1f105c9baf2205877fc6f9852331576daa97b6e` emitted counts only and froze:

| Seed | Baseline samples | Floor |
|---|---:|---:|
| `0x0F1E2D3C4B5A6978` | 15,830 | 12,664 |
| `0x00000000D1A6D05E` | 18,909 | 15,127 |
| `0x5EED000000000003` | 15,671 | 12,536 |
| `0x5EED000000000004` | 15,550 | 12,440 |
| `0x00000000D1A6D05F` | 18,162 | 14,529 |
| `0x1A2B3C4D5E6F7081` | 16,423 | 13,138 |

The first and last counts exactly reproduce the permanent detector's previously frozen values.

## Result run

Governing result run: **`35389986678`**, attempt 1, exact frozen head
**`dbd3053ad191e06f587ce84fce280ebe74e4bec7`**.

All six production legs satisfied their frozen sample floors and strict `> 0.70` predicate. All six
disarmed legs recorded exactly zero tackle outcomes. The exact rows are archived under
`docs/tracking/evidence/w2-six-seed/`.

## Interpretation correction

The earlier draft called the metric a "possession share." That is too strong.

The driver counts `InPoss || OutOfPoss`. `PhaseClassifier` returns those phases when the positioning
snapshot says `HasTeamPossession`; transition phases represent no-team-possession state. The metric
is therefore **team-possession-present / settled-possession-phase occupancy near a final third**, not
one team's share of possession. Home and away values are mirrored views of the same global fact and
are not independent estimates.

That distinction matters because all six **disarmed** runs also satisfy the same predicate, only
0.192–1.335 percentage points below their production counterpart. The negative control therefore
shows that this corpus does not discriminate W2 efficacy.

The justified conclusion is narrower:

> On post-#416 production behavior, the six-seed corpus shows no recurrence of the historical
> W2-associated settled-possession collapse / ERR-001-006 deadlock while W2 is active. The disarmed
> control is also healthy, so this evidence cannot credit W2 for the healthy state and does not
> validate W2's realism or outcome calibration.

## Outcome evidence that must travel with the conclusion

Production W2 resolves 25–37 challenges per full match. Across six matches it records:

- **188 resolved** (31.3/match);
- **11 won** (1.83/match);
- **29 loose** (4.83/match);
- **5 foul** (0.83/match);
- **143 missed** (23.83/match).

Per match, clean wins are 0–3 and tackle-outcome fouls are 0–2. The standing governance state is
unchanged: the ten tackle-outcome `[GT]` values and `TackleCooldownStrides` are uncalibrated.

The later foul/card pass may now measure the complete post-W2 stream because W2 is active and the old
stall is absent, but it must not calibrate from these five tackle-foul observations. A dedicated
sample-bearing measurement is required before fitting foul/card values, and tackle-outcome calibration
remains separate.

## Evidence mechanics and caveats

- Phase-2 production counts reproduce Phase-1 counts exactly on the unchanged deterministic head.
  The 80% floor is therefore a starvation/config/determinism tripwire here, not a statistical-power
  statement.
- Production matrix jobs assert the governed predicates. The aggregate job records summary flags but
  does not itself fail when a flag is false.
- Disarmed jobs assert zero tackle outcomes, then return; their reported phase share is diagnostic.
- The temporary driver suppressed failing-message policing with
  `LogAssert.ignoreFailingMessages = true`. Logs contain substantial `FM-DT-09`,
  `TargetResolver` clamp and executor-warning traffic in both arms. This evidence does not
  disposition those warnings.
- Temporary evidence code and workflow are removed in the closeout commit rather than merged into
  normal CI. The driver asserted on its env selector and was never suitable as a permanent normal-gate
  test.

## Pre-result setup corrections retained for provenance

Two harness corrections occurred before the governing result:

1. `3afe6baf…` used a different roster registration-site label; its run `35388265274` is superseded.
2. `656b9300…` exposed detailed-console duplicate echo of an identical `TestContext` row; validation
   was corrected to accept repeated identical rows while still rejecting conflicting rows.

Neither changed a seed, floor rule, run length, production configuration or result threshold.

## Closure

**Step 3.2 is complete** as the broader six-seed post-#416 stall/non-vacuity evidence obligation.

It does not close tackle-outcome calibration, foul/card calibration, T-DA-DET-005, or any warning
channel observed in the run.
