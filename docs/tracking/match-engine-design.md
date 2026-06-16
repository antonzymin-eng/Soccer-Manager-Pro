# Match Engine — Tick Orchestrator Composition Root (Design Note)

> **Created:** June 15, 2026
> **Last Updated:** June 16, 2026 (v0.4 — Phase A landed: `src/match-engine/` assembly + `MatchEngine` composition root (world-state fields, boot, 7 method-group phase callbacks wired into `TickOrchestrator` as EventBus-lifecycle-only stubs) + digest-load-bearing snapshot serialization + determinism/AI-stride test suite; see §5 Phase A and the Version History. v0.3 — second self-AR fix pass; v0.2 — self-AR fix pass: collision→movement ordering, EventBus AI-phase entry, cross-tick state in snapshot, stride-tick correction, per-agent-instance verification)
> **Status:** DESIGN NOTE (Stage 0+1 integration scaffolding — NOT a formal approved spec). **Phase A implemented** (June 16, 2026); Phases B–F pending.
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
// Collision outputs (pre-allocated):
bool[22] _knockdown; float[22] _knockdownForce; bool[22] _stumble;
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

- ball: position, velocity, spin, state-machine state.
- per-agent: position, velocity, facing, locomotion state, fatigue.
- **per-agent held `MovementCommand`** — produced only on stride ticks but consumed every
  tick (§3, §6.below), so it persists in world state and is digest-relevant.
- **per-agent collision-feedback buffers** (`knockdownForce`, `isGrounded`, `stumble`) —
  produced in Resolve (tick N) and consumed by movement in Physics (tick N+1) per the
  one-tick-lag contract below; carried across the tick boundary, therefore serialized.
- per-agent DecisionTree state-machine state (IDLE/EVALUATING/EXECUTING/INTERRUPTED) and
  any in-flight executor state (Pass/Shot WINDUP/CONTACT) — persists between heartbeats.

If a buffer can be proven fully recomputed before its first read each tick, it may be
excluded — but the default is to serialize cross-tick state, and the proof must be recorded
here per field.

**Seam dependency (Phase C/D blocker).** DecisionTree and the Pass/Shot executors hold this
state internally; they do **not** currently expose get/restore accessors. Serializing it
(and restoring it on replay) requires adding read/restore seams to each — parallel to
`RngStreamState` ↔ `DeterministicRngService.GetStreamState`/`RestoreStream`. These seams are
a prerequisite for Phase C (executors) and Phase D (DecisionTree); they do not exist yet.

---

## 3. Phase → subsystem wiring

`dt = DeterministicSimConstants.FrameMs / 1000f` (fixed 60 Hz step; never wall-clock).

| Phase | Host method | Subsystems invoked | Cadence |
|---|---|---|---|
| Input (0) | `RunInputPhase` | Stage 0 stub (no controller yet); opens EventBus tick | 60 Hz |
| Intent (1) | `RunIntentPhase` | Stage 0: static `TacticalContext`; later set-piece / manager intent | 60 Hz |
| AI (2) | `RunAiPhase` | assemble snapshots → `PerceptionSystem.OnHeartbeat` (×22) → `DecisionTree.ReceiveSnapshot` (×22) → `PositioningAITick` / `PressingAITick` / `DefensiveAITick` / `AttackingAITick` → emit `MovementCommand`s | 10 Hz (stride-gated by orchestrator) |
| Physics (3) | `RunPhysicsPhase` | `BallPhysicsCore.UpdateBallPhysics(ref _ball, dt)`; `AgentMovementSystem.Update(...)` ×22 — consumes the **previous tick's** collision-feedback buffers (see contract below) | 60 Hz |
| Resolve (4) | `RunResolvePhase` | `CollisionSystem.UpdateCollisions(...)` → writes this tick's collision-feedback buffers; `PassExecutor.Update` / `ShotExecutor.Update`; `FirstTouchSystem.EvaluateOnBallContact` on contact; possession → `_matchContext` | 60 Hz |
| Events (5) | `RunEventsPhase` | `EventBus.DrainTick()` → registered consumers | 60 Hz |
| Snapshot (6) | `RunSnapshotPhase` | serialize world state (§2.6) → `SnapshotPayload`; `EventBus.SerializeLedger`; `EventBus.OnTickBoundary` | 60 Hz |

**Collision ↔ movement ordering contract (one-tick lag).** `AgentMovementSystem.Update`
(Physics, phase 3) takes collision force / grounded / knockdown as **inputs**, but
`CollisionSystem.UpdateCollisions` (Resolve, phase 4) runs *after* movement in the same
tick. Movement at tick N therefore consumes the collision-feedback buffers written at tick
N−1. This is deliberate (the canonical phase order is fixed by `#16` and MUST NOT be
reordered): collisions resolve the positions movement just produced, and the response feeds
back next tick. Consequences the implementation MUST honor: (a) the buffers are seeded at
boot to the **standing-at-rest** value — `isGrounded = true`, zero force, no stumble (a
blanket "no contact"/`false` seed would make every agent airborne on tick 1, since
`AgentMovementSystem.Update` consumes `isGrounded` as an input); (b) the buffers are
cross-tick state and are serialized into the snapshot (§2.6); (c) this one-frame feedback
latency is an accepted Stage 0 model property, recorded here rather than hidden.

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
2. `EventRegistry.EnsureInitialized()`, then call all `EventBusRegistrar.Initialize()`
   sites exactly once (Pass, Shot, Perception, Decision, Heading, Goalkeeper). Boot is
   guarded against double-init (a reset seam is required for the replay path — see §6.4).
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
- **Phase B — Physics phase.** Wire ball physics + agent movement (×22) + world-state
  serialization. Pin `SNAPSHOT_SCHEMA_VERSION` + field order (§2.6).
  *Tests: drop-and-settle ball through the real loop; agent locomotion under a fixed
  `MovementCommand`; digest stable across runs.*
- **Phase C — Resolve phase.** Collision (×22) + pass/shot executors + first-touch +
  possession tracking into `MatchContext`.
  *Tests: a scripted pass between two agents completes; possession flips.*
- **Phase D — AI phase + snapshot assembly.** The new assembly helpers (§2.5) + perception
  → decision tree → movement-command chain, then the 4 mechanics AIs feeding tactical
  intent. *Tests: a ball carrier decides PASS/SHOOT/DRIBBLE and the dispatcher drives
  movement; away-team symmetry (closes the deferred Decision Tree away-team scenario).*
- **Phase E — Events phase consumers.** Subscribe real cross-subsystem consumers
  (e.g., possession-changed → AI); confirm Tier A/B ledger digest stability.
- **Phase F — Capstone.** A cross-spec closed-loop scenario on the **#19 `ScenarioRunner`**
  driving a multi-second kickoff sequence through the full host, with (a) gameplay-invariant
  envelope predicates and (b) a pinned determinism digest across two runs. Activate the
  **FR-PO-052** per-tick perf gate (host platform is now pinned per
  `certification-platform.md`).

---

## 6. Risks and open questions

1. **Snapshot payload schema is digest-load-bearing** — decide the field set + order and
   `SNAPSHOT_SCHEMA_VERSION` up front (before Phase B), or later changes force schema bumps.
2. **No governing spec** — this note is the mitigation; keep it current as the engine lands.
3. **Snapshot-assembly seams are net-new** and untested in composition — exactly the
   "passes in isolation, breaks when composed" defect class the AR history keeps surfacing.
   The Phase F closed-loop run is the primary mitigation; assemble-and-run smoke coverage
   should land with each AI subsystem in Phase D.
4. **EventBus boot idempotency** — several registrars were historically non-idempotent; the
   host boots them once, but the replay path needs a reset seam (`#16` `ReplayEngine` step 6
   is a Stage 0 stub today).
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
| 0.4     | 2026-06-16 | —      | **Phase A implemented.** New `src/match-engine/` assembly (`TacticalDirector.MatchEngine`): `MatchEngineConstants.cs`, `MatchEngine.cs` (composition root — boot, world-state fields, 7 method-group phase callbacks wired into `TickOrchestrator` as EventBus-lifecycle-only stubs, digest-load-bearing snapshot serialization), `AssemblyInfo.cs`, `match-engine.asmdef`; tests `MatchEngineDeterminismTests.cs` (same-seed digest-chain equality, chain advance/non-degeneracy, AI-stride cadence, first-tick timing) + `match-engine-tests.asmdef`. Phase-A scope: references only deterministic-sim + event-system; kinematic world-state subset; `SNAPSHOT_SCHEMA_VERSION` pinning deferred to Phase B (§2.6); EventBus registrar boot deferred to Phase E (no events published in A). file-manifest.md updated. |
| 0.1     | 2026-06-15 | —      | Initial design note. Composition-root architecture, phase→subsystem wiring, boot sequence, phased delivery A–F, risks. |
| 0.3     | 2026-06-16 | —      | Second self-AR fix pass (1M+2L). M: snapshot serialization of DecisionTree/executor internal state machines requires get/restore seams those subsystems do not yet expose (parallel to RngStreamState) — recorded as a Phase C/D prerequisite in §2.6. L: collision-feedback boot seed corrected to the standing-at-rest value (`isGrounded = true`), not a blanket "no contact" that would make agents airborne on tick 1. L: §2.4 phase-entry wording tightened (AI/Input carve-outs made explicit). (Note: the Linux compile/test gate could not be executed locally — no .NET SDK in this environment; it runs in CI on push. This change is docs-only and adds no code to the tree.) |
| 0.2     | 2026-06-15 | —      | Self-adversarial-review fix pass (1H+3M+2L). H-1: collision↔movement one-tick-lag ordering contract documented (buffers seeded at boot, serialized, latency accepted). M-1: EventBus `BeginPhase(PhaseId.AI)` moved to end of Intent phase so the AI phase is entered every tick (orchestrator skips `_runAI` on non-stride ticks). M-2: cross-tick state (held MovementCommands, collision-feedback buffers, DecisionTree/executor state) added to the §2.6 snapshot field set. L-1: stride-timing corrected — first processed tick is 1, first AI evaluation is tick 6 (Advance runs first). L-2: per-agent-instance-vs-shared-evaluator verification required before Phase D. Plus: MatchContext home-perspective ball-zone caution (ERR-008-002 regression guard). |
