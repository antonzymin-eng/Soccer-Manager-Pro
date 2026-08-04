# The Keeper Rush Trigger — wiring backlog W1

> **Created:** August 4, 2026
> **Status:** DESIGN SUPPLEMENT — the same governance class as `match-engine-design.md`. Opens no
> numbered spec and changes no `SPEC_INDEX.md` row. Files one cross-spec back-prop against
> Goalkeeper Mechanics **#11** (`ERR-011-009`) — id verified free against `spec-error-log.md`
> (last #11 entry was `ERR-011-008`) and against `docs/specs/` before assignment.
> **Owner document:** `docs/tracking/match-engine-wiring-backlog.md` **W1** (this is that item).
> **Purpose:** `GoalkeeperMechanics.CommitRushIntent` has had zero production callers since it was
> written. Everything downstream of it is built, tested and reachable; only the trigger condition
> was missing, so every one-on-one in this engine has been a stationary keeper on his line waiting
> to dive. This note is the trigger, the spec defect the wiring surfaced, and the measurement that
> has **not yet been run**.

---

## 0. This is a wiring task, not a realism pass

The backlog's own gate (`match-engine-wiring-backlog.md` §0, and the gate at the top of the
`match-realism-pass` skill) says it plainly: *is the subsystem this touches fully wired? If not,
this is a wiring task, not a realism pass.* W1 is the canonical case. The consequence for this
note's shape:

- **No `[GT]` governing an existing subsystem is retuned.** KD-W1 holds. The five constants added
  below are *new dials for a previously dead surface* — there was no prior value to freeze — and
  every one of them is explicitly un-calibrated, waiting on the single calibration pass the backlog
  books after the wiring is complete.
- **The deliverable is a live trigger plus whatever the wiring surfaces.** It surfaced one spec
  defect, filed as `ERR-011-009` (§3).

---

## 1. What exists, and the one thing that did not

Verified in source, August 4, 2026:

| Surface | State |
|---|---|
| `GoalkeeperRushDispatch.ComputeRushLaunchMps` / `UpdateRushFrame` | Built. Advances the keeper toward the locked target and writes the position back into the AM #2 array. |
| `Rushing → OneOnOne → Smothered` transitions, 1v1 + smother radii | Built (`GoalkeeperStateMachine.EvaluatePhysicsTransition`). |
| F-08 `BallIntercepted` abort, `AbortReason`, `GoalkeeperRushEvent`, `RecordRushPhase` telemetry | Built. |
| `RushIntent` serialization (v18 GK block) | Built. |
| `GoalkeeperMechanics.CommitRushIntent` | **Zero production callers.** |

So the whole subsystem hangs off one uncalled method. The cost of that is not subtle: with the
keeper pinned to his line, an attacker through on goal faces a static target, the shooting angle is
never narrowed, and the §5.Z.23 conversion work was fitted against exactly that geometry — which is
why the `pointQuality` owner decision is **parked** until this lands.

---

## 2. The trigger

### 2.1 Where it lives

The save trigger's shape is the precedent and this follows it, with one deliberate divergence.

`SaveArmed` is a pure predicate in `GkHeadingIntentSource`; the engine sets
`TacticalContext.SaveAvailable` and the **Decision Tree** emits `ActionType.SAVE`, dispatched
through `IDtSaveDispatch`. **The rush cannot take that route.** `ActionType.SAVE = 7` is the last
ordinal that fits the 3-bit composure-noise field in `ActionSelector.ComputeOptionNoise`; an eighth
action overflows it and forces a composure-noise digest rebaseline. That is precisely why the
DT-emitted HEADER (backlog W9) is deferred, and paying that cost here would make W1 — the cheapest
large realism lever available — the most expensive item on the board.

So the rush is committed the way the **header** trigger already is: a pure predicate plus a
composition-root commit, which is the `MatchFlowCollisionConsumer` heuristic-foul precedent the
GK/Heading supplement §4.3 already accepted. When W9 takes the rebaseline, `RUSH` folds into the
same DT surface as `SAVE` and this call site becomes the fallback, not a parallel authority.

- `GkHeadingIntentSource.RushArmed(...)` — pure geometry, unit-testable without a booted engine.
- `MatchEngine.TryCommitRushIntents()` — called from `DriveGkHeadingTactical` **before**
  `_goalkeeper.TacticalTick`, so an intent committed this stride is seen by the same tick's
  `Anticipate → Rushing` evaluation. (The header trigger is fired *after* the tactical tick because
  it is consumed at 60 Hz; the rush is a 10 Hz state-machine input, so it must precede.)

### 2.2 The predicate

```
RushArmed(keeperTeam, gkPos, ballPos, ballVel, ballLoose, ballHeldByKeeperTeam,
          nearestOutfieldDefenderDistM, rushSpeedMps) -> bool, out rushTarget

1. our own player has the ball                      -> no rush
2. ball above GkRushMaxBallHeightM                   -> no rush   (a claim, not a rush; W3)
3. a team-mate is nearer the ball than the keeper    -> no rush   (the last-man test)
4. ball loose:
       solve the intercept race at rushSpeedMps;
       no solution                                   -> no rush
       rushTarget = the meeting point
   ball held by an opponent:
       rushTarget = the ball
5. |rushTarget - gkPos| > rushSpeedMps * GkRushMaxInterceptS
                                                     -> no rush   (one time budget, both branches)
6. |rushTarget.x - ownGoalX| > GkRushTriggerRangeM    -> no rush
otherwise -> rush
```

**Condition 3 is the whole football judgement, and it covers both cases a keeper actually comes for.**
A through-ball into the space behind the defence is one the keeper takes only when he is the nearest
player to it; an attacker running clean through has the ball (distance ≈ 0) but is *unattended*, so
his chasing defender being nearer the ball than the keeper is exactly the signal that the keeper
should stay. One condition, no case analysis, and it fails safe: with defenders anywhere near the
ball the keeper does not move.

**Condition 5 is one rule, not two.** For the loose ball it is exactly the intercept cap — the solve
places the meeting point at `rushSpeed × t` by construction, so a distance budget and a time budget
are the same statement — and for a carrier it is that same budget expressed the only way it can be,
there being no race to solve. Stated once, applied to both branches: without it the possessed branch
would let a keeper on his line commit to a 22 m sprint with no time bound at all.

**Condition 4's intercept solve replaces a lead-time constant.** With the target locked at commit
(KD-15 / FR-GK-018), aiming at the ball's *current* position sends the keeper to where the ball was
a second ago. The quadratic `|b + vt − g|² = (s·t)²` gives the meeting point directly, and it is
self-guarding: a ball moving away faster than the keeper can run has no positive root, so a
clearance never drags the keeper out of his goal. That is one fewer `[GT]` and one fewer thing to
calibrate later.

### 2.3 Priority against the save

A rush is skipped whenever `SaveArmed` holds for the same keeper. Without this the two triggers
compete for the same ball: a driven shot arms both, and because `Anticipate → Rushing` is evaluated
when the `ERR-011-007` commit-lead gate has not yet released the dive, the keeper would charge out
instead of diving — a straight regression of the §5.Z.17–§5.Z.22 save pipeline. The rule is stated
at the call site so it is legible: **a ball driving at the goal is a save, not a rush.**

Consequence, recorded and not fixed: a hard through-ball into the box also satisfies `SaveArmed`
(loose, closing, ≥ 3 m/s, within 16.5 m), so it is classified a save. That is the save trigger being
greedy rather than the rush trigger being wrong, and diving on it is not absurd. It is a candidate
for the calibration pass, not for this one.

### 2.4 The latch, and why there is no new engine state

`_rushIntentActive` inside `GoalkeeperMechanics` is already the per-episode latch — set at commit,
cleared when the rush chain resolves — and it is **already serialized** in the v18 GK block. Two new
observation accessors (`GetState`, `HasActiveRushIntent`) let the engine read it instead of keeping a
private copy. **No new cross-tick state, so no `SNAPSHOT_SCHEMA_VERSION` bump.** The save trigger's
`_saveCommittedForGk[]` is the shape this deliberately avoids: a second latch with a different
lifetime from #11's own is what produced `ERR-011-002`'s dive-at-nothing.

The symmetric disarm is `ClearRushIntent`, the exact mirror of `ClearSaveIntent`: the engine calls it
on every stride the geometry is not armed, and it is a **no-op while the keeper is in the rush chain**
(`Rushing` / `OneOnOne` / `Smothered`), because FR-GK-018 makes a committed rush non-abortable on
anything but F-08. One owner decides an episode is over; this is how it says so.

Commit is additionally gated on the keeper being in `Set` or `Anticipate` — the only two states with a
`→ Rushing` row. Committing from `Recovering` would leave the intent sitting armed across the cooldown
and fire it two ticks later against a target already stale.

---

## 3. What the wiring surfaced — `ERR-011-009`

**A rush that reaches its target has no exit.** #11 §3.1.1's transition table gives `Rushing` exactly
three exits — `Smothered` (contact), `OneOnOne` (attacker inside the 1v1 radius), `Recovering` (F-08
interception) — and gives `OneOnOne` two: `Diving` (SaveIntent) and `Smothered` (smother radius). A
keeper who runs to a loose ball and arrives satisfies none of them:

- `CheckAttackerWithinRadius` returns false outright when the ball is unpossessed, so for a loose ball
  the 1v1 and smother triggers **cannot** fire;
- the F-08 abort needs a possessor;
- `UpdateRushFrame` stops dead at the locked target and does not overshoot.

The keeper therefore stands on the ball, in `Rushing`, for the remainder of the match. The spec's own
`RushPhase` enum has always carried a `Reached` member and §3.7.3 reserves `AbortReason.AttackerBeatGK`
for the attacker-passes-the-keeper case — the completion was anticipated everywhere except in the one
table that adjudicates state. This is a spec defect, not merely an implementation gap, and it would
have shipped as an unreachable-code stall the moment the trigger went live.

**Fix (spec and code, same commit):** two new §3.1.1 rows, `Rushing → Recovering` and
`OneOnOne → Recovering`, both triggered by the keeper coming within `RUSH_TARGET_REACHED_RADIUS_M` of
the locked target without contact, emitting `GoalkeeperRushEvent { rushPhase: Reached }`. It is a
completion, not an abort, so FR-GK-018 / KD-15 are untouched: nothing about the ball's trajectory ends
the rush, only the keeper finishing the run he committed to.

`AbortReason.AttackerBeatGK` stays unreachable and is recorded as such. Under the new rows a keeper
whom the attacker has beaten terminates on `Reached` instead, which is the right *state* with the
wrong *label*. Labelling it correctly needs the attacker's position inside `Update`; the initial
attacker id is already stored (`_rushInitialAttackerId`), so it is cheap — but it is a telemetry
refinement, and folding it in here would make the state-machine change harder to review than the
defect it fixes.

---

## 4. Constants (all new; all un-calibrated)

| Constant | Catalogue | Default | What it means |
|---|---|---|---|
| `GkRushTriggerRangeM` | `MatchEngineConstants` | 22.0 m | Max distance from the defended goal line at which a rush may be committed. Roughly the penalty area plus a stride — the region a keeper actually sweeps. |
| `GkRushMaxInterceptS` | `MatchEngineConstants` | 2.0 s | The longest run the keeper will commit to — one budget, both branches (§2.2 condition 5). Also bounds the straight-line ball extrapolation the solve relies on. |
| `GkRushMaxBallHeightM` | `MatchEngineConstants` | 2.5 m | Above this the ball is a cross to be claimed (backlog W3), not a ball to be swept. |
| `GkRushCommitment` | `MatchEngineConstants` | 0.85 | The `RushIntent.CommitmentLevel` the Stage-0 trigger writes. Must exceed `RushCommitThreshold` (0.60) or the state machine ignores it. |
| `RushTargetReachedRadiusM` | `GoalkeeperConstants` | 0.5 m | §3.1.1's new `Reached` rows. |

Every value is a first plausible number, not a fitted one. They are `[GT]` and config-overridable, and
they are the calibration pass's input, not its output.

---

## 5. Test plan

| Level | Test | What it pins |
|---|---|---|
| Pure | `GkRushTriggerTests` | Each predicate arm, **mirrored home and away** (the #8 `ERR-008-002` house rule — three home/away asymmetry defects shipped because every fixture used the home team). The intercept solve: reachable ball, unreachable ball moving away, ball already at the keeper. |
| Unit | `GoalkeeperMechanicsTests` (`StateMachinePhysicsTransitionTests`) | The two new `Reached` rows, and that `Reached` loses to contact / interception / the 1v1 trigger. |
| Unit | `GoalkeeperMechanicsTests` | `ClearRushIntent` disarms an uncommitted intent and is inert mid-chain. |
| Engine | `GkRushTriggerTests` (composed arm) | The whole chain through a real `MatchEngine`: an uncovered loose ball in the box commits exactly one rush and the keeper reaches `Rushing`; the same ball with a defender nearer it commits nothing. Both keepers. Positions are forced via the `TestOnly_SetAgent` / `TestOnly_ForceBallLoose` seams so the assertion does not depend on a formation developing the geometry. |
| Instrument | `GkRushDiagnosticTests` (env-gated `TD_GK_DIAGNOSTIC=1`) | Rushes committed, launched, and how each terminated; keeper displacement from the goal line. Asserts nothing — the ERR-030-014 convention. |

---

## 6. Measurement — **NOT YET RUN**

This is a deviation from the discipline every §5.Z pass in this chain has followed, and it is recorded
here rather than papered over.

**The .NET SDK is not installed in the session environment that authored this landing, and the agent
proxy denies `builds.dotnet.microsoft.com`, so it could not be installed.** Consequently:

- `tools/dotnet-ci/run-gate.sh` was **not** run locally. The gate result for this landing is whatever
  the GitHub `dotnet-compile-test` job reports; until that job has run, no compile or test claim in
  this note has been executed. Nothing here may be cited as "the suite enforces X" before then — that
  is the never-compiled-surfaces trap in this project's own hazard table.
- The instrument in §5 is **written and unrun**. There are no pre/post numbers, and none are invented.

To run the measurement on a host with the SDK:

```bash
bash tools/dotnet-ci/run-gate.sh
TD_GK_DIAGNOSTIC=1 dotnet test -c Release --filter GkRushDiagnostic
```

Expected shape of the finding if the wiring is correct: a non-zero rush count where the pre-fix engine
records exactly zero, keeper X displacement from the goal line becoming non-zero on those episodes,
and — the number this is ultimately for — a change in close-range conversion. Whether that change is
in the right direction is a **question**, not a prediction: the keeper now leaves his goal, which
creates chances as well as ending them, and the §5.Z.23 `pointQuality` decision is parked precisely
because this geometry is what it turns on.

---

## 7. Recorded, NOT fixed

1. **`AbortReason.AttackerBeatGK` remains unreachable** (§3).
2. **`RushPhase.Launched` is never emitted.** Announcing the launch needs an edge detection the
   orchestrator does not currently carry; `Reached` and `Aborted` cover the outcomes the instrument
   reads.
3. **A keeper who sweeps a loose ball cannot pick it up.** §5.Z.15/16 bars the keeper from being the
   designated loose-ball collector — correctly, since that loop held the ball for a third of a half —
   but the exclusion is by role, so a keeper who has *run twenty metres to the ball* is barred too.
   The ball stays live and the opponent's collector comes for it, so there is no stall; it is a
   fidelity gap, and it belongs with backlog W6 (`BallStateType.Controlled` has no producer).
4. **The movement system and the rush both write the keeper's position** on a rushing tick — the
   ordinary `MOVE_TO_POSITION` toward the #12 slot integrates first, `UpdateRushFrame` overwrites it.
   Position is what every consumer reads, so the rush wins; the keeper's `AgentState` velocity is
   nonetheless the movement system's, not the rush's. Harmless today, wrong the moment anything reads
   keeper velocity.
5. **`UpdateBaselineSlot` is fed the keeper's own current position**, not the #12 GK slot, so
   `Recovering → Set`'s at-baseline test is trivially true and the §3.3.0 positioning contract is
   degenerate. Pre-existing, untouched here, and worth its own item.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-08-04 | — | Initial. Wiring backlog W1: the keeper rush trigger. Pure `RushArmed` predicate (last-man test + intercept race) + `TryCommitRushIntents` composition-root commit, deliberately NOT routed through the DT (`ActionType` ordinal 8 overflows the 3-bit composure-noise field — the W9 deferral reason). No new engine state: #11's own serialized `_rushIntentActive` is the latch, read through two new accessors. Files `ERR-011-009` — #11 §3.1.1 gives a reached rush no exit, so a swept loose ball stranded the keeper in `Rushing` for the rest of the match. Five new un-calibrated `[GT]`s; KD-W1 holds. **Measurement not run — no .NET SDK in the authoring environment.** |
