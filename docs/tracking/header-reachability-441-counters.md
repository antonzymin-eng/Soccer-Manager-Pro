# #441 Header Reachability — Counter Definitions and Local Results

> **Created:** October 10, 2026
> **Purpose:** Fixes, before any run, what the `header-reachability` instrument counts and what each
> counter can and cannot distinguish. It is the definitions record that
> `src/match-engine/tests/HeaderReachabilityDiagnosticTests.cs` cites. Issue #441 must be explained
> before W9 (roadmap step 9).
> **Status:** v0.14 — corrected local six-seed result in §6.4: one geometric arrival, whose two
> witness starts cannot be selected under the existing 10 Hz/first-eligible-start contract on the
> observed trajectories. A 1–2-frame delayed take-off remains a timing-only #10 contract-change
> candidate, not a demonstrated composed contact. The independent sanity definition in §3.8 is
> frozen October 20; its cadence was corrected October 22 after the invalid October 21 attempt.
> The accepted October 22 sanity result in §6.6 records sparse broad height/proximity overlap,
> without establishing head-contact reachability or a football rate.
> The earlier arrival result (§6.3) remains withdrawn. #441 remains open and gates W9; development
> proceeds independently of W8 C, with landing held until the C measurement completes.
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

### 3.7 Trigger-independent arrival census (revised October 20, 2026; corrected rerun recorded in §6.4)

The §3.1–§3.6 counters start from a production commit, so they cannot see a genuine arrival that the
trigger misses or that an earlier false commit pre-empts. The second arm observes the same frozen
six-seed run independently of `TryCommitHeaderIntents`:

1. Immediately before Heading.Update on every 60 Hz Physics frame, open a hypothetical jump window
   for every active, non-goalkeeper, non-sent-off outfielder who is not `GROUNDED` or `STUMBLING`,
   regardless of current possession. Heading #10 can start an already-committed deferred intent on
   such a frame even if possession has since been assigned; MatchEngine has no possession-change
   cancellation of that intent. The ball must still be loose at contact (step 3). Off-stride starts
   intentionally form an upper bound on legal tactical commits and deferred jump starts.
   `tacticalStartArrivals` separately counts passages with at least one
   in-volume window whose start frame is divisible by the tactical stride.
2. Use the production `PlayerAttributeProjection.ToHeading(..., fatigue: 0)` and
   `HeadingJumpKinematics.ComputeJumpReach` / `ComputeHeadZ` without changing any constant.
3. On subsequent real 60 Hz frames, count an **arrival** when the actual ball centre enters the
   production 3-D head-contact radius during `[apex − FramesEarlyTolerance,
   apex + FramesLateTolerance]`, while the ball is still loose and the player remains eligible. The
   actual ball path is used; no pass label, current trigger, or gravity-only forecast can create an
   arrival.
4. De-duplicate consecutive in-volume frames for the same player and physical flight. A new kick,
   first touch, unattributed deflection, restart, or detected falling-to-rising ground bounce starts a
   new flight leg for attribution; it must never invalidate an already-open jump window.
5. Correlate each arrival with the actual #10 lifecycle observer. `covered` requires a same-player
   actual jump start at **any** hypothetical start frame that produced in-volume contact during the
   passage, including earlier actual starts when a later start has also been reported and a start
   reported by #10 after the pre-Heading world sample on the contact frame. Actual starts are kept
   for the full bounded contact horizon. Record
   separately an earlier same-player commit and any other-player commit on the flight. The earlier
   same-player flag means **at least one** observed commit by that player on the contact flight
   preceded an in-volume hypothetical start; retain the first such commit frame even when a later
   same-flight commit follows the start. A new flight clears that commit context. Both flags are
   context only: their presence does not prove latch suppression or causal pre-emption.
6. Attribute each arrival to the latest observed `PassKick`, `ShotKick`, `GkHeadingKick`, first touch,
   deflection, bounce, restart or `Unknown` source. A falling-to-rising low ball is classified as a
   bounce only if **no observed world mutation occurred between the two sampled ball states**; a
   Resolve-time kick after one sample and before the next retains its known source. Attribution is an observation dimension only and
   never enters `HeaderIntent`, Heading #10 state, eligibility, or gameplay.

Per seed and aggregate, print `windows`, `actualCommits`, `arrivals`, `covered`, `missed`,
`tacticalStartArrivals`, `earlierSamePlayerCommit`, `otherPlayerCommit`, and source counts, plus one CSV
row per arrival with the **first contact's** ball state, head height and selected hypothetical start,
plus launch identity and passage-level coverage flags. `coveredContactFrame` / `coveredStartFrame`
and `tacticalContactFrame` / `tacticalStartFrame` identify the first in-volume witnesses for flags
that may be acquired from another window on the same frame or later in the passage; `-1` means no
witness. The selected first-contact geometry is not retroactively replaced when a later witness
raises a flag. Also
attribute every production commit (`commitSource` rows and `commitSources` totals), even if no arrival
exists. W8 events are not a source-complete impulse stream: unreported woodwork or other mutations may
retain the latest observed source. Treat source labels as latest-observed provenance, not proof of
uninterrupted ball flight.

**Decision rule frozen before the rerun.** If `arrivals = 0`, a commit-timing-only repair on the
observed ball/player trajectories is not supported; challenge the definition or localize delivery and
positioning next. This does not exclude a producer that also changes player movement. If every
arrival is covered, the current trigger is not the first zero. If arrivals exist and any are missed,
classify **every missed passage** against the proposed producer's legal 10 Hz selection cadence and
#10's jump-start lifecycle: identify an in-volume witness whose start can be selected and started
under those contracts, or mark the passage outside that contract and state the failed condition.
`tacticalStartArrivals` is a useful witness count, but divisibility alone does not establish that
a composed producer could have selected and started the jump. Apply the proposed producer first as
a read-only shadow selector to the same measured trajectories; require it to cover **all missed
passages classified as legally selectable**, and report the outside-contract remainder separately.
Before writing gameplay behavior, retain a composed pre-fix RED fixture proving that the current
producer misses a legally selectable arrival. This census does not authorize widening geometry or
changing any `[GT]` value.

**Known limits.** The callback reads the same pre-Resolve ball and player states as Heading.Update.
The counterfactual leaves each player's actual XY trajectory unchanged and permits starts at 60 Hz,
including starts during possession. It is an opportunity envelope, not proof that a legal 10 Hz
selector could commit an intent before such a start or realize every arrival. The producer's legal
commit classification remains the separate decision-rule step above.
Exact producer timing still needs a composed pre-fix RED fixture. Launch attribution is diagnostic;
Heading #10 must not consume Pass-specific labels (KD-5 remains controlling).

### 3.8 Trigger-independent height-band sanity census (frozen October 20, 2026; cadence corrected October 22; result in §6.6)

This is a separate descriptive check of whether loose balls pass near any outfielder in a plausible
heading-height band. It samples **every actual pre-`HeadingPhysics` callback** on the same six frozen
324,000-`RunTick` seeds. It does not read #10 jump state, the §3.7 arrival census, or a forecast. It does
not infer that a nearby player could have committed, jumped, or contacted the ball.

**Cadence correction, October 22, 2026:** each seed makes exactly 324,000 attempted and completed
`RunTick` calls, but the actual callback/sample interval is **1–323999 inclusive**, exactly 323,999
samples. The orchestrator advances the clock before Input; Input ends the match at tick 324,000,
so Physics returns before the pre-Heading hook on that tick. The snapshot still occurs at tick
324,000. Validate each attempted tick against exactly one same-tick callback while live and zero
at full-time, with no tolerance or other accepted gaps (including halftime, restarts or possession).
At the endpoint require first sample 1, last sample 323999, current tick 324000 and `MatchEnded=true`.

The **primary qualifying pair** on a sampled frame has all of these properties:

1. The ball's primary possession state is `NO_POSSESSION` and its centre Z is in **[1.5, 2.6] m,
   inclusive**.
2. The player is an active outfielder: neither goalkeeper nor sent off. Include `GROUNDED` and
   `STUMBLING`; movement state does not disqualify a pair.
3. Squared horizontal ball–player distance is **≤ 1 m²** (equivalently XY distance ≤ 1 m). Both
   teams use the same absolute world-coordinate test.

Report per seed and aggregate:

- `attemptedRunTicks` and `completedRunTicks`, both exactly 324,000 per seed, separately from
  `callbackFrames` and unique `sampledFrames`, both exactly 323,999 per seed. Within the expected
  callback interval accept no missing, duplicate, or out-of-order frame. `looseFrames` counts callbacks with primary
  `NO_POSSESSION`. `heightBandLooseFrames` counts its subset in the inclusive Z band.
- `outfielderObservations` counts active outfielder–frame observations on
  `heightBandLooseFrames`, before applying the 1 m cutoff. `allFramesOutfielderObservations` may be
  reported as context over every callback. Neither is a count of distinct players or chances.
- `anyNearFrames` counts height-band loose frames with at least one primary qualifying pair;
  `nearAgentFrames` counts **all** qualifying player–frame pairs, including multiple players on
  one frame. Report `nearByTeam[0/1]` and `nearByState` for all movement enum values, including
  `GROUNDED` and `STUMBLING`.
- On **every** `heightBandLooseFrames` frame, find the nearest active outfielder in XY before
  applying the 1 m cutoff. Bucket the distance in metres as `≤0.18`, `(0.18,0.5]`, `(0.5,1]`,
  `(1,2]`, and `>2`; count `noActiveOutfielder` separately. This histogram is not restricted to
  `anyNearFrames`. Report `vzRising` (`vz > 0`), `vzFalling` (`vz < 0`), and `vzStationary`
  (`vz == 0`) separately for all height-band loose frames and for the `anyNearFrames` subset.

A **passage** is one player's maximal run of consecutive sampled frames satisfying every primary
predicate. Any primary-predicate failure, frame-number gap, or run end closes it. Synthetic controls
deliberately exercise gap splitting; unexpected real-adapter gaps invalidate the run through the
per-attempt validator. Emit one row per passage with seed, player, team, inclusive start/end frames,
duration in frames, minimum XY distance
in metres, and the ball X/Y/Z and vertical velocity at that minimum (choose the earliest tied
minimum). Report the passage count and an exact duration distribution, without merging different
players or treating repeated frames in a passage as distinct passages.

Required reconciliations: `attemptedRunTicks = completedRunTicks = 324000` and
`callbackFrames = sampledFrames = 323999` per seed;
`heightBandLooseFrames ≤ looseFrames ≤ callbackFrames`;
`anyNearFrames ≤ heightBandLooseFrames`;
`anyNearFrames ≤ nearAgentFrames ≤ outfielderObservations`;
the team and movement-state pair totals each equal `nearAgentFrames`; passage durations sum to
`nearAgentFrames`; nearest-distance buckets plus `noActiveOutfielder` sum to
`heightBandLooseFrames`; each vertical-velocity split sums to its respective frame denominator.
Lock the bounds and multi-player multiplicity, a deliberate frame-gap passage split, home/away
mirrors, and a positive case through the **actual pre-Heading hook** with separate controls. Compare
all six final digests at tick 324000 with Run 3 before accepting the long-run result; the pinned
seeds and digests are unchanged.

The same per-attempt validator used by the corpus must pass two cheap composed controls before
another long run: a fresh enabled engine produces callbacks `[1,2]`, one on each of its first two
ticks, remaining live; a separate fresh enabled engine set to physics frame 323997 produces
callbacks `[323998,323999]` over four calls, with deltas `[1,1,0,0]`, ticks
`[323998,323999,324000,324001]` and ended flags `[false,false,true,true]`. Rejection controls require
failure for a skipped live callback, duplicate callback, callback at full-time, and wrong callback
tick. Failure diagnostics include seed, attempted/completed calls, expected/actual callback delta,
current/expected tick, first/last sample and ended state; flush output before throwing. Modest
console progress every 32400 attempts reports seed, attempts, callback count and current tick,
without changing any data-count logic.

Reading is deliberately narrow: a large `nearAgentFrames` count alone does not convict the
commit trigger or #10 contact logic, because proximity has different timing and volume criteria.
Near-zero `heightBandLooseFrames` or `anyNearFrames` would identify scarcity at the corresponding
exposure stage. No heading-rate benchmark, gameplay choice, `[GT]` change, or W9 closure follows
from this sanity census alone.

## 4. Behavior neutrality

The #10, W8 and pre-Heading observers are `internal`, null by default, and invoked behind null checks.
The arrival callback only reads public/test-only value copies. Its output is consumed by no simulation
path and its delegate is excluded explicitly in the snapshot proof.
`HeaderReachabilityObserver_DoesNotChangeSnapshotDigests` runs
18,000 frames of seed `0x0F1E2D3C4B5A6978` with all observers and the arrival census attached. It
asserts at least one #10 evaluation and one hypothetical jump window were observed (non-vacuity),
then requires every per-frame snapshot digest to match an unobserved run. The instrument prints each
seed's final digest so a run can be tied to the unobserved corpus.
`ArrivalCensus_StagedApexContact_IsCounted` mirrors both teams and the case where a deflection occurs
between takeoff and contact. Each requires one detected arrival at the staged apex.
`ArrivalCensus_DeferredStartDuringPossession_CoversLooseContact` mirrors both teams with a commit
deferred while the agent is `GROUNDED`, a later actual start during possession, and loose-ball contact
at the apex. It requires an arrival and same-start actual coverage, so a possession-gated hypothetical
start cannot pass as a missed arrival.

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
same frame (1,727 of 1,861 have zero lag). Of the 1,841 `PositionedPoorly` episodes:

- The forecast closest pass to the take-off point falls before the timing window
  (`apex − FramesEarlyTolerance`) in 1,816 (`beforeWindow`). The per-episode rows put it on the
  take-off frame itself in 1,721.
- Over the observed frames (up to the terminal frame, apex + 1), the minimum horizontal ball–head
  distance stays above 1 m in 1,528 (`poorlyMinDistXy`: 1,398 at 1–2 m, 9 at 2–5 m, 121 beyond).
- At that minimum-horizontal-distance frame, the ball is above the ±0.22 m head band in 1,740
  (`ballAboveHead`).

These counters locate the ball at its closest pass, not at the apex. They do not record where the
ball is at the apex itself. §3.5 anticipated commits that fire too early for an arriving ball
(`afterWindow`). The data show the opposite case: the ball's closest pass to the committer comes
before the window, mostly at take-off, so it is passing them rather than arriving at them.

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

### 6.3 Run 2 — October 18, 2026 (WITHDRAWN arrival result)

**Invalid for opportunity inference.** Review found that every observed flight change discarded
existing jump windows, hiding possible contacts following bounces/deflections. Sampling after
RunTick also followed Resolve pickup, rather than observing Heading.Update's actual world. The
staged same-flight positive control did not exercise either blind spot. The recorded zero below is
retained as an audit trail; it does not establish that opportunities are absent or close #441.

Subsequent PR #441 review also found three defects in the revised instrument: only the latest actual
jump start was retained while hypothetical starts had a full contact horizon; a same-frame
post-sample PassKick could be mistaken for a bounce on the next sample; and a passage could gain
coverage or tactical-start status without a corresponding witness in its first-contact CSV row.
Versions 0.8–0.10 correct those definitions and add staged controls. The corrected rerun is recorded
separately in §6.4; it does not rehabilitate this withdrawn result.

- **Run:** local Windows .NET 8 development run from `origin/main` `1a3b4271` plus the measurement-only
  working diff; one test passed in 20 m 25 s. This is not a certified CI/Unity measurement and has no
  GitHub artifact.
- **Validity:** the always-on 18,000-frame observer-neutrality test passed before this run; every seed
  ran to tick 324,000; `replicaMismatchFrames=0`; the census observed exactly the same 1,693 commits as
  the lifecycle arm; and the staged apex-contact detector-positive test is a separate required lock.

Aggregate lines, verbatim:

```text
AGGREGATE,lifecycle,commits=1693,overwrites=1,liveCancels=0,jumpStarts=1693,neverJumped=0,evaluations=36956,everPredicted=20,bodyPartMismatch=0,prepared=0,executed=0,failedEarly=16,failedLate=0,failedPositionedPoorly=1676,failedDisturbed=0,landingDrops=0,openAtEnd=0,replicaMismatchFrames=0
AGGREGATE_ARRIVAL,windows=5574339,actualCommits=1693,arrivals=0,covered=0,missed=0,earlierSamePlayerCommit=0,otherPlayerCommit=0,sources[unknown/pass/shot/gkOrHeader/firstTouch/deflection/bounce/restart]=0/0/0/0/0/0/0/0
```

| Seed | windows | actual commits | arrivals | Final digest (tick 324,000) |
|---|---:|---:|---:|---|
| `0x0F1E2D3C4B5A6978` | 954,167 | 275 | 0 | `6ef1e77f9dcf86c4bce5e1f66a989a64b42f26c268ead53340b9c57d282b4072` |
| `0x00000000D1A6D05E` | 942,525 | 276 | 0 | `06e3daf03f1c45a112f958c4bfac980c30956971e175b35b7d815d685b4caf93` |
| `0x0000000000000001` | 957,195 | 272 | 0 | `25ceac27f7d4e87fb1166227f683eaff87336efd301fa5500b6d212a28deba59` |
| `0x00000000ABCDEF12` | 925,964 | 287 | 0 | `77b4e0cc08a4ac903fbb364f2bb28612eaa39b380955111c58fe040ebf474a80` |
| `0x0000000099887766` | 924,808 | 301 | 0 | `ccc99ba78dce889ed07d18609c568486920ffd97c45cb283f15d7f5c203f6e4f` |
| `0x000000005A5A5A5A` | 869,680 | 282 | 0 | `6961b03ff4cb1512967c01c46158274b3516bb4c632d921f75e5f6baad936eb3` |

**Disposition:** the earlier reading that this was a completed negative prerequisite for W9 remains
withdrawn. The corrected §6.4 run still leaves reachability unresolved before W9. Develop #441 on its
isolated branch now and land after the C measurement; #434/#440 retain their D-05 after-B→C placement.

### 6.4 Run 3 — October 20, 2026 (corrected local arrival census)

**Measured candidate:** `codex/header-reachability-441`, then based on
`1a3b4271b5d7a50bbff3a88ef242345c8e49e89b` plus the measurement-only working diff, **not landed**.
The worktree subsequently fast-forwarded to `c95e27ba7fd398e37d297647ba060c2ab1f4baa7` through
seven documentation-only mainline commits. This does not change Run 3's measured base or source
hashes; the current candidate base is `c95e27ba`.
No gameplay, `[GT]`, snapshot layout, RNG stream or draw-order change. Windows .NET 8 shim lane,
SDK 8.0.425 / runtime 8.0.31: one test passed, zero failed/skipped, in **21 m 0.574 s**
(05:21:23–05:42:24 PDT). This is neither Linux CI nor governing Unity/Mono certification.

Raw artifact: `.test-results/header441/header441-arrival-final.trx`, SHA256
`03D4A3A85FE29B5F7386EC7F44A1E7EB4CBBE8D56BC9CCD03D87CDC756753F92`.
Frozen source SHA256: `MatchEngine.cs`
`909E402087177A3F23EB1EFB923CA43644A085C2DE862D03D9F0F10B4B39C77D`;
`HeaderReachabilityDiagnosticTests.cs`
`14A9755DDF15597CDC756ED7BC1AB24FB5BA2F19EC9E184976AC987E613FBF0D`.
The stable evidence entry point is
[`evidence/header441-reachability-20261020/README.md`](evidence/header441-reachability-20261020/README.md),
with the original classification probe and output, compressed raw and focused TRX files, and
`original-source.patch`. The ignored `.test-results/` and `Temp/` paths below identify where the
local run originally wrote its files; they are not the only preserved reproducer. The source hashes
above identify the **measured Run 3 bytes**. A later date correction in `MatchEngine.cs`'s version
history changes its current file hash without changing gameplay. The archived source patch
reconstructs the measured version; do not substitute a later current-file hash for this run's hash.

Command, from the isolated worktree:

```text
dotnet test src/match-engine/tests/match-engine-tests.gen.csproj --no-build --no-restore --filter "FullyQualifiedName~HeaderReachabilityDiagnostic_ReportsFrozenSixSeedCounters" --environment TD_HEADER441_DIAGNOSTIC=1 --logger "trx;LogFileName=header441-arrival-final.trx" --results-directory .test-results/header441 --verbosity quiet
```

Aggregate lines, verbatim:

```text
AGGREGATE,lifecycle,commits=1693,overwrites=1,liveCancels=0,jumpStarts=1693,neverJumped=0,evaluations=36956,everPredicted=20,bodyPartMismatch=0,prepared=0,executed=0,failedEarly=16,failedLate=0,failedPositionedPoorly=1676,failedDisturbed=0,landingDrops=0,openAtEnd=0,replicaMismatchFrames=0
AGGREGATE_ARRIVAL,windows=36961525,actualCommits=1693,arrivals=1,tacticalStartArrivals=0,covered=0,missed=1,earlierSamePlayerCommit=1,otherPlayerCommit=1,sources[unknown/pass/shot/gkOrHeader/firstTouch/deflection/bounce/restart]=0/1/0/0/0/0/0/0
AGGREGATE_ARRIVAL,commitSources[unknown/pass/shot/gkOrHeader/firstTouch/deflection/bounce/restart]=0/1619/0/0/0/9/65/0
```

The 36,961,525 windows are overlapping hypothetical starts, **not** that many distinct football
opportunities. Latest-observed commit provenance is Pass 1,619, Deflection 9 and Bounce 65; these
labels retain §3.7's source-completeness limitation and do not identify the pass profile.

| Seed | hypothetical starts | commits | arrivals | Final digest (tick 324,000) |
|---|---:|---:|---:|---|
| `0x0F1E2D3C4B5A6978` | 6,232,499 | 275 | 0 | `6ef1e77f9dcf86c4bce5e1f66a989a64b42f26c268ead53340b9c57d282b4072` |
| `0x00000000D1A6D05E` | 6,250,556 | 276 | 0 | `06e3daf03f1c45a112f958c4bfac980c30956971e175b35b7d815d685b4caf93` |
| `0x0000000000000001` | 6,257,243 | 272 | 1 | `25ceac27f7d4e87fb1166227f683eaff87336efd301fa5500b6d212a28deba59` |
| `0x00000000ABCDEF12` | 6,180,040 | 287 | 0 | `77b4e0cc08a4ac903fbb364f2bb28612eaa39b380955111c58fe040ebf474a80` |
| `0x0000000099887766` | 6,143,406 | 301 | 0 | `ccc99ba78dce889ed07d18609c568486920ffd97c45cb283f15d7f5c203f6e4f` |
| `0x000000005A5A5A5A` | 5,897,781 | 282 | 0 | `6961b03ff4cb1512967c01c46158274b3516bb4c632d921f75e5f6baad936eb3` |

All six `seedEnd` lines match Run 2 exactly, and the lifecycle aggregate is unchanged. This supports
behavior neutrality; it does **not** make Run 2's flawed arrival count valid. The replica check is
zero for every seed.

**Classifying the sole missed passage.** Seed 1, agent 6 (team 0), flight 2071, contacts at frames
125466–125467. Latest observed source is the PassKick at 125439 by agent 18 (team 1). Production
committed agent 18 at 125448 and agent 6 at 125454; agent 6 actually started at 125454 and failed
`PositionedPoorly` at 125475. The earlier/other-player flags are context, not proof of pre-emption.

An observation-only bounded replay enumerated **every** in-volume witness for this passage:

| contact frame | hypothetical start | distance (m) | head Z (m) | preceding / next tactical frame |
|---:|---:|---:|---:|---|
| 125466 | 125455 | 0.16765031 | 1.9139999 | 125454 / 125460 |
| 125467 | 125456 | 0.17849958 | 1.9139999 | 125454 / 125460 |

Retention starts at 125398, deriving lookback from contact ages 11–26 and the six-frame tactical
stride. The replay searches contacts 125430–125500, retains starts across possession/launch changes,
and records passage closure at 125468. Agent 6 is already `WALKING` and eligible at the **pre-Heading**
sample at 125454. Under #10's existing lifecycle, a surviving intent committed by that tactical
frame must start no later than 125454; the next tactical selection cannot start before 125460.
Neither can select 125455 or 125456 under the **existing** 10 Hz selection and #10 first-eligible-start
contract. Thus a producer that changes only which player is selected at an existing tactical tick
has no in-volume witness on these fixed observed trajectories. A #10 timing-contract change that
delays this already-committed agent's take-off by one or two physics frames would align it with the
two geometric witnesses. That remains a timing-only **candidate**, with no composed proof that #10
would prepare and execute a contact after the delay. No missed passage in this sample has a legally
selectable witness under the existing contract; no shadow-selector success or gameplay repair is
claimed from an empty selectable subset.

The local replay ran from ignored `Temp/header441-classification/`. Its original source and output
are also archived under the stable evidence entry point above. It remains development evidence,
not a shipped test or CI artifact. It completed successfully with this command:

```text
dotnet run --project Temp/header441-classification/probe.csproj -- Temp/header441-classification/classification.txt
```

`Program.cs` SHA256 `203522E3272C699086A54A01578432E535F70F67F9FD3786EBBE891D1E6A5368`;
output SHA256 `8BB2492CFE9CB53ADDA31F8D5330C5486B0A380AA91514E9FA064E8E1F209A92`.
Replay end: tick 125500, digest
`cf64c07c29edde36ed456865dca3cde20cd6148d576c59eac57d3ff8e2808e00`.
Fresh independent review found no High/Medium/Low defect in the probe or this narrow classification.

**Disposition:** this is not a zero-arrival corpus. It excludes a selection-only repair under the
existing 10 Hz/first-eligible-start contract for the sole missed passage on these observed
trajectories; it does **not** exclude the one- or two-frame delayed-take-off #10 timing-contract
candidate. The two witnesses establish geometry only, not a composed prepared or executed contact,
and no pre-fix RED for that candidate is claimed. It does not establish general header
unreachability or exclude movement/lifecycle changes. Delivery/positioning and the contact/start
contract require the next investigation before choosing behavior. No geometry, gameplay, spec, or
`[GT]` change is made or authorized here. **#441 remains open; W9 reachability is not resolved.** Hold this candidate
until C measurement completes; #434/#440 retain D-05. Tracking versions have been reconciled after
the seven documentation-only mainline commits. Reverify against the then-landed engine before
landing; these pre-C counts are not post-C evidence.

### 6.5 Candidate verification boundary

- Instrumentation AR-5: two fresh available-model reviewers returned zero High/Medium/Low findings.
- Final focused command (`ArrivalCensus` plus observer neutrality): **14 passed, zero failed/skipped**;
  original `.test-results/header441/header441-focused-final.trx`, preserved compressed under the
  [stable evidence root](evidence/header441-reachability-20261020/README.md). Includes both teams'
  real deferred #10 starts during possession and the 18,000-frame per-tick digest lock.
- Full policy composition `bash tools/run-tests-local.sh --pr` completed through native Windows Git
  Bash: **37 projects, 4,143 passed, zero failed, 225 NotExecuted**. Quarantine and owner-held RED
  ledgers have no active rows. This sweep compiled the earlier counter-test revision; the later
  test-only repairs are covered by the final focused run and corrected corpus above. Production
  `MatchEngine.cs` stayed at the recorded hash throughout.
- Changed-assembly coverage selection saw committed HEAD equal to its base (the diff is uncommitted),
  so this is **not changed-code coverage evidence**. Local shim checks are not Linux CI or governing
  Unity/Mono validation. No landing, push, PR or host certification is claimed.

### 6.6 Independent height-band sanity census — October 22, 2026

The §3.8 definition was frozen October 20. The first long attempt on October 21 is **invalid**:
the old assertion required a pre-Heading callback on the full-time tick and aborted after the first
complete match, taking about 31 minutes on the constrained host. No accepted seed summary or
digest comparison was emitted, and the partial output supports no census inference. Its failed
source SHA256 is `66D8AD8934C24BB2F3B04D1A6644CF460A8F05281830A28418615D75DADF0366`.
The source, console and partial output remain losslessly archived as
`raw/header441-sanity-failed-source-66D8AD89-20261021.cs.xz`,
`raw/header441-sanity-failed-console-20261021.log.xz`, and
`raw/header441-sanity-failed-partial-20261021.txt.xz` under the
[stable evidence root](evidence/header441-reachability-20261020/README.md).

October 22 corrects the cadence contract to 324000 calls and 323999 callbacks with exact per-tick
validation and boundary/rejection controls (§3.8); it changes no engine behavior, approved spec or
strict-census result and requires no ERR.

**Accepted corrected local run, October 22.** The Windows .NET 8 shim build of the standalone
probe succeeded with zero warnings and errors in 21.95 s. Direct DLL `--self-test` returned exit 0:
six synthetic cases, one real-hook positive, two real cadence controls and four rejection controls.
The six-seed `--corpus` then returned exit 0. Its timestamps are 12:46:05–13:10:13 UTC
(05:46:05–06:10:13 PDT), about 24 m 08 s wall time from those timestamps, not a certified
performance measurement. The accepted output and console are preserved under the
[stable evidence root](evidence/header441-reachability-20261020/README.md) as
`raw/header441-sanity-corrected-20261022.txt.xz` and
`raw/header441-sanity-corrected-console-20261022.log.xz`. Their decompressed SHA-256 values are
`7FCA13F8F3FBEDC5ACA380804FD240FA168304EECBCAA86241E9A4F468A85D02` and
`01D7F13A35020EC86A46F9B05B334C906A2A27BC6442A8362BB32D7B4669032F` respectively.
The direct-DLL self-test capture is
`raw/header441-sanity-selftest-20261022.txt.xz` (decompressed SHA-256
`3CF3C1AA3F2F74CEBFF293E1324D19B8E3CFCEEE7DE2BFFA043F6ED4B762C4F6`).

Each seed made 324,000 attempted and completed `RunTick` calls, observed exactly 323,999 callbacks
and samples at frames 1–323999, ended at tick 324000, and matched its Run 3 final digest in §6.4.
The aggregate is **1,944,000 attempted/completed calls; 1,943,994 callbacks/samples**. The six
final digest pins are unchanged. The original Run 3 hashes and the invalid October 21 artifacts
remain historical; this is a separate broad spatial census.

| Seed | Height-band loose frames | Frames with any player ≤1 m | Player-frame pairs ≤1 m | Passages |
|---|---:|---:|---:|---:|
| `0x0F1E2D3C4B5A6978` | 6,083 | 59 | 59 | 6 |
| `0x00000000D1A6D05E` | 6,170 | 42 | 42 | 7 |
| `0x0000000000000001` | 5,772 | 96 | 96 | 9 |
| `0x00000000ABCDEF12` | 5,781 | 48 | 48 | 5 |
| `0x0000000099887766` | 6,230 | 68 | 68 | 7 |
| `0x000000005A5A5A5A` | 5,381 | 42 | 42 | 5 |
| **Aggregate** | **35,417** | **355** | **355** | **39** |

The full denominator chain is 1,762,740 loose callbacks → 35,417 loose height-band frames →
703,506 active-outfielder observations on those frames → 355 unique frames and 355 player-frame
pairs within 1 m. The all-callback active-outfielder count is 38,569,934. The pair split is team
0/1 = 226/129 and movement state `IDLE/WALKING/JOGGING/SPRINTING/DECELERATING/STUMBLING/GROUNDED`
= 0/221/52/1/59/10/12. The nearest-outfielder histogram over **all 35,417** height-band loose
frames, before the cutoff, is `≤0.18/(0.18,0.5]/(0.5,1]/(1,2]/>2/noActive` =
5/73/277/1,318/33,744/0. Ball vertical velocity is rising/falling/stationary =
18,281/17,136/0 on those frames and 257/98/0 on the 355 near frames. The 39 passage rows' exact
durations sum to 355 player-frame pairs. These values satisfy the team, state, duration, nearest-bin
and vertical-velocity reconciliations frozen in §3.8; here no frame has more than one qualifying
outfielder. The complete 39 rows and exact duration distribution are in the archived raw output.

Of height-band loose frames, 355/35,417 (1.0023%) have any active outfielder within 1 m, while
33,744/35,417 (95.2763%) have the nearest active outfielder more than 2 m away. These are
descriptive proportions of sampled frames, not independent chances or a football frequency. The
Run 3 witness's seed `0x0000000000000001`, agent 6 appears in a broad sanity passage at frames
125457–125477 with minimum XY distance 0.16745716 m; **the 21 passage frames are not 21
head-reachable contacts**.

The sparse height/player overlap supports investigating delivery and positioning next, but does not
identify a causal repair or distinguish the kicker's departure from receiver arrival. Broad XY
proximity at 1.5–2.6 m does not validate, contradict, or replace §3.7's stricter head-volume and
jump-timing census. A one- or two-frame delayed take-off remains a possible #10 timing-contract
change, not implemented here and without composed contact proof. No header rate or `[GT]` tuning
follows. This local shim result is neither Linux CI nor Unity/Mono certification. **#441 and the W9
prerequisite remain open.** Land #441 only after the W8 C measurement; #434/#440 retain D-05's
after-B→C placement.

## Version history

| Version | Date | Change |
|---|---|---|
| v0.1 | October 10, 2026 | Created with the instrument: definitions, code facts, neutrality gate, run procedure. No result. |
| v0.2 | October 11, 2026 | PR #497 review: §3.3 splits geometric counterfactual hits from timing-valid first frames and reads (R) only from the latter; the replica check compares the exact first frame; the instrument fails on any mismatch. No result yet. |
| v0.3 | October 11, 2026 | First result recorded (§6): run `38104571610` at `0eb36aa8`, replica check 0. It supports (O) at the commit trigger: commits fire on low, rising balls already passing the agent, and the timing-valid counterfactual is 0 of 1,841. It does not support (R). §5 runtime corrected to the measured 18 minutes. |
| v0.4 | October 11, 2026 | PR #497 review (Low): §6.2 v0.3 read `beforeWindow`, `ballAboveHead` and the 1–2 m `poorlyMinDistXy` bucket as positions at the apex. They are a before-window forecast, the offset at the minimum-horizontal-distance frame, and a minimum over the observed frames. §6.2 now states each as measured and adds the per-episode counts (take-off frame 1,721; minimum above 1 m 1,528). The (O) reading is unchanged. |
| v0.5 | October 18, 2026 | Freezes §3.7 before its first run: trigger-independent actual-trajectory arrivals over production jump geometry/timing, exact same-player commit coverage, earlier/other-player pre-emption flags, and launch-leg attribution. Reuses the existing behavior-neutral observers and adds a non-vacuous digest-neutrality read lock. No behavior, `[GT]`, schema or RNG change; no result yet. |
| v0.6 | October 18, 2026 | Recorded the initial local six-seed result (5,574,339 windows / 1,693 commits / 0 arrivals). Its opportunity inference is withdrawn by v0.7. |
| v0.7 | October 18, 2026 | Review withdraws the initial arrival result: flight changes invalidated jumps and post-Resolve sampling missed Heading's world. Samples immediately before Heading.Update; retains windows across flight changes; includes a 60 Hz start envelope and tactical subset; attributes commits even without arrivals; adds mirrored deflection controls. Rerun required; #441 stays open before W9. |
| v0.8 | October 18, 2026 | PR #441 review: retain actual jump starts through the full contact horizon, compare world-mutation sequence between sampled states for bounce attribution, and record explicit coverage/tactical witness contact and start frames while retaining first-contact geometry. Adds staged two-start, post-sample PassKick, and later-witness controls. Six-seed rerun remains pending. |
| v0.9 | October 19, 2026 | AR3 M4: preserve the first same-player commit per observed flight so later same-flight commits cannot erase earlier context; add staged same-flight and new-flight controls. AR3 M5: classify every missed passage against legal 10 Hz selection and #10 lifecycle before requiring shadow-selector coverage of the legally selectable subset; report the remainder and require composed pre-fix RED proof. Corrected six-seed rerun remains pending. |
| v0.10 | October 20, 2026 | AR4 M6: open hypothetical start windows during possession because Heading #10 can start deferred intents independently of possession, while contact still requires a loose ball. Add mirrored possession-at-start/loose-contact positive controls asserting arrival and actual-start coverage. Producer legal-selection classification remains separate; six-seed rerun pending. |
| v0.11 | October 20, 2026 | Corrected local six-seed run: 36,961,525 hypothetical starts, 1,693 commits, one uncovered arrival and zero tactical-start arrivals; six end digests unchanged. Bounded replay classifies both witness starts outside existing 10 Hz selection with #10's first-eligible start on fixed trajectories. Records identities and verification limits; #441/W9 remain unresolved, no gameplay/tuning or pre-C landing. |
| v0.12 | October 20, 2026 | Narrows Run 3's exclusion to selection under the existing 10 Hz/first-eligible-start contract; retains delayed take-off by one or two physics frames as an unproven timing-only #10 contract-change candidate. Links the stable archive and distinguishes measured source bytes from a later history-date correction. Freezes the independent §3.8 height-band sanity definition, controls and reconciliations before its run; results remain pending. |
| v0.13 | October 22, 2026 | H1: correct §3.8 cadence to 324000 attempted/completed calls and 323999 callbacks at frames 1–323999, preserving the tick-324000 digest comparison. Adds exact per-attempt validation, real first-tick/full-time controls, rejected cadence defects, flushed diagnostics and modest progress. Records the invalid October 21 first attempt in §6.6; corrected sanity results pending. Historical October 20 freeze, Run 3 strict census and delayed-takeoff candidate remain unchanged. |
| v0.14 | October 22, 2026 | Records the accepted corrected six-seed independent height-band sanity result and preserved raw/self-test identities. All six 324000-call runs observed 323999 callbacks, matched the Run 3 digest pins, and reconciled 35417 band frames, 355 near frames/pairs and 39 passages. Interprets sparse broad proximity narrowly; #441/W9 remain open, W8 C landing gate and D-05 unchanged. |
