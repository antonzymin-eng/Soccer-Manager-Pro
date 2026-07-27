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
`src/goalkeeper-mechanics/GoalkeeperStateMachine.cs`: parameters renamed to match, and the new
`Anticipate → Set` exit (KD-S4).

`Recovering → Resting` is re-anchored to `ballSafelyUpfield`, and that is a **change of region, not a
rename** — recorded explicitly because the old parameter was not simply mis-computed. `ballInDefensiveThird`
evaluated the keeper's *own* defensive third, which is right for its name; the defect was that it sent
the keeper to full stand-down while the ball was in its own box, the same wrongness ERR-011-002 names
on the entry side. `Resting` is the stand-down state, and what licenses it is the ball being at the
*other* end. The transition stays reachable under either reading — `Recovering → Set` is gated on the
recovery cooldown and baseline distance, not on the ball — so this is a behaviour choice, made on
football grounds, not a forced consequence of the fix.

### 4.4 Constants

| Constant | Tag | Value | Why |
|---|---|---|---|
| `DegeneracyEpsilon` | `[DERIVED]` | `sqrt(DEGENERACY_EPSILON_SQ)` | scalar single-axis guard; the squared form is the wrong scale for a component test |
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
| `keeper-is-notified-of-shots` | **0** (no production caller existed) | 16–51 per match |
| `keeper-does-not-live-in-anticipate` ×8 | **76–92%** vs a 40% ceiling | 10.9–17.6% |
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
match-engine 358 → 360, goalkeeper-mechanics unchanged at 55 passed.

Measured effect of the three fixes, same three seeds, full matches:

| | pre-fix | post-fix |
|---|---|---|
| mean \|diveDirectionLateral\| | 0.000 | **1.000** (every keeper, every match) |
| best dive miss (m short) | 2.75 … 6.04 | **−0.07 … 0.09** |
| hand contacts (6 keeper-matches) | **0** | **15** |
| catches | 0 | **0** — see §7.5 |
| Anticipate share of match | 76–92% | **10.9–17.6%** |
| shots notified to the keeper | 0 | 16–51 per match |
| **goals per match** | 18, 16, 12 (mean 15.3) | 18, 12, 16 (mean **15.3**) |

**The goal rate did not move.** That is the result, and it is stated plainly because the alternative
is to quote a delta the sample cannot support: three matches of a chaotic quantity is a small sample,
and an earlier build of this pass measured 14.0 on the same seeds purely because one different
deflection re-rolls everything downstream. The defensible claim is **no detectable effect on the goal
rate**, against football's ~2.7 — not "worth about a goal a match", which is what an n=3 delta would
have let this note say if it had stopped at the first number it liked.

That makes the §5.Z.15 lever's disposal sharper, not weaker. Three defects that each independently
made a save impossible are now fixed, the keeper dives correctly and its hands reach the ball fifteen
times where they previously never did — **and the scoreline is unchanged**, because a keeper that
touches the ball fifteen times cannot offset the shot-side defects in §7.1–§7.4. The mass is there.

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

`ERR-011-004` wired the shot event and it fires (16–51 notifications a match), and the window is no
longer *arithmetically* pinned at zero — measured, it reads 0.315 and 0.079 for two of six keepers and
0.000 for the other four. But it is still **incoherent**, and the reason is structural rather than a
wiring gap: the §3.2 pipeline is designed around "a shot is struck, the keeper reacts to *that* shot",
while the engine's save trigger is "a loose ball is driving at my goal", which includes deflections,
rebounds and passes. `_shotDetectedTickMs` is also never cleared, so a stale shot dates every later
dive — mean elapsed-when-airborne measures **34–174 s**, i.e. most dives are timed against a shot from
minutes ago. Dating the window from the moment the *episode* armed would make it coherent, but that
changes #11 §3.2 semantics and belongs in its own pass.

The consequence is the §5.Z.15 lever's remaining half, still open. A catch is no longer arithmetically
impossible (at a window of 0.315 a perfect keeper reaches 0.795 against `CatchThreshold` 0.78), but
**no catch occurred in any of the six keeper-matches**: measured quality at contact is 0.357–0.567,
below `CatchThreshold` 0.78 everywhere and below even `ParryThreshold` 0.55 in four of the five
keepers that made contact. The realised outcomes are Spilled and Missed.

Recorded as a caution for whoever takes this up: the window's units were wrong in this pass's own
first landing — the shot was stamped in seconds against a pipeline that compares milliseconds, which
reproduced a permanently-zero window while *looking* fixed. It was caught in adversarial review, not
by any test, because nothing asserts the window's magnitude and the funnel's contact count rose either
way. A pipeline whose only observable is a downstream count can hide an inert stage indefinitely.

### 7.6 Vestigial release-cooldown state

`_gkReleasedAgentId` / `_gkReleaseCooldownRemaining` are written, serialized at v19 and exposed via a
seam, but never read as an exclusion — `SelectLooseBallCollector` excludes the keeper unconditionally,
superseding the cooldown that `GkReleaseCooldownTicks`'s own doc still describes. Live documentation
drift on a surface any future keeper change will read.

### 7.7 The acceptance scenario does not cover the `ConfigureSquads` path

`match-engine-goalkeeper-saves` runs the neutral path; the §1.2 measurements came from the
`ConfigureSquads` path, because that is the path a league match takes. This is sound for the three
defects fixed here — each was geometric or arithmetic and none was a function of an attribute value,
so the neutral path exercises them identically, and the §5 pre-fix evidence was executed against the
scenario itself. What it leaves uncovered is **`LineupSelector` choosing the goalkeeper**: a
regression that seeded the GK slot from the wrong record would satisfy every predicate here. Closing
it means either lifting `GkSaveDiagnosticTests.BuildSquad` to a shared fixture or duplicating a
position-coherent roster builder — the second is the parallel-surface trap, so the first is the route.
Deliberately not done in this pass: adding a fifth, configured seed would change the corpus the §5
pre-fix failure count was executed against, and re-earning that evidence is not free.

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
| AR-5 (hostile, whole surface) | 1L | **L:** `DEGENERACY_EPSILON` was declared ALL_CAPS while tagged `[DERIVED]` — ALL_CAPS is the `[FIXED]`-only convention (FR-CS-001), and every sibling in the `Derived` region is PascalCase; renamed `DegeneracyEpsilon` at its two use sites. Nothing else new: the pass re-walked the production diff, the instrument, the scenario, the six version histories, the meta integrity and the doc numbers, and found no High or Medium — **the review loop terminates here** |
| AR-4 (hostile, whole surface) | 1M | **M:** the ERR-011-002 fix keys the keeper's own goal on `gkIndex == teamId`, but `MaxGkAgents` is a **`[GT]` read off `GameplayConfig`** while `TEAM_COUNT` is a `[FIXED]` const — nothing structural keeps them equal, and nothing anywhere asserted it. A config file alone could therefore reintroduce the wrong-end defect this pass exists to remove, silently. Gated at `MatchEngine` boot (the composition root that depends on it) plus a coupling lock, following the league-bootstrap `MaxRngStreams` precedent. Worth noting the fix *created* this exposure: the pre-fix code read `attrs.TeamId`, which was broken for a different reason and not index-coupled |
| AR-3 (hostile, whole surface) | 1M + 1L | **M:** every one of the six production/test files this pass touched carried a stale `Modified:` header and **no version-history row** — FR-CS-056 and the project's own "append a version history entry to every modified file" rule, violated uniformly. The same class the #30 T1 review filed as its pass-3 finding. **L:** the diagnostic's funnel legend read `diving: entries into Diving (the dive was launched)` while that column reads **0 by construction** (Diving is entered and left inside one 60 Hz step, so no sample lands on it) — an instrument whose own legend invites the reader to conclude no dive was launched |
| AR-2 (hostile, whole surface) | 1H + 2M | **H:** `NotifyKeeperOfShot` stamped the shot in **seconds** (`_clock.CurrentMatchTimeSeconds`) against a §3.2 pipeline that is entirely milliseconds and compares it to `_clock.CurrentMatchTimeMs` — so `elapsed` ran ~1000× large, the §3.2.3 late branch clamped to 0, and `reactionWindowAchieved` was **still permanently zero**: the ERR-011-004 fix looked landed and was inert. No test could have caught it (nothing asserts the window's magnitude, and the contact count rises either way). Fixing it changed the measured outcome enough to force §1.2/§5/§6/§7.5 to be re-measured and rewritten — including the headline, which moved from "worth about a goal a match" to **no detectable effect on the goal rate**. **M:** `Recovering → Resting` was re-anchored from the keeper's own defensive third to the far third — a deliberate change of region, not the rename §4.3 described it as; now recorded as a behaviour choice with its reasoning. **M:** `DivePredictionHorizonS`'s doc claimed it was "sized just above" a 5.5 s flight time while holding 2.0 s — a sizing rule contradicting its own value, which would have led a tuner to raise it |
| AR-1 (hostile, whole surface) | 1H + 1M + 2L | **H:** the KD-S3 fix read the keeper's own goal from `attrs.TeamId`, but `_attrs` is only written by `CommitSaveIntent` / `OnShotExecutedEvent`, so before a keeper's first save episode BOTH keepers carried `TeamId = 0` and keeper 1 never woke for its own box — **verified by executing a probe** (ball at x = 100 ⇒ `gk0 Resting, gk1 Resting`), i.e. the fix reintroduced the exact per-side defect KD-S3 exists to remove. Now derived from `gkIndex` per #11 KD-1. **M:** the engine's `_saveCommittedForGk` latch and the orchestrator's `_saveIntentActive` could disagree on disarm; new `ClearSaveIntent` (no-op mid-dive) called from the disarm branch. **L:** `DEGENERACY_EPSILON` filed under `#region Fixed` though `[DERIVED]`; the acceptance scenario's neutral-vs-`ConfigureSquads` path gap (§7.7). Also repaired collateral damage from the ERR-renumber: a bulk rename had rewritten a pre-existing `ERR-011-001` citation in `GoalkeeperConstants.cs` |

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-27 | — | Initial. Measures the §5.Z.15 goalkeeper lever and finds saves did not happen at all (0 contacts over 3 full matches). Fixes three correctness defects — ERR-011-003 the undirected dive, ERR-011-004 the unwired shot event, ERR-011-002 the inverted wake predicate plus the missing Anticipate exit. Contacts 0 → 15, dive direction 0.000 → 1.000, best miss 2.75 m → −0.07 m. Records that the goal rate did NOT move (15.3 → 15.3), and that the residual is the shot-side model (§7): shots that cannot miss, no crossbar, no blocks. |
| 1.1 | 2026-07-27 | — | Adversarial-review rounds AR-1..AR-5 (§9) applied. The consequential ones: AR-1 H (the KD-S3 fix read `attrs.TeamId`, default 0 for both keepers — now `gkIndex`), AR-2 H (the shot stamped in seconds against a millisecond pipeline, leaving the reaction window still pinned at 0; fixing it re-rolled the corpus and retired the 14.0 goals/match figure as n=3 noise — §1.2/§5/§6/§7.5 re-measured and rewritten, headline now "no detectable effect on the goal rate"), AR-4 M (boot gate + coupling lock on `MaxGkAgents == TEAM_COUNT`, the identity the fix made load-bearing). Plus §7.7 (the acceptance scenario's neutral-path scope note), the six FR-CS-056 version-history repairs, and the `DegeneracyEpsilon` rename. |
