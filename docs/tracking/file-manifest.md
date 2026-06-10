# File Manifest (Post-Migration Baseline)

**Created:** April 30, 2026  
**Last Updated:** June 10, 2026, later same day (Spec #19 ScenarioRunner AR-1 fix pass: 0H+4M+6L, all resolved. New file: `src/testing-strategy/ScenarioIndexEntry.cs` (extracted from ScenarioIndex.cs per FILE NAMING precedent + AR-1 M-1 manifest-coherence guard). Modified: ScenarioRunner.cs v1.1 (M-2 fixture_refs refusal, M-4 path↔name + cross-spec arity, L-6 format-version-first), ScenarioIndex.cs v1.1 (M-4 duplicate-name rejection), ScenarioEnvelope.cs v1.1 + ClosedLoopScenario.cs v1.1 (M-3 CR/LF sanitization + exception_stack line), ScenarioManifest.cs v1.1 (L-1 ReadOnlyCollection wrappers), ScenarioResult.cs / IScenario.cs / ScenarioContext.cs v1.1 (doc), TestingStrategyConstants.cs v1.4 (SCENARIO_PATH_CROSS_SPEC_PREFIX), ScenarioRunnerTests.cs v1.1 (12→18 tests), AgentMovementScenarios.cs v1.1 (L-2 InvariantCulture details, L-3 exact position equality for T-AM-115). Prior June 10, 2026 (Stage 0 closed-loop scenario harness landed — Spec #19 §3.3.3 ScenarioRunner pulled forward from the Stage 0+1 schedule after the third consecutive spec (Ball Physics AR-7, Agent Movement AR-12/AR-13) where H/M-class closed-loop defects were encoded by pure-function unit suites rather than caught by them. New `src/testing-strategy/` files (9 .cs): ScenarioStatus, ScenarioResult, ScenarioManifest, ScenarioEnvelope, ScenarioContext, IScenario, ClosedLoopScenario, ScenarioIndex, ScenarioRunner; new `src/testing-strategy/Tests/` (testing-strategy-tests.asmdef + ScenarioRunnerTests.cs, 12 contract tests); TestingStrategyConstants.cs v1.3 adds SCENARIO_MANIFEST_FORMAT_VERSION. First fixture corpus: T-AM-110..115 migrated out of AgentMovementTests.cs (v2.3) into AgentMovementScenarios.cs (bodies + A.1 manifests) + AgentMovementScenarioTests.cs (`sim_<scenario>` Simulation-layer tests); agent-movement-tests.asmdef gains the testing-strategy reference; docs/specs/agent-movement/test-plan.md v0.4. A `src/testing-strategy/` per-file section was added to this manifest (the June 7 scaffold had been recorded only in this header note). Prior June 9, 2026 (Ball Physics #1 AR-7/AR-8 fix pass: BallGroundInteraction.cs v1.3.1, BallPhysicsCore.cs v1.4, BallPhysicsConstants.cs v1.8, tests/BallPhysicsCoreTests.cs v1.5, tests/BallIntegrationTests.cs v1.4 (new Airborne_FastDescent_Bounces_NoHoverDeadlock test); docs/specs/ball-physics/section-3-1-8-to-3-1-14.md row 2.8; spec-error-log.md v1.23 (ERR-001-001..003). No files added or removed. Prior June 8, 2026 (Event System #17 boot-wiring smoke test landed: new `src/event-system/tests/EventBusWiringSmokeTests.cs` v0.4 — SMOKE-EVT-WIRING-001 drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts SHA-256 digest stability across the 6 currently-wired EventBusRegistrar.Initialize() call sites (Pass / Shot / Perception / Decision / Heading / Goalkeeper); Agent Movement (#2) is a `[CROSS-PENDING]` slot pending the AM-side registrar; golden digest pinned via Assert.Inconclusive until that lands. AR-1 (1H+1M+4L) + AR-2 (0H+1M+4L) + AR-3 (0H+0M+2L cycle-stop) adversarial review cycles complete. `event-system-tests.asmdef` extended with 6 production spec references for the smoke test (Editor-only, no production layering impact — test assemblies are infrastructure per src/CLAUDE.md). Prior June 7, 2026 (AR-hardening sweep complete + test scaffolding landed. New source files: `src/agent-movement/AssemblyInfo.cs` (InternalsVisibleTo for tooling-override factory access). New test files: `src/ball-physics/tests/EnumOrdinalStabilityTests.cs` (AR-6 L-3 — locks int ordinals for all 6 public enums), `src/ball-physics/tests/BodyPartCoefficientsTests.cs` (AR-4 L-2 — throw-on-unknown + catalogue round-trip), `src/ball-physics/tests/SurfacePropertiesTests.cs` (AR-4 L-2 — throw-on-unknown across all 4 Get* methods), `src/agent-movement/Tests/AgentMovementTests.cs` v2.0 (T-AM-001..018, T-AM-030..033, T-AM-040..043 — 18 NUnit tests across 4 fixtures), `src/agent-movement/Tests/AgentMovementUnitTests.cs` (T-AM-007..107 — 59 NUnit tests across 7 fixtures). New tracking doc: `docs/specs/agent-movement/test-plan.md` v0.2 (T-AM-NNN catalogue). File splits: Ball Physics — `BallCollision.cs` split into `BodyPart.cs` + `RestartType.cs` + `KickResult.cs` + `BodyPartCoefficients.cs` + `BallCollision.cs` per FILE NAMING (AR-2 L-2). Performance Optimization — `TraceChannel.cs` split into `ChannelVerbosity.cs` + `ChannelSamplingRule.cs` + `ChannelDeterminismClass.cs` + `TraceChannelDescriptor.cs` + `TraceChannelRegistry.cs` (AR-1 H-1). New Testing Strategy assembly: `src/testing-strategy/` with 14 files (TestingStrategyConstants, TestTier, TestLayer, GoldenVectorKind/Entry/Result/Runner, DeterminismTierKind/Result, DeterminismSuiteResult, DeterminismGate, PerfGateReport, PerfGateRunner, testing-strategy.asmdef). Prior May 31: AR-4 fixes in event-system.))
**Purpose:** Canonical inventory aligned with the current folder-based spec layout in `docs/specs/`.

---

## Scope and Authority

This manifest supersedes the legacy flat-file inventory that referenced historical filenames such as `*_v1_0.md`.

- **Canonical spec status source:** `docs/specs/SPEC_INDEX.md`
- **Canonical schedule source:** `docs/tracking/PROGRESS.md`
- **Canonical cross-spec issue source:** `docs/tracking/spec-error-log.md`

Use this file to track the **current folder structure**, not legacy per-version filenames.

---

## Source Files

| File | Purpose |
|------|---------|
| `src/CLAUDE.md` | Coding guide: C# naming, constant catalogues, Unity project structure, build/test commands. Created May 19, 2026 when coding began. At v1.23 as of May 30, 2026. |

### Spec #1 — Ball Physics (`src/ball-physics/`)

> Relocated May 30, 2026 from `src/Core/Physics/Ball/` to spec-canonical `src/ball-physics/` via `git mv`; history preserved. Two asmdef files added.

| File | Purpose |
|------|---------|
| `src/ball-physics/ball-physics.asmdef` | Assembly definition (references TacticalDirector.DeterministicSim; autoReferenced true) |
| `src/ball-physics/BallPhysicsConstants.cs` | `[FIXED]` / `[GT]` / `[DERIVED]` / `[CROSS]` constant catalogue for Ball Physics |
| `src/ball-physics/BallState.cs` | Mutable value-type game state for the ball (position, velocity, spin, ground contact) |
| `src/ball-physics/BallPhysicsCore.cs` | Core physics calculations: gravity, drag, Magnus effect |
| `src/ball-physics/BallStateMachine.cs` | State machine: CONTROLLED ↔ AIRBORNE ↔ ROLLING transitions |
| `src/ball-physics/BallGroundInteraction.cs` | Ground friction and rolling dynamics |
| `src/ball-physics/BallCollision.cs` | Ball-specific collision response (detection geometry lives in `collision-system/`) |
| `src/ball-physics/BallEventLogger.cs` | Event/logging infrastructure for ball physics events |
| `src/ball-physics/SurfaceProperties.cs` | Surface-specific physics parameters (grass, artificial turf, etc.) |
| `src/ball-physics/tests/ball-physics-tests.asmdef` | Test assembly definition (EditMode; references ball-physics.asmdef; autoReferenced false) |
| `src/ball-physics/tests/BallPhysicsCoreTests.cs` | Unit tests for core physics calculations |
| `src/ball-physics/tests/BallIntegrationTests.cs` | Integration tests for full ball physics pipeline |
| `src/ball-physics/tests/BallStateMachineTests.cs` | Unit tests for ball state machine transitions |

### Spec #2 — Agent Movement (`src/agent-movement/`)

| File | Purpose |
|------|---------|
| `src/agent-movement/AgentMovementConstants.cs` | All movement constants: `MovementThresholds` / `FatigueRates` / `LocomotionConstants` / `DirectionalConstants` / `TurnConstants` / `OscillationGuardConstants` / `SafetyConstants` / `PlayerAttributeConstants` (8 nested classes) |
| `src/agent-movement/AgentMovementState.cs` | Enum: `AgentMovementState` (7 locomotion states) |
| `src/agent-movement/GroundedReason.cs` | Enum: `GroundedReason` (NONE / COLLISION / SLIDING_TACKLE / DIVING_HEADER) |
| `src/agent-movement/FacingMode.cs` | Enum: `FacingMode` (AUTO_ALIGN / TARGET_LOCK) |
| `src/agent-movement/DecelerationMode.cs` | Enum: `DecelerationMode` (CONTROLLED / EMERGENCY) |
| `src/agent-movement/AgentState.cs` | Mutable value-type game state for an agent (ref-mutated, not readonly — pending C# version pin) |
| `src/agent-movement/PlayerAttributes.cs` | Player skill ratings (1–20 scale) used by movement formulas |
| `src/agent-movement/PerformanceContext.cs` | Performance modifier gateway (fatigue, injury, surface) |
| `src/agent-movement/MovementCommand.cs` | Input command structure from Decision Tree / tactical layer |
| `src/agent-movement/AgentMovementSystem.cs` | 12-step 60 Hz pipeline orchestrator |
| `src/agent-movement/AgentStateMachine.cs` | Pure state evaluator (no side effects) |
| `src/agent-movement/OscillationGuard.cs` | Ring-buffer anti-oscillation guard |
| `src/agent-movement/AgentLocomotion.cs` | Acceleration / deceleration formulas |
| `src/agent-movement/AgentTurning.cs` | Turn rate / lean angle / stumble probability |
| `src/agent-movement/AgentDirectionalMovement.cs` | Directional multipliers / facing update |
| `src/agent-movement/AgentSafetySystem.cs` | NaN detection / speed clamp / pitch boundary enforcement |
| `src/agent-movement/agent-movement.asmdef` | Assembly definition (no references; autoReferenced true) |
| `src/agent-movement/AssemblyInfo.cs` | `[InternalsVisibleTo("TacticalDirector.AgentMovement.Tests")]` (added June 4, 2026 for T-AM-030..032 seam) |
| `src/agent-movement/Tests/AgentMovementTests.cs` | v2.3 — Regression-anchored integration roster T-AM-001..018 / 030..033 / 040..043 (closed-loop fixture T-AM-110..115 migrated to the #19 scenario harness June 10, 2026) |
| `src/agent-movement/Tests/AgentMovementUnitTests.cs` | Pure-function unit coverage T-AM-007..009 / 019..023 / 034..039 / 044..047 / 050..052 / 070..109 (T-AM-108..109 added in AR-12 fix pass, June 9, 2026) |
| `src/agent-movement/Tests/AgentMovementScenarios.cs` | Per-spec closed-loop scenario corpus T-AM-110..115 (#19 §3.3.1): scenario bodies + Appendix A.1 manifests; first fixture corpus on the #19 ScenarioRunner (June 10, 2026) |
| `src/agent-movement/Tests/AgentMovementScenarioTests.cs` | Simulation-layer executable tests (`sim_<scenario>` per #19 §3.1.4) running the T-AM-110..115 corpus through ScenarioRunner.Run |
| `src/agent-movement/Tests/agent-movement-tests.asmdef` | Test assembly definition (EditMode; references agent-movement.asmdef + testing-strategy.asmdef) |

### Spec #3 — Collision System (`src/collision-system/`)

| File | Purpose |
|------|---------|
| `src/collision-system/CollisionSystemConstants.cs` | All constant catalogue: `[FIXED]` / `[GT]` / `[DERIVED]` / `[CROSS]` constants for the collision system |
| `src/collision-system/CollisionDetection.cs` | Broad-phase and narrow-phase collision detection logic |
| `src/collision-system/SpatialHashGrid.cs` | Spatial hash grid for broad-phase agent and ball proximity queries |
| `src/collision-system/CollisionManifold.cs` | Contact manifold: penetration depth, contact normal, contact point |
| `src/collision-system/CollisionEvent.cs` | Struct event published to the event bus on confirmed collision |
| `src/collision-system/CollisionResponse.cs` | Impulse-based collision response calculations |
| `src/collision-system/CollisionSystem.cs` | Main orchestrator: 60 Hz pipeline for all agent–agent and agent–ball collisions |
| `src/collision-system/CollisionPairBitfield.cs` | Bitfield tracking already-processed pairs within a tick (prevents double-processing) |
| `src/collision-system/AgentAgentCollisionResult.cs` | Result struct for an agent–agent collision resolution |
| `src/collision-system/AgentBallCollisionData.cs` | Data struct describing an agent–ball contact |
| `src/collision-system/AgentPhysicalProperties.cs` | Physical properties (mass, radius, restitution) per agent |
| `src/collision-system/BallCollisionHandler.cs` | Ball-specific collision response (delegates geometry to `ball-physics/BallCollision.cs`) |
| `src/collision-system/ContactForceData.cs` | Contact force magnitude and direction for logging/events |
| `src/collision-system/ContactType.cs` | Enum: contact classification (SLIDE, SHOULDER, BLOCK, FOUL) |
| `src/collision-system/ContactTypeClassifier.cs` | Classifies a collision manifold into a `ContactType` |
| `src/collision-system/CollisionType.cs` | Enum: collision kind (AGENT_AGENT, AGENT_BALL, AGENT_POST, AGENT_BOUNDARY) |
| `src/collision-system/DeterministicRNG.cs` | Thin wrapper around SplitMix64 for foul-roll and stumble-roll RNG draws |
| `src/collision-system/ICollisionEventConsumer.cs` | Interface for systems that consume collision events (Spec #3 §3.4.2 consumer pattern) |
| `src/collision-system/collision-system.asmdef` | Assembly definition for the collision-system assembly |
| `src/collision-system/tests/collision-system-tests.asmdef` | Test assembly definition (EditMode; references collision-system.asmdef) |

### Spec #4 — First Touch Mechanics (`src/first-touch/`)

| File | Purpose |
|------|---------|
| `src/first-touch/FirstTouchConstants.cs` | All constant catalogue for First Touch |
| `src/first-touch/FirstTouchContext.cs` | Input context struct: incoming ball state, agent state, env conditions |
| `src/first-touch/FirstTouchResult.cs` | Final output struct: displaced ball state + possession outcome |
| `src/first-touch/TouchResult.cs` | Intermediate result of touch quality evaluation |
| `src/first-touch/FirstTouchSystem.cs` | Main orchestrator: entry point for a first-touch resolution |
| `src/first-touch/BallDisplacementProcessor.cs` | Computes post-touch ball displacement vector from error and control quality |
| `src/first-touch/ControlQualityCalculator.cs` | Calculates control quality score (0–1) from player attributes and context |
| `src/first-touch/OrientationDetector.cs` | Detects player body orientation relative to the incoming ball direction |
| `src/first-touch/PossessionStateMachine.cs` | Manages possession state transitions (LOOSE → CONTROLLED → POSSESSED) |
| `src/first-touch/PressureEvaluator.cs` | Evaluates nearby-defender pressure scalar from agent positions |
| `src/first-touch/PressureResult.cs` | Pressure evaluation output struct |
| `src/first-touch/TouchRadiusCalculator.cs` | Computes the acceptance radius for a first-touch attempt |
| `src/first-touch/IAgentMovementSystem.cs` | Interface boundary to Agent Movement (#2) (read-only query surface) |
| `src/first-touch/IBallPhysicsSystem.cs` | Interface boundary to Ball Physics (#1) (read-only query surface) |
| `src/first-touch/IFirstTouchSystem.cs` | Public interface for consumers of the First Touch system |

### Spec #5 — Pass Mechanics (`src/pass-mechanics/`)

| File | Purpose |
|------|---------|
| `src/pass-mechanics/PassMechanicsConstants.cs` | All constant catalogue: physical profiles, error model, timing constants |
| `src/pass-mechanics/PassRequest.cs` | Input struct: passer, target agent/position, requested pass type |
| `src/pass-mechanics/PassResult.cs` | Output struct: actual ball velocity applied + outcome classification |
| `src/pass-mechanics/PassAttemptEvent.cs` | Tier A struct event (IEventA; ordinal 0x0C; 12-byte header): published when a pass is initiated |
| `src/pass-mechanics/PassCancelledEvent.cs` | Tier A struct event (IEventA; ordinal 0x0D; 12-byte header): published when a pass is cancelled |
| `src/pass-mechanics/CancelReason.cs` | Enum: TackleInterrupt — reason a pass was cancelled |
| `src/pass-mechanics/PassExecutor.cs` | Main orchestrator: executes the full pass pipeline |
| `src/pass-mechanics/PassVelocityCalculator.cs` | Calculates launch velocity from physical profile and player attributes |
| `src/pass-mechanics/PassErrorCalculator.cs` | Error / accuracy model: direction and speed deviation |
| `src/pass-mechanics/PassTargetResolver.cs` | Resolves intended target position from agent reference |
| `src/pass-mechanics/PassTypeProfiles.cs` | Factory: returns the `PhysicalProfile` for a given `PassType` |
| `src/pass-mechanics/PhysicalProfile.cs` | Struct: physical parameters (speed range, spin, launch angle) per pass type |
| `src/pass-mechanics/PassType.cs` | Enum: GROUND, DRIVEN, LOB, CHIP, CROSS |
| `src/pass-mechanics/CrossSubType.cs` | Enum: cross sub-type (LOW, DRIVEN, FLOATED, CUTBACK) |
| `src/pass-mechanics/SpinType.cs` | Enum: spin type (TOPSPIN, BACKSPIN, SIDESPIN, NONE) |
| `src/pass-mechanics/PassOutcome.cs` | Enum/struct: outcome classification (ACCURATE, MISPLACED, INTERCEPTED, OUT) |
| `src/pass-mechanics/PassAgentAttributes.cs` | Agent skill attributes consumed by the pass system (passing, vision, weak-foot) |
| `src/pass-mechanics/PassAgentState.cs` | Agent state consumed by the pass system (fatigue, stamina, body orientation) |
| `src/pass-mechanics/IPassAgentQuery.cs` | Interface to query agent attributes and state |
| `src/pass-mechanics/IPassBallSystem.cs` | Interface to Ball Physics (#1) for applying kick velocity |
| `src/pass-mechanics/IPassCollisionQuery.cs` | Interface to Collision System (#3) for interception queries |
| `src/pass-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/pass-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for PassAttemptEvent (0x0C) + PassCancelledEvent (0x0D) |

### Spec #6 — Shot Mechanics (`src/shot-mechanics/`)

| File | Purpose |
|------|---------|
| `src/shot-mechanics/ShotMechanicsConstants.cs` | All GT/Fixed/Cross constants (velocity, angle, spin, error, body mechanics, weak-foot, timing) |
| `src/shot-mechanics/ContactZone.cs` | Enum: Centre / BelowCentre / OffCentre — where on the ball the foot contacts |
| `src/shot-mechanics/ShotOutcome.cs` | Enum: Completed / Cancelled / Invalid / Initiated |
| `src/shot-mechanics/ShotCancelReason.cs` | Enum: TackleInterrupt — reason a shot was cancelled |
| `src/shot-mechanics/ShotRequest.cs` | Input struct from Decision Tree (#8) to ShotExecutor |
| `src/shot-mechanics/ShotResult.cs` | Output struct returned by ShotExecutor (velocity, spin, error offset, BMS, outcome) |
| `src/shot-mechanics/ShotAgentAttributes.cs` | Agent attribute snapshot (Finishing, LongShots, Composure, KickPower, Technique, WeakFootRating, Fatigue) |
| `src/shot-mechanics/ShotAgentState.cs` | Agent physical state snapshot (Position, Velocity, FacingDirection, CurrentState) |
| `src/shot-mechanics/ShotExecutedEvent.cs` | Tier A struct event (IEventA; ordinal 0x01; 12-byte header): published at CONTACT completion after Ball.ApplyKick() |
| `src/shot-mechanics/ShotCancelledEvent.cs` | Tier A struct event (IEventA; ordinal 0x0E; 12-byte header): published when a tackle interrupt fires during WINDUP |
| `src/shot-mechanics/ShotAnimationData.cs` | Tier C struct event (IEventC; ordinal 0x0F): animation data stub for Animation System (unconsumed at Stage 0) |
| `src/shot-mechanics/BodyMechanicsResult.cs` | Output struct from BodyMechanicsEvaluator (Score, CQM, StumbleTriggered) |
| `src/shot-mechanics/GoalGeometry.cs` | Value struct: goal width, height, goal-line X, post Y coords, crossbar Z |
| `src/shot-mechanics/IShotVelocityCalculator.cs` | Interface enabling EC-008 NaN injection seam only (ShotVelocityCalculator + NaNVelocityStub) |
| `src/shot-mechanics/IShotBallSystem.cs` | Interface: IsBallPossessedBy() + ApplyKick() to Ball Physics |
| `src/shot-mechanics/IShotAgentQuery.cs` | Interface: GetAttributes() + GetState() to Agent Movement |
| `src/shot-mechanics/IShotCollisionQuery.cs` | Interface: tackle flag poll + pressure scalar to Collision System |
| `src/shot-mechanics/GoalGeometryProvider.cs` | Static access point for goal geometry; test override seam for SP-009 |
| `src/shot-mechanics/ShotVelocityCalculator.cs` | §3.2 velocity formula; stateless singleton; implements IShotVelocityCalculator |
| `src/shot-mechanics/ShotLaunchAngleCalculator.cs` | §3.3 launch angle formula (base angle + power/spin lift + body lean + body shape); pure static |
| `src/shot-mechanics/ShotSpinCalculator.cs` | §3.4 spin vector assembly (topspin / backspin / sidespin); pure static |
| `src/shot-mechanics/ShotPlacementResolver.cs` | §3.5 goal-relative placement → world-space aim direction; also applies error offset |
| `src/shot-mechanics/BodyMechanicsEvaluator.cs` | §3.7 body mechanics score (run-up angle, plant foot, velocity, body lean); pure static |
| `src/shot-mechanics/WeakFootPenaltyApplier.cs` | §3.8 weak-foot error multiplier and velocity multiplier; pure static |
| `src/shot-mechanics/ShotErrorCalculator.cs` | §3.6 deterministic angular error (magnitude, direction hash, offset); pure static |
| `src/shot-mechanics/ShotEventEmitter.cs` | Publishes ShotExecutedEvent, ShotCancelledEvent, ShotAnimationData via EventBusStub |
| `src/shot-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/shot-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for ShotExecutedEvent (0x01) + ShotCancelledEvent (0x0E) + ShotAnimationData (0x0F) |
| `src/shot-mechanics/ShotExecutor.cs` | Sealed orchestrator: 5-state machine (Idle→Windup→Contact→FollowThrough→Complete) |
| `src/shot-mechanics/Tests/NaNVelocityStub.cs` | #if UNITY_EDITOR\|\|DEVELOPMENT_BUILD; returns float.NaN for EC-008 FM-05 recovery test |

### Spec #10 — Heading Mechanics (`src/heading-mechanics/`)

| File | Purpose |
|------|---------|
| `src/heading-mechanics/heading-mechanics.asmdef` | Assembly definition (references agent-movement, ball-physics, collision-system, event-system; added event-system ref May 30, 2026) |
| `src/heading-mechanics/HeadingMechanicsConstants.cs` | All GT/Fixed/Cross/Derived constants (§3.1); region order Fixed→Derived→Cross→GT |
| `src/heading-mechanics/ContactQualityLabel.cs` | Enum: Early / OnTime / Late — telemetry only; KD-2 |
| `src/heading-mechanics/MistimedDirection.cs` | Enum: None / Early / Late — eligibility output |
| `src/heading-mechanics/FailureCause.cs` | Enum: MistimedEarly / MistimedLate / PositionedPoorly / DisturbedInDuel |
| `src/heading-mechanics/SetPieceContext.cs` | Enum: OpenPlay / Corner / FreeKick — telemetry only |
| `src/heading-mechanics/HeadingAgentAttributes.cs` | Struct: Heading/Strength/Balance [1-20], Fatigue [0,1], TeamId |
| `src/heading-mechanics/HeaderIntent.cs` | Struct: PowerIntent/ContactPointIntent/TargetIntent/AttemptCommittedTick/SetPieceContext (locked at commit; KD-17) |
| `src/heading-mechanics/HeaderContactState.cs` | Struct: per-attempt mutable state (JumpStartFrame, quality, disturbance, etc.) |
| `src/heading-mechanics/EligibilityResult.cs` | Struct: IsEligible, PredictedContactFrame, IdealContactFrame, MistimedDirection |
| `src/heading-mechanics/HeaderExecutedEvent.cs` | Tier B struct event (IEventB; ordinal 0x12; 12-byte header): published on successful contact |
| `src/heading-mechanics/HeaderAttemptFailedEvent.cs` | Tier C struct event (IEventC; ordinal 0x13): published on failure; no ball-state modification |
| `src/heading-mechanics/ContestedDuelContext.cs` | Struct: DuelId, ParticipantCount, WinnerAgentId, BufferStartIndex |
| `src/heading-mechanics/IHeadingBallSystem.cs` | Interface: GetBallState + ApplyKick |
| `src/heading-mechanics/IHeadingRngService.cs` | Interface: NextFloat + NextGaussian |
| `src/heading-mechanics/HeadingRngServiceStub.cs` | Stage 0 SplitMix64 stub; replace at Stage 1 with #16 wiring |
| `src/heading-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/heading-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for HeaderExecutedEvent (0x12 Tier B) + HeaderAttemptFailedEvent (0x13 Tier C) |
| `src/heading-mechanics/HeadingEligibility.cs` | Pure eligibility predicate (§3.2); no side effects |
| `src/heading-mechanics/HeadingJumpKinematics.cs` | FM-010-001 JumpReach + Stage 0 synthetic parabolic Z (KD-18) |
| `src/heading-mechanics/HeadingContactQuality.cs` | FM-010-002 contact-quality scalar (asymmetric timing + point error) |
| `src/heading-mechanics/HeadingPowerAngle.cs` | FM-010-003 outgoing speed + reflection geometry + own-goal flag (§3.8) |
| `src/heading-mechanics/HeadingSpinTransfer.cs` | FM-010-004 head angular-velocity derivation + outgoing spin (§3.6) |
| `src/heading-mechanics/HeadingDuelResolution.cs` | FM-010-005 duel scoring; ICollisionEventConsumer; pre-allocated buffers |
| `src/heading-mechanics/HeadingTelemetry.cs` | Stage 0 stub; emits §2.4 heading.* trace-pipeline channels at Stage 0+1 |
| `src/heading-mechanics/HeadingMechanics.cs` | 60 Hz orchestrator; two-pass per-frame loop (§4.6) |

### Spec #11 — Goalkeeper Mechanics (`src/goalkeeper-mechanics/`)

| File | Purpose |
|------|---------|
| `src/goalkeeper-mechanics/goalkeeper-mechanics.asmdef` | Assembly definition (references agent-movement, ball-physics, collision-system, event-system; added event-system ref May 30, 2026) |
| `src/goalkeeper-mechanics/GoalkeeperConstants.cs` | All GT/Fixed/Cross/Derived constants (§3.4); region order Fixed→Derived→Cross→GT; ~79 constants; 4 draw-site IDs |
| `src/goalkeeper-mechanics/GoalkeeperState.cs` | Enum: Resting/Set/Anticipate/Diving/Airborne/HandsOnBall/Recovering/Distributing/Rushing/OneOnOne/Smothered |
| `src/goalkeeper-mechanics/HandlingQualityLabel.cs` | Enum: Caught/Parried/Deflected/Spilled/Missed — telemetry only (KD-2) |
| `src/goalkeeper-mechanics/ReactionLabel.cs` | Enum: Reflexive/Standard/Sluggish — telemetry only (KD-2) |
| `src/goalkeeper-mechanics/FailureCause.cs` | Enum: MissedContact/MistimedDive/WrongDirection/OutOfReach/DisturbedInDuel |
| `src/goalkeeper-mechanics/ClaimType.cs` | Enum: Cross/Aerial/OneOnOne/ShotCatch — telemetry only |
| `src/goalkeeper-mechanics/RushPhase.cs` | Enum: Launched/InFlight/Reached/Aborted |
| `src/goalkeeper-mechanics/AbortReason.cs` | Enum: BallIntercepted/BallCleared/AttackerBeatGK |
| `src/goalkeeper-mechanics/BodyPartEnum.cs` | Enum: Hand/Head/Body/Foot — collision routing (KD-14) |
| `src/goalkeeper-mechanics/HandEnum.cs` | Enum: Left/Right/Either — anatomy lookup KD-1 carve-out (not physics input) |
| `src/goalkeeper-mechanics/DeliveryKind.cs` | Enum: Throw/Roll/Kick — kinematic profile lookup KD-1 carve-out (not physics input) |
| `src/goalkeeper-mechanics/SaveIntent.cs` | Struct: TargetHand/ClutchFirmness/DeflectionTarget/AttemptCommittedTick (from Decision Tree #8) |
| `src/goalkeeper-mechanics/ClaimIntent.cs` | Struct: TargetContactPoint/ClutchFirmness/AttemptCommittedTick |
| `src/goalkeeper-mechanics/DistributeIntent.cs` | Struct: DeliveryKind/TargetReceiverId/TargetPoint/PowerIntent/SpinIntent |
| `src/goalkeeper-mechanics/RushIntent.cs` | Struct: RushTarget/CommitmentLevel/AttemptCommittedTick |
| `src/goalkeeper-mechanics/GkContactState.cs` | Struct: per-attempt mutable state (PredictedContactFrame, ReactionWindowAchieved, HandlingQualityScalar, etc.) |
| `src/goalkeeper-mechanics/CrossClaimDuelContext.cs` | Struct: DuelId, ParticipantCount, WinnerAgentId, ContactBodyPart, BufferStartIndex |
| `src/goalkeeper-mechanics/GoalkeeperAgentAttributes.cs` | Struct: all GK attributes [1-20] + Fatigue [0,1] + normalised accessors |
| `src/goalkeeper-mechanics/GoalkeeperPositioningContract.cs` | Struct: KD-13 consumer contract — holds gkBaselineSlot + reactive-radius bounds logic |
| `src/goalkeeper-mechanics/SaveAttemptedEvent.cs` | Tier A struct event (IEventA; ordinal 0x14; 12-byte header): published on every save attempt; includes telemetry labels |
| `src/goalkeeper-mechanics/BallClaimedEvent.cs` | Tier A struct event (IEventA; ordinal 0x15; 12-byte header): published on Caught save; includes releaseTickEarliest (6-second rule) |
| `src/goalkeeper-mechanics/DistributionExecutedEvent.cs` | Tier A struct event (IEventA; ordinal 0x16; 12-byte header): published when distribution passIntent is emitted to Pass Mechanics #5. v1.2: AR-2 fix — int? TargetReceiverId → int (sentinel -1); nullable padding bytes are non-deterministic in null case. |
| `src/goalkeeper-mechanics/GoalkeeperRushEvent.cs` | Tier C struct event (IEventC; ordinal 0x17): published on rush launch, update, and abort |
| `src/goalkeeper-mechanics/IGoalkeeperBallSystem.cs` | Interface: GetBallState + ApplyKick + SetPossessor |
| `src/goalkeeper-mechanics/IGoalkeeperRngService.cs` | Interface: NextFloat + NextGaussian (4 registered draw sites) |
| `src/goalkeeper-mechanics/GoalkeeperStateMachine.cs` | Pure state evaluator: EvaluateTacticalTransition + EvaluatePhysicsTransition; no side effects |
| `src/goalkeeper-mechanics/GoalkeeperReactionPipeline.cs` | §3.2 formulas: ComputeShotDetectedTickMs / ComputeRequiredReactionMs / ComputeReactionWindowAchieved / ComputeReactionLabel; pure static |
| `src/goalkeeper-mechanics/GoalkeeperDiveKinematics.cs` | §3.3 Stage 0 synthetic dive trajectory: launch impulse, timing jitter, parabolic Z, reach envelope; pure static |
| `src/goalkeeper-mechanics/GoalkeeperHandlingQuality.cs` | §3.5 handling-quality scalar + band-to-action velocity helpers (parry/deflect/spill); pure static |
| `src/goalkeeper-mechanics/GoalkeeperCrossClaimDuel.cs` | §3.6 body-part determination + duel-score arithmetic + tiebreak; implements ICollisionEventConsumer |
| `src/goalkeeper-mechanics/GoalkeeperRushDispatch.cs` | §3.7 rush launch impulse + per-frame update; pure static |
| `src/goalkeeper-mechanics/GoalkeeperDistribution.cs` | §3.8 release-point geometry, windup duration, accuracy coefficient, F-05/F-09 target validation; pure static |
| `src/goalkeeper-mechanics/GoalkeeperTelemetry.cs` | Stage 0 stub; emits §2.4 gk.* trace-pipeline channels at Stage 0+1 (12 channels) |
| `src/goalkeeper-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/goalkeeper-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for SaveAttemptedEvent (0x14) + BallClaimedEvent (0x15) + DistributionExecutedEvent (0x16) + GoalkeeperRushEvent (0x17) |
| `src/goalkeeper-mechanics/GoalkeeperMechanics.cs` | Main 10 Hz + 60 Hz orchestrator: state machine, dive kinematics, handling quality, cross-claim duels, rush, distribution; constructor-injected |

### Perception System (#7) — 14 files

| File | Role |
|------|------|
| `src/perception-system/perception-system.asmdef` | Assembly definition (AI layer; references AgentMovement, BallPhysics, CollisionSystem, FirstTouch, EventSystem; added event-system ref May 30, 2026) |
| `src/perception-system/PerceptionConstants.cs` | All GT/Fixed/Derived/Cross constants (§3.10): 18 spec constants + system-sizing constants |
| `src/perception-system/PerceptionAgentAttributes.cs` | Struct: Decisions, Anticipation, TeamId, IsHalfTurned snapshot (§4.2.2) |
| `src/perception-system/FilteredView.cs` | FilteredView, PerceptionDiagnostics, PerceivedAgent, ShoulderCheckAnimData, OcclusionDebugRecord, PerceivedAgentDebug struct definitions (§3.7) |
| `src/perception-system/PerceptionEvents.cs` | Tier C struct event PerceptionRefreshEvent (IEventC; ordinal 0x10) + RefreshTrigger enum (§4.6.3) |
| `src/perception-system/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/perception-system/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for PerceptionRefreshEvent (0x10) |
| `src/perception-system/FovCalculator.cs` | FoV formula (§3.1) + angular candidacy test + blind-side and peripheral arc predicates; static, no side effects |
| `src/perception-system/OcclusionFilter.cs` | Shadow cone geometry (§3.2.3) + opponent occlusion test; Stage 0: opponents only (OQ-1); static, no side effects |
| `src/perception-system/PressureEvaluator.cs` | PressureScalar formula (§3.6); reused verbatim from First Touch #4 §3.5; static, no side effects |
| `src/perception-system/BallPerceptionEvaluator.cs` | Ball range/FoV/occlusion tests + BallStalenessFrames tracking (§3.5); no L_rec (OQ-2); static, no side effects. v1.2: AR-2 L-1 removed unused prevBallVisible parameter. |
| `src/perception-system/RecognitionLatencyTracker.cs` | Per-(observer,target) latency counters, L_rec formula, half-turn peripheral bonus, Wang/Jenkins deterministic hash (§3.3); INV-10 pre-allocated int[22×22] arrays. v1.2: AR-2 L-3 DeterministicHash literals cast to unchecked int for true 32-bit wrapping. |
| `src/perception-system/ShoulderCheckScheduler.cs` | Autonomous shoulder check scheduling, window management, blind-side entity L_rec (§3.4); INV-10 pre-allocated arrays |
| `src/perception-system/ViewBuilder.cs` | Pure field-assembly step: sets scalar/count fields on pre-allocated FilteredView + PerceptionDiagnostics without overwriting PerceivedAgent[] references (§3.7); static, no computation |
| `src/perception-system/PerceptionSystem.cs` | 10Hz orchestrator; 7-step pipeline for all 22 agents; forced-refresh handler; zero heap allocation on hot path (§3.0–§3.8, §4.1, §4.6). v1.2: AR-2 L-1/L-2 — removed prevBallVisible argument; added length guards to HandleForcedRefresh; added agentHasPossession length guard. |

### Decision Tree (#8) — 36 files

| File | Description |
|------|-------------|
| `src/decision-tree/decision-tree.asmdef` | Assembly definition (AI layer; references agent-movement, perception-system, pass-mechanics, shot-mechanics, heading-mechanics, goalkeeper-mechanics, collision-system, event-system; AR-2 fix May 30, 2026 added heading-mechanics + goalkeeper-mechanics refs) |
| `src/decision-tree/AssemblyInfo.cs` | [assembly: InternalsVisibleTo("TacticalDirector.DecisionTree.Tests")] |
| `src/decision-tree/DecisionTree.cs` | Public sealed class: 6-step pipeline orchestrator + state machine (§3.6, §3.7, §4.1) |
| `src/decision-tree/DecisionTreeStateMachine.cs` | Pure state evaluator: IDLE/EVALUATING/EXECUTING/INTERRUPTED transitions (§3.7.2) |
| `src/decision-tree/SnapshotValidator.cs` | Step 1: validates FilteredView — phase gate, agent identity, ball state (§3.1.1) |
| `src/decision-tree/DecisionContextAssembler.cs` | Step 2: assembles DecisionContext from all pipeline inputs (§2.2.4, §3.1.1) |
| `src/decision-tree/OptionGenerator.cs` | Step 3: generates all eligible ActionOption candidates (§3.1) |
| `src/decision-tree/UtilityScorer.cs` | Step 4: scores ActionOptions with §3.2 formulas |
| `src/decision-tree/TacticalModifierResolver.cs` | Step 4 helper: resolves tactical multipliers per action type (§3.4) |
| `src/decision-tree/ActionSelector.cs` | Step 5: composure noise injection + highest-EffectiveUtility winner (§3.3) |
| `src/decision-tree/ActionDispatcher.cs` | Step 6: routes selected action to movement controller or physics executor (§3.5) |
| `src/decision-tree/DecisionContext.cs` | Internal struct: all assembled pipeline inputs for one agent-tick (§2.2.4) |
| `src/decision-tree/ActionOption.cs` | Internal struct: one scored candidate (§3.1.0) |
| `src/decision-tree/AgentAction.cs` | Public readonly struct: pipeline output (type, target, params, utility) (§2.2.3) |
| `src/decision-tree/DecisionMadeEvent.cs` | Tier C struct event (IEventC; ordinal 0x11): published after each decision (§2.2.7) |
| `src/decision-tree/DtAgentAttributes.cs` | Struct: all DT-consumed player attributes [1–20] + CreateDefault factory (§3.1) |
| `src/decision-tree/MatchContext.cs` | Struct: authoritative match state per heartbeat (§2.2.5) |
| `src/decision-tree/TacticalContext.cs` | Struct: pressing mode, passing style, formation slots; Stage0Default factory (§2.2.6) |
| `src/decision-tree/DecisionTreeConstants.cs` | Constants: capacity limits / timing budgets / pipeline invariants (§4.2, §3.7) |
| `src/decision-tree/UtilityWeights.cs` | Constants: all 58+ utility scoring constants (§3.2.11) |
| `src/decision-tree/ComposureWeights.cs` | Constants: NOISE_MAX / COMPOSURE_SUPPRESSION / TIEBREAK_EPSILON (§3.3.3–3.3.5) |
| `src/decision-tree/TacticalWeights.cs` | Constants: tactical multipliers for all action types (§3.4) |
| `src/decision-tree/PitchGeometry.cs` | Static helpers: field zone classification, goal post positions, centre (§3.1.1) |
| `src/decision-tree/IDtMovementController.cs` | Public interface: dispatch boundary to Agent Movement #2 (§3.5) |
| `src/decision-tree/EventBusStub.cs` | Wired to EventBus.Publish (internal; single-sig for DecisionMadeEvent) |
| `src/decision-tree/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for DecisionMadeEvent (0x11) |
| `src/decision-tree/ActionType.cs` | Enum: PASS/SHOOT/DRIBBLE/HOLD/MOVE_TO_POSITION/PRESS/INTERCEPT |
| `src/decision-tree/DtState.cs` | Enum: IDLE/EVALUATING/EXECUTING/INTERRUPTED (§3.7.1) |
| `src/decision-tree/FieldZone.cs` | Enum: DEFENSIVE/MIDFIELD/ATTACKING |
| `src/decision-tree/MatchPhase.cs` | Enum: OPEN_PLAY/SET_PIECE_HOME/SET_PIECE_AWAY/KICK_OFF |
| `src/decision-tree/PassingStyle.cs` | Enum: DIRECT/MIXED/SHORT |
| `src/decision-tree/PressingMode.cs` | Enum: HIGH/MEDIUM/LOW |
| `src/decision-tree/PossessionState.cs` | Enum: HOME_TEAM/AWAY_TEAM/CONTESTED |
| `src/decision-tree/Tests/decision-tree-tests.asmdef` | Test assembly (EditMode; references decision-tree.asmdef) |
| `src/decision-tree/Tests/OptionGeneratorTests.cs` | UT-01..07: OptionGenerator generation gates and candidate logic |
| `src/decision-tree/Tests/UtilityScorerTests.cs` | UT-08..09: UtilityScorer per-action-type utility formulas |
| `src/decision-tree/Tests/ActionSelectorTests.cs` | UT-10..15: ActionSelector composure noise + winner selection |
| `src/decision-tree/Tests/DispatcherTests.cs` | UT-16..23: ActionDispatcher movement routing |
| `src/decision-tree/Tests/DecisionTreeIntegrationTests.cs` | UT-24..32: full pipeline state machine + output |

### Positioning AI (#12) — 20 files

| File | Description |
|------|-------------|
| `src/positioning-ai/positioning-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai constants) |
| `src/positioning-ai/PositioningAIConstants.cs` | Single constant catalogue (FR-PA-011/KD-17): pitch/spacing/hysteresis/GK/phase constants + 3 formation tables + pull-factor 13×4 table + lane edges |
| `src/positioning-ai/Phase.cs` | Enum: InPoss/OutOfPoss/TransToAtk/TransToDef (byte) |
| `src/positioning-ai/LineId.cs` | Enum: Defense/Midfield/Attack (byte) |
| `src/positioning-ai/LaneId.cs` | Enum: LW/LH/C/RH/RW — five 13.6 m bins (byte) |
| `src/positioning-ai/RoleId.cs` | Enum: 13 roles GK..ST — row index in 13×4 pull-factor table (byte) |
| `src/positioning-ai/FormationFamily.cs` | Enum: F442/F433/F4231 (byte) |
| `src/positioning-ai/FormationSlotRecord.cs` | Readonly struct: LongPct/LateralPct/Role/DefaultLine/DefaultLane/IsGoalkeeper |
| `src/positioning-ai/ContextModifierInputs.cs` | Readonly struct: ScoreDiff/TeamMeanFatigue/TacticalIntensity |
| `src/positioning-ai/AgentPositioningData.cs` | Readonly struct: EntityId/SlotIndex/Position/IsActive/Role/IsGoalkeeper |
| `src/positioning-ai/AgentHysteresisState.cs` | Struct: CurrentLine/CandidateLine/LineDwellCount/CurrentLane/CandidateLane/LaneDwellCount |
| `src/positioning-ai/HysteresisState.cs` | Sealed class: team phase state + AgentHysteresisState[] Agents; SeedFromFormation() |
| `src/positioning-ai/PositioningPerceptionSnapshot.cs` | Sealed class: pre-allocated tick input (TickIndex/BallPosition/BallVxFiltered/Agents[]) |
| `src/positioning-ai/PhaseClassifier.cs` | Pure static: ClassifyAndCommit() PHASE_HYSTERESIS_TICKS dwell; indeterminate → lastCommitted |
| `src/positioning-ai/AnchorCalculator.cs` | Pure static: ComputeAnchor/ComputeBallRelativeOffset/ComputeGkSlot (own-half ball.x clamp) |
| `src/positioning-ai/ContextModifier.cs` | Pure static: ApplyToAll() — lateral + vertical compactness scaling relative to centroid (§3.5) |
| `src/positioning-ai/SpacingResolver.cs` | Pure static: EnforceHardSpacing() cost-based displacement up to SPACING_MAX_PASSES (§3.6) |
| `src/positioning-ai/ShapeAnalyzer.cs` | Pure static: ResolveAllLines() insertion-sort + LINE_DWELL_TICKS; ResolveAllLanes() LANE_DWELL_TICKS; called AFTER spacing+clamp (AR-S1-03) |
| `src/positioning-ai/SlotComposer.cs` | Pure static: Compose() 7-step pipeline (anchor→offset→modifiers→spacing→clamp→lines→lanes) |
| `src/positioning-ai/PositioningAITick.cs` | Sealed class: 10 Hz orchestrator; zero-alloc hot path; F1 stale detection; GetFormationSlot/GetLine/GetLane/GetPhase |
| `src/positioning-ai/Tests/positioning-ai-tests.asmdef` | Test assembly (EditMode; references positioning-ai.asmdef) |
| `src/positioning-ai/Tests/PositioningAITests.cs` | T-U-001..021 (unit) + T-D-001..002 (determinism) + T-I-001..004 (integration) + T-P-001 (perf) + T-T-001 (tactical) |
| `src/pressing-ai/pressing-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pass-mechanics) |
| `src/pressing-ai/PressingAIConstants.cs` | Single constant catalogue: trigger distances/durations, cover-shadow geometry, stamina costs, pitch constants (GT/Fixed/Derived/Cross regions) |
| `src/pressing-ai/TriggerFlags.cs` | [Flags] enum: None / BadTouch / BackwardPass / SidelineTrap / WeakReceiver (byte) |
| `src/pressing-ai/PressRole.cs` | Enum: HoldShape / PrimaryPress / CoverShadow (byte) |
| `src/pressing-ai/CoverShadow.cs` | Struct: DefenderId, ReceiverId, TargetPosition |
| `src/pressing-ai/PressDirective.cs` | Struct: per-tick output (PrimaryPresserId, PrimaryTargetPosition, Shadow0, Shadow1, CoverShadowCount, ActiveTriggers); static Inactive; IsActive property |
| `src/pressing-ai/PressAssignment.cs` | Struct: per-agent output (EntityId, Role, TargetPosition) |
| `src/pressing-ai/PressTrigger.cs` | Struct: 8 dwell/release counters (4 dwell + 4 release; zero allocation, no arrays) |
| `src/pressing-ai/RoleHysteresisState.cs` | Sealed class: LastRole[], RoleDwell[] arrays; Reset() |
| `src/pressing-ai/PressingAgentSnapshot.cs` | Struct: per-agent tick input (EntityId, TeamId, Position, BaselineSlot, Fatigue, FirstTouchAttribute, Line, IsGoalkeeper, HasBall, IsActive) |
| `src/pressing-ai/PressingSnapshot.cs` | Sealed class: tick input container (TickIndex, BallPosition, BallVelocity, BallCarrierEntityId, AttackingDirection, PossessionTeamId, PressingTeamId, Agents[22]) |
| `src/pressing-ai/PassEventRing.cs` | Sealed class: ring buffer for BackwardPass trigger (Push, TryGetLatest, Clear) |
| `src/pressing-ai/PositioningAIView.cs` | Readonly struct: facade over PositioningAITick (GetFormationSlot, GetLine, GetPhase, IsSentinelSlot) |
| `src/pressing-ai/TriggerEvaluator.cs` | Pure static: Evaluate() debounce pipeline for 4 triggers + ComputeGeometricPressure helper |
| `src/pressing-ai/PrimaryPressSelector.cs` | Pure static: Select() best presser by cost; ComputeInterceptionPoint(); GetCarrierPosition() helper |
| `src/pressing-ai/CoverShadowSelector.cs` | Pure static: Select() up to 2 cover shadows; threat score + greedy defender assignment |
| `src/pressing-ai/RoleHysteresis.cs` | Pure static: Commit() dwell guard; ForceAllHoldShape() |
| `src/pressing-ai/StaminaAccumulator.cs` | Pure static: Apply() per-role fatigue cost; ApplyAll() batch apply |
| `src/pressing-ai/DisengageResolver.cs` | Pure static: Evaluate() disengage conditions (zone exit + timeout); IsInCooldown() |
| `src/pressing-ai/InvariantEnforcer.cs` | Pure static: Enforce() three anti-chaos invariants (MaxPressersBallThird, MinBacklineAgents, MaxPressDisplacementM) |
| `src/pressing-ai/PressingAITick.cs` | Sealed class: 10 Hz orchestrator; 8-step pipeline; pre-allocated buffers; zero-alloc hot path |

### `src/defensive-ai/` — Spec #14 (19 files: 18 .cs + 1 asmdef)

| File | Role |
|------|------|
| `src/defensive-ai/defensive-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pressing-ai) |
| `src/defensive-ai/DefensiveAIConstants.cs` | Single constant catalogue: 22 [GT] + 4 [CROSS] constants (assignment, hysteresis, offside-trap, tackle, anti-chaos, GK-zone bounds) |
| `src/defensive-ai/MarkMode.cs` | Enum: Zonal / ManMark / InterceptRunner / CoverGkZone (byte; FR-DA-011) |
| `src/defensive-ai/TackleMode.cs` | Enum: Hold / Jockey / Commit (byte) |
| `src/defensive-ai/MarkDirective.cs` | Struct: team-level tick output (TeamId, OffensiveLineDepth, OffsideTrapActive, StepUpTargetDepth, EmergencyFlag); Inactive() factory |
| `src/defensive-ai/MarkAssignment.cs` | Struct: per-agent assignment (Mode, TargetEntityId, TargetPosition, ValidThroughTick, OverriddenThisTick, IsManuallyAssigned); MakeZonal() factory |
| `src/defensive-ai/TackleIntentRequest.cs` | Struct: per-agent tackle intent (AgentEntityId, Mode, TargetEntityId, ApproachAngle, CoverageDepth) |
| `src/defensive-ai/MarkHysteresisState.cs` | Struct: per-agent dwell-lock state (DwellCounter, CandidateMode, CandidateTargetEntityId, HoldTicks); Default() factory |
| `src/defensive-ai/OffsideLineState.cs` | Struct: per-team offside state (CurrentLineDepth, StepUpDwellCounter, CooldownTicksRemaining, CoverGkZoneActiveTicks); Default() factory |
| `src/defensive-ai/DefensiveAgentSnapshot.cs` | Struct: per-agent tick input (EntityId, TeamId, Position, Velocity, IsActive, IsGoalkeeper, HasBall, BaselineSlot, Line, PressRole, PerceivedFirstTouch) |
| `src/defensive-ai/DefensiveSnapshot.cs` | Sealed class: tick input container (TickIndex, DefensiveTeamId, BallPosition, BallVelocity, TeamPhase, DefensiveLineDepth, GkEntityId, GkPosition, Agents[22], HasActivePrimaryPress) |
| `src/defensive-ai/HoldShapePoolFilter.cs` | Pure static: BuildPool() filters GK + PrimaryPress/CoverShadow; SnapshotIndexOf(); IndexOf() |
| `src/defensive-ai/LastManDetector.cs` | Pure static: Evaluate() last-man predicate (§3.8) + COVER_GK_ZONE trigger (§3.9); DefendsX0/DistToOwnGoal/DisplacementCost/ComputeAbandonedZoneCenter helpers; LastManResult struct |
| `src/defensive-ai/MarkHysteresis.cs` | Pure static: PreCheck() dwell-lock gate; ApplyGate() transition accumulator; Reset() for emergency overrides |
| `src/defensive-ai/MarkAssigner.cs` | Pure static: Assign() regular assignment loop (§3.3); ThreatScore() (§3.5); SelectBestCandidate(); IsBetter() tie-break comparator |
| `src/defensive-ai/TackleIntentEvaluator.cs` | Pure static: Evaluate() tackle intent (§3.6); ComputeCoverageDepth(); SelectMode() |
| `src/defensive-ai/OffsideTrapController.cs` | Pure static: Update() dwell counter + fire trigger (§3.7); ExecuteStepUp(); ComputeDefenseLineSpread() |
| `src/defensive-ai/InvariantEnforcer.cs` | Pure static: Enforce() 3 anti-chaos invariants (§3.10); 3-pass demotion loop; AreAllSatisfied() post-loop check; F4 hard-fallback detection |
| `src/defensive-ai/DefensiveAITick.cs` | Sealed class: 10 Hz orchestrator; 9-step §3.13 pipeline; pre-allocated buffers; GetMarkDirective/GetAssignment/GetTackleIntentRequests public API |

### `src/attacking-ai/` — Spec #15 (24 files: 23 .cs + 1 asmdef)

| File | Description |
|------|-------------|
| `src/attacking-ai/attacking-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pressing-ai) |
| `src/attacking-ai/AttackingAIConstants.cs` | Single constant catalogue: GT/Derived/Cross constants (run-params bounds, support radius, width, weak-side, overload, invariants, hysteresis, test criteria, angle epsilon) |
| `src/attacking-ai/AttackRole.cs` | Enum: HoldWidth / SupportBall / Runner / WeakSide (byte; FR-AT-012) |
| `src/attacking-ai/Flank.cs` | Enum: Left / Right — overload lateral discriminator (§3.8) |
| `src/attacking-ai/RunParameters.cs` | Readonly struct: DepthOffsetM / LateralOffsetM / RunTriggerTick — exactly 3 fields (FR-AT-011) |
| `src/attacking-ai/AttackHysteresisState.cs` | Struct: per-agent dwell state (CurrentRole, DwellCounter, CandidateRole, CandidateDwell) |
| `src/attacking-ai/TransitionHoldState.cs` | Struct: per-team possession-loss countdown + PrevPhase |
| `src/attacking-ai/AttackDirective.cs` | Readonly struct: team-level tick output (TeamId, OverloadActive, OverloadFlank, TransitionHoldTick); static Empty |
| `src/attacking-ai/AttackIntent.cs` | Readonly struct: per-agent tick output (AgentEntityId, Role, RunParameters?, ValidThroughTick) |
| `src/attacking-ai/StyleProfile.cs` | Readonly struct: 5 profile multipliers + static factories Possession/Direct/Counter |
| `src/attacking-ai/AttackIntentSnapshot.cs` | Readonly struct: read-only zero-copy view over tick output (Directive, Intents[], IntentCount, TickIndex) |
| `src/attacking-ai/AttackingAgentSnapshot.cs` | Readonly struct: per-agent tick input (EntityId, TeamId, Position, BaselineSlot, Line, IsGoalkeeper, HasBall, IsActive, Pace, Stamina, Dribbling) |
| `src/attacking-ai/AttackingSnapshot.cs` | Sealed class: pre-allocated tick input container (TickIndex, AttackingTeamId, BallPosition, BallCarrierEntityId, BallCarrierPosition, TeamAttackAngle, Agents[22]) |
| `src/attacking-ai/AttackPoolEntry.cs` | Internal struct: per-agent scratch entry during pipeline (EntityId, Position, LateralPct, Line, AssignedRole, HasRunParams, run-param fields, RunTargetPosition, TargetPosition) |
| `src/attacking-ai/AttackingPoolBuilder.cs` | Pure static: Build() filters snapshot→pool, EntityId-ascending insertion sort; −1 on F2 sentinel |
| `src/attacking-ai/AttackHysteresis.cs` | Pure static: IsStable() / Update() (with CandidateDwell reset on current-role re-preference) / Reset() — increment-based dwell |
| `src/attacking-ai/SupportHeuristic.cs` | Pure static: IsWithinSupportRadius() / ComputeEffectiveRadius() — floor = MinEffectiveRadiusM |
| `src/attacking-ai/RoleAssigner.cs` | Pure static: Assign() two-pass (pass 1 counts stable, pass 2 evaluates non-stable); GenerateRunParams() §3.4 with Mathf.RoundToInt |
| `src/attacking-ai/WidthHolder.cs` | Pure static: Enforce() near-touchline width-holding; skips near-side HoldWidth+WeakSide in promotion loop |
| `src/attacking-ai/WeakSideController.cs` | Pure static: EnsureWeakSide() post-check; selects max-|Y-ballY| non-RUNNER agent |
| `src/attacking-ai/OverloadDetector.cs` | Pure static: Evaluate() counts non-WEAK_SIDE agents in Y-corridor; fires at ≥OverloadCount |
| `src/attacking-ai/TransitionController.cs` | Pure static: Evaluate() SET-then-DECREMENT transition hold; COUNTER (0 ticks) → instant empty |
| `src/attacking-ai/InvariantEnforcer.cs` | Pure static: Apply() 3 anti-chaos invariants (max runners, min support, no own-half runs); ApplyFallback() all-HoldWidth |
| `src/attacking-ai/AttackingAITick.cs` | Sealed class: 10 Hz orchestrator; §3.13 pipeline; pre-allocated zero-alloc buffers; LastDirective/GetIntent/GetSnapshot public API |

### `src/deterministic-sim/` — Spec #16 (23 files: 21 .cs + 2 asmdef)

> Cross-cutting foundation assembly; all gameplay layers reference it; it references no other gameplay assembly.
> AR-1 (4H+4M) + AR-2 (1L) + AR-3 (1L) adversarial review cycles complete (AR-3 clean). Implementation date: May 29, 2026.

| File | Purpose |
|------|---------|
| `src/deterministic-sim/deterministic-sim.asmdef` | Assembly definition (no references — cross-cutting foundation) |
| `src/deterministic-sim/DeterministicSimConstants.cs` | All [FIXED]/[DERIVED]/[GT] constants: tick rates, error codes (0x1601–0x160D), domain tags (0x10–0x1D), field widths, RNG params, digest/schema versions, END_OF_SNAPSHOT_PHASE_ORDINAL=6 |
| `src/deterministic-sim/PhaseId.cs` | Enum: Input=0 / Intent=1 / AI=2 / Physics=3 / Resolve=4 / Events=5 / Snapshot=6 (byte; AR-1 H-4: AI_NoOp removed; Events=5 added) |
| `src/deterministic-sim/DeterminismTier.cs` | Enum: TierA=0 / TierB=1 / TierC=2 (byte) |
| `src/deterministic-sim/DivergenceClass.cs` | Enum: None / HardDesync / SoftDrift / Cosmetic (byte) |
| `src/deterministic-sim/SubsystemOrdinals.cs` | Compile-time const ints for deterministic intra-phase ordering: BallPhysics=0..GoalkeeperMechanics=7 (Physics 0–19), PositioningAI=20..AttackingAI=23 (Mechanics 20–39), PerceptionSystem=40, DecisionTree=41 (AI 40–59), EventSystem=60 |
| `src/deterministic-sim/ReplayCursor.cs` | Readonly struct: Tick (ulong), PhaseOrdinal (byte), IsAtEndOfSnapshot property, EndOfSnapshot(tick) factory — step-7 boundary assertion in ReplayEngine |
| `src/deterministic-sim/DespawnEntry.cs` | Readonly struct: EntityId (int), FinalActionOrdinal (ulong), FinalRngCursor (ulong), DespawnTick (ulong) — Tier A tombstone written by Resolve phase |
| `src/deterministic-sim/DespawnLog.cs` | Pre-allocated tombstone list: Append / ContainsEntity / GetEntry / Clear; capacity = MaxDespawnEntries (512) |
| `src/deterministic-sim/EnvironmentFingerprint.cs` | Sealed class: 6 readonly fields (WorkerCount, SchedulerPolicy, ReductionTopology, SimdFeatureLevel, FloatModelHash, UnicodeNormalizationVersion); Lock(); ValidateAgainst() → ERR_DS_REPLAY_ENV_MISMATCH; CreateStage0Dev() factory |
| `src/deterministic-sim/RngStreamState.cs` | Mutable struct: StreamKey/RngCursor/ActionOrdinal (ulong), BudgetRemaining/DeclaredBudget/DrawIndex (int), SiteId (string), StreamVersion (ushort), SubsystemOrdinal (int), EntityId (int); ClearReservation() |
| `src/deterministic-sim/MatchClock.cs` | Sealed class: CurrentTick / CurrentTacticalTick (÷AI_PHASE_STRIDE) / CurrentMatchTimeMs (×FrameMs) / IsAiStrideTick; Advance(); RestoreFromSnapshot(tick) for replay step 5 — no System.DateTime (FR-CS-042) |
| `src/deterministic-sim/DeterministicRngService.cs` | Sealed class: HKDF-SHA256 key derivation at construction; SipHash-2-4-64 per-draw hash; RegisterStream / Reserve / DrawReserved / CloseReservation / Skip / RestoreStream; zero-alloc hot path (stackalloc Span<byte>[21]; AR-1 H-3) |
| `src/deterministic-sim/CanonicalSerializer.cs` | Static class: §3.2.4.1 Write/Read for bool, u8/i8, u16/i16, u32/i32, u64/i64, f32 (−0.0→+0.0), f32TierB (NaN→0x7FC00000), f64, strings, bytes, optional tags; FloatUintUnion explicit-layout struct (AR-1 H-1/H-2: eliminates BitConverter.GetBytes heap alloc) |
| `src/deterministic-sim/SnapshotHeader.cs` | Sealed class: SchemaVersion (u32) / DigestVersion (u16) / Tick (u64) / PrevSnapshotDigest[32] / CurrentSnapshotDigest[32] / Fingerprint / Cursor; Initialize(tick, prevDigest, fingerprint) |
| `src/deterministic-sim/SnapshotPayload.cs` | Sealed class: pre-allocated PayloadBytes[MaxSnapshotBytes] / BytesWritten; Reset() |
| `src/deterministic-sim/SnapshotCodec.cs` | Sealed class: Encode() — SHA-256 over payload bytes, digest chain advance; ValidateHeader() → ERR_DS_SCHEMA_INCOMPATIBLE; ValidatePrevDigest() → ERR_DS_DIGEST_CHAIN_BREAK; CommitLoadedDigest() for replay load |
| `src/deterministic-sim/ReplayEngine.cs` | Sealed class: PrepareReplay() executes §4.2.2 steps 1–7; step 6 (RNG restoration) is Stage 0 stub comment (AR-3 L-1: empty loop replaced); step 8 (ReapplyInputsFromT+1) delegated to TickOrchestrator |
| `src/deterministic-sim/SaveManager.cs` | Sealed class: CommitAtomic() implements §4.6.1.1 five-step atomic save (temp write → fsync → rename-with-overwrite → dir fsync); File.Move(overwrite:true) (AR-1 M-2: IOException fix) |
| `src/deterministic-sim/TickOrchestrator.cs` | Sealed class: RunTick() 7-phase 60 Hz pipeline (Input→Intent→AI/AI_NoOp→Physics→Resolve→Events→Snapshot); AI stride-gated on IsAiStrideTick; System.Action phase callbacks; 9 ProfilerMarkers; zero-alloc hot path |
| `src/deterministic-sim/DivergenceDetector.cs` | Static class: CompareDigests / CompareTierAFloat / CompareTierBFloat (AR-1 M-3: one-canonical-NaN case returns SoftDrift) / CompareTierAInt / CompareTierAUlong / Worst(DivergenceClass, DivergenceClass) |
| `src/deterministic-sim/tests/deterministic-sim-tests.asmdef` | Test assembly definition (EditMode; references deterministic-sim.asmdef) |
| `src/deterministic-sim/tests/DeterministicSimTests.cs` | HKDF RFC 5869 Appendix A.1 KAT; SipHash-2-4-64 ref vectors 0–7; canonical serialization (bool, u32/u64 LE, −0.0, PHYSICS_DT bits); T-DS-ORDER-001 clock sequence; T-DS-RNG-002 branch cursor parity; T-DS-SNAP-003 u64 round-trip; T-DS-FAULT-009..014 (budget mismatch, Tier A NaN, Tier B non-canonical NaN, digest chain break, env mismatch, replay boundary); AI stride; DespawnLog |

### `src/event-system/` — Spec #17 (21 files: 19 .cs + 2 asmdef)

> Cross-cutting foundation assembly; autoReferenced true so Assembly-CSharp assemblies get it automatically; spec assemblies with their own .asmdef need explicit references.
> AR-1 (3H+3M+2L) + AR-2 (1L) + AR-3 clean adversarial review cycles complete. Implementation date: May 30, 2026.

| File | Purpose |
|------|---------|
| `src/event-system/event-system.asmdef` | Assembly definition (references TacticalDirector.DeterministicSim; autoReferenced true) |
| `src/event-system/EventSystemConstants.cs` | All [GT]/[CROSS] constants: queue/dispatch/handler/slot capacities + error codes (0x1701–0x1706) + DomainTagEventLedger (0x15). v1.1: AR-2 fix — MaxEventSlotBytes 128→160; ErrEvtUnregisteredOrdinal (0x1706) added. |
| `src/event-system/IEventA.cs` | Marker interface: Tier A (authoritative, ring-buffered, digest-included) |
| `src/event-system/IEventB.cs` | Marker interface: Tier B (bounded-authoritative; Stage 5+ tolerance path) |
| `src/event-system/IEventC.cs` | Marker interface: Tier C (cosmetic; immediate CosmeticChannel dispatch; excluded from digest) |
| `src/event-system/EventHandler.cs` | Delegate: `void EventHandler<T>(in T evt) where T : struct` |
| `src/event-system/SubscriptionToken.cs` | Readonly struct: EventTypeOrdinal + SubscriberIndex; zero allocation (FR-EVT-073) |
| `src/event-system/EventRegistry.cs` | Appendix A registry: 11 seeded rows (0x01–0x0B) + placeholder rows 0x0C–0x17 (updated by owning spec's EventBusRegistrar.Initialize()); RegisterRow<T> / RegisterRowRaw / RegisterExternalRow<T>; EventOrdinalCache<T> O(1) static-field lookup. v1.3: AR-3 fix — IsRegistered now requires StructSize > 0 (placeholder RegisterRowRaw rows return false until Initialize() sets struct size). |
| `src/event-system/EventLedger.cs` | Ring buffer + typed BFS dispatch; EventSlotMeta (FM-017-002 sort key); EventTypeDispatchBase / EventTypeDispatcher<T>; DrainTick; InsertionSort; SerializeLedger; Subscribe. v1.3: AR-4 H-1: EventTypeOrdinal removed from CompareKey (not in FM-017-002); AR-4 H-2: SerializeLedger now sorts by FM-017-002 key (was insertion order). |
| `src/event-system/CosmeticChannel.cs` | Tier C immediate dispatch: per-ordinal pub-count table; ≥ maxPerTick drop predicate; stackalloc span dispatch (zero-alloc FR-EVT-048). v1.5: AR-3 fix — structSize <= 0 guard promoted from silent return to throw; added upper-bound guard structSize > MaxEventSlotBytes preventing ArgumentOutOfRangeException crash on oversized Tier C structs. |
| `src/event-system/EventBus.cs` | Public static API: BeginTick / BeginPhase / DrainTick / SerializeLedger / OnTickBoundary; Publish / Subscribe overloads per tier. v1.3: AR-2 fix — unconditional if/throw guard in PublishAuthoritative; Subscribe<IEventA/B> guard. v1.4: AR-4 M-1: upper-bound structSize > MaxEventSlotBytes guard added to PublishAuthoritative Tier A/B path; AR-4 L-1: structSize<=0 fallback promoted to throw. |
| `src/event-system/PossessionChangedEvent.cs` | Tier A 0x04: PreviousHolder / NewHolder / Reason |
| `src/event-system/FoulCommittedEvent.cs` | Tier A 0x05: Offender / Victim / Location (Vector3) / FoulKind |
| `src/event-system/CardIssuedEvent.cs` | Tier A 0x06: Recipient / CardKind / FoulOrdinal (byte; 0xFF = procedural) |
| `src/event-system/GoalAwardedEvent.cs` | Tier A 0x07: Scorer / Assister / ScoringTeam / BallPosition (Vector3) |
| `src/event-system/SubstitutionEvent.cs` | Tier A 0x08: Outgoing / Incoming / Team / SubstitutionReason |
| `src/event-system/TickHeartbeatEvent.cs` | Tier C 0x09: empty payload (CLR min size 1 byte); MaxPerTick=1 |
| `src/event-system/VfxImpactCue.cs` | Tier C 0x0A: ImpactPoint (Vector3) / ImpactKind / Intensity; MaxPerTick=64 |
| `src/event-system/UiNotificationCue.cs` | Tier C 0x0B: NotificationKind / SubjectEntity; MaxPerTick=32 |
| `src/event-system/tests/event-system-tests.asmdef` | Test assembly (EditMode; autoReferenced false; references TacticalDirector.EventSystem + TacticalDirector.DeterministicSim + 6 production spec assemblies for the boot-wiring smoke test: PassMechanics / ShotMechanics / PerceptionSystem / DecisionTree / HeadingMechanics / GoalkeeperMechanics — added 2026-06-08 with EventBusWiringSmokeTests.cs landing). |
| `src/event-system/tests/EventBusWiringSmokeTests.cs` | SMOKE-EVT-WIRING-001 boot-wiring smoke test (added 2026-06-08). Drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts SHA-256 digest stability across runs. Catches regressions in ordinal allocation, version byte, producer-phase wiring, subsystem-ordinal embedding, struct-size registration, and canonical byte layout. Covers 6 currently-wired registrars (Pass / Shot / Perception / Decision / Heading / Goalkeeper); Agent Movement (#2) is a `[CROSS-PENDING]` slot. Golden digest pinned via Assert.Inconclusive (surfaces unfilled pin to CI as yellow) until the AM-side EventBusRegistrar lands. AR-1 (1H+1M+4L) + AR-2 (0H+1M+4L) + AR-3 (0H+0M+2L cycle-stop) adversarial review cycles complete. |

### `src/performance-optimization/` — Spec #18 (17 files: 16 .cs + 1 asmdef)

> Infrastructure-only assembly (Spec #18). autoReferenced false; references TacticalDirector.DeterministicSim (for EnvironmentFingerprint #16 §4.8). Game-layer assemblies (Physics / Mechanics / AI) MUST NOT import this assembly at runtime. Scaffold added May 30, 2026; promoted to real implementation June 1, 2026.

| File | Purpose |
|------|---------|
| `src/performance-optimization/performance-optimization.asmdef` | Assembly definition; references TacticalDirector.DeterministicSim; autoReferenced false |
| `src/performance-optimization/HotPathAllocExemptAttribute.cs` | Governance attribute (§3.7.5): `[HotPathAllocExempt(justification)]` with optional `SignOffRef`; `AttributeTargets.Method\|Class\|Struct`; `Inherited=false; AllowMultiple=false` |
| `src/performance-optimization/ChannelVerbosity.cs` | F.0 schema enum: Minimal / Standard / Debug / Exhaustive (FR-PO-055). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/ChannelSamplingRule.cs` | F.0 schema enum: EveryTick / PerNTicks / EventDriven (FR-PO-056). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/ChannelDeterminismClass.cs` | F.0 schema enum: TierA / TierB / TierC (FR-PO-058a). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/TraceChannelDescriptor.cs` | v1.2 — F.0 sealed descriptor (11 fields); constructor enforces both structural F.0 invariants (SamplingRule=PerNTicks ⇒ SampleN>0 per AR-1 L-1; InsideTickPipeline ⇒ SignOffLogRef non-empty per AR-2 L-2) and carries XML <summary> per AR-2 M-2. Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/TraceChannelRegistry.cs` | Stage 0 anchor rows: PerfBudget / PerfAlloc / PerfTrace (perf.* channels owned by Spec #18). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/PerformanceOptimizationConstants.cs` | v1.2 — Fixed: HOT_PATH_ALLOC_BUDGET_BYTES=0 / LOOP_TAG_TACTICAL_10HZ / LOOP_TAG_PHYSICS_60HZ; GT: PerPrRegressionFraction=0.05 / AbsoluteDriftFraction=0.10 / BaselineSampleCount=100 / MaxFlakeRate=0.01 / HeadroomMultiplierMin=1.2 / HeadroomMultiplierMax=1.5 / PromotionToleranceFraction=0.20 / ReproducibilityToleranceFraction=0.20; EST: SamplerDefaultHz=1000 / StatisticalSignificanceN=30 / FirstTickWarmupCount=0 |
| `src/performance-optimization/LoopTag.cs` | enum: TacticalTenHz / PhysicsSixtyHz — loop discriminator per KD-8 / §3.2.2; on-disk string keys are LOOP_TAG_* constants |
| `src/performance-optimization/BaselinePassFail.cs` | enum: Pass / Fail / Advisory — capture-time pass/fail verdict per Appendix A (advisory at capture; authoritative at CI gate time) |
| `src/performance-optimization/HardwareCounterSnapshot.cs` | readonly struct: CpuModel / CoreCount / ThermalState — §3.3.2 session manifest hardware field |
| `src/performance-optimization/SessionManifest.cs` | v1.2 — sealed class: all §3.3.2 required fields (GitSha / Seed / EnvironmentFingerprint #16 §4.8 / PlatformPin / ScenarioManifestId / SessionStartUtc / SessionEndUtc / HardwareCounters / HarnessVersion); `IsComplete()` validator used by §3.4.4 baseline validator (AR-2 L-1: validates HardwareCounters.CpuModel/ThermalState; PR #129 Codex P2: also requires HardwareCounters.CoreCount > 0 — completes the §3.3.2 hardware-snapshot triple) |
| `src/performance-optimization/BaselineRecord.cs` | sealed class: SessionManifest + Loop + P50Ms + P99Ms + PerMethodAllocBytes + PassFail + ThresholdCited — immutable baseline record per Appendix A / §4.3.2 |
| `src/performance-optimization/BudgetRollupEntry.cs` | readonly struct: SpecId / SubroutineName / Loop / BudgetMs / AllocBudgetBytes / Citation — one row in the §3.1.3 Appendix C roll-up table; produced by IBudgetSource |
| `src/performance-optimization/HotPathEntry.cs` | readonly struct: SpecId / MethodName / Loop / BudgetMs / HasAllocExemption — one entry in the §3.7.2 hot-path union set (materialised into tools/hot-path-union.json at Stage 0+1) |
| `src/performance-optimization/IPerfHarness.cs` | interface: BeginSession(manifest) / RecordTickSample(declared, actualMs, allocBytes) / FinalizeSession() → BaselineRecord; both producer (§3.3 harness) and consumer (#19 ScenarioRunner) specified per §4.3.1 / §4.4 |
| `src/performance-optimization/IBudgetSource.cs` | v1.1 — interface: SpecId property + GetEntries() → IReadOnlyList<BudgetRollupEntry> (AR-1 L-3 read-only return type); both producer (per-spec §6 extractor) and consumer (budget-auditor.py) specified per §4.4 |
| `src/performance-optimization/RegressionResult.cs` | readonly struct: PerPrPassed / AbsoluteDriftPassed / DeltaFraction / MilestoneDriftFraction / AllPassed — output of RegressionGate.Evaluate (FR-PO-031) |
| `src/performance-optimization/RegressionGate.cs` | v1.2 — static class: PassesPerPrCheck(baselineMs, currentMs) / PassesAbsoluteDriftCheck(milestoneMs, currentMs) / Evaluate(baseline, current, milestoneMs) → RegressionResult; implements FR-PO-031 §3.5.2 + §3.5.6. AR-1 M-1 Evaluate reuses helpers; AR-1 M-2 degenerate baseline (≤0/NaN) fails closed; AR-2 M-1 PassesAbsoluteDriftCheck NaN milestone returns true (skip-drift signal) aligning helper with Evaluate; non-NaN ≤0 milestone still fails closed |
| `src/performance-optimization/ReproducibilityResult.cs` | readonly struct: IsReproducible / OriginalP50Ms / RecapturedP50Ms / AbsDeltaFraction / ScenarioMatched / SeedMatched — output of BaselineReproducibilityAuditor.Validate (FR-PO-067) |
| `src/performance-optimization/BaselineReproducibilityAuditor.cs` | v1.1 — static class (AR-1 L-2 sealed→static): Validate(original, recaptured) → ReproducibilityResult; implements §3.4.4 / §5.4 / FR-PO-067 reproducibility check; AR-1 M-3 degenerate origP50 (≤0/NaN) fails closed; Stage 0 carve-out per §3.4.4 (MUST activates at Stage 0+1) |

### `src/testing-strategy/` — Spec #19 (26 files: 24 .cs + 2 asmdef)

> Infrastructure-only assembly (Spec #19). autoReferenced false; references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization. Game-layer assemblies MUST NOT import this assembly at runtime. Scaffold added June 2, 2026; Stage 0 ScenarioRunner (closed-loop scenario harness, §3.3.3) added June 10, 2026.

| File | Purpose |
|------|---------|
| `src/testing-strategy/testing-strategy.asmdef` | Assembly definition; references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization; autoReferenced false |
| `src/testing-strategy/TestingStrategyConstants.cs` | v1.4 — §3.10 governance constant catalogue (pyramid bounds, coverage thresholds, flake windows, pre-commit budget) + SCENARIO_MANIFEST_FORMAT_VERSION (FR-TS-070) + SCENARIO_PATH_CROSS_SPEC_PREFIX (§3.3.5 layout, AR-1 M-4) |
| `src/testing-strategy/TestLayer.cs` | enum: five-layer test taxonomy per §3.1.1 (Unit / Integration / Simulation / Determinism / EndToEndSoak) |
| `src/testing-strategy/TestTier.cs` | enum: Tier A/B/C classification mirror of #16 §1.1.1 (KD-1) |
| `src/testing-strategy/GoldenVectorKind.cs` | enum: discriminates the three golden-vector corpora pinned by #16 §9.5 #4 (a/b/c) |
| `src/testing-strategy/GoldenVectorEntry.cs` | Catalogue entry for one golden-vector corpus (kind + source path + citation) |
| `src/testing-strategy/GoldenVectorResult.cs` | Per-entry result from GoldenVectorRunner; one failed entry blocks FR-DS-009-GATE |
| `src/testing-strategy/GoldenVectorRunner.cs` | Catalogues the #16 §9.5 #4 corpora; per-entry runner surface for the CI determinism gate |
| `src/testing-strategy/DeterminismTierKind.cs` | enum: canonical tier order of #16 §5's regression suite (FR-TS-011 / FR-TS-018) |
| `src/testing-strategy/DeterminismTierResult.cs` | Per-tier outcome from DeterminismGate; failures block merges (KD-2 / FR-TS-012) |
| `src/testing-strategy/DeterminismSuiteResult.cs` | Aggregated DeterminismGate result; carried verbatim by ITestHarness.RunDeterminismTiers (FR-TS-016) |
| `src/testing-strategy/DeterminismGate.cs` | Single integration point invoking #16 §5's regression suite from Spec #19 (FR-TS-016) |
| `src/testing-strategy/PerfGateReport.cs` | PerfGateRunner result: wraps #18 RegressionResult with spec / loop / scenario context |
| `src/testing-strategy/PerfGateRunner.cs` | v1.2 — CI-side wrapper around #18 RegressionGate; rejects mismatched baseline pairs per FR-PO-031 (PR #132 Codex P2) |
| `src/testing-strategy/ScenarioStatus.cs` | enum: Passed / Failed / Quarantined per §3.3.3 (Quarantined is Stage 0+1 flake-layer only) |
| `src/testing-strategy/ScenarioResult.cs` | §3.3.3 result value: status + machine-readable diagnostics + durationMs + #16 §4.8 fingerprint |
| `src/testing-strategy/ScenarioManifest.cs` | In-memory Appendix A.1 manifest entry (name / owning_spec_ids / seed / tier / fixture_refs / format_version) + load-time field validation; Stage 0 in-code authoring (on-disk encoding pinned at Stage 0+1, D1) |
| `src/testing-strategy/ScenarioEnvelope.cs` | Executable expected_outcome_envelope: bodies record bounded predicate outcomes (CheckTrue / CheckEquals / CheckInRange); zero predicates ⇒ Failed (FR-TS-030); NaN fails in_range |
| `src/testing-strategy/ScenarioContext.cs` | Per-invocation body input: manifest + verbatim run seed + KD-7-seeded DeterministicRngService + envelope; declares ScenarioBody delegate |
| `src/testing-strategy/IScenario.cs` | §4.4.1 interface: single method ScenarioResult Run(ulong seed); both sides specified in #19 |
| `src/testing-strategy/ClosedLoopScenario.cs` | Standard IScenario for closed-loop scenarios: fresh RNG + context per run (hermetic, FR-TS-023), body drives a real subsystem loop, envelope evaluated (implicit pass forbidden), exceptions → Failed with diagnostic |
| `src/testing-strategy/ScenarioIndex.cs` | v1.1 — immutable in-memory root manifest; duplicate paths AND duplicate names rejected (AR-1 M-4: A.1 name uniqueness); the runner refuses unindexed scenarios (§3.3.6 / FR-TS-028) |
| `src/testing-strategy/ScenarioIndexEntry.cs` | One index row (path + manifest + scenario); extracted from ScenarioIndex.cs (AR-1 L-4); rejects a ClosedLoopScenario registered under a different manifest instance than it executes (AR-1 M-1) |
| `src/testing-strategy/ScenarioRunner.cs` | v1.1 — §3.3.3 single entry point Run(manifestPath, seed): index resolution + load-time validation (FR-TS-070 format version first, then A.1 fields, §3.3.5 path↔name coherence, cross-spec ≥2 owning-spec arity, non-empty fixture_refs refusal per §3.3.4 — AR-1 M-2/M-4/L-6) → delegates to IScenario.Run; Stage 0 index injected in code, Stage 0+1 adds the index.<ext> file loader (D1) |
| `src/testing-strategy/Tests/testing-strategy-tests.asmdef` | Test assembly definition (EditMode; references testing-strategy + deterministic-sim) |
| `src/testing-strategy/Tests/ScenarioRunnerTests.cs` | v1.1 — 18 ScenarioRunner contract tests: index refusal, format-version rejection, kebab-case validation, implicit-pass rejection, failure diagnostics, NaN in_range, exception + stack capture, seed plumbing (KD-7), per-invocation hermeticity, AR-1 locks (manifest coherence, fixture-refs refusal, newline flattening, duplicate-name / path↔name / cross-spec arity) |

### `tools/` — Stage 0 perf-gate tooling (Spec #18 Appendix E / FR-PO-070)

> Added June 1, 2026. All tools are Stage 0 deliverables per Appendix E. Stage 0+1 upgrades the harness from manual Stopwatch to automated benchmark per §3.3.5.

| File | Purpose |
|------|---------|
| `tools/run-perf-local.sh` | Stage 0 local pre-commit perf-gate runbook (Appendix E / FR-PO-070): runs budget-auditor.py schema + loop-tag passes, then invokes perf-harness/run.sh for anchor baselines; reviewer pastes output into PR description (FR-PO-071) |
| `tools/budget-auditor.py` | §5.3 schema-conformance auditor + §5.5 loop-tag auditor (FR-PO-070): walks every approved spec §6 against Appendix B template; reports missing sections and untagged ms values as ERR-018-NNN candidates; `--mode schema\|loop-tag\|all` |
| `tools/select-seed.py` | KD-6 deterministic seed selector: Stage 0 returns fixed dev seed; Stage 0+1 derives seed from git SHA + scenario ID via SHA-256 (8-byte truncation) |
| `tools/perf-harness/run.sh` | Stage 0 synthetic harness runner: parses scenario manifest, captures Stage 0 Stopwatch stub metrics, writes JSON baseline record under `docs/specs/performance-optimization/baselines/` per Appendix A §A.3 |
| `tools/perf-harness/scenarios/anchor-baseline.manifest.json` | Stage 0 anchor scenario manifest: PERF-ANCHOR-S0-001; validates JSON projection schema end-to-end; no gameplay code exercised |
| `docs/specs/performance-optimization/baselines/.gitkeep` | Stage 0 baseline storage root (Appendix A §A.3): JSON records land at baselines/\<spec-N\>/\<scenario\>-\<seed\>-\<sha8\>.json; migrates to tests/data/baselines/ at first src/ commit (FR-PO-074) |

---

## Tracking Documents

| File | Purpose |
|------|---------|
| `docs/tracking/PROGRESS.md` | Stage progress, milestones, and current status notes |
| `docs/tracking/spec-error-log.md` | Cross-spec error tracking (`ERR-*`) |
| `docs/tracking/spec-error-log-err012-addendum.md` | ERR-012 addendum details |
| `docs/tracking/fix-manifest-pass-mechanics.md` | Pass Mechanics audit/fix closure tracking |
| `docs/tracking/certification-platform.md` | Stage 0 host platform version pin (required before first Spec #16 certification run) |
| `docs/tracking/file-manifest.md` | This manifest |
| `docs/tracking/stress-test-strategy.md` | Tier A/B/C spec stress-test probe strategy (v1.0, May 18, 2026) |
| `docs/tracking/stress-reports/INDEX.md` | Index of all stress-test run reports |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-1.md` | Tier A Run 1 report (May 18, 2026) — 3 FAIL, 2 WARN; all 3 FAILs resolved before Run 2 |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-2.md` | Tier A Run 2 report (May 18, 2026) — 2 FAIL, 1 WARN (×147); FAIL-4/FAIL-5 fixed in this commit; OBS-1 closed |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-3.md` | Tier A Run 3 report (May 18, 2026) — 1 FAIL (FAIL-6: `[EST]` body-text in #12 §3 + §6.1) fixed in this pass; zero open FAILs after run |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-4.md` | Tier A Run 4 report (May 18, 2026) — 1 FAIL (FAIL-7: `ATTACK_DWELL_TICKS [EST]` in #15 §1.4) + FIND-12 (headers) + FIND-13 (checklist evidence); all fixed; A-16 triage inaugurated (10/147) |
| `docs/tracking/stress-reports/2026-05-19-tier-a-run-5.md` | Tier A Run 5 report (May 19, 2026) — A-16 triage full corpus sweep; 167/167 entries, all XC- confirmed, 0 open; 0 new FAILs |
| `tools/spec-stress/reports/a16-triage.json` | A-16 normative-constraint-audit triage state — 167 entries (all XC- confirmed), 0 open; COMPLETE |

---

## Planning Documents

| File |
|------|
| `docs/planning/master-development-plan.md` |
| `docs/planning/master-vol-1-physics-core.md` |
| `docs/planning/master-vol-2-human-systems.md` |
| `docs/planning/master-vol-3-club-operations.md` |
| `docs/planning/master-vol-4-tech-implementation.md` |
| `docs/planning/development-best-practices.md` |

---

## Current Specification Folders

All 20 spec folders now exist in `docs/specs/`. Status reflects authoritative classification in `SPEC_INDEX.md`. Folders marked NOT STARTED contain header-only scaffolding (outline + section-1…9 + appendices skeletons with no body content).

| # | Folder | Status |
|---|--------|--------|
| 1 | `docs/specs/ball-physics/` | APPROVED |
| 2 | `docs/specs/agent-movement/` | APPROVED |
| 3 | `docs/specs/collision-system/` | APPROVED |
| 4 | `docs/specs/first-touch/` | APPROVED |
| 5 | `docs/specs/pass-mechanics/` | APPROVED (re-approved May 6, 2026) |
| 6 | `docs/specs/shot-mechanics/` | APPROVED |
| 7 | `docs/specs/perception-system/` | APPROVED |
| 8 | `docs/specs/decision-tree/` | APPROVED (draft-level) |
| 9 | `docs/specs/fixed64-math/` | APPROVED (May 15, 2026) — §9 v1.0 lead-developer sign-off; §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented comparator-glossary deferral (CLAUDE.md "Interface Design Principle"). §9.8 implementation-time deliverables (golden-vector corpus, CI bench, harness digest, owning-team ledger) remain post-APPROVED follow-ups. Implementation deferred to Stage 5 per §8.1 v0.2. |
| 10 | `docs/specs/heading-mechanics/` | APPROVED May 16, 2026 (v0.3; section files 1–9 + appendices + outline + outline-PASS-1 + section-files-PASS-1-adversarial-review; ERR-010-001 RESOLVED) |
| 11 | `docs/specs/goalkeeper-mechanics/` | APPROVED (May 18, 2026) — section files v0.2; PASS-1 adversarial review (11 findings: 3 H / 5 M / 3 L) resolved same day; ERR-011-001 CLOSED (`DOMAIN_TAG_GOALKEEPER = 0x1D [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); GK constants `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` / `GK_LATERAL_FACTOR` promoted `[EST]` → `[GT]`; lead-developer R-01..R-05 signed. 44 FRs; ~79 constants; 4 RNG draw sites. |
| 12 | `docs/specs/positioning-ai/` | APPROVED (May 18, 2026) — section files v0.3 (FAIL-4 fix pass); PASS-1 adversarial review (21 findings: 7 H / 9 M / 5 L) filed and resolved in v0.2 (May 16); ERR-012-001 CLOSED (`DOMAIN_TAG_POSITIONING_AI = 0x17 [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); Appendix A.1–A.8 derivations confirmed; GK constants promoted `[EST]` → `[GT]`; lead-developer R-01..R-05 signed. 47 active FRs; 18 `[GT]` + 7 other constants; no `[EST]` remain. |
| 13 | `docs/specs/pressing-ai/` | APPROVED (May 17, 2026) — v0.3. All gate items resolved: ERR-013-001 Option B (`TacticalContext.PressDirective?` nullable field added to DT #8 §2.2.6); ERR-013-004 (`"Fatigue System #13"` → `"Pressing AI #13"` in DT #8 §3.1.8.1); ERR-013-005 (`DOMAIN_TAG_PRESSING_AI = 0x19 [CROSS]` in #16 §3.4 v1.0.3); ERR-013-007 / ERR-013-008 (`GetPhase` / `GetLine` Stage 1 accessor declarations in #12 §4.5.1); Appendix A derivations for TRIGGER_DWELL_TICKS / TRIGGER_RELEASE_TICKS / ROLE_DWELL_TICKS / INTERCEPT_LOOKAHEAD_TICKS all `[EST]` → `[GT]`; §4.4.3/§4.4.4/§4.5.2/§4.6 updated (Option B mechanism); §1.6 boundary table `[CROSS-PENDING]` → `[CROSS]`; T-C-/T-X- test-prefix table added to #19 §3.1.4; lead-developer R-01..R-05 signed 2026-05-17. OI-002 (Stage 1 channel-registry rows) open non-blocking per §9.6. |
| 14 | `docs/specs/defensive-ai/` | APPROVED (May 18, 2026) — section files v0.4 (FAIL-4 fix pass); PASS-1 adversarial review (17 findings: 6 H / 7 M / 4 L) filed and all resolved in v0.2 fix pass same day; ERR-014-001 CLOSED (`TacticalContext.MarkDirective?` nullable field added to #8 §2.2.6 v1.1.3); ERR-014-004 CLOSED (`DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); lead-developer R-01..R-05 signed. 37 FRs; 26 constants (22 `[GT]` + 4 `[CROSS]`); ≥85 tests; ≤0.12 ms per-tick budget. ERR-014-002/003 Stage 1 non-blocking open. |
| 15 | `docs/specs/attacking-ai/` | APPROVED (May 18, 2026) — section files v0.2 (section-1.md through section-9-approval-checklist.md + appendices.md); PASS-1 adversarial review (7 findings: 1 H / 5 M / 1 L) filed and all resolved in v0.2 fix pass; lead-developer R-01..R-05 signed; ERR-015-001 CLOSED (DOMAIN_TAG_ATTACKING_AI = 0x1B allocated in #16 §3.4 v1.0.4); ERR-015-002 CLOSED (AttackIntent[]? added to #8 §2.2.6 v1.1.2; §3.1.7 RUNNER override note added); ERR-015-005 CLOSED ("Attacking AI (#15)" added to #8 §1.3.2 v1.1.2); ERR-015-003/004 Stage 1 non-blocking open. 36 FRs; 38 constants (33 [GT] + 4 [CROSS] + 1 [DERIVED]); 85 tests. |
| 16 | `docs/specs/deterministic-sim/` | APPROVED (May 14, 2026, later same day) — Tier 2 Final Approval. §9 v1.7. All §9.4.2 gates cleared: §9.5 #4(a)/(b)/(c) spec-level sub-conditions SATISFIED (golden-vector files `hkdf-sha256-kat.md` v1.1, `siphash-2-4-kat.md` v1.1, `serialize-canonical-corpus.md` v1.0); §8.3.1 cross-spec re-audit COMPLETE (§8 v1.2, all four upstream rows promoted to `complete`); §9.3 sign-offs (lead-developer Tier 2, QA-automation, platform-certification) granted, platform-certification with explicit Stage-0 host-platform-pin caveat. ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocation in §3.4 v1.0.1 (no `DETERMINISM_DIGEST_VERSION` bump). |
| 17 | `docs/specs/event-system/` | APPROVED (May 13, 2026) — 10 section files + appendices; section-files PASS 1 + PASS 2 adversarial review applied; lead-developer sign-off complete |
| 18 | `docs/specs/performance-optimization/` | APPROVED (May 15, 2026, later same day still) — §9 v1.0 lead-developer sign-off. v1.0 `[TBD-NORMATIVE]` sweep landed across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices (60 citation-qualifier instances resolved against #16 APPROVED text + #19 v1.0.1 patch-revised text). §9.4.1 blocker list all RESOLVED. KD-2 sequencing satisfied. FR count 82 (FR-PO-019 split into 019 MAY + 019a MUST; FR-PO-058a emission constraints). KD-3 inverted: #18 owns trace pipeline; #16 retains §3.2.4.1 / §3.1 / §5. |
| 19 | `docs/specs/testing-strategy/` | APPROVED (May 15, 2026, later same day) — §9 v1.0 lead-developer sign-off; v1.0.1 `[TBD-NORMATIVE]` sweep landed across §1 / §3 / §4 / §5 / §6 / §8 / appendices against #16's APPROVED text and #18's IN REVIEW v0.3 surface (51 citation-qualifier instances resolved); KD-2 sequencing satisfied. Clears #18's last KD-2 gate. |
| 20 | `docs/specs/code-standards/` | APPROVED (May 11, 2026) — 10 section files + appendices; adversarial review pass-1 applied; lead-developer R-01..R-05 sign-off complete |

**Notes:**
- Attacking AI (#15) files (May 17–18, 2026): `outline.md` (high-level v1.0), `outline-detailed.md` (v1.1), `adversarial-review-outline-detailed-v1.md`, `section-1.md` through `section-9-approval-checklist.md` + `appendices.md` (all at v0.2). `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS: #16 §3.4]` (ERR-015-001 CLOSED May 18, 2026). Lead-developer R-01..R-05 signed May 18, 2026. Status: APPROVED.
- Deterministic Simulation (#16) files: `outline.md`, `section-1.md` through `section-9-approval-checklist.md`, `appendices.md`, `critique-log.md` (consolidated review history; supersedes the former `adversarial-review.md` and `third-pass-fix-log.md`, both removed May 3, 2026).
- Fixed64 Math (#9) files include `adversarial-review.md` and `adversarial-critique-pass-2.md` alongside the standard section files.
- Code Standards (#20) outline-tier files: `outline.md` (high-level v1.0), `outline-mid.md` (mid-level v1.3), `outline-detailed.md` (detailed v1.3). Section files authored from the detailed outline May 7–8, 2026; adversarial review pass-1 applied May 11, 2026; lead-developer R-01..R-05 sign-off completed May 11, 2026. Current set: `section-1.md` v1.0.1, `section-2.md` v1.0.1, `section-3.md` v1.0.1, `section-4.md` v1.0, `section-5.md` v1.0.1, `section-6.md` v1.0.1, `section-7.md` v1.0.1, `section-8.md` v1.0, `section-9-approval-checklist.md` v1.1 (APPROVED), `appendices.md` v1.1. SPEC_INDEX.md line 40 reflects APPROVED status.
- Testing Strategy (#19) files: `outline.md` (high-level v1.0 + first adversarial review), `outline-detailed.md` (v1.1 — second adversarial review applied), section-1 through section-9-approval-checklist, and `appendices.md`. Initial section-file draft authored May 12, 2026 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied same day (3 H / 6 M / 8 L findings, all resolved); status `IN REVIEW`. v0.2 corrected #16 section-number citations against current `deterministic-sim/` text (§7 → §5 regression suite, §1.3.1 → §1.1.1 tier classification, §5 → §3.2.4.1 canonical schema, deleted §8 "trace channels" — no such section). Per KD-2 sequencing in the spec, advancement to `APPROVED` is gated on (a) Spec #16 reaching Tier 2 `APPROVED`, (b) Spec #18 having at least an outline-level draft, and (c) all `TBD-NORMATIVE` tags resolved.
- Performance Optimization Strategy (#18) files: `outline.md` (high-level v1.0 with embedded adversarial review of May 6, 2026), `outline-detailed.md` (v1.1 — May 13, 2026), `section-1.md` through `section-9-approval-checklist.md` + `appendices.md`, `pass-2-adversarial-review.md`. Section files authored May 13, 2026 (v0.1) from `outline-detailed.md` v1.1; PASS-1 adversarial review (4 H / 6 M / 13 L findings, 23 total) resolved in v0.2 (May 14, 2026). PASS-2 adversarial review (2 H / 5 M / 8 L findings, 15 total) filed May 14, 2026 against v0.2; resolved in v0.3 fix pass same day. ERR-018-002..018 all resolved. PASS-2 H-1 / H-2 / M-1 traced to PR #59 + PR #60 parallel-branch merge collision on the v0.2 fix pass. Status `IN REVIEW`; lead-developer sign-off pending. KD-3 inverted: #18 owns trace pipeline; #16 retains §3.2.4.1/§5/§3.1.2. `TBD-NORMATIVE` tags throughout for #16 (IN PROGRESS) and #19 (IN REVIEW) citations. FR count is 82 (FR-PO-001 … 080 + FR-PO-019a + FR-PO-058a — FR-PO-019a split from FR-PO-019 per ERR-018-017).
- Event System (#17) files: `outline.md` (high-level v1.0), `outline-detailed.md` (v1.1), `section-files-critique-pass-1.md`, `section-files-critique-pass-2.md`, `section-1.md` through `section-8.md`, `section-9-approval-checklist.md`, and `appendices.md`. All section files authored May 13, 2026 from `outline-detailed.md` v1.1. Section-files PASS 1 adversarial critique (3 H / 5 M / 12 L findings) resolved in v0.2; section-files PASS 2 adversarial critique (2 H / 6 M / 7 L findings) resolved in v0.3. All section files at v0.3; lead-developer sign-off granted May 13, 2026. ERR-017-001 FULLY RESOLVED — #16-side May 14, 2026 (`DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 v1.0.1); #17-side May 15, 2026 (§1.0.1 patch revision across §1 / §2 / §3 / §7 / §8 / §9 / appendices — `[CROSS-PENDING]` → `[CROSS]` promotion, literal value `0x15` inlined).

---

## Naming Convention (Current)

- Spec files are stored inside per-spec folders.
- Filenames do **not** carry version suffixes.
- Git history is the version record.

Examples:
- `section-1.md`
- `section-3-1-to-3-2.md`
- `section-9-approval-checklist.md`
- `appendix-a.md`
- `audit-report.md`

---

## Maintenance Rule

Update this manifest when:
- a new spec folder is added,
- a tracking/planning file is added, removed, or renamed,
- project-level documentation paths change.
