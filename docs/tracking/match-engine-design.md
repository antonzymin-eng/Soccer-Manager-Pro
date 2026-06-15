# Match Engine — Tick Orchestrator Composition Root (Design Note)

> **Created:** June 15, 2026
> **Last Updated:** June 15, 2026
> **Status:** DESIGN NOTE (Stage 0+1 integration scaffolding — NOT a formal approved spec)
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
- each subsequent phase callback first line: `EventBus.BeginPhase(PhaseId.X);`
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
forces a schema bump. Stage 0 minimal field set: ball (position, velocity, spin, state) +
per-agent (position, velocity, facing, locomotion state, fatigue). Serialize via
`CanonicalSerializer` (−0.0 normalization, canonical NaN handling already implemented).

---

## 3. Phase → subsystem wiring

`dt = DeterministicSimConstants.FrameMs / 1000f` (fixed 60 Hz step; never wall-clock).

| Phase | Host method | Subsystems invoked | Cadence |
|---|---|---|---|
| Input (0) | `RunInputPhase` | Stage 0 stub (no controller yet); opens EventBus tick | 60 Hz |
| Intent (1) | `RunIntentPhase` | Stage 0: static `TacticalContext`; later set-piece / manager intent | 60 Hz |
| AI (2) | `RunAiPhase` | assemble snapshots → `PerceptionSystem.OnHeartbeat` (×22) → `DecisionTree.ReceiveSnapshot` (×22) → `PositioningAITick` / `PressingAITick` / `DefensiveAITick` / `AttackingAITick` → emit `MovementCommand`s | 10 Hz (stride-gated by orchestrator) |
| Physics (3) | `RunPhysicsPhase` | `BallPhysicsCore.UpdateBallPhysics(ref _ball, dt)`; `AgentMovementSystem.Update(...)` ×22 | 60 Hz |
| Resolve (4) | `RunResolvePhase` | `CollisionSystem.UpdateCollisions(...)`; `PassExecutor.Update` / `ShotExecutor.Update`; `FirstTouchSystem.EvaluateOnBallContact` on contact; possession → `_matchContext` | 60 Hz |
| Events (5) | `RunEventsPhase` | `EventBus.DrainTick()` → registered consumers | 60 Hz |
| Snapshot (6) | `RunSnapshotPhase` | serialize `_ball` + `_agents` → `SnapshotPayload`; `EventBus.SerializeLedger`; `EventBus.OnTickBoundary` | 60 Hz |

The AI phase only executes on stride ticks (`tick % AI_PHASE_STRIDE == 0`, stride = 6);
the orchestrator runs it as a no-op otherwise. Tick 0 is a stride tick.

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

- **Phase A — Skeleton & determinism spine (chosen first slice).** New assembly + asmdef,
  `MatchEngine` with world-state fields, boot, all 7 callbacks as **EventBus-lifecycle-only
  stubs** (no subsystem calls). Capstone: run N ticks twice with the same seed → identical
  snapshot digest chain. Proves the loop + EventBus lifecycle + digest before any physics.
  *Tests: determinism digest equality across two runs; AI-stride cadence.*
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
5. **Per-agent system fan-out** (PassExecutor/ShotExecutor/DecisionTree ×22) — confirm
   holding these as arrays vs. pooling them respects the zero-alloc budget.
6. **MatchContext authorship** — the host owns and updates `MatchContext` each AI tick
   (score, possession, ball, zone); possession transitions are produced in Resolve and read
   by the next AI tick. Pin the write/read ordering to avoid a one-tick staleness ambiguity.

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
| 0.1     | 2026-06-15 | —      | Initial design note. Composition-root architecture, phase→subsystem wiring, boot sequence, phased delivery A–F, risks. |
