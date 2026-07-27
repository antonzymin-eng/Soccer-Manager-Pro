# Goalkeeper Save Pipeline — the §5.Z.15 lever, measured

> **Created:** July 27, 2026
> **Status:** DESIGN SUPPLEMENT — the same governance class as `match-engine-design.md`. Opens no
> numbered spec and changes no `SPEC_INDEX.md` row. Files three cross-spec back-props against
> Goalkeeper Mechanics **#11** (`ERR-011-002` / `-003` / `-004` — `-001` was taken by the May 2026
> `DOMAIN_TAG_GOALKEEPER` allocation, verified against the log before assignment).
> **Owner document:** `docs/tracking/match-engine-design.md` **§5.Z.17**.
> **Purpose:** §5.Z.15 recorded the next lever on the goal rate as *"the quality of the goalkeeper's
> save, not further shot or finishing tuning"*. This note is the measurement of that lever, the three
> correctness fixes it turned up, and — the part that matters most for **A4a** — the evidence that
> the lever is real but **not** where the goal rate lives.

---

## 1. The finding, and what measurement changed about it

§5.Z.15's framing has a premise inside it: that saves *happen*, and are merely poor. **They did not
happen.** Over three full 90-minute matches, across all six keeper-matches, the goalkeepers made
**zero** hand contacts with the ball. Not one. "Save quality" was not a low number; it was undefined.

This is the project's recurring shape — §5.Z.7 framed the foul problem as a `[GT]` threshold question
and the measurement refuted it; §5.Z.11 named two candidate causes for the scoring asymmetry and the
measurement rejected both. Here the recorded lever was not wrong so much as it stopped one level too
early, and nothing in the tree could have told anyone, because **no instrument had ever reported a
goalkeeper statistic of any kind**.

### 1.1 The instrument

`src/match-engine/tests/GkSaveDiagnosticTests.cs` (env-gated `TD_GK_DIAGNOSTIC=1`, assertion-free per
the ERR-030-014 convention). It reports the save pipeline as a **funnel**, because a funnel localises
*where* a chain breaks instead of only reporting that its end is empty:

```
armed  →  SAVE committed  →  Anticipate  →  Diving  →  Airborne  →  contact  →  caught / parried / spilled
```

plus the per-state tick histogram, the reaction-window and handling-quality distributions at contact,
and — added once the first run showed the collapse point — the **dive miss distance**, which separates
"the reach is slightly too small" from "the keeper is nowhere near it".

A second test reports the **arithmetic ceiling** on handling quality. That one is not a simulation: it
answers "if a keeper *did* reach the ball, could it ever catch it?", which the funnel cannot answer
while the contact count is zero, and which decides whether the remaining work is tuning or a fix.

Its one production seam is `MatchEngine.TestOnly_GoalkeeperState`, reading through the **same** public
`CaptureState` the v19 snapshot writer uses — an instrument on a parallel surface could disagree with
what the engine actually persists.

### 1.2 What it measured (pre-fix, 3 full matches, `ConfigureSquads` path)

| | gk0 | gk1 | reading |
|---|---|---|---|
| armed ticks | 1177–3594 | 1210–3613 | the geometry fires: 20–60 s of armed threat per match |
| SAVE committed | 14–30 | 14–41 | the Decision Tree **does** decide to save |
| Airborne entries | 13–25 | 14–31 | the keeper **does** dive |
| **hand contacts** | **0** | **0** | **the chain ends here** |
| catches | 0 | 0 | — |
| mean \|diveDirectionLateral\| | **0.000** | **0.000** | the dive has no direction |
| best miss over a whole match | 4.58–6.04 m | 2.75–5.58 m | not a near miss |
| Anticipate share of match | 76–92% | 81–92% | the keeper *lives* in Anticipate |

Three defects, each independently sufficient to prevent a save.

---

## 2. Scope

**In scope.** The three correctness defects below, the instrument, and an acceptance scenario that
makes their recurrence a visible regression.

**Explicitly out of scope, and recorded in §7 rather than fixed.** The shot-side and defence-side
defects the measurement exposed. They are larger than this pass, each touches a different APPROVED
spec, and each deserves its own note and review cycle. Landing them inside a goalkeeper pass would
make the result unattributable — and the whole value of this pass is that it *attributes*.

---

## 3. Key decisions

### KD-S1 — Fix the direction of the dive at its root, not by filling in `DeflectionTarget`

`ComputeDiveDirectionLateral` had exactly one non-zero branch, gated on
`SaveIntent.DeflectionTarget.HasValue`, and the engine's sole producer sets `DeflectionTarget = null`.
The tempting fix is to make the producer supply one.

**Rejected.** `DeflectionTarget` is where the keeper wants to *put* the ball (§3.5.3, the deflect aim
point); it is not where the keeper should *dive*. The two were conflated. Filling it in to steer a
dive would encode that conflation permanently and give `ComputeDeflectVelocity` a target chosen for
the wrong reason.

**Chosen:** derive the direction from the ball, because that is what a keeper dives at — and
specifically from where the ball **will cross the keeper's plane**, not where it is now. A ball struck
across the face of goal arrives several metres from its current lateral position; diving at its
current position is diving behind it. A linear XY interception, pure and draw-free, bounded by a new
`[GT] DivePredictionHorizonS` beyond which the linear model is not credible. An explicit
`DeflectionTarget` still wins if a future #8 producer sets one.

### KD-S2 — Give `OnShotExecutedEvent` the attributes, rather than letting it read a stale snapshot

`OnShotExecutedEvent` read `_attrs[gkIndex]`, which only `CommitSaveIntent` ever writes. Wired as-is it
would frequently be the **first** call of an episode and would date the reaction window off a keeper
with zeroed Reflexes.

**Chosen:** the caller supplies the projection, exactly as `CommitSaveIntent` does (KD-P4 — runtime
TeamId/Fatigue are the composition root's to supply). The method had no callers at all, in production
or in tests, so the signature change costs nothing.

### KD-S3 — One signed distance to the keeper's own goal, not a per-side constant pair

The orchestrator computed "the third the keeper's own team **attacks**" and handed it to a
state-machine parameter whose own doc reads *"the attacking third from the perspective of the
**opposing** team (i.e. threatening GK's goal)"* — opposite ends of the pitch. The name was the trap:
`ballInAttackingThird` reads one way at the call site and the other way inside the machine.

**Chosen:** §5.Z.12's rule — *"a pair has two places that must agree; a mirror has one"*. One
`ballDistToOwnGoalM`, both predicates derived from it, and both renamed from the **keeper's**
perspective (`ballThreateningOwnGoal`, `ballSafelyUpfield`) so neither can be read from a team's.

### KD-S4 — Give `Anticipate` an exit

Measurement showed keepers holding Anticipate for 76–92% of a match. The cause is not only the
inverted predicate: `Anticipate` had **no exit at all** but a dive or a rush, so a keeper that entered
it never left. Anticipate is a coiled, committed posture; standing in it for eighty minutes is neither
football nor what §3.1 describes. Added `Anticipate → Set` when the threat passes.

### KD-S5 — Assert reachability, not a save percentage

The acceptance scenario asserts that each stage of the pipeline is *reachable*. It deliberately
asserts **no** save percentage and **no** goal rate. The goal rate is dominated by the §7 shot-side
defects this pass did not fix, so a band here would pin a number this pass did not earn and could not
defend against the next change.

---

## 4. The changes

### 4.1 `ERR-011-003` — the dive had no direction

`src/goalkeeper-mechanics/GoalkeeperMechanics.cs`. `ComputeDiveDirectionLateral` now takes the ball
and returns `sign(predictedCrossingY − gkY)`, where the crossing point is the linear XY interception
of the ball against the keeper's x-plane, used only when the ball is closing and the time-to-plane is
inside `DivePredictionHorizonS`. Falls back to the ball's current lateral position otherwise — never
worse than the pre-fix zero.

### 4.2 `ERR-011-004` — the keeper was never told a shot had been struck

`src/match-engine/MatchEngine.cs`: new `NotifyKeeperOfShot`, called per agent per Resolve tick
immediately after that agent's shot executor advances. Fires on `LastResult.Outcome == Completed &&
ContactFrame == frameNumber`, so exactly once per shot, on the frame the ball is actually struck
(§3.2.1 dates perception from the strike, not the windup). Routes to the keeper defending the goal the
**shooter attacks**, derived from the shooter's team rather than from ball direction so a miscued shot
still starts the right keeper's clock.

`src/goalkeeper-mechanics/GoalkeeperMechanics.cs`: `OnShotExecutedEvent` gains the attributes
parameter (KD-S2).

### 4.3 `ERR-011-002` — the keeper woke for the wrong end of the pitch, and never stood down

`src/goalkeeper-mechanics/GoalkeeperMechanics.cs`: one `ballDistToOwnGoalM`, with
`ballThreateningOwnGoal` and `ballSafelyUpfield` derived from it (KD-S3).
`src/goalkeeper-mechanics/GoalkeeperStateMachine.cs`: parameters renamed to match, `Recovering →
Resting` re-anchored to `ballSafelyUpfield` (play is at the far end, so there is nothing to recover
*for*), and the new `Anticipate → Set` exit (KD-S4).

### 4.4 Constants

| Constant | Tag | Value | Why |
|---|---|---|---|
| `DEGENERACY_EPSILON` | `[DERIVED]` | `sqrt(DEGENERACY_EPSILON_SQ)` | scalar single-axis guard; the squared form is the wrong scale for a component test |
| `DivePredictionHorizonS` | `[GT]` | 2.0 s | bounds the linear interception model without gating any realistic shot |

**No `SNAPSHOT_SCHEMA_VERSION` change.** Nothing new is serialized: `_diveDirectionLateral`,
`_shotDetectedTickMs`, `_requiredReactionMs` and `_attrs` are all already in the v19 GK block, and the
state machine's inputs are recomputed each tactical tick from the ball. **No new RNG stream, domain
tag, subsystem ordinal or draw site** — and, load-bearing, **no change to the draw order**: the fixes
alter the *arguments* to existing draws, never how many are taken or in what sequence.

### 4.5 What deliberately does not change

- **The `SaveArmed` trigger geometry.** It arms on a loose ball inside 16.5 m driving at the goal. Its
  narrowness is a real limitation (§7.3) but changing it is a behaviour change to the trigger, not a
  fix to the pipeline, and it should be calibrated against a corrected shot model rather than this one.
- **`GoalkeeperDiveKinematics`.** Still the KD-12 Stage-0 synthetic dive. The measurement shows the
  envelope now reaches the ball, so its magnitude is no longer the binding constraint.
- **Rush and distribution.** Still un-triggered in production (KD-6). Angle-narrowing and coming for
  crosses remain §5.Z.15's recorded remainder, untouched here.

---

## 5. Acceptance

`tests/scenarios/cross-spec/match-engine-goalkeeper-saves` (#19 ScenarioRunner, Tier B, 4 seeds ×
15 minutes, **56 s**). Owning specs {2, 6, 11, 12, 16, 19}.

| Predicate | Pre-fix | Post-fix |
|---|---|---|
| `keeper-is-notified-of-shots` | **0** (no production caller existed) | 19–57 per match |
| `keeper-does-not-live-in-anticipate` ×8 | **76–92%** vs a 40% ceiling | 12–19% |
| `dives-are-launched` | passes | passes |
| `dives-are-directed` | **0** of every dive ever launched | every dive |
| `keeper-makes-hand-contact` | **0** over three FULL matches | > 0 |

**11 of 12 predicates fail on the pre-fix engine**, three of them at exactly zero. This is not
inferred from the earlier funnel runs — it was *executed*: the three production files were reverted to
their pre-fix state (keeping only the read-only observation seam, so the instrument could still see
in), the scenario was re-run, and it reported:

```
predicates_total=12  predicates_failed=11
  keeper-is-notified-of-shots      shotsNotified=0
  dives-are-directed               directedDives=0 of airborneEntries=22
  keeper-makes-hand-contact        contacts=0 over 4 matches
  keeper-does-not-live-in-anticipate ×8   anticipateShare=0.796 … 0.976  (ceiling 0.40)
```

The one that passes pre-fix (`dives-are-launched`, 22 entries) is deliberately retained: it is the
predicate that would catch a regression re-breaking the *entry* to the pipeline rather than its
middle, and it costs nothing.

`dives-are-launched` is asserted over the corpus rather than per seed — measurement showed one of the
four fifteen-minute windows contains no armed threat at either goal, which is legitimate football, not
a defect. This is the opposite call from the discipline scenario's per-seed rule, and deliberately so:
there the risk was one abandoned match averaging away; here it is a small count made noisy by luck.

---

## 6. Verification

Full `tools/dotnet-ci/run-gate.sh`: **PASSED, 0 failures** (SDK 8.0.129 via apt).
match-engine 358 → 360, goalkeeper-mechanics 54 → 55 passed.

Measured effect of the three fixes, same three seeds, full matches:

| | pre-fix | post-fix |
|---|---|---|
| mean \|diveDirectionLateral\| | 0.000 | **1.000** |
| best dive miss (m short) | 2.75 … 6.04 | **−0.71 … 0.38** |
| hand contacts (6 keeper-matches) | **0** | **12** |
| Anticipate share of match | 76–92% | **12–19%** |
| shots notified to the keeper | 0 | 19–57 |
| **goals per match** | 18, 16, 12 (mean 15.3) | 16, 9, 17 (mean **14.0**) |

**The goal rate barely moved, and that is the headline result of this pass.** Three real defects, each
of which had to be fixed for a save to be possible at all, are worth roughly one goal a match against
a target of ~2.7. §5.Z.15's lever was genuine and is now discharged; it was not where the mass is.

---

## 7. Recorded, NOT fixed

The measurement that closed the save question opened a larger one. Each of the following is verified
against source, and each is a bigger contributor to the goal rate than everything in §4.

### 7.1 A shot cannot miss the goal to the side

Every SHOT aims at `u ∈ {0.1, 0.9}` of the goal width, `v = 0.5`, hardcoded
(`OptionGenerator.cs:313-319`, `TacticalWeights.PlacementCornerOffset = 0.1`). `u = 0.1` is **0.732 m
inside the near post**. Placement error is applied as an absolute displacement at the goal plane
(`ShotPlacementResolver.cs:71-73`), so missing the target requires more than **5.73°** of angular
error, against a neutral shooter's ~2.25° — and the largest live multiplier, the pressure penalty, is
hardcoded to zero in the engine's own adapter (`MatchEngine.cs`, `ComputePressureScalar => 0f`).

### 7.2 A shot cannot miss the goal vertically, and there is no crossbar

Two compounding defects. `ShotExecutor.ExecuteContact` builds the kick from the XY components of
`finalDirection` and derives the vertical purely from launch angle — **`finalDirection.z` is never
read** (`ShotExecutor.cs:465-478`), so `PlacementTarget.v` and the entire vertical half of the error
model influence nothing. And `BallCollision.CheckBoundaries` gates **every** boundary test, goals
included, behind `lowEnough = z < Ball.Diameter` (0.22 m) (`BallCollision.cs:62`): a ball crossing the
line airborne is adjudicated as neither a goal nor out of play, flies on, and is judged when it later
descends. **The goal is effectively 7.32 m wide and of unbounded height.**

### 7.3 Nothing physical stands between the shooter and the goal

`BallCollisionHandler.OnAgentCollision` **is called in production** and its body is an empty `TODO`
(`BallCollisionHandler.cs:23-30`). No agent — defender or keeper — deflects the ball by contact.
`ApplyGoalPostCollision` has no production caller, so posts and crossbar are non-physical. **There are
no blocked shots.** In football roughly 30% of shots are blocked and 30% miss the target; here both
are approximately zero, which on its own is a multiplier on the goal rate far larger than anything a
keeper does.

### 7.4 The goal-visibility gate is vacuous

`ComputeGoalOpeningScore` clamps to `[GOAL_OPENING_MIN, 1.0]` and the SHOOT gate rejects below
`MIN_GOAL_VISIBILITY`; both constants are **0.05** (`UtilityWeights.cs:106,144`). The gate can only
fire on the degenerate zero-arc early return, so a shot with the goal completely walled off is
generated, scored and taken.

### 7.5 The reaction window is fed a signal it cannot use

`ERR-011-004` wired the shot event and it fires (19–57 notifications a match), but
`reactionWindowAchieved` still measures 0 at contact. It is not unwired — it is **incoherent**: the
§3.2 pipeline is designed around "a shot is struck, the keeper reacts to *that* shot", while the
engine's save trigger is "a loose ball is driving at my goal", which includes deflections, rebounds
and passes. `_shotDetectedTickMs` is also never cleared, so a stale shot dates every later dive; mean
elapsed-when-airborne measured ~2000 s. Dating the window from the moment the *episode* armed would
make it coherent, but that changes #11 §3.2 semantics and belongs in its own pass.

The consequence is the §5.Z.15 lever's remaining half, still open: with the window pinned at 0, quality
is capped at `0.70 × rawHandling`, whose **measured ceiling is 0.630 for a perfect keeper** (Handling
20, zero noise, exact contact point) against `CatchThreshold` 0.78. **A catch is still arithmetically
impossible**; the best available band is Parried, and only for Handling ≥ 16. Contacts measured
0.150–0.585 quality, i.e. mostly Spilled/Missed.

### 7.6 Vestigial release-cooldown state

`_gkReleasedAgentId` / `_gkReleaseCooldownRemaining` are written, serialized at v19 and exposed via a
seam, but never read as an exclusion — `SelectLooseBallCollector` excludes the keeper unconditionally,
superseding the cooldown that `GkReleaseCooldownTicks`'s own doc still describes. Live documentation
drift on a surface any future keeper change will read.

---

## 8. Consequence for A4a

**A4a stays blocked, and the reason is now specific rather than general.** The roadmap records the
blocker as "the engine's goal rate (~4.7× football's)". This pass discharges the lever that entry
names and shows it moves the rate by ~1 goal a match. The residual is §7.1–§7.4: **shots that cannot
miss, at a goal with no crossbar, past defenders who cannot block.** Until those land, a fitted
round-resolution corpus would calibrate the quick-sim to reproduce them faithfully across a
380-fixture league — the exact failure `round-resolution-corpus.md` was written to prevent.

The honest next lever is **not** the goalkeeper. It is the shot-outcome distribution.

---

## 9. Adversarial review history

| Round | Findings | Notes |
|---|---|---|
| Measurement-1 | — | Funnel over 3 full matches: contacts 0, the collapse point localised |
| Measurement-2 | — | Miss-distance probe: `diveDir` 0.000, best miss 2.75–6.04 m ⇒ direction, not reach |
| Measurement-3 | — | Ceiling test: max quality 0.630 vs `CatchThreshold` 0.78 ⇒ catch impossible |
| Self-review-1 | 1 | `ComputeDiveDirectionLateral`'s first draft used the ball's current position; corrected to the predicted crossing point (a ball across the face of goal arrives elsewhere) |
| Self-review-2 | 1 | `OnShotExecutedEvent` would have read a default attribute snapshot on the first call of an episode; caller now supplies it (KD-S2) |
| Self-review-3 | 1 | `every-seed-produces-a-dive` was too strict — one quiet 15-min window legitimately has no armed threat; re-expressed over the corpus with the reasoning recorded |
| Test-fallout | 1 | `sim_goalkeeper_save_launch_executes_dive` had encoded the inverted predicate (ball at x = 75 to wake GK0); re-anchored to x = 30 with intent preserved — the Phase-H "tests encoded the old contract" class |

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-27 | — | Initial. Measures the §5.Z.15 goalkeeper lever and finds saves did not happen at all (0 contacts over 3 full matches). Fixes three correctness defects — ERR-011-003 the undirected dive, ERR-011-004 the unwired shot event, ERR-011-002 the inverted wake predicate plus the missing Anticipate exit. Contacts 0 → 12, dive direction 0.000 → 1.000, best miss 2.75 m → −0.71 m. Records that the goal rate moved only 15.3 → 14.0, and that the residual is the shot-side model (§7): shots that cannot miss, no crossbar, no blocks. |
