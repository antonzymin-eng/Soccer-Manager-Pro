# File Manifest (Post-Migration Baseline)

**Created:** April 30, 2026  
**Last Updated:** May 28, 2026 (Goalkeeper Mechanics #11 source files added — 36 files, AR-1/AR-2 clean; Heading Mechanics #10 source files added — 25 files; `src/CLAUDE.md` v1.14)  
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

> **Structural deviation note:** Ball Physics code lives at `src/Core/Physics/Ball/` rather than the spec-canonical `src/ball-physics/`. This is a known deviation to be corrected in a dedicated fix pass. All other spec folders will follow the canonical `src/<spec-folder-name>/` pattern.

| File | Purpose |
|------|---------|
| `src/CLAUDE.md` | Coding guide: C# naming, constant catalogues, Unity project structure, build/test commands. Created May 19, 2026 when coding began. At v1.9 as of May 25, 2026. |

### Spec #1 — Ball Physics (`src/Core/Physics/Ball/`)

| File | Purpose |
|------|---------|
| `src/Core/Physics/Ball/BallPhysicsConstants.cs` | `[FIXED]` / `[GT]` / `[DERIVED]` / `[CROSS]` constant catalogue for Ball Physics |
| `src/Core/Physics/Ball/BallState.cs` | Mutable value-type game state for the ball (position, velocity, spin, ground contact) |
| `src/Core/Physics/Ball/BallPhysicsCore.cs` | Core physics calculations: gravity, drag, Magnus effect |
| `src/Core/Physics/Ball/BallStateMachine.cs` | State machine: CONTROLLED ↔ AIRBORNE ↔ ROLLING transitions |
| `src/Core/Physics/Ball/BallGroundInteraction.cs` | Ground friction and rolling dynamics |
| `src/Core/Physics/Ball/BallCollision.cs` | Ball-specific collision response (detection geometry lives in `collision-system/`) |
| `src/Core/Physics/Ball/BallEventLogger.cs` | Event/logging infrastructure for ball physics events |
| `src/Core/Physics/Ball/SurfaceProperties.cs` | Surface-specific physics parameters (grass, artificial turf, etc.) |
| `src/Core/Physics/Ball/Tests/BallPhysicsCoreTests.cs` | Unit tests for core physics calculations |
| `src/Core/Physics/Ball/Tests/BallIntegrationTests.cs` | Integration tests for full ball physics pipeline |
| `src/Core/Physics/Ball/Tests/BallStateMachineTests.cs` | Unit tests for ball state machine transitions |

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

| `src/agent-movement/Tests/AgentMovementTests.cs` | Unit tests for agent movement system (added May 27, 2026 in AR-2/AR-3 pass) |
| `src/agent-movement/Tests/agent-movement-tests.asmdef` | Test assembly definition (EditMode; references agent-movement.asmdef) |

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
| `src/pass-mechanics/PassAttemptEvent.cs` | Struct event published when a pass is initiated |
| `src/pass-mechanics/PassCancelledEvent.cs` | Struct event published when a pass is cancelled |
| `src/pass-mechanics/PassEvents.cs` | All pass-related event type definitions |
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
| `src/pass-mechanics/EventBusStub.cs` | Stub for event bus integration (pending Event System #17 wiring at Stage 1) |

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
| `src/shot-mechanics/ShotExecutedEvent.cs` | Struct event published at CONTACT completion after Ball.ApplyKick() |
| `src/shot-mechanics/ShotCancelledEvent.cs` | Struct event published when a tackle interrupt fires during WINDUP |
| `src/shot-mechanics/ShotAnimationData.cs` | Struct event stub for Animation System (unconsumed at Stage 0) |
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
| `src/shot-mechanics/EventBusStub.cs` | Stage 0 no-op event bus; replace at Stage 1 with Event System #17 |
| `src/shot-mechanics/ShotExecutor.cs` | Sealed orchestrator: 5-state machine (Idle→Windup→Contact→FollowThrough→Complete) |
| `src/shot-mechanics/Tests/NaNVelocityStub.cs` | #if UNITY_EDITOR\|\|DEVELOPMENT_BUILD; returns float.NaN for EC-008 FM-05 recovery test |

### Spec #10 — Heading Mechanics (`src/heading-mechanics/`)

| File | Purpose |
|------|---------|
| `src/heading-mechanics/heading-mechanics.asmdef` | Assembly definition (EditMode tests; references agent-movement, ball-physics, collision-system) |
| `src/heading-mechanics/HeadingMechanicsConstants.cs` | All GT/Fixed/Cross/Derived constants (§3.1); region order Fixed→Derived→Cross→GT |
| `src/heading-mechanics/ContactQualityLabel.cs` | Enum: Early / OnTime / Late — telemetry only; KD-2 |
| `src/heading-mechanics/MistimedDirection.cs` | Enum: None / Early / Late — eligibility output |
| `src/heading-mechanics/FailureCause.cs` | Enum: MistimedEarly / MistimedLate / PositionedPoorly / DisturbedInDuel |
| `src/heading-mechanics/SetPieceContext.cs` | Enum: OpenPlay / Corner / FreeKick — telemetry only |
| `src/heading-mechanics/HeadingAgentAttributes.cs` | Struct: Heading/Strength/Balance [1-20], Fatigue [0,1], TeamId |
| `src/heading-mechanics/HeaderIntent.cs` | Struct: PowerIntent/ContactPointIntent/TargetIntent/AttemptCommittedTick/SetPieceContext (locked at commit; KD-17) |
| `src/heading-mechanics/HeaderContactState.cs` | Struct: per-attempt mutable state (JumpStartFrame, quality, disturbance, etc.) |
| `src/heading-mechanics/EligibilityResult.cs` | Struct: IsEligible, PredictedContactFrame, IdealContactFrame, MistimedDirection |
| `src/heading-mechanics/HeaderExecutedEvent.cs` | Struct: published on successful contact (Tier B event) |
| `src/heading-mechanics/HeaderAttemptFailedEvent.cs` | Struct: published on failure (Tier C event; no ball-state modification) |
| `src/heading-mechanics/ContestedDuelContext.cs` | Struct: DuelId, ParticipantCount, WinnerAgentId, BufferStartIndex |
| `src/heading-mechanics/IHeadingBallSystem.cs` | Interface: GetBallState + ApplyKick |
| `src/heading-mechanics/IHeadingRngService.cs` | Interface: NextFloat + NextGaussian |
| `src/heading-mechanics/HeadingRngServiceStub.cs` | Stage 0 SplitMix64 stub; replace at Stage 1 with #16 wiring |
| `src/heading-mechanics/EventBusStub.cs` | Stage 0 no-op event bus; replace at Stage 1 with Event System #17 |
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
| `src/goalkeeper-mechanics/goalkeeper-mechanics.asmdef` | Assembly definition (references agent-movement, ball-physics, collision-system) |
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
| `src/goalkeeper-mechanics/SaveAttemptedEvent.cs` | Struct event: published on every save attempt (success or failure); includes telemetry labels |
| `src/goalkeeper-mechanics/BallClaimedEvent.cs` | Struct event: published on Caught save; includes releaseTickEarliest (6-second rule) |
| `src/goalkeeper-mechanics/DistributionExecutedEvent.cs` | Struct event: published when distribution passIntent is emitted to Pass Mechanics #5 |
| `src/goalkeeper-mechanics/GoalkeeperRushEvent.cs` | Struct event: published on rush launch, update, and abort |
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
| `src/goalkeeper-mechanics/EventBusStub.cs` | Stage 0 no-op event bus; replace at Stage 1 with Event System #17 |
| `src/goalkeeper-mechanics/GoalkeeperMechanics.cs` | Main 10 Hz + 60 Hz orchestrator: state machine, dive kinematics, handling quality, cross-claim duels, rush, distribution; constructor-injected |

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
