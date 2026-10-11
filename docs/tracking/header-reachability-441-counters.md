# #441 Header Reachability Counters — Definitions

> **Created:** October 10, 2026
> **Purpose:** Fixes, before any run, what the `header-reachability` instrument counts and what each
> counter can and cannot distinguish. It is the definitions record that
> `src/match-engine/tests/HeaderReachabilityDiagnosticTests.cs` cites. Issue #441 must be explained
> before W9 (roadmap step 9).
> **Status:** v0.3 — first result recorded in §6 (run `38104571610`, October 11, 2026). It supports
> (O), an opportunity problem at the commit trigger, and not (R). #441 stays open: no fix is chosen here.
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

Each search returns the **first** frame inside the head volume, as #10 does, or none.

- `replicaMismatchFrames`: frames where the restated `StaticHeight` search's first frame differs from
  #10's own `PredictedContactFrame` (including −1 against −1). **Must be 0**, or the counterfactuals are
  not comparable and the run is invalid. The instrument asserts this after printing its report, so an
  invalid run fails the lane.
- `cfTrajectoryHit` / `cfTrajectoryMovingHit`: `PositionedPoorly` episodes in which the counterfactual
  search found a **geometric** hit on at least one evaluated frame.
- `cfTrajectoryTimingValid` / `cfTrajectoryMovingTimingValid`: the subset whose first hit frame also lies
  in #10's timing window, `[apex − FramesEarlyTolerance, apex + FramesLateTolerance]` (§3.2 step 5).
  A geometric hit whose first frame falls outside that window would end as `MistimedEarly`/`MistimedLate`,
  not as a contact (PR #497 review: a hit at frame 5 against a window of 11–26 is such a case).

Reading: use the **timing-valid** counts, never the geometric ones, to argue (R). A large timing-valid
share supports (R) via fact 3. A large geometric share with a small timing-valid share means the
trajectory head meets the ball too early or too late, which is a timing/commit problem and points to (O).
A small geometric share means the ball does not pass through a 0.18 m sphere around the head at all,
which points to (O) or to geometry/radius.

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
(`TD_HEADER441_DIAGNOSTIC`). Six full 90-minute seeds; the first run's test took 18 minutes and the
whole lane 20. No host machine is needed; this is not a certified measurement.

## 6. Results

### 6.1 Run 1 — October 11, 2026

- **Run:** measurement lane `38104571610`, instrument `header-reachability`, branch
  `claude/ecstatic-einstein-877edo` (PR #497) at head `0eb36aa8e318597a41384629da6a67f3e4126f42`. Lane
  verification PASS (one test passed in 18 m 4 s; report sentinel captured).
- **Artifact:** `measurement-38104571610`, id `11689855947`,
  `sha256:e684ad3b8f5f0179e4c0207531bf7e5a39863c02f01260e354ad9a0c79bf8cc0`. GitHub expires it; the
  numbers below are the record.
- **Validity:** `replicaMismatchFrames=0` on every seed, so the counterfactual searches are comparable
  with #10's own search. Every seed ran to tick 324,000 with `openAtEnd=0`. Linux .NET 8, not the Unity
  host; Unity Mono gives different full-match digests for the same seed (expected until Stage 5+).

AGGREGATE lines, verbatim:

```text
AGGREGATE,lifecycle,commits=1861,overwrites=0,liveCancels=0,jumpStarts=1861,neverJumped=0,evaluations=40578,everPredicted=21,bodyPartMismatch=0,prepared=0,executed=0,failedEarly=20,failedLate=0,failedPositionedPoorly=1841,failedDisturbed=0,landingDrops=0,openAtEnd=0,replicaMismatchFrames=0
AGGREGATE,poorly,aerialCheck=26,noContactFrame=1815,cfTrajectoryHit=1,cfTrajectoryTimingValid=0,cfTrajectoryMovingHit=0,cfTrajectoryMovingTimingValid=0,ballAboveHead=1740,ballWithinHeadBand=29,ballBelowHead=72
AGGREGATE,poorlyMinDist3d[r/0.5/1/2/5/inf]=0/17/67/1627/8/122,poorlyMinDistXy[r/0.5/1/2/5/inf]=31/27/255/1398/9/121
AGGREGATE,poorlyForecast,minXy[r/0.5/1/2/5/inf]=12/22/264/1413/9/121,beforeWindow=1816,inWindow=23,afterWindow=2
AGGREGATE,commits,ballZ[1.0/1.6/2.0/2.6/inf]=1773/63/18/7/0,jumpLagFrames[0/5/11/inf]=1727/2/2/130
```

Per seed:

| Seed | commits | `PositionedPoorly` | `MistimedEarly` | Final digest (tick 324,000) |
|---|---|---|---|---|
| `0x0F1E2D3C4B5A6978` | 326 | 320 | 6 | `0d019bbf34ca29ad05385b3289ff15140c3de386140785d1a1d81c65697917b5` |
| `0x00000000D1A6D05E` | 329 | 327 | 2 | `4c19d44c7761152ddfdbbeb80753f8019f1df2d8b43b4aa29d781d16ec2ab643` |
| `0x0000000000000001` | 319 | 313 | 6 | `2517376ed7ee21dbaa304786b2d5cbf80812a6e54e46e51978fef1ebcd2f9387` |
| `0x00000000ABCDEF12` | 295 | 293 | 2 | `e2537ed15f7590446e7f50b3dbd564e870b3a25a6a06f935b2b62d85de999bc3` |
| `0x0000000099887766` | 313 | 312 | 1 | `dd92348f001dac5ac8c7b620f733b5ea667fdfbdc0e289a7f0189aba5f814b11` |
| `0x000000005A5A5A5A` | 279 | 276 | 3 | `21226b56c4e9b005c43791781a4e57db2b7682b7976bbe0e0b1117c50d0e4954` |

Derived from the per-episode `ep,` rows. Each row is printed twice in the captured output; the 1,861
unique rows match `commits`.

- At commit the ball is rising in 1,785 of 1,861 episodes (96%). For `PositionedPoorly` episodes,
  commit `vz` is p10/p50/p90 = 3.35 / 4.28 / 4.88 m/s.
- The forecast closest pass falls on the take-off frame itself (`forecastFrameMinusApex = −20`) in
  1,722 of 1,861 episodes (93%). For `PositionedPoorly` episodes, that closest pass is p10/p50/p90 =
  0.93 / 1.23 / 1.60 m from the take-off point, and below 1.6 m high in 1,790 of 1,841 (97%).

**Comparison with September 23** (run `35887487201`, production `f40f085`): 1,811 → 1,861 commits,
18 → 21 ever-predicted, 1,793 → 1,841 `PositionedPoorly`, 16 → 20 `MistimedEarly`, 0 → 0 prepared. The
shape is unchanged on newer production.

### 6.2 Reading against §1

**(R) is not supported.** The timing-valid counterfactual count is 0 of 1,841 for both
`TrajectoryHeight` and `TrajectoryMoving`; the geometric counts are 1 and 0. A head that rises along
the §3.3 parabola, with or without moving at the agent's velocity, still almost never meets the
predicted ball. Fact 3 (the static-height search) is real, but changing it alone would not produce a
contact on these episodes.

**(O) is supported, at the commit trigger.** The trigger fires as soon as a loose ball at
`z ≥ 0.5 m` is within 1.5 m of the nearest outfielder. In this run that is almost always a low ball
going up: 95% are below 1.0 m at commit and 96% are rising, at about +4 m/s. The jump starts on the
same frame (1,727 of 1,861 have zero lag). The ball is already at its closest to the take-off point at
take-off (`beforeWindow` 1,816 of 1,841) and then moves away. By the apex 20 frames later it is above
the head (`ballAboveHead` 1,740) and 1–2 m away horizontally (1,398). §3.5 anticipated commits that
fire too early for an arriving ball (`afterWindow`). The data show the opposite case: the ball is
passing the committer, not arriving at them.

**What this run does not show:**

- **Missed genuine chances.** The counters observe committed episodes only. They do not measure
  whether real aerial arrivals (a dropping cross or long ball at 1.6–2.6 m passing within reach of an
  outfielder) occur and draw no commit, or are pre-empted by an earlier commit.
- **Where the rising balls come from.** Kicker identity and launch type are not recorded, so the run
  cannot say whether these are lofted passes and clearances leaving a nearby foot, or bounces.
- **The latch's effect.** `_headerCommittedThisEpisode` clears for every agent that is not the current
  nearest candidate, so the trigger is not strictly once per flight. Whether that adds or suppresses
  commits is not counted.
- **The 121 episodes with a minimum distance above 5 m.** These are unexplained; the forecast ignores
  bounces and deflections.

None of this authorizes tuning (KD-W1). #441 stays open and still precedes W9. The next measurement
question is an arrival census that does not depend on the commit trigger, plus launch attribution for
committed episodes. Choosing it is #441's decision, not this record's.

## Version history

| Version | Date | Change |
|---|---|---|
| v0.1 | October 10, 2026 | Created with the instrument: definitions, code facts, neutrality gate, run procedure. No result. |
| v0.2 | October 11, 2026 | PR #497 review: §3.3 splits geometric counterfactual hits from timing-valid first frames and reads (R) only from the latter; the replica check compares the exact first frame; the instrument fails on any mismatch. No result yet. |
| v0.3 | October 11, 2026 | First result recorded (§6): run `38104571610` at `0eb36aa8`, replica check 0. It supports (O) at the commit trigger: commits fire on low, rising balls already passing the agent, and the timing-valid counterfactual is 0 of 1,841. It does not support (R). §5 runtime corrected to the measured 18 minutes. |
