# Match Engine — Tick Orchestrator Composition Root (Design Note)

> **Created:** June 15, 2026
> **Last Updated:** July 26, 2026, later same day (v2.2 — **§5.Z Phase H LANDED: a production match now
> plays.** ERR-030-014 is closed. The possession bootstrap is five seams, not one: the kickoff/restart
> **taker award** (KD-H1 — `ApplyRestart` now takes an `awardedTeam` and every call site declares one, so
> no restart can silently grant the ball to nobody); the **loose-ball pickup** (KD-H3 — a ball that comes to
> REST while loose is claimed by an agent standing over it, the exact complement of `RunFirstTouch`'s
> moving-ball gate); the Decision Tree's **loose-ball collect** (KD-H5 / ERR-008-014 — the tree had no
> action at all that fetches a stationary loose ball, so play died the first time a pass ran out of momentum
> more than ten metres from anyone); the DT **PASS/SHOOT completion sweep** (KD-H4 / ERR-008-015 —
> `NotifyActionComplete` had **zero production callers**, so every agent that passed or shot was frozen in
> EXECUTING for the rest of the match); and the **interrupt deferral** that stops a re-plan dispatching into
> an executor that is still mid-lifecycle. Four of those five were found by RUNNING the composed engine, one
> after another, each revealed only once the previous was fixed — §5.Z.4's "expect several findings, not
> zero" was accurate and then some. Acceptance is the new `match-engine-play-develops` scenario (§5.Z.5): six
> seeds × 9 minutes, asserting the ball is kicked and airborne, possession is held (10–21% of ticks) and
> changes hands (262–298 times), **play is still alive at the final tick**, and across the spread the ball
> reaches both penalty areas and goals are scored. Every predicate fails on the pre-Phase-H engine. **Full
> dotnet gate: PASSED, 0 failures (whole tree green).** 21 existing tests needed updating — most encoded the
> old "a restart clears possession" contract, which is exactly the contract that made the deadlock possible.
> Two findings are recorded but deliberately NOT fixed here: the process-static EventBus makes INTERLEAVED
> engines diverge (latent since #17, invisible until a production event was finally published), and the foul
> heuristic issues **7 red cards per 9 minutes** — see §5.Z.7. Prior entry below.)
> **Last Updated:** July 26, 2026, later same day (v2.3 — **§5.Z.9 foul & discipline balance pass landed;
> §5.Z.7 item 1 CLOSED.** A played match no longer sends seven players off every nine minutes: measured
> **480 → 21 fouls, 147 → 3.0 yellows, 75 → 1.0 reds per 90 minutes**, against a football reference of
> ~22 / ~3.5 / ~0.25. **The measurement refuted §5.Z.7's own diagnosis.** The qualifying-force distribution
> is bounded at ~2362 N, so the threshold is a cliff not a dial — 480 fouls at 1200 N, 90 at 2000 N, **0 at
> 3000 N** — and no cooldown rescues it. The missing term was the referee's judgement: the model called
> *every* hard cross-team from-behind contact a foul while the engine produces **seventeen of them per
> second**. Fixed with a force-scaled call probability
> `p(F) = min(1, FoulCallProbability × F / threshold)` whose **single draw** also selects the card from
> the rescaled remainder, so there is no new RNG stream and **no `SNAPSHOT_SCHEMA_VERSION` change**; a
> wave-on arms no cooldown; and the consumer now keeps the **strongest** contact of a tick, since force
> now decides the call. Calibration required a LIVE run — the offline sweep pointed at 0.025, which
> measured 37.5 fouls, because giving 20× fewer fouls means 20× fewer restarts, so play runs on and the
> contact count *rises*. New acceptance scenario `match-engine-discipline-plausible` (6 seeds × 9 min):
> rate bands, **no team reduced below nine players** (per seed), cards a minority of fouls — **9 of its
> 10 predicates fail on the pre-fix engine**. Plus 8 unit locks, the env-gated `FoulRateDiagnosticTests`
> instrument, and the `TestOnly_SetCollisionObserver` seam that made the distribution observable.
> **Recorded, not fixed: the contact rate itself** (17/second is not football — #12 spacing or #3's 60°
> cone). See `docs/tracking/foul-discipline-balance-design.md`.)
> **Last Updated (prior):** July 26, 2026 (v2.1 — **new §5.Z Phase H opened from ERR-030-014: the engine cannot
> develop play.** Discovered by running roadmap item A4a's KD-8 Step 0 pilot from the season loop: a
> production match's ball velocity is **identically zero for all 324 000 ticks**, no agent ever possesses
> it, and every match finishes 0–0 at any squad-strength differential. Root cause is a closed loop, half of
> it already stated in `InitializeKickoffState`'s own comment — the ball starts at rest, `RunFirstTouch`
> will only grant a touch on a *moving* ball, production possession comes only from that path, and only a
> possessing agent can kick. The 600-tick kickoff capstone passes because every predicate it asserts (tick
> count, stride cadence, finiteness, bounds, digest advance) holds for a match in which nothing happens:
> **it verified that the composition runs, never that it plays.** §5.Z records the evidence, the four
> decisions the minimal fix needs (which agent per restart type; possession assignment vs imparted
> velocity; digest/perf-baseline rebaselining; budgeting for the defects that surface once never-composed
> code runs), and the acceptance scenario. NOT BUILT — it is roadmap item A4b, ahead of A4a on the critical
> path. Prior entry below.)
> **Last Updated (prior):** July 14, 2026 (v2.0 — **Match-flow completion landed**: throw-ins, corners,
> goal kicks, fouls/cards, offside, substitutions, half-time break, full-time end — the remaining
> restart/discipline/clock model the v1.4 engine-substrate entry explicitly left "Not built".
> Companion design note `docs/tracking/match-flow-completion-design.md` (new) carries the full
> plan + AR-1..AR-6 adversarial-review history (per the driving instruction: design doc first,
> adversarially reviewed to convergence, then implemented, then the CODE adversarially reviewed to
> convergence). New: `RestartResolver.cs` (pure position/awarded-team resolution for ThrowIn/
> Corner/GoalKick — `awardedTeam = 1 − lastTouchTeam` uniformly, verified against
> `BallCollision.CheckBoundaries`'s actual branches), `OffsideEvaluator.cs` (pure second-nearest-
> to-goal-line geometry + reception-time check — a documented Stage-0 approximation of the Law, not
> a freeze-at-the-pass model), `SubstitutionReason.cs`; three new Tier A events
> (`OffsideCalledEvent` 0x18 / `RestartAwardedEvent` 0x19 / `MatchPhaseChangedEvent` 0x1A).
> `MatchEngine.cs` v1.31: `CheckRestartAndApply` (renamed/extended from `CheckGoalAndRestart`)
> routes non-goal exits through `RestartResolver` + a shared `ApplyRestart` primitive;
> `MatchFlowCollisionConsumer` (replaces the former no-op `NullCollisionEventConsumer`) captures at
> most one FROM_BEHIND high-force cross-team foul candidate per tick against a new
> `match-flow.card-severity` RNG stream, feeding card issuance + sent-off tracking
> (`_yellowCards`/`_isSentOff`) that forces a Stop command in the Physics phase and excludes the
> agent (`IsActive = false`) from all four Mechanics-AI snapshot fill sites; `EvaluateAndApplyOffside`
> hooks into `RunFirstTouch`'s Controlled case; public `SubstitutePlayer` (bench-roster swap,
> cap-enforced, queued `SubstitutionEvent` — the queue exists because `SubstitutePlayer` may be
> called between ticks when `EventBus.CurrentPhase` is not a valid producer phase, an AR-5 finding);
> `CheckMatchFlowTransitions` (every Input phase) fires half-time (ball reset only — no ends-swap,
> since `team 0 attacks +X` is hardcoded across goal detection/offside/Mechanics-AI and a real
> ends-swap is a documented Stage-1+ deferral, an AR-4 finding) and full-time (`_matchEnded` freezes
> AI/Physics/Resolve) once each. **`SNAPSHOT_SCHEMA_VERSION` 14 → 15** (discipline + substitution +
> match-flow-clock state). New tests: `MatchEngineRestartTests` / `MatchEngineOffsideTests` /
> `MatchEngineFoulCardTests` / `MatchEngineSubstitutionTests` / `MatchEngineMatchFlowTests`;
> `MatchEngineSnapshotSchemaTests` v1.12 (pin 15 + 2 probes). The code-review cycle caught (among
> other things) an `OffsideEvaluator` bug where fewer than two active defenders left the accumulator
> at an `Infinity` sentinel instead of `NaN`, making `IsOffside` return true for every finite
> attacker position — the opposite of the intended rule — fixed via an explicit active-defender
> count gating the `NaN` return. Full dotnet gate not runnable in this environment (no SDK access);
> verified by exhaustive manual review of the entire touched surface instead of `dotnet test`. See
> src/CLAUDE.md v2.17 and root `CLAUDE.md` for the parallel entries.)
> **Last Updated (prior):** June 27, 2026 (v0.9.12 — **Phase D D4 final cross-tick surface — Perception (#7) internal state now serialized**, and **Phase D flipped COMPLETE (D5)**. New `CaptureState` seams on `PerceptionSystem` + `RecognitionLatencyTracker` (→ `RecognitionLatencyState`) + `ShoulderCheckScheduler` (→ `ShoulderCheckState`), bundled in a new `PerceptionTickState`; `WritePerceptionTickState` serializes the recognition-latency pair arrays + shoulder-check per-agent/per-pair arrays + per-agent ball-perception carry-over (one shared instance); `SNAPSHOT_SCHEMA_VERSION` 7 → 8. **Cross-tick coverage complete** — no cross-tick gameplay state remains excluded (only boot-deterministic constants + observation counters). D5 reconciliation: Phase D complete; Phases E (events consumers) + F (capstone) pending. New `RecognitionLatencyState.cs` / `ShoulderCheckState.cs` / `PerceptionTickState.cs`, `TestOnly_PerceptionState` seam + `PerceptionState_FeedsSnapshotDigest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PerceptionSystem`. `RecognitionLatencyTracker.cs` v1.4, `ShoulderCheckScheduler.cs` v1.3, `PerceptionSystem.cs` v1.5, `MatchEngine.cs` v1.14, `MatchEngineConstants.cs` v1.14, `MatchEngineSnapshotSchemaTests.cs` v1.5. Prior v0.9.11 — **Phase D D4 continuation 3 — per-team Defensive AI (#14) + Attacking AI (#15) cross-tick state now serialized** via new `DefensiveAITick.CaptureState` / `AttackingAITick.CaptureState` seams returning `DefensiveTickState` / `AttackingTickState` views (offside-line + mark hysteresis + last assignment for #14; transition-hold + frozen directive + role hysteresis for #15; each ×`TEAM_COUNT`); `SNAPSHOT_SCHEMA_VERSION` 5 → 7 (v6 Defensive, v7 Attacking). All four mechanics-AI hysteresis surfaces are now serialized — only the perception internal-state seam remains before D5. New `DefensiveTickState.cs` / `AttackingTickState.cs`, `TestOnly_DefensiveState` / `TestOnly_AttackingState` seams + two digest probes; `match-engine-tests` asmdef gains `TacticalDirector.DefensiveAI` + `TacticalDirector.AttackingAI`. `DefensiveAITick.cs` v1.3, `AttackingAITick.cs` v1.3, `MatchEngine.cs` v1.13, `MatchEngineConstants.cs` v1.13, `MatchEngineSnapshotSchemaTests.cs` v1.4. Prior v0.9.10 — **Phase D D4 continuation 2 — per-team Pressing AI (#13) cross-tick state now serialized** via a new `PressingAITick.CaptureState` seam returning a new `PressingTickState` view (`WritePressingTickState`, ×`TEAM_COUNT` — trigger debounce + disengage/cooldown dwell + per-agent role hysteresis + press fatigue); `SNAPSHOT_SCHEMA_VERSION` 4 → 5; Pressing dropped from the exclusion list (perception + Defensive/Attacking still excluded). New `PressingTickState.cs`, `TestOnly_PressingState` seam + `PressingState_FeedsSnapshotDigest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PressingAI`. `PressingAITick.cs` v1.3, `MatchEngine.cs` v1.12, `MatchEngineConstants.cs` v1.12, `MatchEngineSnapshotSchemaTests.cs` v1.3. Prior v0.9.9 — **Phase D D4 continuation — per-team Positioning AI (#12) `HysteresisState` now serialized** via a new `PositioningAITick.CaptureState` seam (`WritePositioningHysteresis`, ×`TEAM_COUNT` — team phase + dwell + per-agent line/lane membership); `SNAPSHOT_SCHEMA_VERSION` 3 → 4; Positioning dropped from the exclusion list (perception + Pressing/Defensive/Attacking hysteresis still excluded — their seams are the rest of the follow-up before D5). New `TestOnly_PositioningState` seam + `PositioningHysteresis_FeedsSnapshotDigest` probe; `match-engine-tests` asmdef gains `TacticalDirector.PositioningAI`. `PositioningAITick.cs` v1.1, `MatchEngine.cs` v1.11, `MatchEngineConstants.cs` v1.11, `MatchEngineSnapshotSchemaTests.cs` v1.2. Prior v0.9.8 — **Phase D step D4 implemented — snapshot extension + schema bump.** `SerializeWorldState` now serializes the per-agent D0 `DecisionTreeState` (×22) via the new `WriteDecisionTreeState` helper (mirrors the `DecisionTreeStateTests` round-trip order — `DtState` ordinal + dispatched-action flag + last `AgentAction` incl. embedded Pass/Shot request blocks), captured through the existing D0 `CaptureState` seam right after the C5 executor state. `SNAPSHOT_SCHEMA_VERSION` 2 → 3 (v3 doc paragraph). Per-field exclusion proofs recorded: `_perfs` stays excluded (PHASE-D flag not yet fired — AI phase still leaves it boot-neutral); the perception internal state + per-team Positioning/Pressing/Defensive/Attacking hysteresis remain excluded (no get/restore seam yet — same-seed in-process determinism unaffected; only save/restore replay needs them, deferred to a follow-up extension that re-bumps the schema). New `TestOnly_SetDecisionTreeState` seam + `MatchEngineSnapshotSchemaTests` pin 2 → 3 + `DecisionTreeState_FeedsSnapshotDigest` probe (first tick is not an AI stride, so injected EXECUTING state passes through to the snapshot — single-field probe). `MatchEngine.cs` v1.10, `MatchEngineConstants.cs` v1.10, `MatchEngineSnapshotSchemaTests.cs` v1.1. D5 + Phases E–F pending. Prior v0.9 — **Phase C plan folded in** (docs-only; no code). §5 Phase C expanded from a one-liner to ordered sub-steps C0–C6, with three corrections caught in adversarial review against the actual subsystem APIs: (1) the §3 Resolve row's `FirstTouchSystem.EvaluateOnBallContact` was a phantom — the real API is the pure `EvaluateFirstTouch(FirstTouchContext)` + `ApplyTouchResult` via first-touch's own adapters; (2) first-touch has no Stage-0 trigger and needs 2 extra adapters, so it is **deferred to Phase D**; (3) Phase C registers NO `DeterministicRngService` draw sites — collision self-seeds from `matchSeed ^ frameNumber` and pass/shot error is hash-based, so the planned RNG-registration sub-step was dropped. New C1a sub-step makes the six pass/shot executor adapter implementations (`IPass/IShotBallSystem` / `AgentQuery` / `CollisionQuery`) explicit as the highest-risk net-new surface; C0 executor snapshot seam named `CaptureState`/`RestoreState` to avoid colliding with the existing `IPassAgentQuery.GetState`. All claims verified against `PassExecutor`/`ShotExecutor` ctors, `IPass*` interfaces, `FirstTouchContext`/`FirstTouchSystem`, and `CollisionSystem.UpdateCollisions`. Phase D entry updated to absorb first-touch + the DecisionTree restore seam. Prior v0.8 — **Phase B complete**: steps B3 + B4 implemented. B3 — full canonical world-state field-set serialization + schema pin: `PHASE_A_PAYLOAD_FORMAT_VERSION` (u8) replaced with `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` (u32 = 1; distinct from the #16 `SnapshotHeader` schema version — body vs framing); `SerializeWorldState` now writes the full §2.6 field set field-by-field via `CanonicalSerializer` (ball position/velocity/spin/state + `LastValid*`; per-agent full `AgentState` incl. the B0 `OscillationGuard` ring-buffer state via `GetState()`; team/GK flags; the two collision-feedback inputs; the held `MovementCommand`), zero-alloc, ≈3.8 KB. New `TestOnly_SetAgent` seam + `MatchEngineSnapshotSchemaTests.cs` (schema pin; OscillationGuard + ball-spin digest-preimage probes; locked-guard determinism). B4 — design-note reconciliation: corrected the stale §2.3 three-buffer `{_knockdown, _knockdownForce, _stumble}` field block to the real two-input `{_isCollisionKnockdown, _collisionForces}` seam; confirmed no other doc references the phantom model (the remaining Collision System #3 `knockdownForceOut`/`stumbleOut` hits are its legitimate Phase-C OUTPUT API). Files: `MatchEngine.cs` v1.3, `MatchEngineConstants.cs` v1.3, `MatchEngineSnapshotSchemaTests.cs` v1.0. Prior v0.5 — Phase B re-sequenced after adversarial review of the planned Physics-phase wiring: `OscillationGuard` get/restore seam promoted to gating step B0 (its private sliding-window state blocks canonical agent serialization; the omission is invisible to Phase B's same-seed determinism test, only diverging under save/restore); §2.6 corrected — full `AgentState`/`BallState` field set incl. `OscillationGuard` + `LastValid*` checkpoints, and the phantom three-buffer collision model {isGrounded, knockdownForce, stumble} replaced with the real two-input seam {isCollisionKnockdown, collisionForce}; B1 time-unit fix (agent `currentTime` is seconds, clock exposes only ms); B2 uses `UpdateAllAgents` batch seam (skips GKs) + null ball logger. v0.4 — Phase A landed: `src/match-engine/` assembly + `MatchEngine` composition root (world-state fields, boot, 7 method-group phase callbacks wired into `TickOrchestrator` as EventBus-lifecycle-only stubs) + digest-load-bearing snapshot serialization + determinism/AI-stride test suite; see §5 Phase A and the Version History. v0.3 — second self-AR fix pass; v0.2 — self-AR fix pass: collision→movement ordering, EventBus AI-phase entry, cross-tick state in snapshot, stride-tick correction, per-agent-instance verification)
> **Status:** DESIGN NOTE (Stage 0+1 integration scaffolding — NOT a formal approved spec). **Phase A + Phase B implemented** (June 16, 2026); **Phase C complete** (C0–C3 June 19, 2026; **C4–C6 June 22, 2026** — possession→`MatchContext`, EventBus registry boot, executor+context snapshot serialization with `SNAPSHOT_SCHEMA_VERSION` 2); **Phase D steps D0/D1 implemented** (June 22, 2026); **Phase D step D2a implemented** (June 22, 2026 — Positioning AI #12 → per-team formation slots folded into each agent's `TacticalContext` via `RunPositioningAI`, with the away team mapped through the canonical attack-+X frame, `MirrorPitchIfAway`, as the ERR-008-002 home/away guard); **Phase D step D3 implemented** (June 22, 2026 — first-touch wired into Resolve: a loose, ground-level, approaching ball is received via `EvaluateFirstTouch`/`ApplyTouchResult`, CONTROLLED → possession); **Phase D step D2b implemented** (June 26, 2026 — `RunMechanicsAI` ticks the full Positioning→Pressing→Defensive→Attacking chain per team and folds the Defensive `OffensiveLineDepth`/`HasMarkDirective` + Attacking `HasAttackIntent` carriers into each agent's `TacticalContext`, all 22 agents mapped through the canonical attack-+X frame as the ERR-008-002 guard); **Phase D step D4 implemented** (June 27, 2026 — per-agent `DecisionTreeState` (×22) + every cross-tick gameplay surface serialized into the world-state body via `CaptureState` seams — per-agent `DecisionTreeState`, all four mechanics-AI hysteresis (Positioning #12, Pressing #13, Defensive #14, Attacking #15), and Perception #7 — `SNAPSHOT_SCHEMA_VERSION` 2 → 8); **Phase D complete (D5 reconciliation landed June 27, 2026)**; **Phase E complete (June 27, 2026 — possession-changed event producer + AI consumer + `EventBus.ResetForNewMatch` per-match reset seam)**; **Phase F complete (June 28, 2026 — capstone closed-loop kickoff scenario on the #19 `ScenarioRunner` with gameplay-invariant predicates + a two-run determinism digest match + FR-PO-052 perf-gate activation)**. **Match Engine integration (Phases A–F) is complete.** **Match-flow completion landed (July 14,
2026 — throw-ins/corners/goal kicks, fouls/cards, offside, substitutions, half-time break,
full-time end; see the v2.0 Last Updated entry).**
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
  - **D6 — #21 manager-tactic serialization (ERR-021-002). ✅ COMPLETE (June 29, 2026).** The per-team
    `TeamTactic` (#21 T2 runtime activation) was a per-tick input excluded from the snapshot, so a tactic
    changed MID-match was not restore-deterministic. `WriteTeamTactic` now serializes both the active and
    pending `TeamTactic` (×`TEAM_COUNT`, Appendix B field order) after the perception block;
    `SNAPSHOT_SCHEMA_VERSION` 8 → 9. Default Balanced is byte-stable across same-seed runs; a mid-match
    `SetTeamTactic` is now restore-deterministic. The per-agent `PlayerTactic` / team `Tempo` carried in
    `TacticalContext` (#21 §3.3) need no field — re-assembled each AI tick from the serialized team tactic
    plus the boot identity. New `TeamTactic_FeedsSnapshotDigest` probe; `MatchEngine.cs` v1.22,
    `MatchEngineConstants.cs` v1.16, `MatchEngineSnapshotSchemaTests.cs` v1.6.
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

- **Phase G — Snapshot deserialize / restore path. ✅ Phases 1 + 2 COMPLETE (July 20, 2026); Phase 3 open.** Phases
  A–F made the engine *run forward, once, and serialize its state*; nothing reads that state back.
  Phase G adds the reader — the keystone save/load, replay/rewind, #27 T3 distinct-squad restore,
  and #16 §4.8.2 MXCSR validation all sit behind. Governed by its own converged design supplement
  `docs/tracking/snapshot-deserialize-design.md` (v0.5, AR-1..AR-4 CONVERGED), which carries the
  full KD-1..KD-8 decision set, phased plan, and adversarial-review history. Summary: KD-1 one
  symmetric version-gated fail-loud `DeserializeWorldState`; KD-2 reconstruct through `RestoreState`
  seams; KD-3 distinct-squad re-projection via a factory-owned `ISquadProvider` keyed by the
  serialized `_activeBenchSlot`; KD-4 a static `RestoreFromSnapshot(in SnapshotHeader, SnapshotPayload,
  …)` factory; KD-5 digest-chain continuity as the round-trip-determinism correctness contract; KD-6
  the MXCSR/`EnvironmentFingerprint` gate at restore step 0. Phased into slices:
  - **G-Phase 1 (neutral-path reader + round-trip determinism).** ✅ **KD-8 writer half LANDED
    (July 20, 2026):** the `match-flow.card-severity` `RngStreamState` cursor (RngCursor +
    ActionOrdinal — the engine's ONLY mutable RNG stream, the one cross-tick surface the writer
    omitted) is serialized at **`SNAPSHOT_SCHEMA_VERSION` 16 → 17**, so a save after any booking
    round-trips deterministically (before v17 a restore re-registered the stream at cursor 0 and
    the next card draw diverged — the KD-5 contract silently failed for any carded match). Stale v8
    exclusion-proof note corrected; new `TestOnly_SetCardSeverityStreamCursor` seam +
    `MatchEngineSnapshotSchemaTests` v1.14 (pin 17 + `CardSeverityRngCursor` probe). ✅ **READER
    LANDED (July 20, 2026, `MatchEngine.cs` v1.41):** `DeserializeWorldState` (the symmetric mirror
    of `SerializeWorldState` + per-block `Read*` helpers, reconstructing subsystem state through each
    `RestoreState` seam — KD-1/KD-2); the new `RestoreState` counterparts on Pressing/Defensive/
    Attacking/Perception/Positioning + `MovementCommand.ReconstructFromSnapshot` (RotationController's
    `Restore*` seams pre-existed); the static `RestoreFromSnapshot(in SnapshotHeader, SnapshotPayload,
    ulong matchSeed)` factory (fingerprint gate → boot + EventBus reset → deserialize → KD-3
    distinct-squad fail-loud → `CommitLoadedDigest` + clock restore); and `MatchEngineSnapshot-
    RestoreTests` (G3 round-trip determinism — neutral / mid-match-tactics / booking-cursor regression
    + version-gate/trailing-byte/distinct-squad fail-loud). Two findings folded in: the excluded
    `_possessingAgentId`/`_prevPossessingAgentId` reconstructed from the restored MatchContext, and an
    event-ledger-boundary-aware trailing guard (`RunSnapshotPhase` appends the digest-load-bearing
    ledger after the world state; the reader validates the boundary rather than restoring the ledger,
    which is replayed forward). No schema change. Full dotnet gate PASSED (257 match-engine tests).
  - **G-Phase 2 (distinct-squad re-projection).** ✅ **LANDED (July 20, 2026, `MatchEngine.cs` v1.42)** —
    the #27 T3 restore consumer (KD-T3-3). New public `ISquadProvider` (`ISquadProvider.cs`, the
    `ClubId → Squad` resolver) threaded into `RestoreFromSnapshot(…, ISquadProvider squads = null)`;
    `ReprojectDistinctSquads` replaces the Phase-1 fail-loud — neutral fast-path returns immediately, each
    team with a non-sentinel `_rosterClubId` resolves its roster (ClubId-check + size/record validation,
    both teams before any apply), re-runs `LineupSelector` + `PlayerAttributeProjection` for the base
    lineup (`ReprojectBaseLineup`, attribute arrays + the un-serialized bench GK flags; the serialized
    on-pitch `_isGoalkeeper` stays the restored value), then replays the substitutions the serialized
    `_activeBenchSlot` records (`ReprojectSubstitutions`). Fail-loud on absent provider / unresolvable /
    mismatched ClubId (R4). `MatchEngineSnapshotRestoreTests` v1.1 proves G3 round-trip for a distinct
    squad + mid-match / post-restore / keeper-for-keeper substitutions + the provider fail-loud gates. No
    schema change. Full dotnet gate PASSED (263 match-engine tests; whole tree green). Discovered
    out-of-scope (a Phase-1 completeness follow-up): a keeper-onto-outfield-slot substitution post-restore
    diverges via a Positioning-AI (#12) GK-flag-flip formation-slot interaction.
  - **G-Phase 3 (native MXCSR + on-disk fold).** The native float-mode query into the KD-6 seam
    (host-blocked). ✅ **The on-disk `SaveManager` fold (N1) LANDED July 21, 2026** — governed by its
    own converged supplement `docs/tracking/match-save-file-design.md` (v0.3). New `src/match-engine/`
    `MatchSaveManager` (atomic `Save`/`Load`) + `MatchSaveCodec` (the pure version-gated blob codec:
    boot-`matchSeed` boot-header + `SnapshotHeader` incl. `EnvironmentFingerprint` + `SnapshotPayload`,
    fail-loud on version/length-bound/trailing-byte) + `MatchSaveContents`; `MatchEngine` gains a public
    `MatchSeed` property + production `CaptureDurableHeader/Payload` (promoted from `TestOnly_`). Disk
    round-trip determinism green (neutral / booking-before-save / distinct-squad via `ISquadProvider`);
    the KD-6 fingerprint gate runs end-to-end through disk. No schema change. Full dotnet gate PASSED
    (279 match-engine tests). The native MXCSR query landed July 21–22, 2026 (host-block cleared). **The
    N2 unified season save LANDED July 22, 2026** via the new season save-file root
    (`docs/tracking/unified-season-save-design.md`; `src/season-save/` — `TacticalDirector.SeasonSave`
    above both `match-engine` and `living-world`, resolving FR-LW-003): one file bundling the
    `WorldStore` composite + an optional match save blob as two opaque, version-gated sub-blobs.
    `MatchSaveManager` gained public `Encode`/`Restore` (the "match save as a value" blob API the season
    root composes). **Phase G-Phase 3 is complete.**

---

## 5.Z Phase H — possession bootstrap ("make the match playable"), **LANDED**

> **Opened July 26, 2026 from ERR-030-014; LANDED the same day.** Roadmap item **A4b**. It was the
> highest-priority open item on the match engine and the blocker for `PM-1` and for #30's calibration
> (roadmap A4a) — both are now unblocked.

### 5.Z.1 The finding

A production match cannot develop play. Measured over a full 324 000-tick match and again over 60 000
ticks in both a distinct-squad and a plain neutral configuration:

- the ball's velocity is **identically zero for the entire match** (max speed 0.00 m/s),
- it is never airborne (max height 0.11 m = the resting centre height),
- **no agent ever possesses it** (`PossessingAgentId` never ≥ 0),
- so every match finishes **0–0**, at any squad-strength differential (20/20 pilot matches 0–0 at a
  measured rating gap of ±6 on a `[1,20]` scale).

The ball's x-position does wander (≈18–85 m), which is 22 agents jostling a stationary ball by physical
contact. That is not play.

### 5.Z.2 Root cause — a closed loop, half of it already documented in the source

1. `InitializeKickoffState` places the ball at the centre spot at rest, with the comment
   *"Stationary ball at the centre spot (a kick would set it in motion; none at Stage 0)."*
2. `RunFirstTouch` **gate 3** refuses a touch unless the ball is already moving
   (`|ballVelXY| ≥ FIRST_TOUCH_MIN_BALL_SPEED_M_S`, 0.5 m/s).
3. In production, possession is granted **only** on that path. `TestOnly_SetPossessor` carries
   *"Not called by production."*
4. The ball is set in motion **only** by a pass or shot executor, whose adapters gate on
   `IsBallPossessedBy(agentId)`.

No motion ⇒ no reception ⇒ no possession ⇒ no kick ⇒ no motion. `ApplyRestart` does not break it either:
it repositions the ball and clears possession, and reaching a restart requires a boundary crossing, hence
motion.

### 5.Z.3 Why the suite never caught it

The 321 match-engine tests are per-subsystem or per-mechanic and drive their own inputs, so each one is
satisfied. The single composed test — the `match-engine-kickoff-multi-second` capstone — ticks 600 ticks
and asserts tick count, AI-stride cadence, finiteness, on-pitch bounds and digest-chain advance. **Every
one of those predicates holds for a match in which nothing happens.** No test in the tree asserts that the
ball is ever kicked, that possession is ever held, or that play reaches a penalty area.

That is the lesson worth keeping: the capstone verified that the composition *runs*, never that it *plays*.

### 5.Z.4 What was built, and the decisions it made

The opening plan was "award possession at kickoff and at every restart; everything downstream already
exists". That was necessary and **not sufficient**. The loop has more than one entry point, and each of the
next four defects became visible only once the previous one was fixed and play ran a little further. The
landing is five seams.

**KD-H1 — the restart taker award (which agent, per restart type).** `ApplyRestart(position)` becomes
`ApplyRestart(position, awardedTeam)`; every call site must now name the team, so no restart can silently
leave the ball ownerless. Kickoff → `FIRST_HALF_KICKOFF_TEAM` (home; a coin toss would need its own
registered RNG stream and buys nothing yet, so the convention is `[FIXED]`, not `[GT]`); second-half
kickoff → `SECOND_HALF_KICKOFF_TEAM`, `[DERIVED]` from the first so they can never drift to the same side
(Law 8); post-goal restart → the **conceding** team; throw-in / corner / goal kick → `RestartResolver`'s
awarded team, which already existed; offside → the defending team; foul → the victim's team. The taker is
that team's nearest agent to the spot **that is not sent off**, ties to the lower roster index. Goalkeepers
are deliberately not excluded: nearest-to-the-spot gives the keeper a goal kick and an outfielder a corner
without a per-type table.

*Stage-0 approximation, recorded:* the taker is **not walked to the ball**, consistent with the
agents-keep-positions minimalism `ApplyRestart` already documented, so a taker may be some metres from the
spot when they play it. A real restart ceremony (walk-to-ball, wall set-up, the taker's two-touch
restriction) is Stage 1+.

**KD-H2 — assignment, not imparted velocity.** The restart grants possession and leaves the ball at rest,
so `ApplyKick` remains the SOLE producer of ball motion. A second motion source would have to be
serialized, digest-reasoned about, and kept coherent with the executors' possession recheck; the taker's
own AI decides what to do with the ball on the next tactical stride, which is both smaller and more
faithful.

**KD-H3 — loose-ball pickup.** `RunFirstTouch` gate 3 correctly refuses a ball that is not moving (a still
ball is not an *incoming receive*, and First Touch #4's control-quality model is a function of incoming
velocity — applying it at v ≈ 0 would be using that spec outside its domain). So a new, separately-named
`RunLooseBallPickup` runs after it: a loose ball at ground level whose planar speed is **below**
`FIRST_TOUCH_MIN_BALL_SPEED_M_S` — the exact complement of gate 3, so the two mechanics can never both fire
on one ball — is claimed by the nearest non-sent-off agent within `LooseBallPickupRadiusM`. Keeping it
separate leaves `RunFirstTouch` and every #4 contract test untouched. There is deliberately no contest
model: two opponents equidistant over a still ball resolve by roster index. A real 50-50 belongs with the
Collision System #3 duel fan-out (Stage 1+).

**KD-H5 — the loose-ball collect (ERR-008-014).** Pickup is a *reach* gate, so it only helps if somebody is
standing there — and nothing sent anyone. The Decision Tree had **no action at all that fetches a
stationary loose ball**: PRESS targets an opponent, MOVE_TO_POSITION targets the formation slot, and
INTERCEPT bailed out at its `INTERCEPT_MIN_BALL_SPEED` gate. Measured, play stopped the first time a ball
came to rest more than ~10 m (INTERCEPT's `MAX_INTERCEPT_TIME` reach) from the nearest player, with all 22
agents circling their slots around it. The fix emits the collect as the **SOLE** off-ball option for one
designated agent per team — the same shape as SAVE, and for the reason ERR-008-013's AR-4 already
established: *an action that must happen cannot be left to out-score a competitor under composure noise.*
(It does not: the collect scores ~0.35 against MOVE's ~0.21, a gap of 0.14 inside the ±0.15 noise band, so
the collector visibly dithered.)

The designation is made by the **host**, not derived per-agent in the tree, for two reasons. It is a
team-level role assignment from team state — the same class as Pressing AI #13 choosing one primary presser
from the whole team snapshot. And, load-bearing: only the host knows who is **sent off**. The first
implementation used a perception-derived "no teammate I can see is closer" rule and deadlocked anyway, with
the ball lying 4 m from a frozen red-carded agent that eleven teammates were all deferring to.

**KD-H4 — the DT PASS/SHOOT completion sweep (ERR-008-015).** §3.7.2 parks a tree in EXECUTING after a
PASS/SHOOT dispatch and re-evaluates only on `NotifyActionComplete` / `NotifyInterrupt` / a forced refresh.
**Nothing in production ever called `NotifyActionComplete`** (verified: zero callers outside tests), and the
possession-changed consumer interrupts only the NEW holder, never the passer. So every agent that passed or
shot — or whose `Execute` was *rejected*, which the dispatcher deliberately does not inspect (§3.5.2) — was
frozen in EXECUTING for the remainder of the match: no further decisions, no further movement commands, and
if it still held the ball, no way to release it. The composition root owns both the trees and the
executors, so it is the only layer that can observe an executor lifecycle ending; one rule covers both
completion and rejection — *a tree waiting on an executor that is not running has nothing left to wait
for.* Paired with it: `OnPossessionChanged` no longer interrupts a holder whose own executor is still in
flight, which was re-planning agents straight into their own busy executor (`Execute() called while shot in
progress`) once rebounds started happening.

**Digest baselines.** As predicted, play developing moves every engine digest. No absolute digest is pinned
anywhere in the tree, so every determinism lock — all comparative two-run checks — survived untouched. Two
`MatchEngineSnapshotSchemaTests` preimage probes did need re-anchoring, but not for that reason: they
perturbed a tree into EXECUTING using `default(AgentAction)`, whose `Type` is PASS (ordinal 0), so the new
completion sweep erased the perturbation during the very tick being measured and left the probe silently
vacuous. They now perturb with a continuous action. The certified `FR-PO-052` per-tick perf baseline is
**not** revisited here — it is a pinned-host artifact and a match that actually plays will not cost what an
idle one did; re-capturing it is a cert-run task (`cert-run-runbook.md`), listed in §5.Z.7.

**No `SNAPSHOT_SCHEMA_VERSION` change.** Nothing new is serialized: `_possessingAgentId` already round-trips
via `MatchContext.PossessingAgentId`, and `TacticalContext` (which carries the new `LooseBallCollector`
routing flag) is rebuilt each AI tick and never serialized.

### 5.Z.5 Acceptance — `match-engine-play-develops`

`src/match-engine/tests/MatchEnginePlayDevelopmentScenarios.cs` on the #19 ScenarioRunner (Tier B,
cross-spec, owning specs {1,2,3,4,5,6,7,8,12,13,14,15,16,17,19}). Six seeds × 32 400 ticks (9 minutes of
match time each; ~90 s wall clock on the Linux gate). **Every predicate fails on the pre-Phase-H engine.**

Per seed: the ball is kicked (peak planar speed ≥ 5 m/s; measured 16.2–17.2, pre-fix **0.00**); the ball
goes airborne (peak height ≥ 0.5 m; measured 2.45–2.91, pre-fix **0.11 m** = the resting centre height);
possession is held (≥ 5% of ticks; measured 10.5–20.9%, pre-fix **0%**); possession changes hands (≥ 50
times; measured 262–298, pre-fix **0**); and — the predicate that earned its keep twice —
**`play-still-alive-at-final-tick`**: the last possession change lands in the final quarter of the run and
the ball is still moving in the final tenth (measured 96.7–99.9% and 98.7–100%).

Across the seed spread: the ball reaches **both** penalty areas, and a non-zero scoreline is produced. These
are asserted over the set rather than per seed, per §5.Z.5's own wording — with the current provisional
balance only some nine-minute runs produce a goal, and which ones is a property of the tuning, not of the
possession bootstrap this scenario locks.

Plus a two-run byte-identical digest chain over 6 000 ticks of **live play** — the Phase F capstone already
matched two 600-tick chains, but 600 ticks of the old engine were 600 ticks of nothing, so that check could
not observe the possession loop, the executors, or a goal.

Per-seam unit locks live in `MatchEnginePossessionBootstrapTests` (11) and `OptionGeneratorTests` (+3).

### 5.Z.6 Why the run-fix-run loop was the method

Worth recording, because it generalises. Each of the four post-award defects was **invisible until the
previous fix landed**: the ball cannot come to rest in open play until something first sets it moving; no
agent can fail to fetch a resting ball until balls start coming to rest; a tree cannot be observed frozen
in EXECUTING until it actually dispatches passes; and a re-plan cannot collide with a busy executor until
rebounds occur. No amount of reading would have surfaced them in one pass — they are strictly sequential.
The corollary is that the acceptance scenario's **duration** is load-bearing: two of the four stalls let
play run for eight or nine minutes before dying, and a short scenario would have certified the engine as
fixed while it was not.

### 5.Z.7 Recorded, NOT fixed here

Two real findings surfaced by the landing that are deliberately out of scope, plus two carried forward.

1. **The foul heuristic issues ~7 red cards per 9 minutes** — **RESOLVED July 26, 2026, see §5.Z.9.**
   (measured, consistently, across three seeds — extrapolating, every player on the pitch would be
   dismissed inside a full match). `MatchFlowCollisionConsumer`'s FROM_BEHIND high-force capture fires
   roughly every 4–5 seconds where real football fouls once every ~3.5 minutes, so ordinary jostling is
   clearing `FOUL_MIN_FORCE_N`. This entry framed it as a threshold/`[GT]` question needing a foul-rate
   target and a measurement pass. **The measurement pass ran and refuted the framing** — the threshold is
   a cliff, not a dial. See §5.Z.9.
2. **The process-static EventBus makes INTERLEAVED engines non-deterministic.** Ticking two engines
   `a.RunTick(); b.RunTick();` in one loop diverges at tick 1; run sequentially they are byte-identical
   (verified both ways). This is a latent property of #17 §3.2.1's mandated static bus — `ResetForNewMatch`
   handles *sequential* matches, not concurrent ones — and it was invisible only because no production
   event was ever published. Three tests interleaved and now run sequentially. Making the bus per-engine is
   an #17 architecture change, not a match-engine one.
3. **Pass Mechanics #5 logs FM-08 at Error level.** "Lost possession before CONTACT. Race condition." is now
   an ordinary match event — a restart awarded against a passer mid-windup — occurring several times per
   match. The cancel path itself is correct; only the severity is stale. Downgrading it is a #5 change with
   its own tests, so the two composed scenarios declare the log instead.
4. **The `FR-PO-052` certified per-tick perf baseline** now describes an engine that did nothing and needs
   re-capturing on the pinned host (`cert-run-runbook.md`). The Linux gate's anchor is deliberately
   generous and still passes.

### 5.Z.9 Foul & discipline balance pass (July 26, 2026) — §5.Z.7 item 1 CLOSED

Detail: `docs/tracking/foul-discipline-balance-design.md` (converged; AR-1 1H+2M, AR-2 1M+2L,
code AR-3 3M).

**Measured, then fixed.** Over four seeds × 9 minutes of composed play, with an observer on every
collision event:

| | Before | After | Football |
|---|---|---|---|
| Fouls per 90 min | **480** | **21.0** | ~22 |
| Yellow cards per 90 min | **147** | **3.0** | ~3.5 |
| Red cards per 90 min | **75** | **1.0** | ~0.25 |
| Players dismissed from one team, per 9 min | **up to 7** | 0 – 1 | — |

**The finding the measurement produced is that §5.Z.7's own diagnosis was wrong.** The peak
qualifying force distribution is bounded and narrow — p99 = 1175 N, max **2362 N**, because a
collision impulse over `ContactDurationS` cannot exceed it. Replaying the production gate across a
threshold ladder gives 480 fouls at 1200 N, 90 at 2000 N and **0 at 3000 N**: there is no threshold
that yields ~22, and the values in between sit on the last thirty samples of a 130 000-tick run — a
setting that would read as calibrated while actually being noise. A longer cooldown does not rescue
it either (at 2000 N even a ten-second window still leaves 75).

The gap was never a bigger number. The model said *every hard cross-team contact from behind is a
foul*, and the engine produces **seventeen of those per second**. What was missing is the referee's
judgement — a **probability**.

**The change** (`foul-discipline-balance-design.md` KD-F1..KD-F5): a candidate that clears every
existing gate is whistled with `p(F) = min(1, FoulCallProbability × F / FoulImpactForceThresholdN)`,
so a harder challenge is likelier to be given but a hard contact is never automatically a foul. The
**same single draw** decides the call and, on a call, the card severity from the rescaled remainder
`v = u / p` — so there is no new RNG stream and **no `SNAPSHOT_SCHEMA_VERSION` change**. A wave-on
arms no cooldown (arming it would swallow the genuine foul two ticks later), and the consumer now
keeps the **strongest** contact of a tick rather than the first, since force now decides the call.
`FoulCallProbability` = 0.015 `[GT]` (new), `YellowCardProbability` 0.35 → 0.16, `RedCardProbability`
0.05 → 0.011, `FoulCooldownTicks` 60 → 180.

**Calibration needed a live run, not the offline sweep**, and that generalises: the sweep pointed at
0.025, where a real match measured 37.5 fouls per 90 min. Giving 20× fewer fouls means 20× fewer
restarts, so play runs on and the qualifying-contact count *rose* from 36 000 to 129 000 over a
comparable corpus. An offline gate replay finds the right shape cheaply; it never gives the value.

**Acceptance:** `match-engine-discipline-plausible` (#19 ScenarioRunner, Tier B, 6 seeds × 9 min,
~52 s) asserts foul/yellow/red rates in plausibility bands, that **no team is reduced below nine
players** (per seed, not aggregated — one abandoned match must not average away), and that cards stay
a minority of fouls. **Nine of its ten predicates fail on the pre-fix engine**, each by more than an
order of magnitude. Plus 8 unit locks in `MatchEngineFoulCardTests` covering the probability shape,
the wave-on leaving no trace, and strongest-wins capture driven through the real consumer.

Committed with it: `FoulRateDiagnosticTests` (env-gated, `TD_FOUL_DIAGNOSTIC=1`) — the instrument,
which replays the gate offline across a (threshold, cooldown, probability) ladder so one composed run
yields the whole curve; and `MatchEngine.TestOnly_SetCollisionObserver`, the seam that makes the force
distribution observable at all (the collision system takes exactly one consumer, and it is private).

**Recorded, not fixed:** the *contact rate itself*. Seventeen hard cross-team from-behind contacts per
second, on 20% of ticks, is not football — the refereeing model now sits plausibly on top of it, but
the stream underneath is wrong, and it is the next thing to look at for match realism (most likely #12
agent spacing or #3's 60° `BehindDotThreshold` cone). `FoulCallProbability` is a rate knob calibrated
against *that* stream; if it changes, re-measure with the committed diagnostic.

### 5.Z.10 Kickoff keeper placement (July 26, 2026) — both goals were unguarded for ninety minutes

Found by running roadmap A4a's KD-8 Step 0 pilot after the §5.Z.9 balance pass. Step 0's own assertion
now **passes** — the squad-strength extremes are distinguishable (mean margin +28.4 strong-at-home vs
+1.9 strong-away) — but its raw scorelines were not football:

```
strong-at-home:  home 15, 19, 21, 27, 31, 31, 32, 33, 36, 39   away 0 — every match
strong-away:     home  0,  0,  2,  2,  2,  2,  2,  4,  4,  2   away 0 — every match
```

Two things wrong at once: an order-of-magnitude goal rate, and **the away team never scoring in twenty
full matches.** Step 0 could not see either — it asserts only that the two buckets *differ*.

**Root cause.** `InitializeKickoffState` placed every agent of a team on one x-line
(`HomeLineXM` / `AwayLineXM` = 26.25 / 78.75), spread evenly across the pitch width by roster index.
The keeper is index 0, so it got the first lateral slot: `y = WIDTH × 1/12 = 5.67`. Each keeper
therefore began the match **26 m upfield of the goal it defends and 28 m off-centre** — on the
touchline, nowhere near the goal mouth.

That would be a cosmetic kickoff wrinkle if keepers moved. **They do not:** the Physics phase skips
goalkeepers at Stage 0 (GK locomotion is Goalkeeper Mechanics #11), so boot placement *is* the
keeper's position for the entire ninety minutes. Both goals stood completely unguarded, all match,
in every match the engine has ever played.

**The fix** is four lines: a keeper spawns at `(GkKickoffDepthM, WIDTH/2)` on the goal line it
defends, mirrored for the away side by the existing `MirrorPitchIfAway` (so `(5.5, 34)` becomes
`(99.5, 34)` — each keeper facing the pitch from its own line). `GkKickoffDepthM` is a `[CROSS]`
mirror of `PositioningAIConstants.GK_DEPTH_M`, the resting depth #12's own `ComputeGkSlot` produces
for a ball on the centre spot, so the boot placement and the positioning model agree by construction
rather than drifting. Outfield placement is untouched.

**Effect, measured at both scales — and the second scale is the important one.** In neutral 9-minute
runs the away team began scoring immediately and the ball crossed the goal line it attacks for the
first time (`min ball x` went from 8.2 to **−0.1**); scorelines went 1–0 / 0–0 to 1–1 / 1–0. But
re-running the full Step 0 pilot (20 × 90 minutes) shows the fix is **necessary and nowhere near
sufficient**:

| bucket | home goals | away goals | mean margin |
|---|---|---|---|
| strong-at-home (+3 / −3) | 19 – 40 | **0 in all ten** | 25.3 (was 28.4) |
| strong-away (−3 / +3) | 0 – 6 | 0 – 2 | 1.7 (was 1.9) |

The away side did start scoring — in 3 of 20 matches, up from 0 of 20 — so the blocked path was real.
But a **strong away team still averages 0.5 goals while a weak home team averages 2.2**, and the
strong-at-home margin barely moved. Whatever produces that is structural and is NOT the keeper spawn.
Recorded as its own finding in §5.Z.11.

New locks in `MatchEngineMatchFlowTests`: each keeper stands off the goal line it defends and centred
on the mouth, and the two keepers guard **opposite** ends — the second being the load-bearing one,
since two keepers on one line is what the defect amounted to in effect.

### 5.Z.11 Recorded, NOT fixed — a structural home/away scoring asymmetry, and a goal rate ~10× football's

Two findings from the Step 0 re-run above. Both are measured, neither is diagnosed, and the second
depends on the first.

1. **Home/away asymmetry (severe).** Over 20 full matches with a ±6-point squad differential correctly
   measured and applied, the home side scores 19–40 when strong and 0–6 when weak, while the away side
   scores 0–2 in every configuration. A strong side is worth ~25 goals a match at home and ~0.5 away —
   roughly a fiftyfold home advantage, where football's is about 0.3 goals. Strength is applied
   correctly (`dSquad` measures ±6.0 as intended), and 9-minute NEUTRAL runs look roughly balanced
   (ball in each attacking box 2.7% vs 1.9% of ticks, 1–1 and 1–0 scorelines), so the asymmetry either
   compounds over a full match or is specific to the `ConfigureSquads` path — the two candidates a
   measurement pass would separate. This is the project's recurring ERR-008-002 defect class (*"every
   spec worked example and every AR-1 fixture used the home team"*) and should be attacked the same
   way: measure per-team shots, final-third time and possession over a FULL match, not nine minutes.
2. **Goal rate ~10× football's**, on top of the asymmetry. A stationary keeper is a collision body so
   it deflects what hits it, but it cannot dive, close down, or narrow an angle. The honest next step
   is Goalkeeper Mechanics #11 — already wired and snapshot-safe but **opt-in and default-off**
   (`EnableGkHeading`), with "flip the default to on, take the digest rebaseline" already recorded as
   its remaining work — plus GK locomotion, without which a committed `SaveIntent` has no body behind
   it.

**Step 0 does not catch either, and that is a gap in Step 0.** Its assertion is
`strongHomeMargin > strongAwayMargin`, which 25.3 > 1.7 satisfies comfortably — so the pilot now
**passes** while reporting 25–0 scorelines. It was designed to ask "is there signal?", not "is the
signal football?". **A4a must stay blocked** regardless: fitting the round-resolution model's three
parameters against 25–0 results would calibrate the quick-sim to reproduce a defect faithfully across
a whole league, which is worse than not fitting it at all.

### 5.Z.12 Per-side constant pairs removed from boot placement

A follow-up to §5.Z.10 rather than a new defect: the keeper bug's *shape* was a Home/Away pair of
constants stating one fact twice, and that shape is the common factor behind three defects in this
engine's history — ERR-008-002 (away zone modifiers inverted), ERR-013-009/010 (`AttackingDirection`
inverted) and the §5.Z.10 keeper spawn. A pair has two places that must agree; a mirror has one.

`InitializeKickoffState` now writes each agent's position and facing **once**, in the acting team's
own-half frame, and passes both through the mirror helpers the engine already uses everywhere else
(`MirrorPitchIfAway` for the position, an affine point; `MirrorVelocityIfAway` for the facing, a free
vector). Deleted: `HomeLineXM` / `AwayLineXM` → one `OutfieldKickoffLineXM`; `HOME_FACING_DEG` /
`AWAY_FACING_DEG` → nothing at all, since a mirrored `+X` needs no degrees; and `FacingFromHeading`,
now unused.

Removing the trig also *strengthens* the property that helper existed for. Its doc explained that it
special-cased the axis-aligned headings to keep `Mathf.Sin(180°) ≈ 8.7e-8` out of the deterministic
snapshot; negating exact unit components cannot produce that fuzz in the first place.

**This one is not behaviour-neutral, and deliberately so.** The x line is byte-identical
(`105/4` mirrors exactly to `105×3/4`), but the away side's lateral spread now mirrors too
(`y → WIDTH − y`) where both teams previously got the same `spreadY`. Boot state therefore differs
and every digest moves from tick 1. What it does *not* change is anything durable: outfielders are
moved onto real formation slots by the AI on the first stride tick, and the keeper is placed
explicitly. All determinism tests are comparative (two same-seed runs), so none needed rebaselining;
the full gate is green.

Also fixed alongside, in the same de-duplication spirit — **ERR-008-016**: Decision Tree #8's zone
bands were `0–35 / 35–65 / 65–105` under a `[DERIVED] — split pitch into thirds` tag, making the
attacking third 40 m and the middle third 30 m. Both bounds now derive from the pitch length
(`L/3`, `2L/3`). Equal thirds make the boundary pair **self-mirroring**, so the bands stop depending
on attacking direction at all — the same duplication being removed one level up. Measured
behaviour-neutral over two 9-minute composed runs.

### 5.Z.13 Contact rate (July 27, 2026) — §5.Z.9's recorded finding CLOSED, and its diagnosis refuted

§5.Z.9 recorded the contact stream as "not football" and named two suspects: "#12 agent spacing or #3's
60° `BehindDotThreshold` cone". Measured first (`MatchBalanceDiagnostic_ReportsContactStream`, which
counts every agent-agent contact by type and cross-team-ness AND samples the pairwise-separation
distribution once a second), **both are wrong**:

| | Before | After | 
|---|---|---|
| agent-agent contacts | 94–112 / s | **2.5 / s** |
| cross-team FROM_BEHIND | 26.6–58.2 / s | **0.5 / s** |
| ticks with ≥1 qualifying | 35.9–70.9 % | **0.8 %** |
| agent pairs closer than 0.85 m (the combined hitbox) | 1.3–1.5 % | 1.5 % — **unchanged** |
| agent pairs ≥ 3 m apart | 97.5 % | 97.6 % — **unchanged** |

Agents are **≥ 3 m apart 97.5% of the time** and only ~3.4 of the 231 pairs are touching at any instant.
Spacing was never the problem, and neither was the cone. `ProcessAgentAgent` emitted a fresh
`CollisionEvent` **every physics tick a pair overlapped** — so two players leaning on each other for one
second produced sixty "contacts". ~3.4 sustained overlaps × 60 Hz is exactly the ~110/s observed.

**The fix** is a contact-**onset** gate: a second `CollisionPairBitfield` records the pairs in contact on
the previous tick, and a pair emits one event on the rising edge. The physical response — impulse,
position correction, grounded/stumble — stays per-tick, because that is genuine physics and gating it
would let agents sink into each other. Separating *the physics* from *the event* is the whole change.

That set is the collision system's only cross-tick state, so it is captured through a new
`CollisionContactState` carrier (the `PressingTickState` convention — typed carrier out, byte layout
owned by the composition root) and serialized at **`SNAPSHOT_SCHEMA_VERSION` 18 → 19**. Without it a
restore mid-contact would re-emit an onset the uninterrupted run had already spent.

**The denominator moved, so the referee was re-calibrated**, exactly as §5.Z.9's own note instructed
("if that contact rate changes, re-measure with `FoulRateDiagnosticTests`"). Left alone, 0.015 gave
~0.4 fouls per 90 min. Re-measured on the committed ladder: 0.020 → 18, **0.030 → 24**, 0.040 → 35.
`FoulCallProbability` 0.015 → **0.030**, and `match-engine-discipline-plausible` passes unchanged.
Note the live-vs-sweep correction now runs the **opposite** way from §5.Z.9's: the sweep replays a stream
generated at the near-zero shipped rate, so restoring fouls *adds* restarts, which stops play and lowers
the contact count.

**Recorded, not fixed:** 0.5 cross-team from-behind contacts/s is ~2700 per match, and ~3.4 pairs
touching at any instant is perhaps 2–3× a real match. That residual *is* a #12/#3 balance question — but
it is a factor of two, not the factor of fifty the per-tick re-emission was contributing.

### 5.Z.14 The home/away scoring asymmetry (July 27, 2026) — §5.Z.11 item 1 CLOSED

§5.Z.11 named the two candidates and the measurement that separates them: "the asymmetry either
compounds over a FULL match or is specific to the `ConfigureSquads` path". `MatchBalanceDiagnostic`
now runs **full 90-minute matches**, both configurations, attributing every column to the team it
belongs to. Neither candidate survived: the asymmetry is present in **neutral** runs, present in
**configured** runs, and present **from the first half** — so it is structural, and it does not compound.

What the per-team columns showed is the whole diagnosis. Possession (1.8–2.4% each), passes (~700 each)
and time in the third **each team attacks** (10–15% each) were **symmetric**. Two columns were not:

- ball in the box each team attacks: team 0 **0.9–1.9%**, team 1 **0.2–0.4%**
- ball x range: max **105.1–106.6** (past the goal line), min **2.1** — *the ball never reached x = 0*.

Equal territory, one goal ever threatened. That is not a possession defect; it is an aiming defect.

**Root cause — ERR-006-001.** `GoalGeometryProvider.Get()` returns `GoalLineX = PitchLength`
unconditionally, and says so in its own doc: *"Assumes the attacking team is shooting toward
X = PitchLength (right goal). Stage 1+ will supply attack direction from match context."* Nothing ever
supplied it. `ShotPlacementResolver` is written to match, down to `Mathf.Max(baseAimDirection.x, ε)`.
So **both teams shot at x = 105** — team 1 at the goal it defends, and any that went in were credited by
the exit-half-space rule to team 0, inflating one side while zeroing the other. Decision Tree #8 is
correctly team-relative (`GetOpponentGoalCentre(teamId)`), which is why team 1 *decided* to shoot in the
right places and then kicked the ball the wrong way. This is the ERR-008-002 / ERR-013-009 class again.

**The fix is the mirror, not a second goal.** Per §5.Z.12 — "a pair has two places that must agree; a
mirror has one" — `ShotWorldAdapter` now maps the away team's world state **into** #6's canonical
attack-+X frame on the way in (`MirrorPitchIfAway` for the position, `MirrorVelocityIfAway` for velocity
and facing) and maps the resulting kick back **out** on `ApplyKick`. The mirror is a 180° rotation about
Z, so the same negate-x-y rule is correct for velocity and for spin (a proper rotation transforms a
pseudovector exactly as it transforms a vector). Every APPROVED #6 formula, constant and test is
untouched, and the boundary sits in the composition root that already owns team-relativity.

| | Before | After |
|---|---|---|
| scorelines (4 full matches) | 6–0, 10–0, 2–0, 3–0 | **6–6, 12–5, 2–6, 11–10** |
| team 1 goals | **0** | 5, 5, 6, 10 |
| ball min x | 2.1 | **−2.4** (crosses the goal line) |

Team 1 now scores in every match and wins one.

### 5.Z.15 Goalkeeper on, and mobile (July 27, 2026) — §5.Z.11 item 2

Two things were true at once: Goalkeeper Mechanics **#11 was built, wired, snapshot-safe and switched
OFF** (`EnableGkHeading`, default false, with "flip the default to on, take the digest rebaseline"
recorded as its remaining work), and **keepers could not move** — §5.Z.10 fixed the *spawn* but left the
note that the Physics phase skips goalkeepers, so boot placement was the keeper's position for ninety
minutes. Every match was played without a keeper who could either attempt a save or close an angle.

Both are now on. The default flips to ON (`DisableGkHeading()` added for the tests and hosts that want
the old path), and `RunPhysicsPhase` drives each keeper through the **same** per-agent
`AgentMovementSystem.Update` the batch seam calls. No new locomotion model was needed: the Decision Tree
already runs for keepers and dispatches `MOVE_TO_POSITION` at the #12-composed GK slot, which
`ComputeGkSlot` makes a function of ball position — only the integration was missing. #2's documented
batch contract is untouched, and #11 keeps what is genuinely its own: the dive, the save, the claim.

**Flipping the default surfaced a real wiring gap**, which is roadmap C5 exactly as predicted: the
engine boots the Pass/Shot/DecisionTree `EventBusRegistrar`s but never the Heading/Goalkeeper ones, so
the first mistimed header threw `ERR_EVT_UNREGISTERED_ORDINAL` out of `EmitFailedAttempt`. Invisible
while the flag was off, because the publish path had never run. Both registrars now boot alongside the
other three.

**Goal rate — improved, NOT closed, and the honest number is the one below.** Over the same four full
matches: **14.5 goals per match with the keeper absent → 12.75 with the keeper on and mobile.** Against
football's ~2.7 that is still ~4.7×.

Two things are worth stating plainly rather than rounding away. First, §5.Z.11's ~10× was measured on
an engine where one side could not score at all; fixing that (§5.Z.14) *raised* the goal count before
any of this lowered it, so "10× → 4.7×" is not a like-for-like improvement and should not be read as
one. Second, an intermediate measurement of this work looked far better (≈ 4.25 per match) and was
wrong: one of the four matches was stalled by §5.Z.16's keeper defect and was suppressing goals. The
number above is post-fix and is the one to believe.

So the item §5.Z.11 named — "flip #11 on, plus GK locomotion" — is now **done**, and it was worth doing
(a keeper that can neither dive nor move is not a keeper), but it was not sufficient. What remains is
the quality of the save, not its existence: the Stage-0 `SaveIntent` trigger is a conservative
world-state heuristic, `GoalkeeperDiveKinematics` is a synthetic Stage-0 dive, and nothing narrows an
angle or comes for a cross. **Recorded as the next lever on the goal rate**, ahead of further shot or
finishing tuning — tuning the shooter against a keeper this rudimentary would fit the finishing model
to the keeper's deficiencies.

### 5.Z.16 The keeper stall, found by the same measurement pass

Making the keeper a live, mobile agent gave it something it never had: the ability to **win**
possession. Nothing in the engine could make it give the ball back up — #11's distribution is not
engine-driven, and the Decision Tree has no keeper-distribution action. Measured with a GK-possession
column added to the same driver, one of four full matches stalled: **team 1's keeper held the ball for
33.5% of the second half** (0.0–0.1% in the three healthy matches), with the ball parked in one box and
passes collapsing 358 → 117.

**Two changes, and the measurement is what said the first one alone was not enough.** The Laws' own
answer — **Law 12's six-second rule** — went in first: on expiry possession is cleared, leaving the ball
at rest at the keeper's feet, exactly the state `RunLooseBallPickup` already handles. Re-measured, the
stall barely moved: **33.5% → 33.4%**. The keeper was not holding one long possession; it was being
*re-designated* as the loose-ball collector every time, because the collector is "this team's nearest
agent to the ball" and for a ball sitting in a team's own six-yard box that is always the keeper. The
defect was re-acquisition, not hold duration.

So the keeper is also **never the designated loose-ball collector**. A keeper claiming a ball that
*arrives* is untouched — that is First Touch #4 and #11's save, and it is what a keeper is for; what is
removed is sending the keeper to fetch a ball that has come to rest, which is not a thing keepers do.
Re-measured: **GK possession 0.0–0.1% across all four matches**, every match plays out, and the
suppressed match's scoreline recovers from 1–0 to 12–6.

Both are kept. The six-second rule is correct football and a genuine backstop; the collector exclusion
is what actually closed this stall. No new physics and no invented distribution model: when #11's
distribution becomes engine-driven it replaces the rule's body, not its trigger. `GkMaxHoldTicks` is
`[DERIVED]` from `GK_MAX_HOLD_SECONDS` — a Law, not a balance lever. Serialized at v19 with the
collision contact set, since a restore with a zeroed counter hands a keeper a fresh six seconds.

**The rule is a backstop, and the tests say so.** Measured, healthy play has a keeper distribute after
~54 ticks, well inside the 360 — so a composed run never reaches the release branch. Locking it needed
two tests, not one: the branch itself driven through a `TestOnly_` seam (or the code that exists solely
to break the stall would itself be untested — the never-compiled-surface trap), plus the invariant over
composed play that the hold counter never exceeds the cap, with an explicit non-vacuity assertion that
the run actually put the ball in a keeper's hands.

### 5.Z.17 The goalkeeper's save (July 27, 2026) — §5.Z.15's lever, measured and discharged

§5.Z.15 recorded the next lever on the goal rate as *"the quality of the save, not its existence"*.
That framing carries a premise: that saves happen and are merely poor. **They did not happen.**

Measured over three full 90-minute matches with a new instrument — `GkSaveDiagnosticTests`, the first
in this tree ever to report a goalkeeper statistic of any kind — the keepers made **zero** hand
contacts with the ball across all six keeper-matches. "Save quality" was not a low number; it was
undefined. Detail note: `docs/tracking/goalkeeper-save-pipeline-design.md`.

The instrument reports the pipeline as a **funnel**, which is what localised it: `armed → SAVE
committed → Anticipate → Diving → Airborne → contact → caught`. Every stage up to and including the
dive fired at healthy rates — 14–41 SAVE commitments and 13–31 dives a match. The chain ended at
contact, at exactly zero. Three defects, each independently sufficient (**ERR-011-002/003/004**):

1. **The dive had no direction.** `ComputeDiveDirectionLateral`'s only non-zero branch is gated on
   `SaveIntent.DeflectionTarget`, which the engine's sole producer sets `null`. Mean
   `|diveDirectionLateral|` measured **0.000** across every dive ever launched; the envelope's closest
   approach to the ball over a whole match was **2.75 m short**. Not a near miss — the keeper dived
   straight up on the spot. The conflation is the cause: `DeflectionTarget` is where the keeper wants
   to *put* the ball, not where it should *dive*.
2. **A catch was arithmetically impossible.** `OnShotExecutedEvent` had zero callers anywhere, so
   `reactionWindowAchieved` was permanently 0, capping quality at `0.70 × rawHandling` — a **measured
   ceiling of 0.630** for a perfect keeper against `CatchThreshold` 0.78.
3. **The keeper woke for the wrong end of the pitch and never stood down** — the §5.Z.12 per-side-pair
   class again, plus an `Anticipate` state with no exit but a dive. Keepers held Anticipate for
   **76–92%** of every match.

Post-fix: dive direction 0.000 → **1.000**, best miss 2.75 m → **−0.07 m**, contacts **0 → 15**,
Anticipate share 76–92% → **11–18%**. Locked by `match-engine-goalkeeper-saves` (#19 ScenarioRunner,
Tier B, 4 seeds × 15 min, 56 s), whose predicates assert *reachability* stage by stage; **11 of its 12
fail on the pre-fix engine**, two at exactly zero. Full gate green; no `SNAPSHOT_SCHEMA_VERSION`
change, no new RNG stream or draw site, and no change to the draw order.

**And the goal rate did not move at all: 15.3 → 15.3 per match, against football's ~2.7.** That is the
result. Three genuine defects, each of which had to be fixed before a save was possible, are worth
**nothing measurable** on the scoreline. **§5.Z.15's lever was real, is now spent, and was not where
the mass is** — the same shape as §5.Z.9 and §5.Z.11, where the measurement refuted its own brief, and
the reason the acceptance scenario deliberately pins no save percentage and no goal rate.

Worth recording *how* that number was arrived at, because an earlier build of this pass published
**14.0** on the identical seeds and would have let §5.Z.17 claim the keeper was worth about a goal a
match. The difference was a single unit defect found in adversarial review — the shot was stamped in
seconds against a §3.2 pipeline that is entirely milliseconds — and correcting it re-rolled every
subsequent deflection. Three matches of a chaotic quantity does not resolve a one-goal difference. The
claim this section makes is therefore the weaker, defensible one (*no detectable effect*), not the
stronger one the first number happened to support.

**Recorded, NOT fixed — and this is now the honest next lever.** The measurement that closed the save
question opened a larger one on the shot side, verified against source and detailed in the note's §7:

- **A shot essentially cannot miss.** Aim is hardcoded to `u ∈ {0.1, 0.9}`, i.e. **0.732 m inside the
  post**, against ~2.25° of typical angular error where >5.73° is needed to miss — and the largest
  live error multiplier, the pressure penalty, is hardcoded to zero in the engine's own adapter.
- **There is no crossbar.** `BallCollision.CheckBoundaries` gates *every* boundary test, goals
  included, behind `z < Ball.Diameter` (0.22 m), so a ball crossing the line airborne is neither a
  goal nor out of play; and `ShotExecutor` never reads `finalDirection.z`, so the entire vertical half
  of the placement and error model influences nothing. The goal is 7.32 m wide and of unbounded height.
- **There are no blocked shots.** `BallCollisionHandler.OnAgentCollision` is called in production and
  its body is an empty `TODO`. No agent deflects the ball by contact; posts are non-physical.

In football roughly 30% of shots are blocked and 30% miss the target. Here both are approximately
zero, which is a larger multiplier on the goal rate than anything a goalkeeper does. **A4a stays
blocked, but the reason is now specific:** the residual is the shot-outcome distribution, not the
keeper.

### 5.Z.18 The shot-outcome distribution (July 27, 2026) — §5.Z.17's residual, fixed and measured

Owner document: `docs/tracking/shot-outcome-distribution-design.md` (KD-1..KD-8, the measured
table, and the AR history). The four §5.Z.17 §7 defects are closed:

- **ERR-006-002** — `ShotExecutor` now conforms to #6's own §3.5.6/§3.5.7: the intended aim is
  the launch-tilted composition and `finalVelocity = finalDirection × kickSpeed`, so the vertical
  half of the placement/error model is live for the first time.
- **ERR-006-003** — the error cone is a cone: goal-plane displacement is `tan(err) × distance`
  (the former mapping was a fixed 0.128 m/° at every range; the spec's own reference-anchored
  value was 0.35 m/° at 20 m, which the tangent form reproduces exactly).
- **ERR-001-004** — the `z < Diameter` gate is out of `CheckBoundaries` AND `IsOutOfBounds`
  (Law 9/10): an airborne crossing is adjudicated at the crossing — goal under the bar, corner /
  goal kick / throw-in otherwise. **The goal has a crossbar.**
- **ERR-003-007** — `BallCollisionHandler.OnAgentCollision`'s TODO is live: fast balls deflect
  off bodies via the new `BallCollision.ApplyAgentDeflection` (#1 §3.1.10.1 `BodyPartCoefficients`
  — first consumer), gated Controlled-ball-out / sub-10 m/s-out, with the approaching-only
  response as the **stateless** self-block guard (no cooldown state, no schema bump).
- Plus: the `ShotWorldAdapter` pressure query went live (was hardcoded `0f`; reuses the
  first-touch `PressureEvaluator` with the §5.Z.14 canonical-frame un-mirror), and
  `MIN_GOAL_VISIBILITY` rose 0.05 → 0.12 so the #8 SHOOT gate can actually reject a walled-off
  shot.

**Measured (3 full matches, `ConfigureSquads` path, same seeds pre/post):** goals per match
**15.3 → 12.3**, goals per shot **0.24–0.29 → 0.14–0.25**, fast-ball body contacts
**0 → 560–612 per match**. Every previously-unreachable outcome class now occurs. **And the
measurement names the remaining mass, which is NOT this pass's mechanisms:** shot volume
(59–70 shots/match, ~2.5× football — a DT-selection/possession-churn property) and **shot speed**
(measured means 7–10 m/s against football's ~25 — `VFloor`/`VCeiling`/`PowerIntent` shaping in
#6/#8), which keeps shots on the ground (the new crossbar rarely bites at these speeds) and gives
keepers easy contacts they still rarely hold (§5.Z.17 §7.5). Those, with the keeper's conversion,
are the recorded next levers.

Acceptance: `match-engine-shot-outcomes` (#19 ScenarioRunner, Tier B, 4 seeds × 9 min, ~59 s) —
**3 of 8 predicates fail on the pre-fix engine, verified by executing the scenario in a worktree
at the pre-fix commit**: the over-bar crossing adjudicated as *nothing* (`cue=None`), the
under-bar airborne crossing scoring nothing, and deflections at exactly zero. Its determinism
predicate runs its two engines **sequentially** — the first draft interleaved them and failed on
the documented §5.Z.7 process-static-EventBus property. Unit locks: `ShotOutcomeBallPhysicsTests`
(9), `BallCollisionHandlerTests` (3), `ShotPlacementResolverShotOutcomeTests` (5), plus the two
inverted-contract tests (`Goal_AirborneCrossing_UnderTheBar_IsAGoal` + over-bar sibling;
`OutOfBounds_HighAboveTouchline_ReturnsTrue`) — the Phase-H "tests encoded the old contract"
class, intent preserved. Instrument: `ShotOutcomeDiagnosticTests` (env-gated
`TD_SHOT_DIAGNOSTIC=1`, assertion-free). **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG
stream / domain tag / draw site, no draw-order change**; digests move for any match containing a
shot or an airborne crossing, as intended.

### 5.Z.19 Shot speed and the physical goal frame (July 28, 2026) — §5.Z.18's residual lever (b)

Owner document: `docs/tracking/shot-speed-woodwork-design.md` (KD-1..KD-7, the measured table,
the AR history). §5.Z.18 measured shot-tick speed means of 7–10 m/s against football's ~20–25
and named `VFloor`/`VCeiling` × `PowerIntent` shaping as the lever. Two structural causes
composed, and fixing them made a third defect load-bearing:

- **ERR-008-016** — #8 §3.5.3's `PowerIntent = clamp(goalOpening × A_Finishing, 0.1, 1.0)` is a
  product of two [0,1] factors, so nearly every shot pinned at the formula's own 0.1 clamp floor
  — the engine's strikers were tapping at 10–30% power. Now floor-plus-modulation
  (`[GT] POWER_INTENT_FLOOR` = 0.65): a deliberate shot is always struck hard; opening ×
  finishing modulates the top band (an elite finisher with an open goal reaches exactly 1.0).
- **ERR-006-004** — #6's `VFloor` 10 → **24** over two measured calibration iterations (the
  formula multiplies the ceiling span by attrFraction AND powerIntent, so the anchor, not the
  span, must carry the base pace; at 10 a neutral FULL-power vBase capped at ~16 m/s before
  reducers). `VCeiling`/`VAbsoluteMin` unchanged; A.1.4's stacked-penalty visibility preserved.
- **ERR-001-005** — at football pace the ball moves ~0.42 m per tick, so the goal frame became
  physical and precisely adjudicated: `BallCollision.ApplySweptGoalFrameCollision` (the tick's
  movement segment against six capped cylinders, earliest hit wins — a discrete test tunnels a
  0.12 m post; **`ApplyGoalPostCollision` finally has a production caller**), and
  `CheckBoundaries` gains a `prevPosition` overload adjudicating goal-line crossings at the
  segment's interpolated plane crossing (the detected position is up to 0.42 m past the plane —
  pre-fix a rising ball crossing UNDER the bar read as over it). Engine wiring is
  capture-before-integrate / collide-after-integrate in the Physics phase;
  `_prevTickBallPosition` is WITHIN-tick (the `RestartAppliedThisTick` class), so **no
  `SNAPSHOT_SCHEMA_VERSION` change**; the woodwork counter is diagnostic observation (the
  `AiPhaseRunCount` class).

**Measured (3 full matches, `ConfigureSquads` path, same seeds pre/post):** shot-tick means
**6.9–10.3 → 14.7–16.1 m/s**, maxima **15.3–18.9 → 23.3–27.6**; shots per match **59–70 →
31–45** (football ~25 — pace changes the possession economy, fewer but real attempts);
off-target exits roughly doubled; woodwork strikes 0 (structural) → **1 / 0 / 5 per match**
(football ~0.5–1 — the right order of magnitude). **Goals per shot ROSE, 0.14–0.25 →
0.38–0.42** (goals 12.3 → 14.7 per match): a
football-pace shot beats this keeper far more often than a roller — the catch/parry conversion
(§5.Z.17 §7.5, residual lever (c)) is now measured against real shot speeds for the first time
and is the clear next lever, alongside shot volume (lever (a), unchanged at ~2× football).

Acceptance: `match-engine-shot-speed` (#19 ScenarioRunner, Tier B, 2 seeds × 9 min + scripted
frame probes, ~46 s) — **5 of 7 predicates fail on the pre-fix engine, verified by executing the
scenario against the unmodified tree before the fix landed** (speed floors unreachable at
mean 7.39 / max 12.50; both frame probes adjudicated as exits; the rising crossing misread as a
goal kick). Unit locks: `SweptGoalFrameTests` (11 — headlined by the tunneling discriminator: a
segment that fully crosses a post inside one tick, invisible to any discrete test) +
`OptionGeneratorTests` PowerIntent floor/ceiling/monotonicity (3). No new RNG stream / domain
tag / draw site; digests move for any match containing a shot, as intended.

### 5.Z.20 The keeper's catch/parry conversion (July 28, 2026) — §5.Z.19's residual lever (c)

Owner document: `docs/tracking/gk-catch-parry-conversion-design.md` (KD-C1..KD-C5, the measured
tables, the AR history). §5.Z.19 measured goals/shot RISING to 0.38–0.42 at real shot pace and
named the keeper's conversion the dominant goal-rate term; §5.Z.17 §7.5 had recorded the reaction
window as incoherent. The baseline measurement sharpened both: the §3.2.3 window — 30% of the
§3.5.1 quality blend — read **0.000** at contact, with dives dated against shots struck
**85–349 seconds** earlier, and one catch in three full matches.

- **ERR-011-005** — the window was re-evaluated per frame, so the value the contact consumed was
  dated by the ball's whole FLIGHT time; the spec's own §3.2.5 worked example scores the dive
  COMMIT. Now computed once at the dive-launch frame and frozen into `GkContactState`.
- **ERR-011-006** — the detection stamp was never cleared (stale-shot dating), and save episodes
  with no #6 shot event (rebounds, deflections) had no anchor at all. The stamp now dies with its
  episode (`ClearSaveIntent` + save resolution), and the new `OnThreatArmed` — called by the
  engine each armed stride — seeds it at episode onset when none is live; a live stamp always
  wins, so the stamp itself is the latch and **no new engine state exists** (already serialized
  in the v19 GK block).
- **KD-C3 `[GT]` recalibration, all inside the #11 §3.4.3/§3.4.5 spec ranges**, measured over two
  full-match iterations: `ReactionBaseMs` 350 → 220, `ReactionBallSpeedCoeff` 8 → 3, tolerances
  120/80 → 200/140 (the engine's discrete ~100–300 ms commit grid read as deep-early against the
  human-continuous-time values); `HandlingBase`/`HandlingKAttr` 0.45 → 0.60 and `CatchThreshold`
  0.78 → 0.74 (the Stage-0 pointQuality term is a fixed noise lottery — E ≈ 0.68, invariant under
  every `[GT]`, blind to attributes, recorded in the owner doc §4.3 — and the old values could
  not reach the catch band through it even with a perfect window).

**Measured (3 full matches, `ConfigureSquads` path, same seeds pre/post):** reaction window at
contact **0.000 → 0.30–0.67**, elapsed-when-airborne **85–349 s → ~0.3 s**, quality at contact
0.36–0.50 → **0.41–0.79**, catches **1 → 6** of 15 contacts, goals per match **14.7 → 8.0**
(13/13/18 → 6/9/9), **goals per shot 0.38–0.42 → 0.19–0.26** at 31–38 genuine strikes per match.
Scorelines 8-5 / 7-6 / 13-5 → **3-3 / 6-3 / 8-1**. **No `SNAPSHOT_SCHEMA_VERSION` change, no new
RNG stream / domain tag / draw site, no draw-order change.**

**The measurement also bounds what is left of lever (c), and it is not conversion:** a contact
almost always stops the shot, and the keeper contacts only ~a quarter of on-target shots — the
CONTACT RATE (the #12 GK slot's lateral positioning and the commit-to-arrival timing, measured at
1.7–4.6 m mean lateral offset while airborne) is the residual, recorded in the owner doc §7.1 as
a behaviour change to APPROVED specs, not a `[GT]` dial. With shot volume (lever (a)) it bounds
the remaining ~3× gap to football's ~2.7 goals/match.

Acceptance: `match-engine-keeper-conversion` (#19 ScenarioRunner, Tier B, `ConfigureSquads` path)
— the frozen dive window is alive, a contact converts to the parry band, the keeper holds a ball;
plus the 7-lock `GoalkeeperConversionTests` unit fixture driven through the real orchestrator.
Instrument fallout fixed with the pass: shot counting re-anchored from `ShotDetectedTickMs` edges
to the new `TestOnly_ShotContacts` genuine-strike counter (the arming stamps redefined a stamp
edge as "a threat episode").

### 5.Z.21 Shot volume (July 28, 2026) — §5.Z.19's remaining lever (a), the distance term U_SHOOT never had

**The lever.** §5.Z.19 discharged roughly half the shot-volume excess as a side effect of real
pace (59–70 → 31–45 shots/match; football ≈ 25) and named the remainder a DT-selection /
possession-churn property. Measured first (`ShotOutcomeDiagnosticTests` v1.3 gains the two
dimensions the question turns on — per-shot distance to the attacked goal, and possession-churn
context): the finding is the DISTRIBUTION, not the count. Mean shot distance ran **30–34 m**
against football's ~17, with ~60% of shots beyond 22 m — clustered AT the §3.1.4.2 range-gate
boundary. Cause, verified against source (**ERR-008-017**): `U_SHOOT` contains **no distance
term**, and `GoalOpeningScore` is scale-free by construction (goal arc and near-goal-blocker
occlusion both shrink ~1/d), so within range a 34 m shot scored identically to a 10 m one —
while football's P(goal | shot) falls ~tenfold over that span. The ERR-008-016 class: the
formula omitted the strongest single predictor of shot value in the game it models.

**The fix** (spec + code same commit): §3.2.3.1 gains a multiplicative `DistanceQuality_SHOOT`
— 1.0 inside `[GT] SHOOT_SWEET_RANGE_M` (12 m; every close-range utility bitwise untouched, so
the §5.Z.17–§5.Z.20 calibrations stand), hyperbolic decay `FALLOFF / (FALLOFF + (d − SWEET))`
beyond. The range gate stays the hard cap: a preference, not a cliff — the ±0.15 composure-noise
band still lets an adventurous agent occasionally take the 30 m shot, which is football.

**Calibration refused half the design target, and that is the finding worth keeping.** The
falloff ladder (3 full matches per rung, same seeds): FALLOFF 10 → 30 shots/match at 38%
beyond 22 m, goals 9.0; 9 → 24.0 shots but 39% long and goals 7.7; **8 → 17.7 shots at 30%
long, goals 4.7 — the closest this engine has ever measured to football's ~2.7, with
football-shaped scorelines (2-2 / 3-2 / 5-0)**; 6 → 12.3. The two halves of the design target
(count ≈ 25 AND mean distance ≤ 22 m) are not simultaneously reachable by this lever: once
long shots correctly lose to passes, volume is bounded by close-chance CREATION, and at ~3×
football's final-third churn almost no possession penetrates the box (0.05 shots per third
entry vs football's ~0.2). FALLOFF = 8 chosen — the roadmap chain wants a goal rate that makes
the A4a corpus worth fitting, and a football-shaped distribution at 18 shots serves that
strictly better than a football-count 24 still dominated by range-boundary strikes.

**Measured (3 full matches, same seeds pre/post):** shots 31/35/38 → **17/19/17**, mean shot
distance 30–34 m → **16.5–27.1 m**, long-shot share 60% → 30%, goals 8.0 → **4.7**/match,
goals/shot 0.19–0.26 → 0.24–0.29. Speed floors unaffected (the decay changes which shots are
taken, not how they are struck). No schema/RNG/draw-order change.

Acceptance: the `match-engine-shot-speed` scenario gains a mean-shot-distance ceiling (24 m)
that **fails on the pre-fix engine at exactly 30.0, verified by execution before the scorer
change landed**, + 5 `UtilityScorerTests` locks (sweet-range bitwise indifference, monotone
decay + knee continuity, exact half-quality at SWEET + FALLOFF, the discriminating
long-vs-close-vs-pass comparison, [GT] shape guards). One existing lock re-anchored with intent
preserved (`ShootMidfield_LongShotsRaw12` compared a zone ratio at 28 m, where the decay pushes
the suppressed branch into the UTILITY_FLOOR clamp — moved inside the sweet range), and the
`match-engine-shot-outcomes` corpus resized 9 → 18 min/seed after the full gate caught its
`goals-still-scored` reachability predicate failing at the calibrated rate (its 9-min
neutral-path corpus measured zero goals — the keeper-conversion corpus-sizing lesson; the
sanity ceiling rescaled with the window, still failing the pre-fix engine). Recorded,
not fixed: the churn/creation residual above, and the midfield long-shot machinery
(`LONG_SHOT_THRESHOLD`, `SHOOT_ZONE_MID_*`) being production-unreachable dead surface (zone
minimum 40 m vs range-gate maximum 35 m). Owner: `docs/tracking/shot-volume-design.md`.

### 5.Z.22 The keeper's contact rate (July 28, 2026) — §5.Z.20 §7.1's residual, both named levers landed

Owner document: `docs/tracking/gk-contact-rate-design.md` (KD-CR1..KD-CR7, the measured tables,
the AR history). §5.Z.20 measured that a contact almost always stopped the shot and the keeper
contacted only ~a quarter of on-target shots; its §7.1 named the two levers and put both out of
scope as behaviour changes to APPROVED specs. This pass is those two changes, each with its ERR
filed and the spec patched:

- **ERR-011-007** — #11's `Anticipate → Diving` row was unconditional on `SaveIntent`, so the
  fixed 600 ms dive envelope opened and closed during the ball's 925–2006 ms flight. Measured
  per episode at the goal-plane crossing (the new `GkContactRateDiagnosticTests`): **9 of 15**
  crossed un-contacted episodes were `dive-early`, the dive over by **456–2000 ms**, with
  `dive-late` exactly 0 — never slow, always too eager. New #11 §3.3.6: the transition gates on
  predicted time-to-plane against a lateral-need-scaled commit lead
  (`[GT] DIVE_COMMIT_MIN_LEAD_FRAC`), sharing ONE crossing predictor with the §3.3.4 dive
  direction. The §3.2.3 window's `elapsed` anchor refines to the keeper's first decision
  opportunity at or after the live stamp — the first full-corpus run measured the window
  collapsing back to ~0 under the hold (the shot is usually struck AFTER the intent commit and
  re-stamps the episode), the pass's one calibration iteration.
- **ERR-012-010** — #12 §3.3.3's GK-slot lateral term (`GK_LATERAL_FACTOR × basisY` over the
  pitch width, ±2 m of travel over 68 m) becomes the ball-line point clamped inside the goal
  mouth (`[GT] GK_LATERAL_CLAMP_M` replaces the factor, retired not retuned — no value of a
  pitch-anchored gain expresses goal-anchored tracking). Central ball is the exact pre-fix
  identity.

**Measured (3 full matches, `ConfigureSquads` path, same seeds pre/post):** contacted episodes
8 → **23**, crossed un-contacted 15 → **9**, contact rate ~35% → **~72%**, deep dive-early GONE
(residue 83–183 ms, the 10 Hz grid), catches 6 → **10**, window at contact recovered to
0.34–0.44 — **and goals 14 → 15 over the corpus (4.7 → 5.0/match), unchanged within n=3 noise.**
The §5.Z.17 shape again: the "contact rate → goals/shot" prediction assumed a contact stops the
shot, and that premise does not survive tripling the contact count — the added contacts are
marginal, end-of-envelope touches whose parries and spills keep the ball alive in the box
(one match ran 6-3 on such chains). **The goal-rate residual moves to conversion AT contact:
the pointQuality lottery (E ≈ 0.68, attribute-blind) and parry placement (nothing steers a
parry away from the goal mouth), recorded in the owner doc §7.**

No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order
change — both mechanisms are pure functions of the current tick's ball state and keeper
position. Acceptance: `match-engine-keeper-contact` (#19 ScenarioRunner, Tier B, 2 seeds ×
45 min) — **3 of 4 predicates fail on the pre-fix engine, verified by executing the scenario in
a worktree at the pre-fix commit** (`heldCommits = 0` — the hold is structurally impossible
pre-fix; contacts 3 vs 4 crossings, inverted; one deep dive-early) — plus
`GoalkeeperCommitGateTests` (11), four ball-line GK-slot locks in `PositioningAITests`, and the
`GoalkeeperConversionTests` re-anchor (a parked ball now correctly holds; +1 lock for the
shot-after-commit window anchor).

**AR-4 (full-gate fallout — two failures, both instruments, neither a mechanism defect):** the
shot instruments sampled strike speed and attacked-goal attribution from `BallView` at the END
of the strike tick, and this pass made same-tick post-strike touches common enough to break that
(a defender or keeper within first-touch reach redirects the ball in the same Resolve tick) —
a measured 13 m strike read as **92.3 m** by the velocity-sign attribution, and the speed mean
had survived the same dilution by 0.08. Fixed at the root with the strike-time
`MatchEngine.TestOnly_LastShotStrikePosition/Velocity` seam (captured beside the `_shotContacts`
increment, post-ApplyKick before anything else can move the ball; the `WoodworkStrikes`
diagnostic class), consumed by `match-engine-shot-speed` (windows also 9 → 18 min/seed — this
pass thinned the 9-min windows to 3 strikes, a per-sample lottery for a mean; predicates and
bounds UNCHANGED, measured clean distMean 22.7 ≤ 24.0) and `ShotOutcomeDiagnosticTests`. The P1
observer-neutrality test's non-vacuity guard also tripped (this pass moved its seed's first
restart ~3 900 → measured 7 270 ticks); its window was re-measured 6 000 → 8 000, guard intact.
See the owner doc §8 AR-4.

**AR-5 (CI-gate fallout on PR #282 — a third instrument of the same class):** the #37
MatchAnalytics liveness test (`TapIsLive_PossessionRecordsReachTheAggregatorAndResolveBothTeams`)
asserts both teams hold possession within a 30-second window on its fixed seed; this pass moved
that seed's away-possession onset past the window (per-window probe: away 0.000% at ticks
900–1 800, first accrual 3.625% at 2 400). The window was re-measured 1 800 → 3 600 ticks (60 s;
home 9.4% / away 3.4% at the new window), assertions unchanged. See the owner doc §8 AR-5.

### 5.Z.8 What this unblocks

`PM-1` ("watch a match") is no longer blocked by the engine. Roadmap **A4a** — the round-resolution
calibration corpus — is unblocked upstream: re-run #30's KD-8 Step 0 pilot (~33 min) and, **only if the
squad-strength extremes now separate**, the ~1.4 h corpus and the fit. Note Step 0 may still refuse: Phase H
makes matches *play*, it does not make them *discriminate by squad strength*, and that is exactly the
question Step 0 exists to ask.

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
| 2.8     | 2026-07-28 | —      | **§5.Z.22 — the keeper's contact rate (§5.Z.20 §7.1's residual), both named levers landed.** ERR-011-007 (#11 §3.3.6 commit-to-arrival gate — baseline measured 9 of 15 crossed threat episodes dive-early by 456–2000 ms, dive-late exactly 0; the §3.2.3 window anchor refined to the first decision opportunity at/after the live stamp after the first corpus run measured the window collapsing under the hold) + ERR-012-010 (#12 §3.3.3 GK-slot lateral term → the ball-line point clamped inside the goal mouth; `GK_LATERAL_CLAMP_M` replaces `GK_LATERAL_FACTOR`, retired not retuned). Measured (3 full matches, same seeds): contact rate ~35% → ~72%, catches 6 → 10, deep dive-early gone — and goals 14 → 15, unchanged at n=3: the added contacts are marginal touches whose parries/spills keep the ball alive, so the goal-rate residual moves to conversion AT contact (pointQuality lottery + parry placement). No schema/RNG/draw-order change. `match-engine-keeper-contact` scenario: 3 of 4 predicates fail pre-fix, verified by execution. AR-4 gate fallout (both instruments, not the mechanisms): the shot instruments' end-of-tick `BallView` sampling replaced with the strike-time `TestOnly_LastShotStrike*` seam (a same-tick post-strike touch reversed the sampled velocity — a 13 m strike attributed 92.3 m by vx sign), `match-engine-shot-speed` windows 9 → 18 min/seed, the P1 observer-neutrality window re-measured 6 000 → 8 000 ticks (first restart moved ~3 900 → 7 270); predicates and bounds unchanged. AR-5 CI-gate fallout (third instrument, same class): the #37 MatchAnalytics liveness window re-measured 1 800 → 3 600 ticks against the shifted away-possession onset (first accrual measured by tick 2 400), assertions unchanged. Owner: `gk-contact-rate-design.md` (§8 AR-4/AR-5). |
| 2.7     | 2026-07-28 | —      | **§5.Z.21 — shot volume (§5.Z.19's remaining lever (a)) fixed, calibrated and measured.** ERR-008-017: `U_SHOOT` had NO distance term while `GoalOpeningScore` is scale-free and the range gate is a cliff — measured shots clustered AT the range-gate boundary (means 30–34 m vs football's ~17, ~60% beyond 22 m). §3.2.3.1 gains `DistanceQuality_SHOOT` (1.0 inside `[GT] SHOOT_SWEET_RANGE_M` = 12, hyperbolic decay with `[GT] SHOOT_DIST_FALLOFF_M` = 8). The calibration ladder refused half the design target — count ≈ 25 AND mean ≤ 22 m are not jointly reachable while close-chance creation is churn-bounded — and the distribution + goal-rate landing was chosen: shots 34.7 → **17.7**/match, long-shot share 60% → 30%, **goals 8.0 → 4.7/match (closest ever to football's ~2.7)**, scorelines 2-2 / 3-2 / 5-0. `match-engine-shot-speed` gains the mean-distance ceiling (fails pre-fix at exactly 30.0, verified by execution) + 5 scorer locks. No schema/RNG/draw-order change. Owner: `shot-volume-design.md`. |
| 2.6     | 2026-07-28 | —      | **§5.Z.20 — the keeper's catch/parry conversion (§5.Z.19's residual lever (c)) fixed, calibrated and measured.** ERR-011-005 (#11 §3.2.3 window anchored at the dive COMMIT and frozen — the per-frame re-evaluation dated the contact-consumed value by the ball's whole flight time), ERR-011-006 (the detection stamp dies with its episode + the `OnThreatArmed` episode-onset fallback for threats with no shot event — no new engine state, the stamp is the latch and is already in the v19 GK block), KD-C3 `[GT]` recalibration inside the #11 spec ranges over two measured full-match iterations. Measured (3 full matches, same seeds): window at contact 0.000 → 0.30–0.67, elapsed-when-airborne 85–349 s → ~0.3 s, catches 1 → 6 of 15 contacts, goals/match 14.7 → 8.0, **goals/shot 0.38–0.42 → 0.19–0.26**; scorelines 3-3 / 6-3 / 8-1. Residual bounded and recorded: the CONTACT RATE (~¼ of on-target shots met — #12 GK-slot positioning + commit timing) and shot volume. New `match-engine-keeper-conversion` scenario + `GoalkeeperConversionTests` (7); shot counting re-anchored to `TestOnly_ShotContacts`. No schema/RNG/draw-order change. Owner: `gk-catch-parry-conversion-design.md`. |
| 2.5     | 2026-07-28 | —      | **§5.Z.19 — shot speed + the physical goal frame (the §5.Z.18 residual lever (b)) fixed and measured.** ERR-008-016 (#8 §3.5.3 PowerIntent floor-plus-modulation — the product form pinned nearly every shot at its own 0.1 clamp floor), ERR-006-004 (#6 `VFloor` 10 → 24 over two measured calibration iterations), ERR-001-005 (the goal frame physical: `ApplySweptGoalFrameCollision` six-cylinder segment test — `ApplyGoalPostCollision`'s first production caller; crossing-point goal-line adjudication via the `CheckBoundaries` prevPosition overload). Engine: `_prevTickBallPosition` within-tick capture + swept call in RunPhysicsPhase, crossing-point adjudication in CheckRestartAndApply, `TestOnly_WoodworkStrikes`. Measured: shot-tick means 6.9–10.3 → 14.7–16.1 m/s, maxima to 27.6, shots/match 59–70 → 31–45, goals/shot ROSE 0.14–0.25 → 0.38–0.42 (the keeper's conversion — lever (c) — now measured against real pace). New `match-engine-shot-speed` scenario (5 of 7 predicates fail pre-fix, verified by execution) + `SweptGoalFrameTests` (11) + PowerIntent locks (3). No schema/RNG/draw-order change. Owner: `shot-speed-woodwork-design.md`. |
| 2.4     | 2026-07-27 | —      | **§5.Z.18 — the shot-outcome distribution (the §5.Z.17 residual) fixed and measured.** ERR-006-002 (`finalVelocity = finalDirection × kickSpeed` per #6's own §3.5.7; the §3.5.6 launch-tilt aim — the vertical half of the placement/error model live for the first time), ERR-006-003 (the error cone is a cone: `tan(err) × distance` at the goal plane), ERR-001-004 (the `z < Diameter` gate removed from `CheckBoundaries` + `IsOutOfBounds` — the goal has a crossbar, airborne crossings adjudicate at the crossing per Law 9/10), ERR-003-007 (`OnAgentCollision` live: `BallCollision.ApplyAgentDeflection`, `BodyPartCoefficients`' first consumer, stateless approaching-only self-block guard, `[GT] AgentDeflection.MinBallSpeedMps` = 10 re-anchored from measurement), the `ShotWorldAdapter` pressure query live (was `0f`; first-touch `PressureEvaluator` + §5.Z.14 un-mirror), `MIN_GOAL_VISIBILITY` 0.05 → 0.12. Measured: goals/match 15.3 → 12.3, goals/shot 0.24–0.29 → 0.14–0.25, fast-ball body contacts 0 → 560–612/match. New `match-engine-shot-outcomes` scenario (3 of 8 predicates fail pre-fix, by execution in a worktree at the pre-fix commit) + 17 unit locks + the `ShotOutcomeDiagnosticTests` instrument. Two tests inverted (encoded the old z-gate contract). No schema/RNG/draw-order change. Residual levers recorded: shot volume (~2.5× football), shot speed (~7–10 m/s means vs ~25), keeper conversion. Owner: `shot-outcome-distribution-design.md`. |
| 2.2     | 2026-07-26 | —      | **§5.Z Phase H LANDED — ERR-030-014 closed; a production match now plays.** Five seams, four of them found by running the composed engine one after another (each visible only once the previous was fixed — §5.Z.6). KD-H1 restart taker award: `ApplyRestart(position, awardedTeam)` with every call site declaring its team (kickoff home / second half the other side per Law 8 / post-goal the conceding team / RestartResolver's award / offside the defenders / foul the victim's team); taker = nearest non-sent-off agent of that team, ties to lower index. New `[FIXED] FIRST_HALF_KICKOFF_TEAM` + `[DERIVED] SECOND_HALF_KICKOFF_TEAM`. KD-H2 assignment not imparted velocity (`ApplyKick` stays the sole motion producer). KD-H3 `RunLooseBallPickup` — a loose ball at REST is claimed by an agent within the new `[GT] LooseBallPickupRadiusM`, the exact speed-gate complement of `RunFirstTouch` so the two can never both fire. KD-H5 / **ERR-008-014** the DT loose-ball collect, emitted as the SOLE off-ball option for one host-designated collector per team (`TacticalContext.LooseBallCollector`; host-designated because only it knows who is sent off — a perception-derived "nearest teammate" rule deadlocked on a frozen red-carded agent). KD-H4 / **ERR-008-015** the PASS/SHOOT completion sweep — `NotifyActionComplete` had zero production callers, so every agent that passed or shot was frozen in EXECUTING for the rest of the match; plus `OnPossessionChanged` no longer interrupts a holder whose executor is still in flight. New acceptance scenario `match-engine-play-develops` (6 seeds × 9 min; every predicate fails pre-Phase-H, incl. `play-still-alive-at-final-tick`, which caught two of the four stalls) + `MatchEnginePossessionBootstrapTests` (11) + `OptionGeneratorTests` (+3). 21 existing tests updated — most encoded the "a restart clears possession" contract that made the deadlock possible. No `SNAPSHOT_SCHEMA_VERSION` change. **Full dotnet gate: PASSED, 0 failures (whole tree green).** Recorded NOT fixed (§5.Z.7): the foul heuristic's ~7 red cards per 9 minutes; the process-static EventBus's interleaved-engine divergence; #5's FM-08 Error-level log; the `FR-PO-052` perf baseline needing re-capture. |
| 2.3     | 2026-07-20 | —      | **Phase G Phase-2 LANDED — distinct-squad re-projection (#27 T3 / KD-3).** New public `ISquadProvider` (`src/match-engine/ISquadProvider.cs`, the `ClubId → Squad` resolver) threaded into `RestoreFromSnapshot(…, ISquadProvider squads = null)`; `ReprojectDistinctSquads` replaces the Phase-1 distinct-squad fail-loud — neutral fast-path returns immediately, each team with a non-sentinel `_rosterClubId` resolves its roster (ClubId-check + `ValidateSquadSize`/`ValidateSelectedRecords`, both teams before any apply), re-runs `LineupSelector` + `PlayerAttributeProjection` for the base lineup (`ReprojectBaseLineup`, attribute arrays + the un-serialized bench GK flags `_benchIsGoalkeeper`; the serialized on-pitch `_isGoalkeeper` stays the restored value), then replays the substitutions the serialized `_activeBenchSlot` records (`ReprojectSubstitutions`, the attribute half of `SubstitutePlayer`). Fail-loud on absent provider / unresolvable ClubId (`NotSupportedException`) / mismatched returned ClubId (`InvalidOperationException`) (R4). `MatchEngineSnapshotRestoreTests` v1.1: distinct-squad G3 round-trip (base / mid-match sub / post-restore sub / post-restore keeper-for-keeper sub) + three provider fail-loud gates; new `TestOnly_BenchIsGoalkeeper` seam. No `SNAPSHOT_SCHEMA_VERSION` change. `MatchEngine.cs` v1.42. Full dotnet gate: PASSED, 0 failures (263 match-engine tests; whole tree green). Implementation finding folded in: `_benchIsGoalkeeper` is NOT serialized (only on-pitch `_isGoalkeeper` is), re-projected for post-restore keeper subs. Discovered out-of-scope (Phase-1 completeness follow-up, root `CLAUDE.md` OPEN ISSUES): a keeper-onto-outfield-slot substitution post-restore diverges via a Positioning-AI (#12) GK-flag-flip formation-slot interaction. See `snapshot-deserialize-design.md` v0.8. |
| 2.2     | 2026-07-20 | —      | **Phase G Phase-1 COMPLETE — reader LANDED.** `DeserializeWorldState` (symmetric mirror of `SerializeWorldState`, restore-seam reconstruction, version-gate + event-ledger-boundary trailing guard) + the `RestoreState` counterparts (Pressing/Defensive/Attacking/Perception/Positioning + `MovementCommand.ReconstructFromSnapshot`) + the static `RestoreFromSnapshot` factory (fingerprint gate → boot + EventBus reset → deserialize → KD-3 distinct-squad fail-loud → digest-chain + clock restore) + `MatchEngineSnapshotRestoreTests` (G3 round-trip determinism + fail-loud guards). Findings folded in during landing: `_possessingAgentId`/`_prevPossessingAgentId` reconstructed from the restored MatchContext; the trailing guard made event-ledger-aware. No `SNAPSHOT_SCHEMA_VERSION` change. `MatchEngine.cs` v1.41. Full dotnet gate: PASSED, 0 failures (257 match-engine tests; whole tree green). See `snapshot-deserialize-design.md` v0.7. |
| 2.1     | 2026-07-20 | —      | **Phase G opened — snapshot deserialize / restore path** (governed by the converged design supplement `docs/tracking/snapshot-deserialize-design.md` v0.5, AR-1..AR-4). §5 gains the Phase G entry (the reader that reconstructs full engine state from the payload `SerializeWorldState` already writes — the keystone save/load, replay/rewind, #27 T3 distinct-squad restore, and #16 §4.8.2 MXCSR validation sit behind; nothing read the snapshot back before). Phased: G-Phase 1 (neutral-path reader + round-trip determinism), G-Phase 2 (#27 T3 distinct-squad re-projection), G-Phase 3 (native MXCSR + on-disk fold). **G-Phase 1 KD-8 writer half LANDED** the same day: the `match-flow.card-severity` `RngStreamState` cursor (RngCursor + ActionOrdinal — the engine's only mutable RNG stream, the one cross-tick surface the writer omitted; AR-3's High) is serialized at `SNAPSHOT_SCHEMA_VERSION` 16 → 17, so a save after any booking round-trips deterministically, and the stale v8 "no cross-tick state excluded" note is corrected. `MatchEngine.cs` v1.40, `MatchEngineConstants.cs` v1.24; new `TestOnly_SetCardSeverityStreamCursor` seam + `MatchEngineSnapshotSchemaTests` v1.14 (pin 17 + `CardSeverityRngCursor` probe). Remaining G-Phase-1 slices: the missing `RestoreState` counterparts, `DeserializeWorldState`, the `RestoreFromSnapshot` factory, and the G3 round-trip determinism test. (dotnet gate not runnable in this environment; verified by manual review, CI runs on push.) |
| 2.0     | 2026-07-14 | —      | **Match-flow completion landed** — throw-ins, corners, goal kicks, fouls/cards, offside, substitutions, half-time break, full-time end (the v1.4 entry's "Not built" list, now closed). See the v2.0 Last Updated entry above for the full description; companion design note `docs/tracking/match-flow-completion-design.md` (new) carries the plan + the AR-1..AR-6 adversarial-review history. `MatchEngine.cs` v1.31; `SNAPSHOT_SCHEMA_VERSION` 14 → 15. New: `RestartResolver.cs`, `OffsideEvaluator.cs`, `SubstitutionReason.cs`; `OffsideCalledEvent`/`RestartAwardedEvent`/`MatchPhaseChangedEvent` (0x18/0x19/0x1A). New tests: `MatchEngineRestartTests`/`MatchEngineOffsideTests`/`MatchEngineFoulCardTests`/`MatchEngineSubstitutionTests`/`MatchEngineMatchFlowTests`; `MatchEngineSnapshotSchemaTests` v1.12 (pin 15 + 2 probes). Full dotnet gate not runnable in this environment — verified by exhaustive manual adversarial review of the entire touched surface instead. |
| 1.4     | 2026-07-11 | —      | **Engine substrate — goal detection + score state + match-length/halves model (the #26 §9.3 upstream deliverables).** (a) **Match-length model:** `MatchEngineConstants` v1.20 gains `[FIXED] MATCH_LENGTH_MINUTES` (90 — Laws of the Game; no stoppage/extra time at Stage 0) + `[DERIVED] MATCH_TICKS_TOTAL` (= 90 × 60 × `PHYSICS_TICK_HZ` = 324 000; the #26 §3.5 `[CROSS-PENDING]` allocation, promoted to `[CROSS]` in the spec) + `[DERIVED] HALF_TIME_BOUNDARY_TICK` (= 162 000; the FR-TP-019 Stage-0 halves model — **boundary only**: the engine does NOT stop play, swap ends, model a break, or end the match; `ticksRemaining` clamps at 0 past full time). (b) **Goal detection (Resolve phase):** new `CheckGoalAndRestart` between the executor advance (C3) and first touch (D3) — `BallCollision.CheckBoundaries` ⇒ `RestartType.KickOff` means the ball fully crossed a goal line between the posts under the crossbar (the z-gate + corner-precedence are that predicate's own documented Stage-0 scope); the scoring TEAM is classified by exit half-space geometry (own goals credit the correct side); increments the per-team score, publishes the first-ever Tier A `GoalAwardedEvent` (0x07; Scorer = the last settled holder via a new `_lastHolderAgentId` tracker, Assister −1), restarts the ball at the centre spot stationary and clears possession (minimal Stage-0 restart — agents keep positions). Non-goal exits (throw-in/corner/goal-kick classifications) remain untouched: no restart model, pre-substrate behaviour preserved exactly. (c) **Serialization:** `SNAPSHOT_SCHEMA_VERSION` 13 → 14 — per-team `_goals` + `_lastHolderAgentId` appended (digest-load-bearing; a save resumes with the correct score). (d) **#26 activation:** the RunAiPhase manager block extracted to `RunManagerDecisionPoints`, now passing LIVE `goalDiff` (v14 score) + `ticksRemaining`/`MATCH_TICKS_TOTAL` — closes the #26 §3.4 PASS-1 M-1 gates; `ManagerDecisionGate` v1.1 activates the half-time trigger (fires once, at the first stride evaluation at/after the boundary, regardless of interval position; no new clock state beyond `LastDecisionTick`). New seams: `TestOnly_Goals` / `TestOnly_SetGoals` / `TestOnly_LastHolderAgentId` / `TestOnly_RunManagerDecisionPoints` (late-match ladder arithmetic testable without ~270 000 real ticks). Tests: new `MatchEngineGoalTests.cs` (6 — both goal mouths + centre restart, non-goal/airborne exits untouched, last-holder scorer credit, two-run determinism with a goal in the run) + `ManagerAITests` v1.1 (+4 half-time/live-ladder) + `MatchEngineSnapshotSchemaTests` v1.11 (pin 14 + `ScoreState_FeedsSnapshotDigest`). `MatchEngine.cs` v1.30. |
| 1.3     | 2026-06-28 | —      | **FR-PO-052 certified perf baseline corpus machinery.** The Phase F capstone activated the per-tick perf gate against a *generous in-code Stage-0 anchor* (NON-certifying Linux gate). This adds the certified-baseline layer the anchor stands in for: `src/performance-optimization/CertifiedPerfBaseline.cs` (+ `CertificationStatus.cs`) models a corpus entry tagged Pending or Certified. At Stage 0 the kickoff entry is **Pending** — it carries NO measured metric and refuses `TryBuildBaselineRecord`, because a certified number must come from the pinned Windows/Unity tuple (`certification-platform.md` v1.2) and the Linux compile/test gate is explicitly NON-certifying, so admitting a Linux number would be a fabricated certification. A `Certified(manifest, loop, p50, p99, threshold)` entry validates a complete `SessionManifest` + finite positive metrics (p99≥p50; fail-closed) and projects to a corpus `BaselineRecord` ready for `PerfGateRunner`/`RegressionGate`. First on-disk corpus artifact `docs/specs/performance-optimization/baselines/match-engine/kickoff-multi-second.cert.md` (status PENDING_CERT_RUN + promotion runbook). `MatchEngineCapstoneTests.cs` v1.1 swaps the hardcoded non-cert pin string for the named `CertifiedPerfBaseline.LinuxNonCertPlatformPin` constant (behaviour-neutral). New `CertifiedPerfBaselineTests.cs` locks the PENDING refusal, the certified projection (self-compare through `PerfGateRunner` → pass), and the fail-closed invariants. **No production `MatchEngine.cs` change.** New files `CertificationStatus.cs` v1.0, `CertifiedPerfBaseline.cs` v1.0, `CertifiedPerfBaselineTests.cs` v1.0, `kickoff-multi-second.cert.md` v1.0. (Linux gate not runnable locally — runs in CI on push.) |
| 1.2     | 2026-06-28 | —      | **Decision Tree (#8) deferred away-team closed-loop scenario landed** — the `audit-report.md` follow-up that was gated on "the DT orchestrator seam"; the v1.1 #21 runtime-activation single-writer is that seam. New `src/match-engine/tests/MatchEngineAwayTeamScenarios.cs` + `MatchEngineAwayTeamTests.cs`: registers `away-team-tactic-mirror` on the #19 `ScenarioRunner` (Tier B; owning specs `{2,8,16,19,21}`; path under `SCENARIO_PATH_CROSS_SPEC_PREFIX`), boots a real `MatchEngine`, sets home=defending / away=attacking via `SetTeamTactic`, ticks 300× (5 s), and records envelope predicates that lock — **through the composed host** — that every away agent's DecisionTree input carries the away (attacking) routed tactic, every home agent the home (defending) one, and the two partitions are distinct on all three routed dimensions. This is the composition-level inverse of the audit's home/away root cause (ERR-008-002/M-1/M-2 — *every* DT worked example and AR-1 fixture used the home team, so the away team was silently served the home perspective). Plus `away-agents-stay-in-bounds` and a two-run same-seed determinism digest match. `MatchEngineAwayTeamTests` runs it through `ScenarioRunner.Run` → `Passed`. **No production change** — reads world state via the existing `TestOnly_Mentality/Pressing/Passing` + `TestOnly_AgentSnapshot` seams; `match-engine-tests.asmdef` already carried every ref. New files `MatchEngineAwayTeamScenarios.cs` v1.0, `MatchEngineAwayTeamTests.cs` v1.0. (Linux gate not runnable locally — runs in CI on push.) |
| 1.1     | 2026-06-28 | —      | **Tactical Instructions (#21) T2 runtime activation — the Phase-D single-writer for Decision Tree (#8).** Post-(A–F) follow-on: the match engine now routes a live per-team `TeamTactic` into the AI input. New per-team `_active`/`_pendingTeamTactics` fields (default `TeamTactic.Balanced`); public `MatchEngine.SetTeamTactic(teamId, in TeamTactic)` stages a *pending* change; `RunAiPhase` commits pending→active at the AI-stride boundary (FR-TI-027 — RunAiPhase runs only on stride ticks, so a mid-tick set cannot take effect until the next stride); `RunMechanicsAI` overlays the active tactic's `Mentality` (drives the #8 `UtilityScorer` §3.2/§3.3 risk multiplier) + `TacticTranslation.ToPressingMode/ToPassingStyle` (rank-mapped, non-inverting — the #21 enums order ascending vs the #8 enums descending) into each agent's `TacticalContext`. `TacticTranslation` (decision-tree) promoted internal→public (the match-engine is its #21 §3.1 caller). **Behaviour-neutral by default:** Balanced ⇒ MEDIUM/MIXED/×1.0 = `Stage0Default`, so a default match is byte-identical to pre-#21; `TacticalContext` + the tactic arrays are NOT serialized → **no `SNAPSHOT_SCHEMA_VERSION` bump**, but consequently a *mid-match* tactic change is not yet restore-deterministic (ERR-021-002 — the `TeamTactic.DefensiveLine` snapshot field + schema bump is the deferred #16 back-prop; at Stage 0 set the tactic before kickoff). A non-Balanced tactic activates both the new Mentality multiplier and the pre-existing `TacticalModifierResolver` Pressing/Passing modifiers. `DefensiveLineDepth` stays the #14 output (the §3.4 mentality-line recompute is deferred with the #12/#14 depth-ownership wiring). New `TestOnly_Mentality/Pressing/Passing` seams; `match-engine.asmdef` + `match-engine-tests.asmdef` gain `TacticalDirector.TacticalInstructions`. New `MatchEngineTacticTests.cs` (per-team routing + translation + FR-TI-027 stride-gating + explicit-Balanced behaviour-neutrality vs the default digest chain + same-tactic determinism + invalid-teamId throw). Remaining #21: per-agent `PlayerTactic` + the §3.3 product factors (G2 balance pass), the #12–#15 Mechanics maps + snapshot fields, and the `[GT]` config-loader that would populate `SetTeamTactic` from disk. `MatchEngine.cs` v1.16, `TacticTranslation.cs` v1.1, `TacticalContext.cs` v1.2, `UtilityScorer.cs` v1.5. (Linux gate not runnable locally — runs in CI on push.) |
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
