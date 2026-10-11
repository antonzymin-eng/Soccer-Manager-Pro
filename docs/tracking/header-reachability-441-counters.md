# #441 Header Reachability Counters — Definitions

> **Created:** October 10, 2026
> **Purpose:** Fixes, before any run, what the `header-reachability` instrument counts and what each
> counter can and cannot distinguish. It is the definitions record that
> `src/match-engine/tests/HeaderReachabilityDiagnosticTests.cs` cites. Issue #441 must be explained
> before W9 (roadmap step 9).
> **Status:** v0.1 — instrument landed; **no result recorded yet.** Results go in §6 when a run on the
> frozen seeds exists.
> **Authority:** a measurement record, not a spec. It changes no `[GT]` value, gameplay path, schema,
> RNG stream or draw order. KD-W1 still applies: nothing here authorizes tuning.

## 1. Question

Issue #441's September 23 localization (run `35887487201`, production parent `f40f085…`) found, over
the six frozen seeds: **1,811 commits → 1,811 jump starts → 18 ever-predicted → 0 prepared Head
contacts → 0 executed**, with 1,793 `PositionedPoorly` and 16 `MistimedEarly`. It left open whether the
zero is:

- **(O) an opportunity problem:** committed headers are not genuine chances. The ball never comes near
  a jumping head, or not at the right time; or
- **(R) a realization problem:** genuine chances exist, but Heading #10's predict-then-contact logic
  cannot turn them into a contact frame.

These counters split the 1,793-class `PositionedPoorly` outcome into causes that point one way or the
other. They do not decide a fix.

## 2. Code facts the counters are built on (verified at `2a396d5`)

1. **Commit** (`MatchEngine.TryCommitHeaderIntents`): the nearest active outfielder within
   `HeaderTriggerRangeM` (1.5 m, XY) of a loose ball at `z ≥ HeaderTriggerMinBallHeightM` (0.5 m)
   commits, once per airborne episode.
2. **Jump** (`HeadingMechanics.Update`): the jump starts on the first frame at or after the commit tick
   on which the agent is neither `GROUNDED` nor `STUMBLING`. Head height follows
   `HeadingJumpKinematics.ComputeHeadZ`: a parabola from **0 m** at take-off to `JumpReach`
   (2.20–2.60 m) at the apex (frame +20 of 39 at 650 ms) and back to 0 m.
3. **Prediction** (`HeadingEligibility.FindContactFrame`): searches frames `[now, apex + late]` for the
   first frame at which the gravity-only predicted ball centre lies within
   `HeadContactVolumeRadiusM` (0.18 m) of the head centre. The head centre is held at **this frame's**
   position and **this frame's** height for the whole search.
4. **`PositionedPoorly`** is emitted when no contact frame is found (or the aerial check fails) and the
   current frame is past the apex.

Fact 3 is the most specific candidate for (R). Early in the rise the head is near the ground, so a
search that holds today's head height cannot find a ball that the rising head would meet later. By
the time the head is high, little of the window remains. The counterfactual counters in §3.3 test
exactly this.

## 3. Counters

All counters are per seed and aggregated. Each closed episode also prints one CSV row (`ep,…`) so
later analysis needs no rerun.

### 3.1 Lifecycle (reproduces the September 23 shape)

`commits`, `overwrites`, `liveCancels`, `jumpStarts`, `neverJumped`, `evaluations` (Pass-1 frames
observed), `everPredicted` (episodes with any `PredictedContactFrame ≥ 0`), `bodyPartMismatch`
(predicted but rejected by the §3.2 step-4 height band, not by timing), `prepared`, `executed`,
`failedEarly`, `failedLate`, `failedPositionedPoorly`, `failedDisturbed`, `landingDrops`, `openAtEnd`.

Production `main` has changed since `f40f085`, so these need not equal the September figures. A
large difference is itself a finding to record, not an error.

### 3.2 `PositionedPoorly` terminal sub-cause

- `aerialCheck`: the agent was `GROUNDED`/`STUMBLING` at the terminal frame (§3.2 step 1).
- `noContactFrame`: aerial, but the search found no frame.

These must sum to `failedPositionedPoorly` (asserted).

### 3.3 Counterfactual searches (the (R) test)

For every evaluated frame of an episode, the instrument re-runs the §3.2 search three ways, using the
same gravity-only predictor and radius:

| Model | Head height | Head position |
|---|---|---|
| `StaticHeight` (as shipped) | this frame's | this frame's |
| `TrajectoryHeight` | §3.3 parabola at each searched frame | this frame's |
| `TrajectoryMoving` | §3.3 parabola at each searched frame | moves at the agent's current velocity |

- `replicaMismatchFrames`: frames where the restated `StaticHeight` search disagrees with #10's own
  `PredictedContactFrame ≥ 0`. **Must be 0**, or the counterfactuals are not comparable and the run
  is invalid. The instrument asserts this after printing its report, so an invalid run fails the lane.
- `cfTrajectoryHit` / `cfTrajectoryMovingHit`: `PositionedPoorly` episodes in which the counterfactual
  search would have found a contact frame on at least one evaluated frame.

Reading: a large `cfTrajectoryHit` share supports (R) via fact 3. A small one means the ball does not
pass through a 0.18 m sphere around a correctly-timed head, which points to (O) or to geometry/radius.

### 3.4 Observed miss geometry

Per `PositionedPoorly` episode, over its evaluated frames, using the actual (not predicted) ball and
the actual head:

- `poorlyMinDist3d[...]`: minimum 3-D ball–head distance, bucketed at `r`(0.18)/0.5/1/2/5 m/∞.
- `poorlyMinDistXy[...]`: minimum horizontal distance, same buckets.
- `ballAboveHead` / `ballWithinHeadBand` / `ballBelowHead`: vertical offset at the minimum-horizontal
  frame, against `HeadContactVolumeHeightM` (±0.22 m).

The observation window ends at the terminal frame (apex + 1), so a ball that arrives later shows as a
large minimum. §3.5 covers that case.

### 3.5 Arrival forecast at jump start

At jump start, the gravity-only ball is projected for the jump plus 60 frames. The instrument records
the closest horizontal pass to the agent's take-off position, its frame relative to the apex, and the
ball height there.

- `poorlyForecast minXy[...]`: closest predicted pass, same buckets.
- `beforeWindow` / `inWindow` / `afterWindow`: closest pass before `apex − early`, inside the
  tolerance window, or after `apex + late`.

Reading: mostly `afterWindow` means commits fire too early for the ball's arrival. Mostly a large
`minXy` means the committing agent is not under the flight path. Both point to (O) at the commit
trigger, not to #10's contact logic. Bounces are ignored by the forecast (gravity-only, like #10).

### 3.6 Commit context

- `ballZ[1.0/1.6/2.0/2.6/∞]`: ball height at commit (the trigger floor is 0.5 m).
- `jumpLagFrames[0/5/11/∞]`: frames from commit to jump start.

## 4. Behavior neutrality

The observer is `internal`, null by default, invoked only behind a null check, receives copies, and is
read by no simulation path. `HeaderReachabilityObserver_DoesNotChangeSnapshotDigests` runs 18,000
frames of seed `0x0F1E2D3C4B5A6978` with the observer attached. It asserts at least one #10 evaluation
was observed (non-vacuity), then requires every per-frame snapshot digest to match an unobserved run.
The instrument prints each seed's final digest so a run can be tied to the unobserved corpus.

## 5. How to run

Measurement lane (`.github/workflows/measure.yml`), instrument id **`header-reachability`**
(`TD_HEADER441_DIAGNOSTIC`). Six full 90-minute seeds; expect roughly 10 minutes. No host machine is
needed; this is not a certified measurement.

## 6. Results

None yet. Record: run id, head SHA, artifact id, the AGGREGATE lines, `replicaMismatchFrames`, and a
reading against §1 that says which of (O)/(R) the numbers support and what they do not show.

## Version history

| Version | Date | Change |
|---|---|---|
| v0.1 | October 10, 2026 | Created with the instrument: definitions, code facts, neutrality gate, run procedure. No result. |
