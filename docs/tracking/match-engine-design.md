# Match Engine — Tick Orchestrator Composition Root (Design Note)

> **Created:** June 15, 2026
> **Last Updated:** June 27, 2026 (v0.9.12 — **Phase D D4 final cross-tick surface — Perception (#7) internal state now serialized**, and **Phase D flipped COMPLETE (D5)**. New `CaptureState` seams on `PerceptionSystem` + `RecognitionLatencyTracker` (→ `RecognitionLatencyState`) + `ShoulderCheckScheduler` (→ `ShoulderCheckState`), bundled in a new `PerceptionTickState`; `WritePerceptionTickState` serializes the recognition-latency pair arrays + shoulder-check per-agent/per-pair arrays + per-agent ball-perception carry-over (one shared instance); `SNAPSHOT_SCHEMA_VERSION` 7 → 8. **Cross-tick coverage complete** — no cross-tick gameplay state remains excluded (only boot-deterministic constants + observation counters). D5 reconciliation: Phase D complete; Phases E (events consumers) + F (capstone) pending. New `RecognitionLatencyState.cs` / `ShoulderCheckState.cs` / `PerceptionTickState.cs`, `TestOnly_PerceptionState` seam + `PerceptionState_FeedsSnapshotDigest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PerceptionSystem`. `RecognitionLatencyTracker.cs` v1.4, `ShoulderCheckScheduler.cs` v1.3, `PerceptionSystem.cs` v1.5, `MatchEngine.cs` v1.14, `MatchEngineConstants.cs` v1.14, `MatchEngineSnapshotSchemaTests.cs` v1.5. Prior v0.9.11 — **Phase D D4 continuation 3 — per-team Defensive AI (#14) + Attacking AI (#15) cross-tick state now serialized** via new `DefensiveAITick.CaptureState` / `AttackingAITick.CaptureState` seams returning `DefensiveTickState` / `AttackingTickState` views (offside-line + mark hysteresis + last assignment for #14; transition-hold + frozen directive + role hysteresis for #15; each ×`TEAM_COUNT`); `SNAPSHOT_SCHEMA_VERSION` 5 → 7 (v6 Defensive, v7 Attacking). All four mechanics-AI hysteresis surfaces are now serialized — only the perception internal-state seam remains before D5. New `DefensiveTickState.cs` / `AttackingTickState.cs`, `TestOnly_DefensiveState` / `TestOnly_AttackingState` seams + two digest probes; `match-engine-tests` asmdef gains `TacticalDirector.DefensiveAI` + `TacticalDirector.AttackingAI`. `DefensiveAITick.cs` v1.3, `AttackingAITick.cs` v1.3, `MatchEngine.cs` v1.13, `MatchEngineConstants.cs` v1.13, `MatchEngineSnapshotSchemaTests.cs` v1.4. Prior v0.9.10 — **Phase D D4 continuation 2 — per-team Pressing AI (#13) cross-tick state now serialized** via a new `PressingAITick.CaptureState` seam returning a new `PressingTickState` view (`WritePressingTickState`, ×`TEAM_COUNT` — trigger debounce + disengage/cooldown dwell + per-agent role hysteresis + press fatigue); `SNAPSHOT_SCHEMA_VERSION` 4 → 5; Pressing dropped from the exclusion list (perception + Defensive/Attacking still excluded). New `PressingTickState.cs`, `TestOnly_PressingState` seam + `PressingState_FeedsSnapshotDigest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PressingAI`. `PressingAITick.cs` v1.3, `MatchEngine.cs` v1.12, `MatchEngineConstants.cs` v1.12, `MatchEngineSnapshotSchemaTests.cs` v1.3. Prior v0.9.9 — **Phase D D4 continuation — per-team Positioning AI (#12) `HysteresisState` now serialized** via a new `PositioningAITick.CaptureState` seam (`WritePositioningHysteresis`, ×`TEAM_COUNT` — team phase + dwell + per-agent line/lane membership); `SNAPSHOT_SCHEMA_VERSION` 3 → 4; Positioning dropped from the exclusion list (perception + Pressing/Defensive/Attacking hysteresis still excluded — their seams are the rest of the follow-up before D5). New `TestOnly_PositioningState` seam + `PositioningHysteresis_FeedsSnapshotDigest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PositioningAI`. `PositioningAITick.cs` v1.1, `MatchEngine.cs` v1.11, `MatchEngineConstants.cs` v1.11, `MatchEngineSnapshotSchemaTests.cs` v1.2. Prior v0.9.8 — **Phase D step D4 implemented — snapshot extension + schema bump.** `SerializeWorldState` now serializes the per-agent D0 `DecisionTreeState` (×22) via the new `WriteDecisionTreeState` helper (mirrors the `DecisionTreeStateTests` round-trip order — `DtState` ordinal + dispatched-action flag + last `AgentAction` incl. embedded Pass/Shot request blocks), captured through the existing D0 `CaptureState` seam right after the C5 executor state. `SNAPSHOT_SCHEMA_VERSION` 2 → 3 (v3 doc paragraph). Per-field exclusion proofs recorded: `_perfs` stays excluded (PHASE-D flag not yet fired — AI phase still leaves it boot-neutral); the perception internal state + per-team Positioning/Pressing/Defensive/Attacking hysteresis remain excluded (no get/restore seam yet — same-seed in-process determinism unaffected; only save/restore replay needs them, deferred to a follow-up extension that re-bumps the schema). New `TestOnly_SetDecisionTreeState` seam + `MatchEngineSnapshotSchemaTests` pin 2 → 3 + `DecisionTreeState_FeedsSnapshotDigest` probe (first tick is not an AI stride, so injected EXECUTING state passes through to the snapshot — single-field probe). `MatchEngine.cs` v1.10, `MatchEngineConstants.cs` v1.10, `MatchEngineSnapshotSchemaTests.cs` v1.1. D5 + Phases E–F pending. Prior v0.9 — **Phase C plan folded in** (docs-only; no code). §5 Phase C expanded from a one-liner to ordered sub-steps C0–C6, with three corrections caught in adversarial review against the actual subsystem APIs: (1) the §3 Resolve row's `FirstTouchSystem.EvaluateOnBallContact` was a phantom — the real API is the pure `EvaluateFirstTouch(FirstTouchContext)` + `ApplyTouchResult` via first-touch's own adapters; (2) first-touch has no Stage-0 trigger and needs 2 extra adapters, so it is **deferred to Phase D**; (3) Phase C registers NO `DeterministicRngService` draw sites — collision self-seeds from `matchSeed ^ frameNumber` and pass/shot error is hash-based, so the planned RNG-registration sub-step was dropped. New C1a sub-step makes the six pass/shot executor adapter implementations (`IPass/IShotBallSystem` / `AgentQuery` / `CollisionQuery`) explicit as the highest-risk net-new surface; C0 executor snapshot seam named `CaptureState`/`RestoreState` to avoid colliding with the existing `IPassAgentQuery.GetState`. All claims verified against `PassExecutor`/`ShotExecutor` ctors, `IPass*` interfaces, `FirstTouchContext`/`FirstTouchSystem`, and `CollisionSystem.UpdateCollisions`. Phase D entry updated to absorb first-touch + the DecisionTree restore seam. Prior v0.8 — **Phase B complete**: steps B3 + B4 implemented. B3 — full canonical world-state field-set serialization + schema pin: `PHASE_A_PAYLOAD_FORMAT_VERSION` (u8) replaced with `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` (u32 = 1; distinct from the #16 `SnapshotHeader` schema version — body vs framing); `SerializeWorldState` now writes the full §2.6 field set field-by-field via `CanonicalSerializer` (ball position/velocity/spin/state + `LastValid*`; per-agent full `AgentState` incl. the B0 `OscillationGuard` ring-buffer state via `GetState()`; team/GK flags; the two collision-feedback inputs; the held `MovementCommand`), zero-alloc, ≈3.8 KB. New `TestOnly_SetAgent` seam + `MatchEngineSnapshotSchemaTests.cs` (schema pin; OscillationGuard + ball-spin digest-preimage probes; locked-guard determinism). B4 — design-note reconciliation: corrected the stale §2.3 three-buffer `{_knockdown, _knockdownForce, _stumble}` field block to the real two-input `{_isCollisionKnockdown, _collisionForces}` seam; confirmed no other doc references the phantom model (the remaining Collision System #3 `knockdownForceOut`/`stumbleOut` hits are its legitimate Phase-C OUTPUT API). Files: `MatchEngine.cs` v1.3, `MatchEngineConstants.cs` v1.3, `MatchEngineSnapshotSchemaTests.cs` v1.0. Prior v0.5 — Phase B re-sequenced after adversarial review of the planned Physics-phase wiring: `OscillationGuard` get/restore seam promoted to gating step B0 (its private sliding-window state blocks canonical agent serialization; the omission is invisible to Phase B's same-seed determinism test, only diverging under save/restore); §2.6 corrected — full `AgentState`/`BallState` field set incl. `OscillationGuard` + `LastValid*` checkpoints, and the phantom three-buffer collision model {isGrounded, knockdownForce, stumble} replaced with the real two-input seam {isCollisionKnockdown, collisionForce}; B1 time-unit fix (agent `currentTime` is seconds, clock exposes only ms); B2 uses `UpdateAllAgents` batch seam (skips GKs) + null ball logger. v0.4 — Phase A landed: `src/match-engine/` assembly + `MatchEngine` composition root (world-state fields, boot, 7 method-group phase callbacks wired into `TickOrchestrator` as EventBus-lifecycle-only stubs) + digest-load-bearing snapshot serialization + determinism/AI-stride test suite; see §5 Phase A and the Version History. v0.3 — second self-AR fix pass; v0.2 — self-AR fix pass: collision→movement ordering, EventBus AI-phase entry, cross-tick state in snapshot, stride-tick correction, per-agent-instance verification)
> **Status:** DESIGN NOTE (Stage 0+1 integration scaffolding — NOT a formal approved spec). **Phase A + Phase B implemented** (June 16, 2026); **Phase C complete** (C0–C3 June 19, 2026; **C4–C6 June 22, 2026** — possession→`MatchContext`, EventBus registry boot, executor+context snapshot serialization with `SNAPSHOT_SCHEMA_VERSION` 2); **Phase D steps D0/D1 implemented** (June 22, 2026); **Phase D step D2a implemented** (June 22, 2026 — Positioning AI #12 → per-team formation slots folded into each agent's `TacticalContext` via `RunPositioningAI`, with the away team mapped through the canonical attack-+X frame, `MirrorPitchIfAway`, as the ERR-008-002 home/away guard); **Phase D step D3 implemented** (June 22, 2026 — first-touch wired into Resolve: a loose, ground-level, approaching ball is received via `EvaluateFirstTouch`/`ApplyTouchResult`, CONTROLLED → possession); **Phase D step D2b implemented** (June 26, 2026 — `RunMechanicsAI` ticks the full Positioning→Pressing→Defensive→Attacking chain per team and folds the Defensive `OffensiveLineDepth`/`HasMarkDirective` + Attacking `HasAttackIntent` carriers into each agent's `TacticalContext`, all 22 agents mapped through the canonical attack-+X frame as the ERR-008-002 guard); **Phase D step D4 implemented** (June 27, 2026 — per-agent `DecisionTreeState` (×22) + every cross-tick gameplay surface serialized into the world-state body via `CaptureState` seams — per-agent `DecisionTreeState`, all four mechanics-AI hysteresis (Positioning #12, Pressing #13, Defensive #14, Attacking #15), and Perception #7 — `SNAPSHOT_SCHEMA_VERSION` 2 → 8); **Phase D complete (D5 reconciliation landed June 27, 2026)**; **Phase E complete (June 27, 2026 — possession-changed event producer + AI consumer + `EventBus.ResetForNewMatch` per-match reset seam)**; **Phase F complete (June 28, 2026 — capstone closed-loop kickoff scenario on the #19 `ScenarioRunner` with gameplay-invariant predicates + a two-run determinism digest match + FR-PO-052 perf-gate activation)**. **Match Engine integration (Phases A–F) is complete.**
> **Author:** —
> **Purpose:** Authoritative design for the match engine: the composition root that owns
> match world state and drives the existing `TickOrchestrator` 7-phase pipeline by wiring
> every implemented subsystem into its phase callbacks. This is the integration backbone
> that turns "compiles and unit-passes in isolation" into "runs as a composed match."

---

## 0. Scope and governance

The match engine is **not** covered by any of the 20 approved technical specifications
(`SPEC_INDEX.md` remains the canonical 1–20 list — this note does **not** introduce a
spec #21). It is Stage 0+1 integration scaffolding. This note is the governance anchor
for that scaffolding: it pins the phase→subsystem mapping, the snapshot-payload field
order and its schema version, world-state ownership, and the EventBus-lifecycle decision,
so the integration is not built undocumented.

Read `CLAUDE.md` and `src/CLAUDE.md` first. Every rule there (zero-alloc hot path,
constructor injection, no static mutable singletons / service locator / ambient context,
struct state passed by `ref`, deterministic time via `MatchClock`) applies here.

---

## 1. What already exists vs. what this adds

| Concern | Status |
|---|---|
| Phase runner `TickOrchestrator` (Input→Intent→AI→Physics→Resolve→Events→Snapshot), `MatchClock`, `PhaseId`, AI stride gating | ✅ exists (`src/deterministic-sim/`), AR-reviewed |
| `DeterministicRngService` (HKDF-SHA256 + SipHash-2-4; `RegisterStream`/`Reserve`/`DrawReserved`) | ✅ exists |
| `SnapshotCodec` / `SnapshotPayload` / `SnapshotHeader` / `CanonicalSerializer` (digest chain) | ✅ exists |
| `EventBus` lifecycle (`BeginTick`/`BeginPhase`/`DrainTick`/`SerializeLedger`/`OnTickBoundary`) | ✅ exists — **not invoked by anything yet** |
| Per-subsystem entry points (`Update`/`Tick`/`OnHeartbeat`/`ReceiveSnapshot`/`Execute`) | ✅ exist, unit/AR-tested in isolation |
| 6 `EventBusRegistrar.Initialize()` boot sites (Pass, Shot, Perception, Decision, Heading, Goalkeeper) | ✅ exist |
| **Composition root owning the 22-agent roster + ball + match state** | ❌ this note |
| **Snapshot-assembly layer** (world state → per-subsystem snapshot inputs) | ❌ this note |
| **World-state → `SnapshotPayload` serialization** (replay/save/digest) | ❌ this note |
| **EventBus tick-lifecycle invocation** | ❌ this note |

The "game-loop primitive" is therefore already done. The missing piece is the
composition root, the snapshot-assembly seams, and the world-state serialization.

---

## 2. Architectural decisions

### 2.1 New assembly `src/match-engine/` (`TacticalDirector.MatchEngine`)

A new top-level assembly that sits **above** the AI layer (the slot the future `UI`
assembly will occupy). It references every game layer plus `deterministic-sim` and
`event-system`. This does **not** violate the `Physics ← Mechanics ← AI` import ban
(`src/CLAUDE.md` §Reference Direction): that ban forbids a *lower* assembly importing an
*upper* one. A composition root that imports everything is permitted. Game-layer
assemblies MUST NOT reference `match-engine` back.

`match-engine` is **infrastructure/composition**, not a member of any gameplay layer.

### 2.2 `MatchEngine` is a `sealed class`, constructor-injected

No service locator, no static mutable singleton, no ambient context (all four are banned
by `src/CLAUDE.md` §Banned Architectural Patterns). The engine holds world state in
private, pre-allocated fields and exposes the 7 phase methods as `System.Action`
method-group references handed to `TickOrchestrator`. Method-group conversion allocates
once at construction — zero per-frame closures.

### 2.3 World state lives in `MatchEngine` fields, passed by `ref`

```
BallState                  _ball;
AgentState[22]             _agents;          // + parallel arrays:
AgentPhysicalProperties[22] _agentProps;
int[22]                    _teamIds;
bool[22]                   _isGoalkeeper;
MatchContext               _matchContext;    // host-authored each AI tick
TacticalContext[2]         _tactical;         // per team; Stage 0 = Stage0Default
// AI snapshot buffers (pre-allocated, reused):
FilteredView[22]                 _views;
PositioningPerceptionSnapshot    _posSnapshot;
PressingSnapshot                 _pressSnapshot;
DefensiveSnapshot                _defSnapshot;
AttackingSnapshot                _atkSnapshot;
// Collision-feedback inputs to movement (pre-allocated) — the real two-input seam
// {isCollisionKnockdown, collisionForce}; written by Resolve (tick N), consumed by movement
// (tick N+1) per the one-tick-lag contract in §3. There is NO {isGrounded, knockdownForce,
// stumble} three-buffer model — GroundedReason lives inside AgentState, and stumble is not a
// movement input at Stage 0 (B4 reconciliation; matches §2.6 / §3 and the B2 implementation):
bool[22] _isCollisionKnockdown; float[22] _collisionForces;
```

All buffers are allocated once at construction. The hot path mutates them by `ref`.

### 2.4 EventBus lifecycle is driven from inside the phase callbacks

`TickOrchestrator.RunTick` advances the clock, runs the 7 phase callbacks, and calls
`SnapshotCodec.Encode` — it does **not** touch the EventBus. Rather than modify that
certified file, the engine drives the EventBus lifecycle from inside its callbacks:

- `RunInputPhase` (first phase) first lines:
  `EventBus.BeginTick(_clock.CurrentTick); EventBus.BeginPhase(PhaseId.Input);`
  (`MatchClock.Advance()` has already run inside `RunTick` before the callback, so
  `CurrentTick` is the tick being processed.)
- each subsequent phase callback first line calls `EventBus.BeginPhase(PhaseId.X)` for its
  own phase — **except the AI phase** (handled by the next bullet) and Input (handled above).
- **AI phase entry is unconditional.** `TickOrchestrator` does **not** invoke `_runAI`
  on non-stride ticks (it runs an empty marker scope instead), so a `BeginPhase(PhaseId.AI)`
  placed inside `RunAiPhase` would be skipped 5 of every 6 ticks and the EventBus phase
  stream would diverge on non-stride ticks. The engine therefore calls
  `EventBus.BeginPhase(PhaseId.AI)` at the **end of `RunIntentPhase`** (i.e.
  unconditionally, before the orchestrator's stride branch), so the AI phase is entered
  every tick regardless of stride. `RunAiPhase` itself does **not** call `BeginPhase`.
- `RunEventsPhase`: `EventBus.DrainTick();`
- `RunSnapshotPhase`: write world state into the payload, then
  `EventBus.SerializeLedger(...)`, then `EventBus.OnTickBoundary();`

**Alternative considered:** extend `TickOrchestrator` to own the EventBus lifecycle —
cleaner, but edits a determinism-load-bearing file, needs its own adversarial review, and
would couple the orchestrator to the event system. Deferred; revisit only if more than one
host driver appears.

### 2.5 Snapshot-assembly helpers (the bulk of the genuinely new code)

Pure functions that read world state and populate the per-subsystem input snapshots
(`PerceptionSystem` inputs, `PositioningPerceptionSnapshot`, `PressingSnapshot`,
`DefensiveSnapshot`, `AttackingSnapshot`). These seams do not exist today because each
subsystem was unit-tested with hand-built inputs. They are the highest-risk new surface
(see §6.3) and must be zero-alloc (write into the pre-allocated buffers in §2.3).

### 2.6 Snapshot payload schema is digest-load-bearing

The world-state serialization order written into `SnapshotPayload.PayloadBytes` feeds the
digest chain. It MUST be pinned and versioned with a `SNAPSHOT_SCHEMA_VERSION` (parallel
to `PhaseId`'s schema-bump rule) **before Phase B lands**, or every later field change
forces a schema bump. Serialize via `CanonicalSerializer` (−0.0 normalization, canonical
NaN handling already implemented).

**The payload MUST capture all state that survives across ticks — not just kinematics.**
A field that is read on tick N but written on tick N−1 (or earlier) is simulation state
and its omission diverges replay. Stage 0 field set:

- ball: position, velocity, spin, state-machine state, **plus the `LastValidPosition` /
  `LastValidVelocity` NaN-recovery checkpoints** (written each valid tick, read on an invalid
  tick — cross-tick, therefore serialized). All `BallState` fields are public.
- per-agent: **the full `AgentState` struct, field for field** — not just kinematics. Beyond
  position / velocity / facing / locomotion state / fatigue, this includes `PreviousState`,
  `TimeInState`, `GroundedReason`, `CollisionForce`, `LeanAngle`, `CurrentTurnRate`, the three
  `LastValid*` checkpoints, `Speed`, **and the embedded `OscillationGuard` sub-struct's internal
  sliding-window state** (8 transition timestamps + write index + lock flag + lock-until time).
  Each is read-before-written on a later tick; omitting any diverges replay. The `OscillationGuard`
  fields are *private* — serializing them needs a new accessor seam (see seam dependency below).
- **per-agent held `MovementCommand`** — produced only on stride ticks but consumed every
  tick (§3, §6.below), so it persists in world state and is digest-relevant.
- **per-agent collision inputs** — the real `AgentMovementSystem.Update` seam takes exactly
  **two**: `isCollisionKnockdown` (bool) and `collisionForce` (float). There is **no** `isGrounded`
  or `stumble` parameter — `GroundedReason` lives *inside* `AgentState` (above), and stumble is not
  consumed by movement at Stage 0. These two buffers are produced in Resolve (tick N) and consumed
  by movement in Physics (tick N+1) per the one-tick-lag contract below; cross-tick, so serialized.
- per-agent DecisionTree state-machine state (IDLE/EVALUATING/EXECUTING/INTERRUPTED) and
  any in-flight executor state (Pass/Shot WINDUP/CONTACT) — persists between heartbeats.

If a buffer can be proven fully recomputed before its first read each tick, it may be
excluded — but the default is to serialize cross-tick state, and the proof must be recorded
here per field.

**Excluded-field proofs (B3).** Two world-state arrays are deliberately NOT serialized:
- **`_attrs` (`PlayerAttributes[]`) / `_perfs` (`PerformanceContext[]`)** — read every tick by
  `AgentMovementSystem.UpdateAllAgents` but passed by `in` (read-only; never mutated mid-sim). At
  Stage 0 both are boot-deterministic constants (`CreateDefault()` / `CreateNeutral()`), so a
  save/restore reconstructs them identically at boot and their omission cannot diverge replay.
  **PHASE-D FLAG:** when the AI phase begins writing per-agent form/fatigue context into `_perfs`,
  `_perfs` becomes cross-tick state and MUST be added to the payload (bump `SNAPSHOT_SCHEMA_VERSION`).
- **`_aiPhaseRanThisTick` / `_aiPhaseRunCount`** — Phase-A observation instrumentation, fully
  derivable from the tick number (stride cadence); not gameplay state.

**Seam dependency (blocker — affects Phase B *and* C/D).** Several subsystems hold cross-tick
state in *private* fields with no get/restore accessor; serializing (and replay-restoring) it
requires adding read/restore seams — parallel to
`RngStreamState` ↔ `DeterministicRngService.GetStreamState`/`RestoreStream`:
- **Phase B:** `AgentState.OscillationGuard` (`_t0.._t7`, `_writeIndex`, `_isLocked`,
  `_lockUntilTime`, all private). This is the gating item for Phase B's snapshot — canonical
  field-by-field serialization via `CanonicalSerializer` (mandatory for −0.0 / NaN normalization;
  a raw struct blit would bypass it and is determinism-unsafe) cannot read these fields without
  the seam. **Subtlety:** Phase B's same-seed-in-process determinism test passes *even if the
  guard state is omitted* (both runs omit it identically) — the omission only diverges under
  save/restore replay, which Phase B doesn't exercise. The seam is therefore required up front
  (step 0 below), not deferred to a later phase that "needs it for a test."
- **Phase C/D:** the Pass/Shot executors and DecisionTree hold their state-machine / in-flight
  state internally and likewise expose no get/restore accessors. These are a prerequisite for
  Phase C (executors) and Phase D (DecisionTree); they do not exist yet.

---

## 3. Phase → subsystem wiring

`dt = DeterministicSimConstants.FrameSeconds` (the per-tick seconds step landed in B1; fixed 60 Hz, never wall-clock).

| Phase | Host method | Subsystems invoked | Cadence |
|---|---|---|---|
| Input (0) | `RunInputPhase` | Stage 0 stub (no controller yet); opens EventBus tick | 60 Hz |
| Intent (1) | `RunIntentPhase` | Stage 0: static `TacticalContext`; later set-piece / manager intent | 60 Hz |
| AI (2) | `RunAiPhase` | assemble snapshots → `PerceptionSystem.OnHeartbeat` (×22) → `DecisionTree.ReceiveSnapshot` (×22) → `PositioningAITick` / `PressingAITick` / `DefensiveAITick` / `AttackingAITick` → emit `MovementCommand`s | 10 Hz (stride-gated by orchestrator) |
| Physics (3) | `RunPhysicsPhase` | `BallPhysicsCore.UpdateBallPhysics(ref _ball, dt)`; `AgentMovementSystem.Update(...)` ×22 — consumes the **previous tick's** collision-feedback buffers (see contract below) | 60 Hz |
| Resolve (4) | `RunResolvePhase` | `CollisionSystem.UpdateCollisions(...)` → writes this tick's collision-feedback buffers; in-flight `PassExecutor.Update(matchTime, frameNumber, ref _ball)` / `ShotExecutor.Update(...)` (×22; `Execute(in request)` initiates, `Update` advances the lifecycle); possession → `_matchContext`. **First-touch is NOT here** — `FirstTouchSystem.EvaluateFirstTouch(FirstTouchContext)` is pure and `ApplyTouchResult` mutates via first-touch's own `IBallPhysicsSystem`/`IAgentMovementSystem` adapters, and it has no Stage-0 trigger without an AI carrier/receiver decision; it lands in **Phase D** (see §5). | 60 Hz |
| Events (5) | `RunEventsPhase` | `EventBus.DrainTick()` → registered consumers | 60 Hz |
| Snapshot (6) | `RunSnapshotPhase` | serialize world state (§2.6) → `SnapshotPayload`; `EventBus.SerializeLedger`; `EventBus.OnTickBoundary` | 60 Hz |

**Collision ↔ movement ordering contract (one-tick lag).** `AgentMovementSystem.Update`
(Physics, phase 3) takes collision force / grounded / knockdown as **inputs**, but
`CollisionSystem.UpdateCollisions` (Resolve, phase 4) runs *after* movement in the same
tick. Movement at tick N therefore consumes the collision-feedback buffers written at tick
N−1. This is deliberate (the canonical phase order is fixed by `#16` and MUST NOT be
reordered): collisions resolve the positions movement just produced, and the response feeds
back next tick. Consequences the implementation MUST honor: (a) the buffers are seeded at
boot to the **standing-at-rest** value — `isCollisionKnockdown = false`, `collisionForce = 0`
(the two real inputs; `GroundedReason` is internal `AgentState`, default `NONE`, and is not an
external seed); (b) the two buffers are cross-tick state and are serialized into the snapshot
(§2.6); (c) this one-frame feedback latency is an accepted Stage 0 model property, recorded here
rather than hidden.

**Stride timing.** `RunTick` calls `MatchClock.Advance()` *first*, so the first processed
tick is **1**, not 0; the initial state (tick 0) is never "run." The AI phase executes when
`tick % AI_PHASE_STRIDE == 0` (stride = 6), so the **first AI evaluation is tick 6**
(~100 ms after kickoff) — agents hold their boot-time `MovementCommand` until then. The
orchestrator runs the AI phase as a no-op on non-stride ticks (but the EventBus AI phase is
still entered every tick per §2.4).

---

## 4. Boot sequence (`MatchEngine.Boot(ulong matchSeed)`)

1. Construct `DeterministicRngService(matchSeed)`; `RegisterStream(...)` for every
   subsystem draw site (collision foul/stumble, pass/shot error, perception latency, GK,
   heading) — stable `siteId`s per `#16` §3.2.5.1.
2. `EventBus.ResetForNewMatch()` (Phase E — clears subscribers + reopens the boot phase so this
   match can subscribe; leaves the row schema intact), then call all `EventBusRegistrar.Initialize()`
   sites exactly once (Pass, Shot, Decision wired today; Perception/Heading/Goalkeeper as they wire in).
   `RegisterExternalRow` forces `EventRegistry.EnsureInitialized()`. Then `EventBus.Subscribe` the
   Phase E cross-subsystem consumers (possession-changed → AI) while the boot phase is still open
   (#17 FR-EVT-020/021). The registrars are idempotent (per-registrar `s_registered` guard); the
   reset seam is what makes the replay / second-match path safe (§6.4 — now resolved).
3. Construct each subsystem with injected dependencies (`MatchClock`, RNG, ball-system
   seams). Per-agent systems (`PassExecutor`, `ShotExecutor`, `DecisionTree`) held as
   22-element arrays (confirm against the zero-alloc budget — see §6.5).
4. Initialize world state: kickoff positions from formation slots
   (`PositioningAIConstants`), ball at the center spot, `MatchContext` = `KICK_OFF`.
5. Construct `MatchClock(0)`, `SnapshotCodec`, `EnvironmentFingerprint.CreateStage0Dev()`.
6. Construct `TickOrchestrator` with the 7 method-group callbacks.

---

## 5. Phased delivery

Each phase is its own commit + adversarial review (per project convention), gated by the
Linux compile/test CI (`tools/dotnet-ci/run-gate.sh`).

- **Phase A — Skeleton & determinism spine (chosen first slice). ✅ IMPLEMENTED (June 16, 2026).**
  New assembly + asmdef (`src/match-engine/`, `TacticalDirector.MatchEngine`),
  `MatchEngine` with world-state fields, boot, all 7 callbacks as **EventBus-lifecycle-only
  stubs** (no subsystem calls). Capstone: run N ticks twice with the same seed → identical
  snapshot digest chain. Proves the loop + EventBus lifecycle + digest before any physics.
  *Tests (`tests/MatchEngineDeterminismTests.cs`): determinism digest equality across two
  same-seed runs; digest-chain non-degeneracy + advance; AI-stride cadence; first-tick / first-
  AI-tick timing.* Phase-A scope notes: references only `deterministic-sim` + `event-system`
  (game-layer refs land with B–F); world state is a deterministic kinematic subset (ball + 22
  agent slots) — the full §2.3 game-struct field set and the `SNAPSHOT_SCHEMA_VERSION` pinning
  (§2.6) land in Phase B; `MatchEngineConstants.PHASE_A_PAYLOAD_FORMAT_VERSION` versions the
  interim payload; the EventBus registry boot (§4 step 2 registrars) lands when real consumers
  wire in (Phase E) — Phase A publishes nothing, so the empty-ledger lifecycle is sufficient.
- **Phase B — Physics phase.** Wire ball physics + agent movement (×22) + full world-state
  serialization. Ordered sub-steps (B0 first — it gates the snapshot):
  - **B0 — `OscillationGuard` get/restore seam (gating).** Add a public read/restore accessor to
    `OscillationGuard` (8 timestamps + write index + lock flag + lock-until time), parallel to
    `RngStreamState`. Determinism-load-bearing movement file → its own focused AR + a
    `CanonicalSerializer` round-trip test before anything else lands. Without this, the §2.6
    snapshot cannot serialize agent state canonically (§2.6 seam dependency).
  - **B1 — time-unit plumbing. ✅ IMPLEMENTED (June 16, 2026).** `dt =
    DeterministicSimConstants.FrameSeconds` (the per-tick seconds step; B2 sources dt here). Agent
    `currentTime` MUST be **seconds** — `OscillationGuard` compares against `WindowSeconds` — but
    `MatchClock` only exposed `CurrentMatchTimeMs`. Added `[DERIVED] FrameSeconds = FrameMs / 1000`
    and `MatchClock.CurrentMatchTimeSeconds` (= `CurrentTick × FrameSeconds`) so the seconds clock and
    the integration dt share one derivation chain (`PHYSICS_TICK_HZ → FrameMs → FrameSeconds`). Silent
    if wrong: the `Update` finite/≥0 assert passes for ms too. *Tests
    (`tests/DeterministicSimTests.cs`): FrameSeconds = FrameMs/1000; CurrentMatchTimeSeconds tick
    tracking + one-second landing + seconds↔ms agreement.* Files: `DeterministicSimConstants.cs` v1.2,
    `MatchClock.cs` v1.1, `DeterministicSimTests.cs` v1.6.
  - **B2 — physics wiring in `RunPhysicsPhase`. ✅ IMPLEMENTED (June 16, 2026).** World state
    migrated from the Phase-A kinematic float arrays to real `BallState` + `AgentState[]` plus the
    per-agent input buffers (`PlayerAttributes`/`PerformanceContext`/`MovementCommand`) and the two
    collision-feedback buffers. `RunPhysicsPhase` calls
    `BallPhysicsCore.UpdateBallPhysics(ref _ball, dt, SurfaceType.GrassDry, Vector3.zero, logger: null,
    matchTime: 0f)` — the logger is the *only* consumer of ball `matchTime`, so a `null` logger drops it
    (no alloc, non-load-bearing) — then `AgentMovementSystem.UpdateAllAgents(...)` (the batch seam,
    which **skips goalkeepers** via `if isGoalkeeper continue`, so the 2 GKs stay put at Stage 0 — GK is
    #11), with `dt = FrameSeconds` and `currentTime = clock.CurrentMatchTimeSeconds` (B1). Boot seeds
    the collision inputs standing-at-rest (`false` / `0`), `PlayerAttributes.CreateDefault()`,
    `PerformanceContext.CreateNeutral()`, and a `MovementCommand.Stop` hold command per agent (the AI
    phase replaces it at Phase D). Interim serialization sources the kinematic subset (position +
    facing) from the real structs (`PHASE_A_PAYLOAD_FORMAT_VERSION` bumped 1 → 2); the full field set +
    schema pin remain B3. New test seams: `TestOnly_SetBall` / `BallSnapshot` / `SetCommand` /
    `AgentSnapshot` / `IsGoalkeeper`. *Tests (`tests/MatchEnginePhysicsTests.cs`): dropped-ball
    integration, outfield walk-toward-target + goalkeeper-skip, same-seed determinism with live
    dynamics.* Files: `MatchEngine.cs` v1.2, `MatchEngineConstants.cs` v1.2, `match-engine.asmdef` +
    `match-engine-tests.asmdef` (+BallPhysics +AgentMovement), `MatchEnginePhysicsTests.cs` v1.0.
  - **B3 — serialization + schema pin. ✅ IMPLEMENTED (June 16, 2026).** `PHASE_A_PAYLOAD_FORMAT_VERSION`
    (u8) replaced with `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` (u32 = 1; distinct from the #16
    `SnapshotHeader` schema version — that versions the codec framing, this versions the world-state
    body inside the payload). `SerializeWorldState` now writes the **full** §2.6 field set field-by-field
    via `CanonicalSerializer`: ball position/velocity/spin/state + `LastValid*` checkpoints; per agent the
    full `AgentState` (kinematic + state-machine + turning + dual-energy fatigue + `LastValid*` + `Speed`)
    **including the B0 `OscillationGuard` ring-buffer state via its `GetState()` accessor**; plus the
    ancillary per-agent world state not carried inside `AgentState` — team id, goalkeeper flag, the two
    collision-feedback inputs (`isCollisionKnockdown`/`collisionForce`), and the held `MovementCommand`.
    Enum fields are written as i32 (ordinal). Zero-alloc (the guard seam returns a value type). Measured
    payload ≈ 3.8 KB, well under `MaxSnapshotBytes` (65536). DecisionTree / executor in-flight state stays
    excluded until Phase C/D (those seams do not exist yet — §2.6 seam dependency). New
    `TestOnly_SetAgent` seam + `MatchEngineSnapshotSchemaTests.cs` (schema-version pin; OscillationGuard
    and ball-spin digest-preimage probes proving the expanded field set feeds the digest; locked-guard
    determinism). *Tests: `tests/MatchEngineSnapshotSchemaTests.cs`.* Files: `MatchEngine.cs` v1.3,
    `MatchEngineConstants.cs` v1.3, `MatchEngineSnapshotSchemaTests.cs` v1.0.
  - **B4 — design-note reconciliation. ✅ IMPLEMENTED (June 16, 2026).** The §2.6 / §3 phantom
    `isGrounded` three-buffer seam was corrected in v0.5; B4 also corrected the stale §2.3 world-state
    field block (it still listed the `{_knockdown, _knockdownForce, _stumble}` three-buffer model) to the
    real two-input `{_isCollisionKnockdown, _collisionForces}` seam matching §2.6 / §3 and the B2
    implementation. Sweep confirms no other doc references the three-buffer model: the only remaining
    `knockdownForce`/`stumble` hits are the **Collision System #3** `UpdateCollisions` OUTPUT API
    (`knockdownForceOut`/`stumbleOut`) — a legitimate separate seam (collision produces both; movement
    consumes only the knockdown subset), wired in Phase C, not part of the movement-input contract.
  *Tests: B0 guard round-trip; drop-and-settle ball through the real loop; outfield-agent
  locomotion under a fixed `WalkTo` command (GKs excluded); digest stable across two same-seed
  runs with real dynamics.*
- **Phase C — Resolve phase. ✅ COMPLETE (C0–C3 June 19, 2026; C4–C6 June 22, 2026).** Collision (×22) + pass/shot executor lifecycle + possession
  tracking into `MatchContext`. Ordered sub-steps; intra-Resolve call order is fixed and
  digest-load-bearing: **collision → executor `Update` → possession** (first-touch joins this
  chain in Phase D). NOTE: Phase C registers **no** `DeterministicRngService` draw sites —
  `CollisionSystem.UpdateCollisions` self-seeds its own `DeterministicRNG` from
  `matchSeed ^ frameNumber` internally, and pass/shot error is deterministic-hash-based
  (`ShotErrorCalculator` explicitly chose hashing over `DrawReserved()`), so the host's `_rng`
  stays unused in Resolve. (Verified against the executor ctors + `CollisionSystem.cs`,
  June 2026.)
  - **C0 — executor snapshot get/restore seams (gating; do first). ✅ IMPLEMENTED (June 19, 2026).**
    Added value-type `CaptureState()` / `RestoreState(...)` accessors to `PassExecutor` (v1.14) +
    `ShotExecutor` (v1.9), parallel to the B0 `OscillationGuard` seam and `RngStreamState`. Named
    `CaptureState` (NOT `GetState`) to avoid colliding with the existing `IPassAgentQuery.GetState(int)`
    / `IShotAgentQuery` surface. New plain-data DTOs `PassExecutorState` / `ShotExecutorState` carry the
    state-machine ordinal + every value frozen at INITIATING that survives across WINDUP/CONTACT frames.
    The Pass executor's internal `PhysicalProfile` is **not** serialized — it is a pure function of
    (`PassType`, effective sub-type), so `RestoreState` recomputes it via `PassTypeProfiles.GetProfile`
    (the §2.6 "fully recomputed before its first read" exclusion; also dodges the fact that
    `PhysicalProfile` is an `internal` type and cannot be a public DTO field). Shot has no such
    exclusion (no internal in-flight type). Focused round-trip tests landed in each spec's test
    assembly: `PassExecutorStateTests` / `ShotExecutorStateTests` prove (a) a populated state survives a
    `CanonicalSerializer` write/read round-trip byte-for-byte (the field-order serialization C5 will
    reuse) and (b) `Capture → Restore → Capture` is the identity on the cross-tick field set. Both test
    asmdefs gained the `TacticalDirector.DeterministicSim` reference for `CanonicalSerializer`. No change
    to the `Execute`/`Update` execution paths. DecisionTree state-machine state stays deferred to Phase D
    (its restore seam does not exist yet — §2.6 seam dependency).
  - **C1 — boot wiring. ✅ IMPLEMENTED (June 19, 2026).** `_matchSeed` retained as a field
    (`UpdateCollisions` self-seeds from the raw value). `CollisionSystem(22)` + a null-object
    `ICollisionEventConsumer` (`NullCollisionEventConsumer`, drains events; real consumers Phase E)
    constructed at boot. §6 item 5 **resolved: per-agent INSTANCE** — `PassExecutor[22]` /
    `ShotExecutor[22]` arrays (each holds its own C0 state machine, so a shared evaluator cannot
    serve them); a single adapter per family backs all 22 (adapter methods take `agentId`). New
    `_possessingAgentId` field (NO_POSSESSION at kickoff).
  - **C1a — executor adapter implementations. ✅ IMPLEMENTED (June 19, 2026).** Two nested sealed
    classes — `PassWorldAdapter` / `ShotWorldAdapter` — implement all six executor query interfaces
    over host world state via a back-reference: `IsBallPossessedBy` (== `_possessingAgentId`) +
    `ApplyKick` (→ `BallCollision.ApplyKick` + possession release), `GetAttributes`/`GetState`
    (`BuildPass*`/`BuildShot*` mappers over `_agents`/`_attrs`; ERR-007 attribute fields are
    Stage-0 neutral `[GT]` proxies, fatigue derived from `AerobicPool`). `GetAndClearTackleFlag`/
    `ComputePressureScalar` are deterministic Stage-0 stubs (no tackle flags / pressure model until
    Phase D/E). First-touch's 7th/8th adapters land at Phase D.
  - **C2 — collision wiring in `RunResolvePhase`. ✅ IMPLEMENTED (June 19, 2026).** First call after
    `BeginPhase(PhaseId.Resolve)`: `UpdateCollisions(_agents, _attrs, _teamIds, _isGoalkeeper,
    knockdownOut: _isCollisionKnockdown, knockdownForceOut: _collisionForces, stumbleOut:
    _stumbleScratch, ball: ref _ball, …)`. Reuses `_attrs` (`PlayerAttributes[]`); `_stumbleScratch`
    discarded (B4). Buffers written tick N, consumed by movement tick N+1 (the §3 one-tick-lag
    contract). `frameNumber = (int)_clock.CurrentTick`.
  - **C3 — executor lifecycle. ✅ IMPLEMENTED (June 19, 2026).** All 22 pass + 22 shot executors
    advanced each Resolve tick via `Update(matchTime, frameNumber, ref _ball)` (idle ones no-op);
    `TestOnly_InitiatePass`/`InitiateShot` + `TestOnly_SetPossession` script `Execute` (production
    trigger is the Phase D AI dispatcher). NOTE: no executor reaches the CONTACT publish at Stage 0
    (idle in production / determinism tests; `EventRegistry.EnsureInitialized` is `internal` so the
    host cannot boot the registry yet) — the registry boot + the pass-completes / possession-flips
    test move to C4. *Tests (`MatchEngineResolveTests.cs`): collision separates an overlapping pair
    in Resolve; same-seed digest equality with a live collision; scripted pass/shot initiates through
    the adapters and advances one tick (below the CONTACT boundary).*
  - **C4 — possession → `MatchContext`. ✅ IMPLEMENTED (June 22, 2026).** New `MatchContext _matchContext`
    world-state field authored by `UpdateMatchContext()` at the END of `RunResolvePhase` (after possession
    settles) and once at boot: folds `_possessingAgentId` into `PossessingAgentId`, derives the
    `Possession` state (loose → CONTESTED, else the possessing agent's team), copies ball position/velocity,
    and authors `BallZone` via `PitchGeometry.ComputeFieldZone(ballX)` **home-perspective only** — the
    `DecisionContextAssembler` derives the team-relative zone downstream (ERR-008-002 regression guard).
    Score is 0 and `Phase` is a fixed `OPEN_PLAY` at Stage 0 — the running tick loop IS open play, and
    `OPEN_PLAY` is the only phase for which `OptionGenerator` produces options (`§3.1` gates all five
    branches on `Phase == OPEN_PLAY`), so `KICK_OFF` would silently make the Phase D AI a no-op; Phase D /
    match-flow drives real `KICK_OFF`→`OPEN_PLAY` / set-piece transitions. Write(Resolve)→read(next AI
    tick) ordering pinned. **Absorbed from C3:** boot now boots the
    Pass/Shot `EventBusRegistrar.Initialize()` sites (both carry an idempotent `s_registered` guard;
    `RegisterExternalRow` forces the seeded-row cctor, so the host needs no access to the internal
    `EventRegistry.EnsureInitialized`) so a scripted pass reaches CONTACT and publishes `PassAttemptEvent`.
    The `match-engine` asmdef gains the `TacticalDirector.DecisionTree` reference (`MatchContext` /
    `PitchGeometry`). *Tests (`MatchEngineMatchContextTests.cs`): home-perspective ball-zone authoring;
    loose=CONTESTED + possessing-agent-team derivation; a scripted ground pass reaches CONTACT, releases
    possession (NO_POSSESSION), and kicks the ball; same-seed digest equality across two runs with a live
    CONTACT publish (the ledger header is deterministic per boot — `Tick` + per-tick-reset
    `IntraPhaseDrawIndex`, no process-global counter).*
  - **C5 — snapshot extension + schema bump. ✅ IMPLEMENTED (June 22, 2026).** `SerializeWorldState` now
    writes, per agent, the C0 `PassExecutorState` / `ShotExecutorState` capture (value types, zero heap
    alloc) after the existing `AgentState` + ancillary block, then the authoritative `MatchContext` after the
    loop; `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` bumped 1 → 2. New `WritePassExecutorState` /
    `WriteShotExecutorState` / `WriteMatchContext` helpers mirror the C0 round-trip field order (the
    `PassExecutorStateTests` / `ShotExecutorStateTests` lock the order the snapshot reuses). `_possessingAgentId`
    is captured via `MatchContext.PossessingAgentId` (exclusion proof updated; the raw field is not serialized
    separately). The `_attrs` / `_perfs` exclusion (Phase-D `_perfs` flag) + Phase-A observation counters
    remain excluded. *Tests (`MatchEngineMatchContextTests.cs`): possession feeds the digest via MatchContext;
    a mid-windup pass executor feeds the digest (isolated from MatchContext by setting possession in both the
    baseline and perturbed engines). `MatchEngineSnapshotSchemaTests` schema pin updated 1 → 2.*
  - **C6 — design-note reconciliation. ✅ IMPLEMENTED (June 22, 2026).** §3 Resolve row already corrected
    `EvaluateOnBallContact` → `EvaluateFirstTouch` + first-touch deferral (v0.9); Phase C header + status line
    flipped to complete; Phase D D1 (AI-phase wiring) is unblocked now that C4's `MatchContext` is in place.
- **Phase D — AI phase + snapshot assembly + first-touch.** The new assembly helpers (§2.5) +
  perception → decision tree → movement-command chain, then the 4 mechanics AIs feeding tactical
  intent, then first-touch. Ordered sub-steps (D0 first — the gating seam, per the B0/C0 precedent):
  - **D0 — DecisionTree snapshot get/restore seam (gating; do first). ✅ IMPLEMENTED (June 22, 2026).**
    Added value-type `CaptureState()` / `RestoreState(in DecisionTreeState)` accessors to `DecisionTree`
    (v1.2), the C0 analogue deferred from Phase C, parallel to the Pass/Shot executor seams and the B0
    `OscillationGuard` seam. New plain-data DTO `DecisionTreeState` carries the cross-tick state machine —
    the `DtState` ordinal, the last selected `AgentAction` (incl. its embedded `PassRequest`/`ShotRequest`),
    and the §3.7.2 `_hasDispatchedAction` flag. `_matchSeed` is excluded (boot-deterministic — set at
    construction / `SetMatchSeed`, constant per match; the §2.6 boot-reconstruct exclusion) and the per-tick
    `_optionBuffer` is excluded (overwritten by OptionGenerator before its first read each tick — scratch,
    not cross-tick). Focused round-trip tests landed in `DecisionTreeStateTests`: (a) a populated state
    survives a `CanonicalSerializer` write/read round-trip byte-for-byte (the field-order serialization the
    D-snapshot extension will reuse), (b) `Capture → Restore → Capture` is the identity on the cross-tick
    field set, (c) a fresh instance captures IDLE/undispatched, and (d) a reflection field-count lock (9
    instance fields) trips first if cross-tick state is added without extending the DTO + seam (the §2.6
    silent-omission guard, B0 BufferSize / C0 field-count analogue). `decision-tree-tests.asmdef` gained the
    `TacticalDirector.DeterministicSim` reference for `CanonicalSerializer` (parallel to the C0 test asmdefs).
    No change to the `ReceiveSnapshot` pipeline.
  - **D1 — AI-phase wiring (perception → decision → movement). ✅ IMPLEMENTED (June 22, 2026).**
    `RunAiPhase` (stride-gated) rebuilds a host-owned perception `SpatialHashGrid` from current agent
    positions, refreshes the per-tick `_hasPossession` input, then runs `PerceptionSystem.OnHeartbeat` (×22)
    → `DecisionTree.ReceiveSnapshot` (×22). Each per-agent `DecisionTree` (constructed with a shared
    `HostMovementController` adapter + this agent's Pass/Shot executor) dispatches a `MovementCommand` into
    the held `_commands` buffer (consumed by the Physics phase the same tick) or a PASS/SHOOT into its
    executor (advanced in Resolve). The Stage-0 static §2.5 AI input snapshots (`PerceptionAgentAttributes`
    neutral + real TeamId; `DtAgentAttributes.CreateDefault`; `TacticalContext.Stage0Default` with the
    kickoff slot) are assembled once at boot (`InitializeAiSnapshots`); only `_hasPossession` + the grid are
    per-tick. The DecisionTree `EventBusRegistrar` is booted (idempotent) so `DecisionMadeEvent` (Tier C,
    excluded from the digest) can publish from the AI phase. `pressureScalar` is taken from
    `FilteredView.PressureScalar`; the heartbeat index is `MatchClock.CurrentTacticalTick` (exact on stride
    ticks). Snapshot schema UNCHANGED — DT/perception cross-tick state serialization is the D4 step (below).
    The B2 `TestOnly_SetCommand` injection is superseded by AI ownership; `MatchEnginePhysicsTests.Outfield-
    Agent_MovesTowardTarget...` was replaced by `AiPhase_DrivesChain_GoalkeepersSkipped` (chain runs every
    stride tick without throwing; GKs byte-exact). AI-driven determinism is covered by the same file's
    live-dynamics two-run digest test. **NOTE (D4 follow-up):** perception's internal
    `RecognitionLatencyTracker` / `ShoulderCheckScheduler` / ball-prev arrays AND the per-agent `DecisionTree`
    state machine are now cross-tick state NOT yet in the snapshot — same-seed-in-process determinism holds,
    but save/restore replay needs get/restore seams + serialization (the DT seam exists from D0; the
    perception seams do not yet — fold both into D4). A "≥1 outfielder moves at kickoff" assertion was
    deliberately NOT made: at kickoff the loose ball is ~26 m from the nearest agents, likely outside
    PRESS/INTERCEPT range, so the DT may hold every outfielder at its formation slot — real off-ball motion
    arrives with Positioning AI slots at D2 and the Phase F closed-loop scenario.
  - **D2 — mechanics-AI wiring.** `PositioningAITick` / `PressingAITick` / `DefensiveAITick` /
    `AttackingAITick` feeding tactical intent into the decision context (per the 4 Mechanics-layer specs).
    - **D2a — Positioning AI (#12). ✅ IMPLEMENTED (June 22, 2026).** One `PositioningAITick` INSTANCE +
      reused `PositioningPerceptionSnapshot` per team (`_positioning[2]` / `_posSnapshots[2]`), seeded at
      boot from the shared `STAGE0_FORMATION` (F442). `RunAiPhase` now runs `RunPositioningAI(heartbeat)`
      BEFORE the perception/DecisionTree loop: it fills each team's snapshot from world state, ticks #12
      with `ContextModifierInputs` (score 0, real team-mean fatigue from `AerobicPool`, `STAGE0_TACTICAL_INTENSITY`),
      and folds `GetFormationSlot(entityId)` back into each agent's `TacticalContext` via
      `TacticalContext.Stage0Default(worldSlot)` — the DT `MOVE_TO_POSITION` / HOLD anchor (§3.1.7), so
      agents settle into formation shape instead of holding the kickoff scaffold line (the D1 note's deferred
      "real off-ball motion arrives with Positioning AI slots at D2" payoff). **Home/away guard:** the #12
      formation table is authored attack-toward-+X, so the away team's world state (ball + agent positions +
      longitudinal ball velocity) is mapped into that canonical frame and the resulting slot mapped back
      (`MirrorPitchIfAway`, a self-inverse 180° pitch rotation) — the ERR-008-002 asymmetry guard applied at
      the mechanics layer (with the ball on the centre spot the away GK slot is the exact pitch-mirror of the
      home GK slot, locked by `MatchEngineMechanicsTests`). New constants `MaxEntityId` ([DERIVED]),
      `STAGE0_FORMATION` / `STAGE0_TACTICAL_INTENSITY` ([GT]); `match-engine` asmdef gains
      `TacticalDirector.PositioningAI`. Snapshot schema UNCHANGED — `_tacticalContexts` is fully recomputed
      from world state each AI tick before the DT reads it (scratch, not cross-tick), but the per-team
      `PositioningAITick` hysteresis is cross-tick state NOT yet serialized (same class as the D1
      perception / DecisionTree internal state — fold the get/restore seam into D4). *Tests
      (`MatchEngineMechanicsTests.cs`): formation slots feed the decision context (home defender deep /
      striker advanced); away-team slots mirror the home team (exact GK pitch-mirror — ERR-008-002 guard);
      same-seed determinism of the slot output.* New helpers `RunPositioningAI` / `FillPositioningSnapshot` /
      `ComputeTeamMeanFatigue` / `MirrorPitchIfAway` + `TestOnly_FormationSlot`.
    - **D2b — Pressing (#13) / Defensive (#14) / Attacking (#15). ✅ IMPLEMENTED (June 26, 2026).**
      `RunPositioningAI` → `RunMechanicsAI`: per team it now ticks the full Positioning→Pressing→Defensive→
      Attacking chain in dependency order (Pressing's per-agent `PressRole` is read back via `GetAssignment`
      into the Defensive snapshot) and then folds the Stage-0 carriers into each agent's `TacticalContext`:
      Defensive `MarkDirective.OffensiveLineDepth` → `DefensiveLineDepth` + `HasMarkDirective` (raised only
      for the team WITHOUT the ball — the Stage-1 `MarkDirective?` = null shape for attackers)
      (ERR-014-001); a committed Attacking run (`AttackIntent.RunParameters.HasValue`) → `HasAttackIntent`
      (ERR-015-002). Pressing's `PressDirective` has no Stage-0 `TacticalContext` carrier (`PressingMode` is a
      static team tactic) — it runs only to feed `PressRole` to Defensive. One INSTANCE + reused 22-agent
      snapshot per team (`_pressing`/`_pressSnapshots`/`_passRings`, `_defensive`/`_defSnapshots`,
      `_attacking`/`_attackSnapshots`); Pressing + Attacking take the `PositioningAIView` facade over the
      team's Positioning instance and Attacking a Stage-0 balanced `StyleProfile`. **Home/away guard:** each
      snapshot carries all 22 agents discriminated by `TeamId`, mapped into the acting team's canonical
      attack-toward-+X frame — positions via `MirrorPitchIfAway`, velocities/facing via the new free-vector
      `MirrorVelocityIfAway` (180° rotation negates both planar components, no PITCH offset); the consumed
      `OffensiveLineDepth` is a frame-invariant [0,1] depth so no inverse map is needed. New constants
      `STAGE0_PASS_EVENT_RING_CAPACITY` / `STAGE0_DEFENSIVE_LINE_DEPTH` / `STAGE0_NEUTRAL_NORMALIZED` ([GT]);
      `match-engine` asmdef gains `TacticalDirector.PressingAI` / `DefensiveAI` / `AttackingAI`. Snapshot
      schema UNCHANGED — the per-team tick hysteresis is cross-tick state NOT yet serialized (same class as
      D1/D2a; fold the get/restore seams into D4). *Tests (`MatchEngineMechanicsTests.cs`): the Defensive
      line-depth + `HasMarkDirective` carriers reach the decision context; all three carriers are byte-stable
      across two same-seed runs.* New helpers `RunMechanicsAI` / `FillPressingSnapshot` / `FillDefensive-
      Snapshot` / `FillAttackingSnapshot` / `CanonicalAttackDir` / `MirrorVelocityIfAway` / `HasActiveAttack-
      Intent` + `TestOnly_DefensiveLineDepth` / `TestOnly_HasMarkDirective` / `TestOnly_HasAttackIntent`. D4–D5
      pending.
  - **D3 — first-touch. ✅ IMPLEMENTED (June 22, 2026).** `RunFirstTouch` runs each Resolve, AFTER the
    executor `Update` (C3) and BEFORE `UpdateMatchContext` (C4): a loose, ground-level (`z − RADIUS ≤
    GroundControlHeight`), moving ball arriving within `FIRST_TOUCH_ACCEPTANCE_RADIUS_M` (1.0 m) of the
    nearest **approaching** agent triggers a touch. The trigger is physically grounded — not an AI
    receiver-decision carrier (none exists at Stage 0). The "approaching" gate (`ballVel · (agentPos −
    ballPos) > 0`) is what makes the chain correct: it excludes the agent the ball just departed after a
    kick (its dot is negative), so a kicker never re-touches the ball it played, and it excludes a resting
    ball (zero-dot). The host assembles the ~20-field `FirstTouchContext` via `BuildFirstTouchContext`: a
    real `PressureEvaluator` pass over the opposing team (pre-allocated `_opponentScratch` Vector2 buffer,
    zero alloc — `NearestOpponentDistance` is `+inf` only if the span is empty, which it never is here) +
    an `OrientationDetector.IsHalfTurnOriented` pass; Technique / FirstTouch are the ERR-007 neutral
    placeholders. `EvaluateFirstTouch` → `ApplyTouchResult` writes the displaced ball via the new
    `FirstTouchWorldAdapter` (its `IBallPhysicsSystem.SetBallState` writes `_ball`; its
    `IAgentMovementSystem.SetDribblingState` is a Stage-0 no-op — AgentState has no dribbling modifier
    yet), and the host maps the outcome onto authoritative possession: CONTROLLED → toucher, INTERCEPTION →
    `InterceptingAgentID` (`AGENT_ID_NONE` at Stage 0 per the ERR-004-002 spec gap → released to loose,
    ball redirected toward the opponent per §3.4.5 to be re-received later), LOOSE_BALL / DEFLECTION → loose.
    **Host grants:** first-touch's `AssemblyInfo` adds `InternalsVisibleTo("TacticalDirector.MatchEngine")`
    so the composition root can call the internal `PressureEvaluator` / `OrientationDetector` seams (the
    design-note producer path) rather than duplicating the §3.5 / §3.6 formulas; `match-engine` asmdef gains
    `TacticalDirector.FirstTouch`. **Snapshot schema UNCHANGED** — `FirstTouchSystem` is stateless; it
    writes only `_ball` (serialized) and `_possessingAgentId` (serialized via `MatchContext.PossessingAgentId`),
    so no D4-class cross-tick state is introduced. New constants `FIRST_TOUCH_ACCEPTANCE_RADIUS_M` /
    `FIRST_TOUCH_MIN_BALL_SPEED_M_S` ([GT]). *Tests (`MatchEngineFirstTouchTests.cs`): a loose slow ball
    arriving at an unpressured agent is CONTROLLED → possession (home + away, proving first-touch is
    frame-agnostic — the away-team symmetry leg); receding / above-control-height / already-possessed balls
    are not touched; a scripted receive is byte-identical across two same-seed runs.* The 7th/8th
    executor-family adapters (`FirstTouchWorldAdapter` implementing both first-touch boundaries) land here.
  - **D4 — snapshot extension + schema bump. ✅ IMPLEMENTED (June 27, 2026).** `SerializeWorldState` now
    writes the per-agent D0 `DecisionTreeState` (×22) via the new `WriteDecisionTreeState` helper (mirrors
    the `DecisionTreeStateTests` round-trip field order — `DtState` ordinal + dispatched-action flag + the
    last `AgentAction` incl. its embedded Pass/Shot request blocks), captured through the D0 `CaptureState`
    seam in the per-agent loop right after the C5 executor state. `SNAPSHOT_SCHEMA_VERSION` 2 → 3 with a v3
    doc paragraph; `MatchEngineSnapshotSchemaTests` pin 2 → 3 + a new `DecisionTreeState_FeedsSnapshotDigest`
    probe (inject EXECUTING via the new `TestOnly_SetDecisionTreeState` seam; the first tick is not an AI
    stride so the injected state passes through to the snapshot unchanged — a clean single-field probe).
    **Per-field exclusion proofs recorded:** `_perfs` stays excluded — the PHASE-D flag has NOT fired (the AI
    phase still leaves it at the boot-neutral constant); the perception internal state
    (RecognitionLatency / ShoulderCheck / ball-prev) and the per-team Pressing/Defensive/Attacking
    hysteresis remain excluded because none expose a get/restore seam yet — same-seed in-process determinism
    is unaffected, only save/restore replay needs them, so their seams + serialization (and the next schema
    bump) are a follow-up extension. New: `WriteDecisionTreeState` / `TestOnly_SetDecisionTreeState`. `Match-
    Engine.cs` v1.10, `MatchEngineConstants.cs` v1.10, `MatchEngineSnapshotSchemaTests.cs` v1.1.
    **D4 continuation (June 27, 2026, same day):** the per-team Positioning AI (#12) `HysteresisState` is now
    also serialized via a new `PositioningAITick.CaptureState` seam (`WritePositioningHysteresis`, ×`TEAM_COUNT`
    — team phase + dwell + per-agent line/lane membership); `SNAPSHOT_SCHEMA_VERSION` 3 → 4; Positioning
    dropped from the exclusion list. New `TestOnly_PositioningState` seam + `PositioningHysteresis_FeedsSnapshot-
    Digest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PositioningAI`. `PositioningAITick.cs`
    v1.1, `MatchEngine.cs` v1.11, `MatchEngineConstants.cs` v1.11, `MatchEngineSnapshotSchemaTests.cs` v1.2.
    **D4 continuation 2 (June 27, 2026, same day):** the per-team Pressing AI (#13) cross-tick state is now
    also serialized via a new `PressingAITick.CaptureState` seam returning a new `PressingTickState` view
    (`WritePressingTickState`, ×`TEAM_COUNT` — trigger debounce counters + disengage/cooldown dwell + per-agent
    role hysteresis + accumulated press fatigue); `SNAPSHOT_SCHEMA_VERSION` 4 → 5; Pressing dropped from the
    exclusion list. New `PressingTickState.cs`, `TestOnly_PressingState` seam + `PressingState_FeedsSnapshot-
    Digest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PressingAI`. `PressingAITick.cs` v1.3,
    `MatchEngine.cs` v1.12, `MatchEngineConstants.cs` v1.12, `MatchEngineSnapshotSchemaTests.cs` v1.3.
    **D4 continuation 3 (June 27, 2026, same day):** the per-team Defensive AI (#14) and Attacking AI (#15)
    cross-tick state are now also serialized via new `DefensiveAITick.CaptureState` / `AttackingAITick.Capture-
    State` seams returning new `DefensiveTickState` / `AttackingTickState` views. `WriteDefensiveTickState`
    serializes per-team offside-line state + per-agent mark hysteresis + last committed assignment;
    `WriteAttackingTickState` serializes transition-hold state + frozen in-possession directive + per-agent
    role hysteresis (each ×`TEAM_COUNT`). `SNAPSHOT_SCHEMA_VERSION` 5 → 7 (v6 Defensive, v7 Attacking). New
    `DefensiveTickState.cs` / `AttackingTickState.cs`, `TestOnly_DefensiveState` / `TestOnly_AttackingState`
    seams + `DefensiveState_` / `AttackingState_FeedsSnapshotDigest` probes; `match-engine-tests` asmdef gains
    `TacticalDirector.DefensiveAI` + `TacticalDirector.AttackingAI`. `DefensiveAITick.cs` v1.3,
    `AttackingAITick.cs` v1.3, `MatchEngine.cs` v1.13, `MatchEngineConstants.cs` v1.13,
    `MatchEngineSnapshotSchemaTests.cs` v1.4.
    **D4 continuation 4 — final cross-tick surface (June 27, 2026, same day):** Perception (#7) internal state is
    now serialized via new `CaptureState` seams on `PerceptionSystem` + its two helpers
    (`RecognitionLatencyTracker.CaptureState` → `RecognitionLatencyState`; `ShoulderCheckScheduler.CaptureState`
    → `ShoulderCheckState`; `PerceptionSystem.CaptureState` → `PerceptionTickState` bundling both + the per-agent
    ball-perception carry-over). `WritePerceptionTickState` serializes the recognition-latency pair arrays, the
    shoulder-check per-agent + per-pair arrays, and the ball-prev arrays (one shared instance, not per team);
    `SNAPSHOT_SCHEMA_VERSION` 7 → 8. **Cross-tick coverage is now complete** — every cross-tick gameplay surface
    (ball, agents incl. OscillationGuard, executors, DecisionTree, all four mechanics-AI hysteresis, perception)
    is serialized; the only un-serialized fields are the boot-deterministic constants (`_attrs`/`_perfs`) and
    tick-derivable observation counters. New `RecognitionLatencyState.cs` / `ShoulderCheckState.cs` /
    `PerceptionTickState.cs`, `TestOnly_PerceptionState` seam + `PerceptionState_FeedsSnapshotDigest` probe;
    `match-engine-tests` asmdef gains `TacticalDirector.PerceptionSystem`. `RecognitionLatencyTracker.cs` v1.4,
    `ShoulderCheckScheduler.cs` v1.3, `PerceptionSystem.cs` v1.5, `MatchEngine.cs` v1.14,
    `MatchEngineConstants.cs` v1.14, `MatchEngineSnapshotSchemaTests.cs` v1.5.
  - **D5 — design-note reconciliation. ✅ COMPLETE (June 27, 2026).** All D4 cross-tick seams landed
    (mechanics-AI #12–#15 + perception #7 + DecisionTree D0), so the snapshot now covers every cross-tick
    gameplay surface (`SNAPSHOT_SCHEMA_VERSION` 8). **Phase D is complete.** Remaining: Phase F (capstone
    closed-loop scenario on the #19 ScenarioRunner + FR-PO-052 perf gate) — Phase E landed June 27, 2026.
  *Tests: a ball carrier decides PASS/SHOOT/DRIBBLE and the dispatcher drives movement; a scripted receive
  runs first-touch to a CONTROLLED outcome; away-team symmetry (closes the deferred Decision Tree away-team
  scenario).*
- **Phase E — Events phase consumers. ✅ IMPLEMENTED (June 27, 2026).** Subscribe real
  cross-subsystem consumers; confirm Tier A/B ledger digest stability. PRODUCER: the host diffs the
  settled possession holder once per Resolve (after C4 `UpdateMatchContext`) against a new
  `_prevPossessingAgentId` field and, on a net change, publishes a Tier A `PossessionChangedEvent`
  (#17 ordinal 0x04, producer phase Resolve) into the digest-load-bearing ledger
  (`PublishPossessionChangeIfChanged`). CONSUMER: `Boot` subscribes `OnPossessionChanged`
  (`EventBus.Subscribe` — MUST be in the boot phase per #17 FR-EVT-020/021), which calls
  `DecisionTree.NotifyInterrupt()` on the NEW holder so it re-plans on its next AI stride
  (EXECUTING → INTERRUPTED → EVALUATING; a safe no-op otherwise — the existing §3.7.2 interrupt path is
  reused, no new DecisionTree seam). The previous holder is not interrupted (its in-flight pass executor
  self-cancels via the Pass #5 FM-08 possession recheck). **Reset seam (closes Risk #4):** because the
  EventBus is a process-static singleton (#17 §3.2.1), `Boot` first calls the new public
  `EventBus.ResetForNewMatch()` — it clears the Tier A/B subscriber `Dispatchers` table + the Tier C
  channel and reopens the boot phase (leaving the `EventRegistry` row schema intact, so the idempotent
  registrar `Initialize()` calls stay correct), so a second match (and the two same-seed determinism
  runs) can re-subscribe without `ERR_EVT_REGISTRATION_PHASE` or handler-table leakage. No
  `SNAPSHOT_SCHEMA_VERSION` bump (the world-state body is unchanged) — only the serialized ledger digest
  now carries the event. Collision/foul real consumers stay deferred (no Stage-0 card/foul model;
  `NullCollisionEventConsumer` retained). *Tests (`tests/MatchEngineEventsTests.cs`): publish-on-change
  interrupts only the new holder; no-change publishes nothing; two same-seed runs with a transition
  produce byte-identical (ledger-backed) digest chains (also locks the reset seam across the two
  engines); transition-vs-baseline effect; Tier A boot-phase Subscribe guard.* Files: `MatchEngine.cs`
  v1.15, `MatchEngineConstants.cs` (POSSESSION_CHANGE_REASON_UNSPECIFIED), `EventBus.cs` v2.1
  (ResetForNewMatch), `match-engine-tests.asmdef` (+EventSystem ref).
- **Phase F — Capstone. ✅ IMPLEMENTED (June 28, 2026).** A cross-spec closed-loop scenario on
  the **#19 `ScenarioRunner`** drives a multi-second kickoff sequence (600 ticks = 10 s @ 60 Hz)
  through the FULLY composed host. New `src/match-engine/tests/MatchEngineCapstoneScenarios.cs`
  (`match-engine-kickoff-multi-second`, path under `SCENARIO_PATH_CROSS_SPEC_PREFIX`, owning specs
  `{1,2,3,4,5,6,7,8,12,13,14,15,16,17,19}`, Tier B) boots a real `MatchEngine`, ticks it through
  all 7 phases, and records **(a) gameplay-invariant envelope predicates** — `tick-count` (600),
  `ai-stride-cadence` (exactly `NumTicks / AI_PHASE_STRIDE` = 100 AI strides, locking the
  10 Hz/60 Hz loop separation), `ball-stays-in-bounds` + `agents-stay-in-bounds` (finite and on
  the pitch envelope every tick — catches NaN/divergence of the composed Physics/Resolve/AI loop),
  and `digest-chain-advances` (the chained snapshot digest changes every tick) — and **(b) a
  pinned determinism digest across two runs**: the body runs the engine twice with the same seed
  and asserts the per-tick `CurrentSnapshotDigest` chains are byte-identical (also re-locks
  `EventBus.ResetForNewMatch()` across two in-process matches — Risk #4). `MatchEngineCapstoneTests.cs`
  runs the scenario through `ScenarioRunner.Run` (→ `Passed`), adds a direct two-run digest-chain
  equality test, and **activates the FR-PO-052 per-tick perf gate**: a real per-tick measurement of
  the kickoff loop flows through `PerfGateRunner.Run` (#18 `RegressionGate`) against a generous
  Stage-0 anchor `BaselineRecord` (loop `PhysicsSixtyHz`, `thresholdCited` FR-PO-052). The Linux
  dotnet gate is NON-certifying (`certification-platform.md` v1.2) — this proves the perf-gate
  WIRING; the authoritative per-tick budget stays on the pinned Windows/Unity tuple. No production
  `MatchEngine.cs` change: the scenario reads world state through the existing internal `TestOnly_*`
  seams plus the public `CurrentTick` / `AiPhaseRunCount` / `CurrentSnapshotDigest`. Files:
  `MatchEngineCapstoneScenarios.cs` v1.0, `MatchEngineCapstoneTests.cs` v1.0,
  `match-engine-tests.asmdef` (+TestingStrategy, +PerformanceOptimization refs). **Match Engine
  integration (Phases A–F) is complete.**

---

## 6. Risks and open questions

1. **Snapshot payload schema is digest-load-bearing** — decide the field set + order and
   `SNAPSHOT_SCHEMA_VERSION` up front (before Phase B), or later changes force schema bumps.
2. **No governing spec** — this note is the mitigation; keep it current as the engine lands.
3. **Snapshot-assembly seams are net-new** and untested in composition — exactly the
   "passes in isolation, breaks when composed" defect class the AR history keeps surfacing.
   The Phase F closed-loop run is the primary mitigation; assemble-and-run smoke coverage
   should land with each AI subsystem in Phase D.
4. **EventBus boot idempotency — RESOLVED (Phase E, June 27, 2026).** The registrars carry
   per-registrar `s_registered` guards, but subscriptions had no reset path, so the process-static
   EventBus (#17 §3.2.1) could not be re-subscribed by a second match (it would throw
   `ERR_EVT_REGISTRATION_PHASE` after the first match's first `DrainTick`) and leaked handlers toward
   `MaxHandlersPerEventType`. Phase E adds the public `EventBus.ResetForNewMatch()` (clears subscribers
   + reopens the boot phase, leaves the row schema), called at the start of `Boot`. The `#16`
   `ReplayEngine` step 6 RNG-state reset remains a separate Stage-0 stub, but the EventBus side of the
   replay/second-match reset is now production.
5. **Per-agent system fan-out and per-agent state** (PassExecutor/ShotExecutor/DecisionTree
   ×22) — `DecisionTree` holds per-agent state-machine state that persists between
   heartbeats (EXECUTING holds mid-pass-windup), and the executors hold WINDUP/CONTACT
   state. **Before Phase D, verify** whether each is a per-agent instance (22 independent
   objects) or a shared evaluator backed by per-agent state arrays; the choice determines
   both the zero-alloc construction strategy and exactly which fields §2.6 must serialize.
6. **MatchContext authorship** — the host owns and updates `MatchContext` each AI tick
   (score, possession, ball, zone); possession transitions are produced in Resolve and read
   by the next AI tick. Pin the write/read ordering to avoid a one-tick staleness ambiguity.
   The host MUST author `MatchContext.BallZone` from the home-team perspective only — per
   the Decision Tree AR-2 fix, the `DecisionContextAssembler` derives the team-relative zone
   downstream; re-deriving it host-side would reintroduce ERR-008-002.
7. **Snapshot serialization is write-only at B3 — no symmetric reader yet.** `SerializeWorldState`
   writes the canonical field order, but there is no `ReadWorldState` (deserialize/restore is
   ReplayEngine / Phase C–D territory per the §2.6 seam dependency). Consequence: the field *order*
   is currently verified only by relative-digest equality + the per-field B0 / `CanonicalSerializer`
   round-trips, not end-to-end. **When the restore reader lands, it MUST mirror the exact write
   order in `SerializeWorldState` field-for-field** (ball → per-agent `AgentState` incl. the guard
   via `RestoreState` → team/GK flags → collision inputs → held command), and a payload round-trip
   test must lock the two in sync. (Recorded from the Phase B full-surface AR — accepted, not a B3
   defect.)
8. **World-state floats are written Tier A without non-finite enforcement.** `SerializeWorldState`
   uses `CanonicalSerializer.WriteF32` (Tier A), which normalizes `-0.0` but does **not** enforce
   the "Tier A NaN/±Inf is a hard error" contract — it writes the raw bits. Determinism still holds
   (identical bits across same-seed runs), and the match engine relies on upstream sanitisation
   (`BallPhysicsCore.ValidatePhysicsState`, `AgentSafetySystem`) to keep non-finite values out of
   world state. Note: the `OscillationGuard` empty-slot sentinel is `float.NegativeInfinity` by
   design and flows through `WriteF32` intentionally (it round-trips). If a future field set admits
   a non-finite value that upstream does *not* guard, add an explicit gate at the serialization
   boundary. (Pre-existing `CanonicalSerializer` behaviour; recorded from the Phase B AR — accepted.)
9. **Two constants named `SNAPSHOT_SCHEMA_VERSION` — confusion hazard.**
   `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` versions the world-state *body* written into the
   payload; `DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION` versions the #16 `SnapshotHeader` /
   codec *framing* that wraps it. They evolve independently. The collision is mitigated by
   cross-referencing doc comments on both constants (and the §2.6 / B3 text), and the match-engine
   name is mandated by this design note. A maintainer editing one MUST NOT assume it covers the
   other. (Recorded from the Phase B AR — accepted, no rename.)

---

## 7. Verification strategy

- Every phase passes the Linux compile/test gate; new tests are non-quarantined.
- Determinism is the spine: two same-seed runs must produce byte-identical snapshot digest
  chains at every phase from A onward.
- The capstone (Phase F) runs through the #19 `ScenarioRunner` so the match engine inherits
  the closed-loop harness the rest of the project already uses, including cross-spec
  ownership tagging and envelope predicates.

---

## Version History

| Version | Date       | Author | Notes                                  |
|---------|------------|--------|----------------------------------------|
| 1.0     | 2026-06-28 | —      | **Phase F implemented — capstone closed-loop scenario; Match Engine integration (A–F) complete.** New `src/match-engine/tests/MatchEngineCapstoneScenarios.cs` registers `match-engine-kickoff-multi-second` (path under `SCENARIO_PATH_CROSS_SPEC_PREFIX`, owning specs `{1,2,3,4,5,6,7,8,12,13,14,15,16,17,19}`, Tier B) on the #19 `ScenarioRunner`: boots a real `MatchEngine` and ticks it 600× (10 s @ 60 Hz), recording **(a) gameplay-invariant envelope predicates** — `tick-count` (600), `ai-stride-cadence` (`NumTicks / AI_PHASE_STRIDE` = 100, locking the 10 Hz/60 Hz loop separation), `ball-stays-in-bounds` + `agents-stay-in-bounds` (finite + on-pitch every tick — NaN/divergence guard over the composed Physics/Resolve/AI loop), `digest-chain-advances` (chained snapshot digest changes every tick) — and **(b) a two-run same-seed determinism digest match** (runs the engine twice and asserts byte-identical per-tick `CurrentSnapshotDigest` chains; also re-locks `EventBus.ResetForNewMatch()` across two in-process matches, Risk #4). `MatchEngineCapstoneTests.cs` runs the scenario through `ScenarioRunner.Run` (→ `Passed`), adds a direct two-run digest-chain equality test, and **activates the FR-PO-052 per-tick perf gate**: a real per-tick measurement flows through `PerfGateRunner.Run` (#18 `RegressionGate`) against a generous Stage-0 anchor `BaselineRecord` (loop `PhysicsSixtyHz`, `thresholdCited` FR-PO-052). The Linux dotnet gate is NON-certifying (`certification-platform.md` v1.2) — this proves the perf-gate WIRING; the authoritative per-tick budget stays on the pinned Windows/Unity tuple. No production `MatchEngine.cs` change — the scenario reads world state through the existing internal `TestOnly_*` seams + the public `CurrentTick`/`AiPhaseRunCount`/`CurrentSnapshotDigest`. `match-engine-tests.asmdef` gains `TacticalDirector.TestingStrategy` + `TacticalDirector.PerformanceOptimization`. New files: `MatchEngineCapstoneScenarios.cs` v1.0, `MatchEngineCapstoneTests.cs` v1.0. (Linux compile/test gate not runnable locally — no .NET SDK in this environment; runs in CI on push.) |
| 0.9.13  | 2026-06-27 | —      | **Phase E implemented — events-phase consumers.** PRODUCER: `MatchEngine.RunResolvePhase` now calls `PublishPossessionChangeIfChanged` after C4 `UpdateMatchContext` — it diffs the settled holder against the new `_prevPossessingAgentId` field and on a net change publishes a Tier A `PossessionChangedEvent` (#17 ordinal 0x04, producer phase Resolve) into the digest-load-bearing ledger (intra-tick flicker that ends on the same holder emits nothing). CONSUMER: `Boot` subscribes `OnPossessionChanged` (Tier A Subscribe in the boot phase per #17 FR-EVT-020/021), which `NotifyInterrupt()`s the NEW holder's DecisionTree so it re-plans next AI stride (EXECUTING→INTERRUPTED→EVALUATING; safe no-op otherwise — reuses the §3.7.2 interrupt path, no new DT seam); the previous holder self-cancels via Pass #5 FM-08. **Reset seam (closes Risk #4):** new public `EventBus.ResetForNewMatch()` clears the Tier A/B subscriber `Dispatchers` table + Tier C channel and reopens the boot phase (leaves the `EventRegistry` row schema intact, so registrar `Initialize()` stays idempotent), called at the start of `Boot` — without it the process-static bus (#17 §3.2.1) would throw `ERR_EVT_REGISTRATION_PHASE` on a second match's Subscribe and leak handlers toward `MaxHandlersPerEventType` (the determinism tests build two engines per process). No `SNAPSHOT_SCHEMA_VERSION` bump (world-state body unchanged) — only the serialized ledger digest now carries the event. Collision/foul real consumers stay deferred (no Stage-0 card/foul model; `NullCollisionEventConsumer` retained). New `POSSESSION_CHANGE_REASON_UNSPECIFIED` constant + `TestOnly_DtState` seam; new `MatchEngineEventsTests.cs` (publish-on-change interrupts only the new holder; no-change publishes nothing; two same-seed runs with a transition give byte-identical ledger-backed digest chains + reset-seam lock; transition-vs-baseline effect; Tier A boot-phase Subscribe guard). `EventBus.cs` v2.1, `MatchEngine.cs` v1.15, `MatchEngineConstants.cs` v1.15, `match-engine-tests.asmdef` (+`TacticalDirector.EventSystem`). Phase F pending. |
| 0.9.12  | 2026-06-27 | —      | **Phase D D4 final cross-tick surface — Perception (#7) serialized; Phase D flipped COMPLETE (D5).** New `CaptureState` seams: `RecognitionLatencyTracker` → `RecognitionLatencyState`, `ShoulderCheckScheduler` → `ShoulderCheckState`, `PerceptionSystem` → `PerceptionTickState` (bundles both + per-agent ball-perception carry-over). `MatchEngine.SerializeWorldState` writes it via new `WritePerceptionTickState` (recognition-latency pair arrays + shoulder-check per-agent/per-pair arrays + ball-prev; one shared instance); `SNAPSHOT_SCHEMA_VERSION` 7 → 8. **Cross-tick coverage complete** — every cross-tick gameplay surface serialized; only boot-deterministic constants + observation counters excluded. D5 reconciliation: Phase D complete; Phases E + F pending. New `TestOnly_PerceptionState` seam + `PerceptionState_FeedsSnapshotDigest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PerceptionSystem`. New `RecognitionLatencyState.cs` / `ShoulderCheckState.cs` / `PerceptionTickState.cs` (all v1.0); `RecognitionLatencyTracker.cs` v1.4, `ShoulderCheckScheduler.cs` v1.3, `PerceptionSystem.cs` v1.5, `MatchEngine.cs` v1.14, `MatchEngineConstants.cs` v1.14, `MatchEngineSnapshotSchemaTests.cs` v1.5. |
| 0.9.11  | 2026-06-27 | —      | **Phase D D4 continuation 3 — per-team Defensive AI (#14) + Attacking AI (#15) cross-tick state serialized.** New `DefensiveAITick.CaptureState` / `AttackingAITick.CaptureState` seams return `DefensiveTickState` / `AttackingTickState` views bundling the live cross-tick state. `MatchEngine.SerializeWorldState` writes each per team via new `WriteDefensiveTickState` (offside-line + per-agent mark hysteresis + last committed assignment) / `WriteAttackingTickState` (transition-hold + frozen in-possession directive + per-agent role hysteresis); `SNAPSHOT_SCHEMA_VERSION` 5 → 7 (v6 Defensive, v7 Attacking). All four mechanics-AI hysteresis surfaces now serialized; perception internal state is the only remaining exclusion before D5. New `TestOnly_DefensiveState` / `TestOnly_AttackingState` seams + two digest probes. `match-engine-tests` asmdef gains `TacticalDirector.DefensiveAI` + `TacticalDirector.AttackingAI`. `DefensiveTickState.cs` v1.0 (new), `AttackingTickState.cs` v1.0 (new), `DefensiveAITick.cs` v1.3, `AttackingAITick.cs` v1.3, `MatchEngine.cs` v1.13, `MatchEngineConstants.cs` v1.13, `MatchEngineSnapshotSchemaTests.cs` v1.4. D5 + E–F pending. |
| 0.9.10  | 2026-06-27 | —      | **Phase D D4 continuation 2 — per-team Pressing AI (#13) cross-tick state serialized.** New `PressingAITick.CaptureState` seam returns a new `PressingTickState` view bundling the live cross-tick state (role hysteresis, trigger debounce counters, disengage/cooldown dwell, accumulated press fatigue); `MatchEngine.SerializeWorldState` writes it per team via new `WritePressingTickState` (8 trigger counters + 2 dwell ints + per-EntityId role/dwell/fatigue, ×`TEAM_COUNT`); `SNAPSHOT_SCHEMA_VERSION` 4 → 5. Pressing dropped from the exclusion list; perception + Defensive/Attacking hysteresis still excluded (rest of the follow-up before D5). New `TestOnly_PressingState` seam + `PressingState_FeedsSnapshotDigest` probe. `match-engine-tests` asmdef gains `TacticalDirector.PressingAI`. `PressingTickState.cs` v1.0 (new), `PressingAITick.cs` v1.3, `MatchEngine.cs` v1.12, `MatchEngineConstants.cs` v1.12, `MatchEngineSnapshotSchemaTests.cs` v1.3. D5 + E–F pending. |
| 0.9.9   | 2026-06-27 | —      | **Phase D D4 continuation — per-team Positioning AI (#12) `HysteresisState` serialized.** New `PositioningAITick.CaptureState` read seam returns the live `HysteresisState`; `MatchEngine.SerializeWorldState` writes it per team via new `WritePositioningHysteresis` (team phase + dwell + per-agent line/lane membership, ×`TEAM_COUNT`); `SNAPSHOT_SCHEMA_VERSION` 3 → 4. Positioning dropped from the exclusion list; perception + Pressing/Defensive/Attacking hysteresis still excluded (no get/restore seam yet — rest of the follow-up before D5). New `TestOnly_PositioningState` seam + `PositioningHysteresis_FeedsSnapshotDigest` probe (first tick is not an AI stride, so injected dwell passes through to the snapshot). `match-engine-tests` asmdef gains `TacticalDirector.PositioningAI`. `PositioningAITick.cs` v1.1, `MatchEngine.cs` v1.11, `MatchEngineConstants.cs` v1.11, `MatchEngineSnapshotSchemaTests.cs` v1.2. D5 + E–F pending. |
| 0.9.8   | 2026-06-27 | —      | **Phase D step D4 implemented — snapshot extension + schema bump.** `SerializeWorldState` writes the per-agent D0 `DecisionTreeState` (×22) via the new `WriteDecisionTreeState` helper (mirrors the `DecisionTreeStateTests` round-trip order — `DtState` ordinal + dispatched-action flag + last `AgentAction` incl. embedded Pass/Shot request blocks), captured through the existing D0 `CaptureState` seam in the per-agent loop right after the C5 executor state. `SNAPSHOT_SCHEMA_VERSION` 2 → 3 (v3 doc paragraph). **Per-field exclusion proofs recorded:** `_perfs` excluded (PHASE-D flag not yet fired — AI phase still leaves it boot-neutral); perception internal state (RecognitionLatency / ShoulderCheck / ball-prev) + per-team Positioning/Pressing/Defensive/Attacking hysteresis excluded (no get/restore seam yet — same-seed in-process determinism unaffected; only save/restore replay needs them, so seams + serialization + the next schema bump are a follow-up extension). New `WriteDecisionTreeState` helper + `TestOnly_SetDecisionTreeState` seam; `MatchEngineSnapshotSchemaTests` pin 2 → 3 + `DecisionTreeState_FeedsSnapshotDigest` probe (first tick is not an AI stride, so injected EXECUTING passes through to the snapshot — single-field probe). `MatchEngine.cs` v1.10, `MatchEngineConstants.cs` v1.10, `MatchEngineSnapshotSchemaTests.cs` v1.1. D5 + E–F pending. |
| 0.9.7   | 2026-06-26 | —      | **Phase D step D2b implemented — Pressing #13 / Defensive #14 / Attacking #15 wiring.** `RunPositioningAI` → `RunMechanicsAI`: per team it ticks the full Positioning→Pressing→Defensive→Attacking chain in dependency order (Pressing's per-agent `PressRole` is read back via `GetAssignment` into the Defensive snapshot), then folds the Stage-0 carriers into each agent's `TacticalContext` — Defensive `MarkDirective.OffensiveLineDepth` → `DefensiveLineDepth` + `HasMarkDirective` (D2b AR L-1: raised only for the team WITHOUT the ball — the Stage-1 `MarkDirective?` = null shape for attackers) (ERR-014-001); a committed Attacking run (`AttackIntent.RunParameters.HasValue`) → `HasAttackIntent` (ERR-015-002). Pressing's `PressDirective` has no Stage-0 carrier (`PressingMode` is a static team tactic) — it runs only to feed `PressRole` to Defensive. One INSTANCE + reused 22-agent snapshot per team (`_pressing`/`_pressSnapshots`/`_passRings`, `_defensive`/`_defSnapshots`, `_attacking`/`_attackSnapshots`); Pressing + Attacking take a `PositioningAIView` facade over the team's Positioning instance, Attacking a balanced `StyleProfile`. **Home/away guard:** each snapshot carries all 22 agents discriminated by `TeamId`, mapped into the acting team's canonical attack-+X frame — positions via `MirrorPitchIfAway`, velocities/facing via the new free-vector `MirrorVelocityIfAway` (180° rotation negates both planar components, no PITCH offset); the consumed `OffensiveLineDepth` is a frame-invariant [0,1] depth so no inverse map is needed. New constants `STAGE0_PASS_EVENT_RING_CAPACITY` / `STAGE0_DEFENSIVE_LINE_DEPTH` / `STAGE0_NEUTRAL_NORMALIZED` ([GT]); `match-engine` asmdef gains `TacticalDirector.PressingAI` / `DefensiveAI` / `AttackingAI`. **Snapshot schema UNCHANGED** — the per-team tick hysteresis is cross-tick state NOT yet serialized (same class as D1/D2a; fold the get/restore seams into D4). New `MatchEngineMechanicsTests.cs` tests (Defensive line-depth + `HasMarkDirective` carriers reach the decision context; all three carriers byte-stable across two same-seed runs). New helpers `RunMechanicsAI` / `FillPressingSnapshot` / `FillDefensiveSnapshot` / `FillAttackingSnapshot` / `CanonicalAttackDir` / `MirrorVelocityIfAway` / `HasActiveAttackIntent` + `TestOnly_DefensiveLineDepth` / `TestOnly_HasMarkDirective` / `TestOnly_HasAttackIntent`. Carriers byte-stable across two same-seed runs; D2b AR added an `AwayTeamCarriers_MirrorHomeTeam` home↔away symmetry lock (L-2). `MatchEngine.cs` v1.9.1, `MatchEngineConstants.cs` v1.9, `MatchEngineMechanicsTests.cs` v1.2. D4–D5 + E–F pending. |
| 0.9.6   | 2026-06-22 | —      | **Phase D step D3 implemented — first-touch wiring.** `RunFirstTouch` runs each Resolve (after the C3 executor `Update`, before the C4 `UpdateMatchContext`): a loose (`_possessingAgentId == NO_POSSESSION`), ground-level (`z − RADIUS ≤ GroundControlHeight`), moving (`> FIRST_TOUCH_MIN_BALL_SPEED_M_S`) ball arriving within `FIRST_TOUCH_ACCEPTANCE_RADIUS_M` (1.0 m) of the nearest **approaching** agent (`ballVel · (agentPos − ballPos) > 0` — the gate that excludes the just-kicked owner and a resting ball) triggers a touch. `BuildFirstTouchContext` assembles the ~20-field `FirstTouchContext` via a real `PressureEvaluator` pass over the opposing team (pre-allocated `_opponentScratch`, zero alloc) + `OrientationDetector.IsHalfTurnOriented`, with ERR-007 neutral touch attributes. `EvaluateFirstTouch` → `ApplyTouchResult` (writes `_ball` through the new `FirstTouchWorldAdapter`; `SetDribblingState` is a Stage-0 no-op), and the host maps the outcome onto possession: CONTROLLED → toucher, INTERCEPTION → `InterceptingAgentID` (`AGENT_ID_NONE` per ERR-004-002 → loose), LOOSE_BALL / DEFLECTION → loose. first-touch `AssemblyInfo` grants `InternalsVisibleTo("TacticalDirector.MatchEngine")` (host calls the internal `PressureEvaluator` / `OrientationDetector` seams rather than duplicating §3.5 / §3.6); `match-engine` asmdef gains `TacticalDirector.FirstTouch`. New `FIRST_TOUCH_ACCEPTANCE_RADIUS_M` / `FIRST_TOUCH_MIN_BALL_SPEED_M_S` ([GT]). **Snapshot schema UNCHANGED** — `FirstTouchSystem` is stateless; writes only `_ball` (serialized) + `_possessingAgentId` (serialized via `MatchContext`). New `MatchEngineFirstTouchTests.cs` (CONTROLLED receive → possession, home + away frame-agnostic; receding / high / possessed not touched; same-seed digest determinism). `MatchEngine.cs` v1.8, `MatchEngineConstants.cs` v1.8, `first-touch/AssemblyInfo.cs` v1.1. D2b (#13/#14/#15) + D4–D5 + E–F pending. |
| 0.9.5   | 2026-06-22 | —      | **Phase D step D2a implemented — mechanics-AI wiring (Positioning AI #12).** One `PositioningAITick` INSTANCE + reused `PositioningPerceptionSnapshot` per team (`_positioning[2]` / `_posSnapshots[2]`), seeded at boot from `STAGE0_FORMATION` (F442). `RunAiPhase` runs `RunPositioningAI(heartbeat)` before the perception/DT loop: fills each team's snapshot from world state, ticks #12 with `ContextModifierInputs` (score 0, team-mean fatigue from `AerobicPool`, `STAGE0_TACTICAL_INTENSITY`), and folds `GetFormationSlot` back into each agent's `TacticalContext` (the DT `MOVE_TO_POSITION` / HOLD anchor) — so agents settle into formation shape, the deferred D1 off-ball-motion payoff. Home/away guard: the #12 table is authored attack-toward-+X, so the away team's world state is mapped into that canonical frame and the slot mapped back via the self-inverse 180° rotation `MirrorPitchIfAway` (ERR-008-002). New constants `MaxEntityId` ([DERIVED]) + `STAGE0_FORMATION` / `STAGE0_TACTICAL_INTENSITY` ([GT]); `match-engine` asmdef gains `TacticalDirector.PositioningAI`. Snapshot schema UNCHANGED (`_tacticalContexts` recomputed each AI tick = scratch; the per-team #12 hysteresis is cross-tick state deferred to the D4 get/restore seam, same class as the D1 perception/DT state). New `MatchEngineMechanicsTests.cs` (slots feed the decision context; away-team mirror; same-seed slot determinism). New helpers `RunPositioningAI` / `FillPositioningSnapshot` / `ComputeTeamMeanFatigue` / `MirrorPitchIfAway` + `TestOnly_FormationSlot`. `MatchEngine.cs` v1.7, `MatchEngineConstants.cs` v1.7. D2b (Pressing #13 / Defensive #14 / Attacking #15) + D3–D5 pending. |
| 0.9.4   | 2026-06-22 | —      | **Phase C steps C4/C5/C6 implemented — Phase C complete.** C4: new `MatchContext _matchContext` world-state field authored by `UpdateMatchContext()` at the end of Resolve (+ at boot) — folds `_possessingAgentId` into `PossessingAgentId`, derives `Possession` (loose→CONTESTED, else possessing team), ball position/velocity, and the home-perspective `BallZone` (`PitchGeometry.ComputeFieldZone(ballX)`; ERR-008-002 guard — team-relative zone derived downstream); Phase = OPEN_PLAY (the only phase OptionGenerator produces options for — KICK_OFF would no-op the Phase D AI). Boot now boots the Pass/Shot `EventBusRegistrar.Initialize()` sites (idempotent `s_registered` guards; `RegisterExternalRow` forces the seeded-row cctor) so a scripted pass reaches CONTACT + publishes. C5: `SerializeWorldState` adds the per-agent C0 `PassExecutorState`/`ShotExecutorState` capture + `MatchContext`; `SNAPSHOT_SCHEMA_VERSION` 1 → 2; new `WritePassExecutorState`/`WriteShotExecutorState`/`WriteMatchContext` helpers (mirror the C0 round-trip order); `_possessingAgentId` captured via `MatchContext.PossessingAgentId`. C6: Phase C header + status flipped complete. `match-engine` + `match-engine-tests` asmdefs gain the `TacticalDirector.DecisionTree` reference. New `MatchEngineMatchContextTests.cs` (ball-zone authoring, possession derivation, scripted pass reaches CONTACT + releases possession, same-seed determinism with a live CONTACT publish, C5 digest-preimage probes); `MatchEngineSnapshotSchemaTests` pin 1 → 2. `MatchEngine.cs` v1.5, `MatchEngineConstants.cs` v1.5. Phase D D1 unblocked. |
| 0.9.3   | 2026-06-22 | —      | **Phase D step D0 implemented** (DecisionTree snapshot get/restore seam — the gating sub-step, the C0 analogue deferred from Phase C). `DecisionTree.CaptureState()`/`RestoreState(in DecisionTreeState)` (v1.2), parallel to the Pass/Shot executor C0 seams and the B0 OscillationGuard seam. New DTO `DecisionTreeState` carries the cross-tick state machine (`DtState` ordinal + last `AgentAction` + `_hasDispatchedAction`); `_matchSeed` (boot-deterministic) and `_optionBuffer` (per-tick scratch) excluded per §2.6. Round-trip locks in `DecisionTreeStateTests` (CanonicalSerializer byte round-trip + Capture/Restore identity + fresh-IDLE default + reflection field-count guard); `decision-tree-tests.asmdef` gains the DeterministicSim reference. §5 Phase D expanded into ordered sub-steps D0–D5 (D1 AI-phase wiring requires C4's `MatchContext`). No execution-path change. `DecisionTree.cs` v1.2, `DecisionTreeState.cs` v1.0 (new), `DecisionTreeStateTests.cs` v1.0 (new). C4–C6 + D1–D5 + E–F pending. |
| 0.9.2   | 2026-06-19 | —      | **Phase C steps C1/C1a/C2/C3 implemented** (Resolve-phase wiring). C1: retain `_matchSeed`; construct `CollisionSystem(22)` + null-object `ICollisionEventConsumer` + per-agent `PassExecutor[22]`/`ShotExecutor[22]` instance arrays (§6 item 5 resolved — per-agent instance, shared adapter) + `_possessingAgentId`. C1a: `PassWorldAdapter`/`ShotWorldAdapter` nested classes implement all six executor query interfaces over world state (ERR-007 neutral attribute proxies; fatigue from AerobicPool; Stage-0 no-tackle/zero-pressure stubs). C2: `RunResolvePhase` calls `UpdateCollisions` (reuses `_attrs`; `_stumbleScratch` discarded; writes the one-tick-lag feedback buffers). C3: advances all 44 executors via `Update` each Resolve tick; `TestOnly_` seams script `Execute`+possession. No CONTACT publish at Stage 0 (executors idle in production/determinism; registry boot deferred to C4 — `EventRegistry.EnsureInitialized` is `internal`). `MatchEngine.cs` v1.4, `MatchEngineConstants.cs` v1.4 (NO_POSSESSION + STAGE0_NEUTRAL_* in a new GT region), both asmdefs (+CollisionSystem/PassMechanics/ShotMechanics), `MatchEngineResolveTests.cs` v1.0. C4–C6 pending. |
| 0.9.1   | 2026-06-19 | —      | **Phase C step C0 implemented** (executor snapshot get/restore seams — the gating sub-step). `PassExecutor.CaptureState()`/`RestoreState(in PassExecutorState)` (v1.14) + `ShotExecutor.CaptureState()`/`RestoreState(in ShotExecutorState)` (v1.9), parallel to the B0 `OscillationGuard` seam. New DTOs `PassExecutorState` / `ShotExecutorState` (state-machine ordinal + INITIATING-frozen in-flight field set). Pass's internal `PhysicalProfile` recomputed on restore (§2.6 recompute exclusion + it is an `internal` type); Shot carries its full field set. Round-trip locks `PassExecutorStateTests` / `ShotExecutorStateTests` (CanonicalSerializer byte round-trip + Capture/Restore identity); both test asmdefs gain the DeterministicSim reference. No execution-path change. C1–C6 remain pending. |
| 0.9     | 2026-06-19 | —      | Phase C plan folded in (docs-only; no code). §5 Phase C expanded to ordered sub-steps C0–C6 + new C1a (executor adapter implementations as the highest-risk net-new surface); §3 Resolve row corrected (`EvaluateOnBallContact` phantom → pure `EvaluateFirstTouch`; first-touch deferred to Phase D; executor `Execute`-initiates / `Update`-advances split made explicit). Three AR corrections vs. the actual APIs: phantom first-touch method; first-touch has no Stage-0 trigger + needs 2 extra adapters (→ Phase D); Phase C registers no RNG draw sites (collision self-seeds, pass/shot error is hash-based) so the RNG-registration sub-step was dropped. C0 snapshot seam named `CaptureState`/`RestoreState` to avoid the existing `IPassAgentQuery.GetState` collision. Phase D entry updated to absorb first-touch + the DecisionTree restore seam. All claims verified against the executor ctors, `IPass*` interfaces, `FirstTouchContext`/`FirstTouchSystem`, and `CollisionSystem.UpdateCollisions`. Intra-Resolve order pinned: collision → executor Update → possession. |
| 0.8.2   | 2026-06-16 | —      | Phase B full-surface AR (B0–B4): no new H/M (the one substantive finding, the `_attrs`/`_perfs` exclusion proof, was already fixed in 0.8.1). Recorded three inherited/accepted scope observations as §6 items 7–9: (7) B3 serialization is write-only — no symmetric `ReadWorldState` yet, so the restore reader (Phase C–D) MUST mirror the `SerializeWorldState` field order + add a round-trip lock; (8) world-state floats are written Tier A without non-finite enforcement (relies on upstream sanitisation; `OscillationGuard` `-Infinity` sentinel flows through intentionally); (9) the `SNAPSHOT_SCHEMA_VERSION` name collision (match-engine body vs #16 header framing) is a documented confusion hazard. Doc-only. |
| 0.8.1   | 2026-06-16 | —      | B3 self-AR (0H+1M+2L). M-1: §2.6 gains the excluded-field proofs for `_attrs`/`_perfs` (boot-deterministic constants, passed `in`, never mutated mid-sim — omission cannot diverge replay) + the Phase-A observation counters, with a PHASE-D flag that `_perfs` MUST be serialized once the AI phase writes it (the omission is invisible to the same-seed determinism test — the §2.6 trap). L-1/L-2 (code-side): MatchEngine.cs Modified annotation B2 → B3; Modified header field added to the new test file. No functional code change. |
| 0.8     | 2026-06-16 | —      | **Phase B steps B3 + B4 implemented — Phase B complete.** B3 (serialization + schema pin): `PHASE_A_PAYLOAD_FORMAT_VERSION` (u8) → `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` (u32 = 1, distinct from the #16 `SnapshotHeader` schema version — world-state body vs codec framing); `SerializeWorldState` writes the full §2.6 field set field-by-field via `CanonicalSerializer` — ball position/velocity/spin/state + `LastValid*`; per-agent full `AgentState` incl. the embedded `OscillationGuard` ring-buffer state via the B0 `GetState()` seam; team/GK flags; the two collision-feedback inputs; the held `MovementCommand`. Enums as i32; zero-alloc (guard seam returns a value type); payload ≈3.8 KB ≪ `MaxSnapshotBytes`. DecisionTree/executor in-flight state stays excluded (Phase C/D seam dependency). New `TestOnly_SetAgent` seam + `MatchEngineSnapshotSchemaTests.cs` (schema-version pin; OscillationGuard + ball-spin digest-preimage probes; locked-guard determinism). B4 (design-note reconciliation): corrected the stale §2.3 three-buffer `{_knockdown, _knockdownForce, _stumble}` world-state field block to the real two-input `{_isCollisionKnockdown, _collisionForces}` seam (matches §2.6 / §3 + the B2 code); sweep confirms no other doc references the phantom three-buffer model — the remaining Collision System #3 `knockdownForceOut`/`stumbleOut` hits are its legitimate Phase-C OUTPUT API, not a movement input. Files: `MatchEngine.cs` v1.3, `MatchEngineConstants.cs` v1.3, `MatchEngineSnapshotSchemaTests.cs` v1.0 (new). CI gate runs on push. |
| 0.7     | 2026-06-16 | —      | **Phase B step B2 implemented (Physics-phase wiring).** World state migrated from the Phase-A kinematic float arrays to real `BallState` + `AgentState[]` plus per-agent input buffers (attrs/perfs/commands) and the two collision-feedback buffers. `RunPhysicsPhase` now drives `BallPhysicsCore.UpdateBallPhysics` (null logger, GrassDry, no wind) and `AgentMovementSystem.UpdateAllAgents` (skips GKs) with `dt = FrameSeconds` and the seconds-domain clock; boot seeds `Stop` hold commands + default attrs + neutral perfs. Interim serialization sources the kinematic subset (position + facing) from the structs (`PHASE_A_PAYLOAD_FORMAT_VERSION` 1 → 2); full field set + `SNAPSHOT_SCHEMA_VERSION` pin remain B3. New test seams + `MatchEnginePhysicsTests.cs` (ball drop, outfield walk + GK skip, same-seed determinism with live dynamics). asmdefs gain BallPhysics + AgentMovement. B0 + B1 already landed; B3 + B4 remain. CI gate runs on push. Files: `MatchEngine.cs` v1.2, `MatchEngineConstants.cs` v1.2, both asmdefs, `MatchEnginePhysicsTests.cs` v1.0. |
| 0.6     | 2026-06-16 | —      | **Phase B step B1 implemented (time-unit plumbing).** Added `[DERIVED] DeterministicSimConstants.FrameSeconds` (= `FrameMs / 1000`) and `MatchClock.CurrentMatchTimeSeconds` (= `CurrentTick × FrameSeconds`) so seconds consumers (AgentMovement `OscillationGuard.WindowSeconds`) read a real seconds clock instead of risking the silent 1000× ms↔s unit error; the seconds clock and the B2 integration dt share one derivation chain (`PHYSICS_TICK_HZ → FrameMs → FrameSeconds`). Tests added in `DeterministicSimTests.cs` (FrameSeconds value; seconds-clock tick tracking / one-second landing / seconds↔ms agreement). Files: `DeterministicSimConstants.cs` v1.2, `MatchClock.cs` v1.1, `DeterministicSimTests.cs` v1.6. (B0 already merged; B2–B4 remain.) CI gate runs on push. |
| 0.5     | 2026-06-16 | —      | **Phase B re-sequenced (adversarial review of the planned wiring; 2H+3M+2L).** H-1: `AgentState.OscillationGuard` holds private cross-tick sliding-window state with no accessor → canonical (`CanonicalSerializer`) agent serialization is impossible without a new get/restore seam; promoted to gating step **B0**; the gap is invisible to Phase B's same-seed-in-process determinism test (both runs omit identically) and only diverges under save/restore. H-2: agent `currentTime` must be **seconds** (`OscillationGuard.WindowSeconds`) but `MatchClock` exposes only `CurrentMatchTimeMs` — silent 1000× bug (the finite/≥0 assert passes for ms); step **B1**. M-1: the §2.6/§3 three-buffer collision model {`isGrounded`, `knockdownForce`, `stumble`} is a phantom — the real `Update` seam takes two inputs {`isCollisionKnockdown`, `collisionForce`}; `GroundedReason` is internal `AgentState`; boot-seed corrected to `false`/`0`. M-2: use the existing `UpdateAllAgents` batch seam (it **skips goalkeepers**) instead of a hand-rolled loop. M-3: serialize the **full** `AgentState` + `BallState.LastValid*`, not the kinematic subset. L-1: `MaxSnapshotBytes` (65536) is ample (~4 KB) — risk dropped. L-2: ball `matchTime` feeds only `BallEventLogger`; pass `null` logger (non-load-bearing, no alloc). Confirmations: `AgentMovementSystem` is stateless except `_physicsHz` (shared instance safe); Phase B uses no RNG (determinism holds without draw-site plumbing). Docs-only; CI gate runs on push. |
| 0.4     | 2026-06-16 | —      | **Phase A implemented.** New `src/match-engine/` assembly (`TacticalDirector.MatchEngine`): `MatchEngineConstants.cs`, `MatchEngine.cs` (composition root — boot, world-state fields, 7 method-group phase callbacks wired into `TickOrchestrator` as EventBus-lifecycle-only stubs, digest-load-bearing snapshot serialization), `AssemblyInfo.cs`, `match-engine.asmdef`; tests `MatchEngineDeterminismTests.cs` (same-seed digest-chain equality, chain advance/non-degeneracy, AI-stride cadence, first-tick timing) + `match-engine-tests.asmdef`. Phase-A scope: references only deterministic-sim + event-system; kinematic world-state subset; `SNAPSHOT_SCHEMA_VERSION` pinning deferred to Phase B (§2.6); EventBus registrar boot deferred to Phase E (no events published in A). file-manifest.md updated. |
| 0.1     | 2026-06-15 | —      | Initial design note. Composition-root architecture, phase→subsystem wiring, boot sequence, phased delivery A–F, risks. |
| 0.3     | 2026-06-16 | —      | Second self-AR fix pass (1M+2L). M: snapshot serialization of DecisionTree/executor internal state machines requires get/restore seams those subsystems do not yet expose (parallel to RngStreamState) — recorded as a Phase C/D prerequisite in §2.6. L: collision-feedback boot seed corrected to the standing-at-rest value (`isGrounded = true`), not a blanket "no contact" that would make agents airborne on tick 1. L: §2.4 phase-entry wording tightened (AI/Input carve-outs made explicit). (Note: the Linux compile/test gate could not be executed locally — no .NET SDK in this environment; it runs in CI on push. This change is docs-only and adds no code to the tree.) |
| 0.2     | 2026-06-15 | —      | Self-adversarial-review fix pass (1H+3M+2L). H-1: collision↔movement one-tick-lag ordering contract documented (buffers seeded at boot, serialized, latency accepted). M-1: EventBus `BeginPhase(PhaseId.AI)` moved to end of Intent phase so the AI phase is entered every tick (orchestrator skips `_runAI` on non-stride ticks). M-2: cross-tick state (held MovementCommands, collision-feedback buffers, DecisionTree/executor state) added to the §2.6 snapshot field set. L-1: stride-timing corrected — first processed tick is 1, first AI evaluation is tick 6 (Advance runs first). L-2: per-agent-instance-vs-shared-evaluator verification required before Phase D. Plus: MatchContext home-perspective ball-zone caution (ERR-008-002 regression guard). |
