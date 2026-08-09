# Match Engine — Wiring Backlog

> **Created:** August 4, 2026
> **Status:** AUDIT — a finding list, not a design. No spec is opened here and no `[GT]` is proposed.
> **Owning doc:** `match-engine-design.md` (the composition root this audit measures).
> **Purpose:** Enumerate every subsystem surface that is built, tested, and reachable from the
> match engine's assembly graph but has **no production caller** — the code that exists and never
> runs. Produced because four such surfaces were found by accident while answering questions about
> goalkeeper behaviour, and nobody had ever gone looking.

---

## 0. Why this document exists, and the rule it establishes

Seven consecutive `§5.Z` match-realism passes fitted `[GT]` constants against the composed engine.
Every one of those fits was made against a machine with dormant subsystems in it. That is not
merely a calibration inefficiency — it is a **diagnostic** hazard:

> The measured shot conversion is ~18% against football's ~11%. That reads as "the shot model is
> too generous." At least part of it is "no keeper has ever narrowed an angle, and no defender has
> ever made a tackle." A realism pass aimed at the shot model would have chased the wrong lever and
> left behind a `[GT]` that later has to be un-tuned.

**KD-W1 — the `[GT]` freeze.** Do not land a `[GT]` change governing a subsystem that is not fully
wired. Defect fixes, instruments, and measurement are unaffected and should continue freely;
constants wait for the calibration pass that follows this backlog.

**KD-W2 — scope.** This audit covers the match engine and the assemblies it composes only. The 22
approved specs with no `src/` assembly are a different problem, tracked in
`path-to-playable-roadmap.md`, and are explicitly out of scope here.

---

## 1. Method

Three passes, each of which found things the others missed:

1. **Comment sweep** — grep for self-declared deferrals (`intentionally not called`, `zero
   production call sites`, `Stage 1 deliverable`, `not plumbed`). High precision, low recall: it
   only finds gaps someone knew about and wrote down.
2. **Call-graph sweep** — for every `public` method on every type in the 18 assemblies the engine
   references, count production (non-test) callers across the whole tree. Zero callers ⇒ candidate.
   This is the pass that found the tackle gap, which no comment records.
3. **Manual triage** — every candidate read in source to separate a genuine dormant capability from
   an internal helper, a redundant setter, or a test seam.

Scripts: `scratchpad/audit*.py` (not committed — the finding list below is the deliverable).

### 1.1 What this method CANNOT see

The sweep detects **method-level** dormancy: nothing calls X. It is blind to the more common and
more expensive failure — **gate-level** dormancy, where the call site exists and executes but its
condition is almost never true. Those surfaces look perfectly wired to a call-graph scan.

At least one is already known and measured (§3 below, C1: #12 commits `InPoss` on **9.5%** of
final-third samples, so every phase-gated mechanism in #13/#14/#15 is starved). That was found by
runtime instrumentation during §5.Z.24, not by any static analysis, and there is no reason to
believe it is the only one.

**Therefore this backlog is a floor, not a ceiling.** A second detection pass — an env-gated
instrument counting how often each phase gate and trigger condition actually fires over a match —
should run before the calibration pass, and belongs on this board as item **W12**.

---

## 2. Class A — dormant capability (no production caller)

Ordered by measured or expected impact on match realism. "Evidence" cites the declaration site; in
every case the whole-tree production caller count is zero.

### W1 — The goalkeeper never comes off his line — ✅ **WIRED August 4, 2026**
**Evidence (as filed):** `goalkeeper-mechanics/GoalkeeperMechanics.cs:281` `CommitRushIntent`.
The engine called `CommitSaveIntent` and only `CommitSaveIntent`.

**Landed:** `MatchEngine.TryCommitRushIntents` (fired from `DriveGkHeadingTactical`, *before* the
tactical tick — the rush is a 10 Hz state-machine input, unlike the 60 Hz-consumed header) over a
new pure `GkHeadingIntentSource.RushArmed`. **The keeper comes out to reduce the shooting angle**,
and the predicate is built from that sentence: the only thing that keeps him home is a team-mate
already **goal-side** of the ball inside the shot corridor — a defender merely *chasing* the carrier
narrows nothing and does not stop him — while **how far** he comes out is #11 §3.7.0's
attribute-driven `ComputeRushCommitDistanceM` (`OneVsOne` / `Composure` / fatigue), not an engine
range. For a loose ball the locked target is an **intercept race solve**, not the ball's current
position, because KD-15 locks the target at commit. Skipped whenever `SaveArmed` holds for the same
keeper: a ball driving at the goal is a save, not a rush, and without that exclusion a shot would
send the keeper charging out while the ERR-011-007 lead gate still held the dive. Deliberately
**not** a Decision Tree action — `ActionType.SAVE = 7` is the last ordinal that fits the 3-bit
composure-noise field, so an eighth would force the same digest rebaseline that defers W9. **No new
engine state** (#11's own serialized `_rushIntentActive` is the latch, read through new `GetState` /
`HasActiveRushIntent` accessors), so no schema bump.

**What it surfaced: two spec defects.** **`ERR-011-010`** — §3.7 delegated the rush DECISION to
Decision Tree #8, which has no goalkeeper model and structurally cannot acquire one, so the condition
belonged to nobody and the method sat uncalled for ten weeks; and because the "when" was delegated,
the spec never said what a keeper is *deciding* either — **the first cut of this trigger guessed
wrong**, using a last-man test that keeps the keeper home in exactly the situation he exists for.
New §3.7.0 takes the decision back and states the model. **`ERR-011-009`** — a rush that REACHED its
target had no exit. Both fixed spec-and-code in the same commit; see §3 of the owner doc.

**Measurement NOT run.** No .NET SDK in the authoring environment (the agent proxy denies the
installer), so neither the gate nor the new `GkRushDiagnosticTests` instrument executed. Owner doc
`docs/tracking/gk-rush-trigger-design.md` §6 carries the command and the honest status.

**Owner doc:** `docs/tracking/gk-rush-trigger-design.md`.

Everything downstream of the trigger is built and works: `GoalkeeperRushDispatch.UpdateRushFrame`
genuinely advances the keeper toward a locked target and writes the position back to the movement
array; the `Rushing → OneOnOne → Smothered` transitions exist with abort reasons, a 1v1 trigger
radius, a smother radius, and telemetry. The `RushIntent` is even serialized into the snapshot.
Only the trigger condition is missing.

**Consequence (as filed):** every one-on-one in the game was a stationary keeper on his line waiting
to dive. This was the single most likely contributor to the conversion gap, and the cheapest to
close. Whether closing it moved the conversion gap is **unmeasured** — see above.

### W2 — No player has ever made a tackle
**Evidence:** three independent dormant links in one chain.
- `defensive-ai/DefensiveAITick.cs:22` — `GetTackleIntentRequests` is populated every tick and read
  by nobody. The class doc says so outright: *"all output surfaces are populated at Stage 0 but
  integration with the match orchestrator and Decision Tree #8 occurs at Stage 1 (KD-16)."*
- `match-engine/MatchEngine.cs:6721` and `:6789` — **both** collision-query adapters hardcode
  `public bool GetAndClearTackleFlag(int agentId) => false;`
- Consequently `pass-mechanics/PassExecutor.cs:393`'s §3.8.5 tackle-interrupt branch, and the
  `CancelReason.TackleInterrupt` outcome it raises, are **unreachable code**.

**Consequence:** the defensive AI decides who should tackle, nothing acts on the decision, and the
two systems that would respond are wired to a constant `false`. Possession can be lost to
interception and to physical collision, but never to a tackle. This has a direct bearing on the
possession-churn and turnover numbers used in earlier passes.

### W3 — Keepers never claim crosses
**Evidence:** `goalkeeper-mechanics/GoalkeeperMechanics.cs:496–501` — the duel buffer is cleared
every frame, no participants are ever registered, and the source states
*"ResolveHandContactDuel is intentionally not called."*
`GoalkeeperCrossClaimDuel` and `CrossClaimDuelContext` have no reference outside their own assembly.

Blocked on the same missing multi-agent contact feed as the GK/Heading `CollisionConsumer`
AGENT_BALL duel fan-out already recorded in OPEN ISSUES — these are one dependency, not two.

### W4 — The keeper is never unsighted
**Evidence:** `match-engine/GkHeadingIntentSource.cs:27` `SaveArmed` is four lines of pure geometry
(ball loose, within range of the goal line, closing, above a minimum speed). Reaction latency is a
flat constant scaled by Reflexes.

A real, tested `OcclusionFilter` (shadow-cone test against other agents) is live in the perception
system for outfield players — `perception-system/PerceptionSystem.cs:396`,
`BallPerceptionEvaluator.cs:72`. The keeper is simply not on that path.

**Consequence:** traffic in front of goal costs the keeper nothing, and a deflection off a defender
does not restart his reaction window. Shots *do* deflect off bodies
(`CollisionSystem.ProcessAgentBall`) — the keeper just doesn't notice.

### W5 — The pressing AI's pass-event trigger never fires
**Evidence:** `pressing-ai/PassEventRing.cs` `Push` has no production caller anywhere.
`MatchEngine.cs:809` constructs one ring per team and hands it to `PressingAITick`
(`PressingAITick.cs:76`), which reads it via `TryGetLatest`. Nothing ever writes to it.

**Consequence:** the ring is permanently empty, so #13's BackwardPass press trigger is dead. A
press that should be sprung by a backward pass never is.

### W6 — `BallStateType.Controlled` has no producer
**Evidence:** `ball-physics/BallCollision.cs` — `CheckPossession` and `SetBallControlled` both have
zero production callers. The doc comment describes the intended protocol
(*"Caller must: record possession in agent system, call SetBallControlled(), drive position"*) and
no caller implements it.

Already recorded from the other direction in OPEN ISSUES §5.Z.23 item (c): a claimed ball is not
held at hand height and the keeper cannot carry it, because the parked ball settles under gravity.
Same root cause. Possession in the engine is a flag, never a kinematic constraint.

### W7 — The AI manager never picks a kickoff preset
**Evidence:** `match-engine/ManagerAdaptation.cs:250` `ApplyKickoff` has no caller. Its own doc says
*"Call BEFORE the first RunTick."* The mid-match half **is** wired
(`MatchEngine.cs:2510–2514` — `ManagerDecisionGate.DecisionDue` → `RunDecisionPoint`).

**Consequence:** #26's FR-TP-004 boot path is dead. An AI-managed team starts every match on the
human baseline tactic and can only ladder away from it mid-match.

### W8 — Goalkeeper distribution
**Evidence:** `goalkeeper-mechanics/GoalkeeperMechanics.cs:301` `CommitDistributeIntent`, no caller.
The engine substitutes its own six-second-rule release (`_gkHoldTicks` /
`_gkReleaseCooldownRemaining`), so #11's `GoalkeeperDistribution` model — delivery kind, target
selection — is unused.

Lower priority than W1–W7: unlike the others there **is** a working substitute, so this is a
fidelity gap rather than a missing behaviour.

### W9 — DT-emitted HEADER
Already recorded in OPEN ISSUES. Headers are triggered by an engine-side proximity heuristic, not
decided by the tree. Blocked on `ActionType` ordinal 8 overflowing the 3-bit composure-noise field,
which forces a rebaseline — a real cost, and the reason it has not been done.

### W10 — Attribute-modulated save commit
Already recorded in OPEN ISSUES. The save's *existence* is attribute-driven; its *quality at commit*
is not.

---

## 3. Class B — wired but starved (gate-level dormancy)

Not found by this audit's method — carried here from measured evidence in `§5.Z.24` and
`close-chance-creation-design.md` because they belong on the same board and are, in effect, larger
dormancy than anything in Class A.

- **C1 — ✅ FIXED August 8, 2026 (`ERR-012-011`).** #12 committed `InPoss` on 9.5% of final-third
  samples (7.5% when re-measured at the fix, the corpus having moved under the -021/-022/-023
  shot-lane chain), because `PossessionOwnerEntityId >= 0` is false for the entire flight of every
  pass. Phase now classifies from TEAM possession — the on-ball carrier's team, else the intended
  receiver of a pass in flight — composed by the orchestrator over a new `_passInFlightReceiverId`
  latch (`SNAPSHOT_SCHEMA_VERSION` 19 → 20). No new `[GT]`: the latch expires by reusing
  `RunFirstTouch`'s own receding predicate.
  **This entry called C1 "probably the highest-value item in this document" and that claim was
  wrong — the pre-implementation council refuted it, and the correction outlives the fix.** The
  starvation is real, but the three consumers it was supposed to unblock cannot use what it
  delivers: `TacticalContext.HasAttackIntent` is written by the engine and **read by no production
  code anywhere**, so #15 is inert regardless of its gate; #13's `PressDirective.PrimaryTargetPosition`
  and `PressAssignment.TargetPosition` have **no consumer outside `pressing-ai`** (only the `Role`
  label reaches #14's hold-shape pool), so more-correct pressing still steers nobody; and the one
  large behavioural lever C1 does pull is #12's own `PullFactor` table, whose `InPoss` column is
  LESS advanced than `TransToAtk` for every attacking role (ST 0.60 vs 0.75, AM 0.50 vs 0.60) —
  so the fix was predicted to push the deepest composed slot FURTHER from goal, not nearer.
  C1's real value is that the phase label is now correct and the `InPoss` column becomes
  exercisable for the first time — a precondition for the calibration pass, not a creation fix.
  **The named creation lever remains C4.** Two new Class-A items fell out of this and are listed
  below.
- **C2 — #15's TRANSITION branch never republishes per-agent intents**, so `GetIntent` serves stale
  ones for the whole transition window.
- **C5 — `TacticalContext.HasAttackIntent` has no production consumer** (found at C1, Aug 8, 2026).
  The engine writes it every AI stride from `_attacking[t].GetIntent(i)`; nothing reads it. This is
  the SECOND lock on #15's door and the larger of the two — the phase gate was only the first.
  Class A by the audit's own definition; it was missed because the method counts *public methods
  with no caller*, and this is a *field with no reader*.
- **C6 — `GkHeadingWorldAdapter.ApplyKick` is not reachable from any test** (found at C1, Aug 8,
  2026). Headers and keeper parry/deflect/spill all strike the ball through it. Pure-function tests
  do not construct an engine; the `MatchEngineGkHeading*` tests and scenarios stop at *intent
  committed*; `GkRushTriggerTests` moves the keeper without a strike; and the only paths that
  plausibly reach it (`GkSaveDiagnosticTests`, `GkContactRateDiagnosticTests`) are env-gated and end
  in `Assert.Pass(…)`, so they cannot fail. There is no counter on that adapter and no seam that
  forces a contact frame. Not dormancy — reachability — but the same blind spot: nobody would notice
  if it stopped working.
- **C3 — `RunParameters.RunTriggerTick` is inert**, because run params are regenerated every
  heartbeat.
- **C4 — #8 §3.1.3 cannot pass to a place, only to a player** — one PASS candidate per visible
  teammate at that teammate's *current* position. No pass into space, no through-ball to a run, no
  cross to an arriving header. A generator change, not a `[GT]`.

---

## 4. Class C — small, lifecycle, or non-defects

Recorded so a later sweep does not re-litigate them.

| Surface | Assessment |
|---|---|
| `HeadingMechanics.CancelIntent` | No interrupt path exists. Becomes load-bearing when W2 lands (a tackle should cancel a header). |
| `RecognitionLatencyTracker.RemoveEntity` | Per-pair state is never reclaimed on expiry or substitution. Bounded arrays, so a leak in accuracy, not memory. Low. |
| `ShoulderCheckScheduler.ClearBlindSideState` | Window-close cleanup never runs. Same class as above. Low. |
| `AttackingAITick.GetSnapshot` | Observation accessor. Not a gap. |
| `CoverShadowCurve.ComputeCurveEffectiveness` | Telemetry-only. Not a gap. |
| `DecisionTree.SetMatchSeed` | **Not a defect** — the seed is supplied at construction (`MatchEngine.cs:829`). Redundant setter; delete or leave. |
| `BallCollision.ApplyGoalPostCollision`, most of `BallPhysicsCore` / `AgentLocomotion` / `PassTargetResolver` | Internal helpers driven by their own assembly's orchestrator. Correctly wired. |

---

## 5. Proposed sequence

Each item is *wire + fix whatever the wiring surfaces*. Measurement and instruments are encouraged
throughout; `[GT]` landings are frozen per KD-W1 until the final pass.

| Order | Item | Rationale |
|---|---|---|
| 1 | ~~**W1** keeper rush trigger~~ ✅ **WIRED Aug 4, 2026** | Whole subsystem existed; a trigger-condition problem. Surfaced `ERR-011-010` + `ERR-011-009`. Its measurement is still owed. |
| 2 | ~~**C1** the `InPoss` gate~~ ✅ **FIXED Aug 8, 2026** (`ERR-012-011`) | Cheap, and the phase label was simply wrong. But the "unblocks #13/#14/#15" rationale was refuted before implementation — see the C1 entry: two of the three consumers are inert for reasons the gate does not touch. Re-measurement is the deliverable, not a creation gain. |
| 3 | **W2** tackles | Three-link chain, all three links understood. High realism value; touches pass cancellation, so expect findings. |
| 4 | **W4** keeper perception | Reuses tested occlusion. Upstream of all keeper behaviour, so it should precede any keeper calibration. |
| 5 | **W12** the gate-firing instrument | Before calibration, and before assuming Class B is only four items. |
| 6 | **W5**, **W7**, **W6** | Small, independent, each self-contained. |
| 7 | **W3** + AGENT_BALL fan-out | One dependency, two consumers. The largest single build in this document. |
| 8 | **W8**, **W9**, **W10** | Fidelity items with working substitutes or a known rebaseline cost. |
| — | **then** one calibration pass | Against the complete engine, using the §5.Z instruments and seeded-corpus method. |

C2/C3/C4 are folded into whichever item touches their assembly; C4 in particular is the recorded
next lever on close-chance creation and is large enough to want its own pass.

---

## 6. What this changes elsewhere

- **`CLAUDE.md`** — KD-W1 (the `[GT]` freeze) needs to sit beside the match-realism-pass entry, or
  it will be forgotten.
- **`.claude/skills/match-realism-pass`** — currently encodes measure → localize → ladder → land.
  The ladder step is premature for any target whose subsystem is unwired. Needs a gate at the top:
  *is the subsystem this touches fully wired? If not, this is a wiring task, not a realism pass.*
- **OPEN ISSUES** — W3/W9/W10 restate items already filed under the GK/Heading entry; W6 restates
  §5.Z.23 item (c); C1–C4 restate §5.Z.24's remainder. They are consolidated here rather than
  re-filed. The §5.Z.23 `pointQuality` owner decision is **parked**, not resolved: W1 changes the
  contact geometry that decision turns on, so deciding it now risks paying for a fix to a problem
  about to change shape.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.2 | 2026-08-08 | — | **C1 fixed** (`ERR-012-011`) — #12 §3.0 classifies phase from TEAM possession; the engine gains a pass-in-flight receiver latch (`SNAPSHOT_SCHEMA_VERSION` 19 → 20), no new `[GT]`. **This document's own claim that C1 was "probably the highest-value item" is retracted in place**, refuted by the pre-implementation council: the gate is real but two of the three consumers it was meant to unblock are inert for unrelated reasons, and the third lever (#12's `PullFactor` `InPoss` column) is LESS advanced than the `TransToAtk` column it replaces, so the fix was predicted to move the shape slightly AWAY from goal. C1's value is a correct label plus a first-time-exercisable `InPoss` column for the calibration pass. **Two new Class-A items filed from the same investigation**: **C5** `TacticalContext.HasAttackIntent` is written by the engine and read by no production code (the second, larger lock on #15's door — missed by the v1.0 method because it counts methods with no caller, not fields with no reader), and **C6** `GkHeadingWorldAdapter.ApplyKick` is not reachable from any test. Next in sequence is **W2**, tackles. |
| 1.1 | 2026-08-04 | — | **W1 wired** (`docs/tracking/gk-rush-trigger-design.md`) — `CommitRushIntent` has a production caller for the first time. Surfaced and fixed **two** spec defects: `ERR-011-010` (§3.7 delegated the rush decision to Decision Tree #8, which cannot make it — so the condition had no owner for ten weeks, and the spec never said what the keeper was deciding; new §3.7.0 states it, and the keeper comes out to REDUCE THE SHOOTING ANGLE, so a chasing defender does not keep him home and the distance is his own attributes) and `ERR-011-009` (a rush that reached its target had no §3.1.1 exit, so a swept loose ball stranded the keeper in `Rushing` for the rest of the match). Measurement not run — no .NET SDK in the authoring environment. Nine Class-A items remain; the next in sequence is **C1**, the `InPoss` gate. |
| 1.0 | 2026-08-04 | — | Initial audit. Three-pass sweep over the 18 assemblies the match engine references; 10 Class-A dormant capabilities, 4 Class-B starved gates carried from §5.Z.24, 7 Class-C non-defects. Establishes KD-W1 (`[GT]` freeze) and KD-W2 (scope). |
