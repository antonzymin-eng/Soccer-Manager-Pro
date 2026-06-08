# src/CLAUDE.md — Tactical Director Coding Guide

> **Created:** May 19, 2026
> **Last Updated:** June 8, 2026 (v1.50 — cross-spec routing close-out: `Possession.ControlHeight` ↔ `GroundControlHeight` resolved. Ball Physics #1 §3.1.11 `BallPhysicsConstants.Possession.ControlHeight` declared the authority; `FirstTouchConstants.GroundControlHeight` relocated from `#region GT` to `#region Cross`, retagged `[CROSS]`, RHS now `BallPhysicsConstants.Possession.ControlHeight` (single-consumer mirror per Spec #20 §4.2). Ball Physics XML drift warning replaced with an authority/consumer pointer. Sibling-hazard sweep cleared — no other spec re-declares `ControlRadius` / `ControlVelocity` / `ChallengeRadius`. Files: FirstTouchConstants.cs v1.7, BallPhysicsConstants.cs v1.7, docs/specs/first-touch/section-4-4-to-4-6.md §3.4.3, root CLAUDE.md OPEN ISSUES. Prior v1.49 (AR-hardening sweep complete. New AR rounds since v1.45: Ball Physics #1 AR-6 (4L); Collision System #3 AR-1..AR-6 (final 3L); First Touch #4 AR-5..AR-6 (final 3L); Pass Mechanics #5 AR-2..AR-5 (final 1M+3L cycle-stop — `ComputeErrorDirection` agentId now multiplied by xxHash64 prime 0xC2B2AE3D27D4EB4F before seed addition for better hash quality); Shot Mechanics #6 AR-2..AR-4 (final 3L); Event System #17 AR-8 (2M+1L), AR-9 (1M+1L), AR-10 (1M+2L), AR-11 (no findings — whole-surface sweep clean); Agent Movement #2 AR-10 (1M+2L — `SanitiseCollisionForce` maps NaN/±Inf → 0 before Clamp01 since Unity's Clamp01 does not filter NaN), AR-11 (2L — EvaluateState defaults dropped parallel to AR-9 L-2; OscillationGuard.WriteTime default-arm assert); Performance Optimization #18 AR-3 + AR-3 full-surface (3M+5L / 3M+3L) + AR-4 (2L). v1.48 prior: Performance Optimization #18 RegressionGate.Evaluate now reports skipped-drift (NaN) vs degenerate-milestone (+Infinity) distinctly in MilestoneDriftFraction. v1.47 prior: PO #18 AR-3 full-surface (M-1 RECORD_FORMAT_VERSION_STAGE0_PENDING sentinel constant). v1.46 prior: PO #18 AR-3 (M-1 PerformanceOptimizationConstants RECORD_FORMAT_VERSION_STAGE0_PENDING). — Prior v1.45 (Agent Movement #2 pure-function unit coverage expansion. New `src/agent-movement/Tests/AgentMovementUnitTests.cs` v1.0 adds 59 NUnit tests across 7 `[TestFixture]` classes.) — Prior v1.44 (Agent Movement #2 regression-test roster initiated.) — Prior v1.43 (Agent Movement #2 AR-9 fix pass.) — Prior v1.42 (AR-8 fix pass.) — Prior v1.41 (AR-7 fix pass.) — Prior v1.40 (AR-5+AR-6 fix pass.) — Prior v1.39 (Ball Physics #1 AR-6 fix pass.) — Prior v1.38 (Ball Physics #1 AR-5 fix pass.) — Prior v1.37 (Ball Physics #1 AR-4 fix pass.) — Prior v1.36 (Ball Physics #1 AR-3 fix pass.) — Prior v1.35 (Ball Physics #1 AR-2 fix pass.) — Prior v1.34 (Ball Physics #1 AR-1 fix pass.) — Prior v1.33 (Testing Strategy #19 PR #132 Codex P2 follow-up.) — Prior v1.32 (Testing Strategy #19 AR-5 fix pass.) — Prior v1.27..v1.31 (Testing Strategy #19 scaffold + AR-1..AR-4.) — Prior v1.25..v1.26 (Performance Optimization #18 AR-1..AR-2.))
> **Purpose:** Concrete coding rules for any AI agent or developer writing C# source code in this project. Covers file naming, constant catalogues, Unity project structure, and build/test commands. Cites Spec #20 (Code Standards & Style Guide) as the source for every convention here. Read the root `CLAUDE.md` first — this file supplements it, not replaces it.

---

## BEFORE YOU WRITE ANY CODE

1. Read the root `CLAUDE.md` completely.
2. Read Spec #20 (`docs/specs/code-standards/`) for the full rule set with rationale.
3. Read the `§4` (Architecture) file of the spec you are implementing.
4. Check `docs/specs/SPEC_INDEX.md` to confirm the spec's status is `APPROVED`.

---

## UNITY PROJECT STRUCTURE

```
src/
├── CLAUDE.md                          ← You are here
│
├── project-constants/
│   ├── project-constants.asmdef       ← one assembly per folder (FR-CS-055)
│   └── ProjectConstants.cs            ← source-of-truth for constants consumed by more than one spec assembly (Spec #20 §4.2)
│
├── ball-physics/                      ← Spec #1
│   ├── ball-physics.asmdef            ← references TacticalDirector.DeterministicSim; autoReferenced true
│   ├── BallPhysicsConstants.cs
│   ├── BallState.cs
│   ├── BallPhysicsCore.cs
│   ├── BallStateMachine.cs
│   ├── BallGroundInteraction.cs
│   ├── BallCollision.cs               ← ball-specific collision response; detection geometry lives in collision-system/
│   ├── BallEventLogger.cs
│   ├── SurfaceProperties.cs
│   ├── BodyPart.cs                    ← enum: Foot / Shin / Thigh / Torso / Head / Arm
│   ├── RestartType.cs                 ← enum: None / ThrowIn / GoalKick / Corner / KickOff
│   ├── KickResult.cs                  ← enum: Applied / RejectedNonFiniteVelocity (ApplyKick contract)
│   ├── BodyPartCoefficients.cs        ← static class: per-body-part (speedRetention, spinRetention) lookup
│   └── tests/
│       ├── ball-physics-tests.asmdef  ← EditMode; references ball-physics.asmdef
│       ├── BallPhysicsCoreTests.cs
│       ├── BallIntegrationTests.cs
│       ├── BallStateMachineTests.cs
│       ├── BodyPartCoefficientsTests.cs ← AR-4 L-2 throw-on-unknown + catalogue round-trip
│       ├── SurfacePropertiesTests.cs    ← AR-4 L-2 throw-on-unknown + catalogue round-trip (4 Get* methods)
│       └── EnumOrdinalStabilityTests.cs ← AR-6 L-3 locks int ordinals for all 6 public enums
│
├── agent-movement/                    ← Spec #2
│   ├── agent-movement.asmdef
│   ├── AssemblyInfo.cs                ← [InternalsVisibleTo("TacticalDirector.AgentMovement.Tests")]
│   ├── AgentMovementConstants.cs      ← constants: MovementThresholds / FatigueRates /
│   │                                  │   LocomotionConstants / DirectionalConstants /
│   │                                  │   TurnConstants / OscillationGuardConstants /
│   │                                  │   SafetyConstants / PlayerAttributeConstants
│   ├── AgentMovementState.cs          ← enum: AgentMovementState (7 locomotion states)
│   ├── GroundedReason.cs              ← enum: GroundedReason (NONE / COLLISION / SLIDING_TACKLE / DIVING_HEADER)
│   ├── FacingMode.cs                  ← enum: FacingMode (AUTO_ALIGN / TARGET_LOCK)
│   ├── DecelerationMode.cs            ← enum: DecelerationMode (CONTROLLED / EMERGENCY)
│   ├── AgentState.cs                  ← mutable value-type game state (ref-mutated, not readonly)
│   ├── PlayerAttributes.cs
│   ├── PerformanceContext.cs
│   ├── MovementCommand.cs             ← public AI factories + internal ToolingOverrideOnly_NaNInjection (T-AM-030..032)
│   ├── AgentMovementSystem.cs         ← 12-step 60 Hz pipeline orchestrator
│   ├── AgentStateMachine.cs           ← pure state evaluator (no side effects)
│   ├── OscillationGuard.cs            ← ring-buffer anti-oscillation guard
│   ├── AgentLocomotion.cs             ← acceleration / deceleration formulas
│   ├── AgentTurning.cs                ← turn rate / lean angle / stumble probability
│   ├── AgentDirectionalMovement.cs    ← directional multipliers / facing update
│   ├── AgentSafetySystem.cs           ← NaN detection / speed clamp / pitch boundary
│   └── Tests/
│       ├── agent-movement-tests.asmdef  ← EditMode; references agent-movement.asmdef
│       ├── AgentMovementTests.cs        ← T-AM-001..018 / T-AM-030..033 / T-AM-040..043 regression roster
│       └── AgentMovementUnitTests.cs    ← T-AM-007..009 / T-AM-019..023 / T-AM-034..039 /
│                                       │   T-AM-044..047 / T-AM-050..052 / T-AM-070..107
│                                       │   pure-function coverage (test-plan.md v0.2)
│
├── collision-system/                  ← Spec #3
│   ├── collision-system.asmdef
│   ├── CollisionSystemConstants.cs
│   ├── CollisionDetection.cs          ← broad-phase and narrow-phase detection
│   ├── SpatialHashGrid.cs             ← spatial hash grid for broad-phase queries
│   ├── CollisionManifold.cs           ← contact manifold (depth, normal, point)
│   ├── CollisionEvent.cs              ← struct event published on confirmed collision
│   ├── CollisionResponse.cs           ← impulse-based response calculations
│   ├── CollisionSystem.cs             ← 60 Hz pipeline orchestrator
│   ├── CollisionPairBitfield.cs       ← processed-pair tracking (no double-processing)
│   ├── AgentAgentCollisionResult.cs   ← result struct for agent–agent resolution
│   ├── AgentBallCollisionData.cs      ← data struct for agent–ball contact
│   ├── AgentPhysicalProperties.cs     ← mass, radius, restitution per agent
│   ├── BallCollisionHandler.cs        ← ball-specific collision response
│   ├── ContactForceData.cs            ← contact force magnitude and direction
│   ├── ContactType.cs                 ← enum: SLIDE / SHOULDER / BLOCK / FOUL
│   ├── ContactTypeClassifier.cs       ← classifies manifold into ContactType
│   ├── CollisionType.cs               ← enum: AGENT_AGENT / AGENT_BALL / AGENT_POST / AGENT_BOUNDARY
│   ├── DeterministicRNG.cs            ← SplitMix64 wrapper for foul/stumble rolls
│   ├── ICollisionEventConsumer.cs     ← interface for systems consuming collision events (Spec #3 §3.4.2)
│   └── tests/
│       └── collision-system-tests.asmdef  ← EditMode; references collision-system.asmdef
│
├── first-touch/                       ← Spec #4
│   ├── FirstTouchConstants.cs
│   ├── FirstTouchContext.cs           ← input: incoming ball state, agent state, env
│   ├── FirstTouchResult.cs            ← output: displaced ball state + possession outcome
│   ├── TouchResult.cs                 ← intermediate touch quality evaluation result
│   ├── FirstTouchSystem.cs            ← main orchestrator
│   ├── BallDisplacementProcessor.cs   ← post-touch ball displacement vector
│   ├── ControlQualityCalculator.cs    ← control quality score (0–1) from attributes + context
│   ├── OrientationDetector.cs         ← body orientation relative to incoming ball
│   ├── PossessionStateMachine.cs      ← LOOSE → CONTROLLED → POSSESSED transitions
│   ├── PressureEvaluator.cs           ← nearby-defender pressure scalar
│   ├── PressureResult.cs              ← pressure evaluation output
│   ├── TouchRadiusCalculator.cs       ← acceptance radius for touch attempt
│   ├── IAgentMovementSystem.cs        ← read-only query boundary to Agent Movement (#2)
│   ├── IBallPhysicsSystem.cs          ← read-only query boundary to Ball Physics (#1)
│   └── IFirstTouchSystem.cs           ← public interface for First Touch consumers
│
├── pass-mechanics/                    ← Spec #5
│   ├── PassMechanicsConstants.cs
│   ├── PassRequest.cs                 ← input: passer, target, requested pass type
│   ├── PassResult.cs                  ← output: ball velocity applied + outcome
│   ├── PassAttemptEvent.cs            ← struct event on pass initiation
│   ├── PassCancelledEvent.cs          ← struct event on pass cancellation
│   ├── CancelReason.cs                ← enum: TackleInterrupt (reason pass was cancelled)
│   ├── PassExecutor.cs                ← main orchestrator: full pass pipeline
│   ├── PassVelocityCalculator.cs      ← launch velocity from profile + attributes
│   ├── PassErrorCalculator.cs         ← direction / speed deviation error model
│   ├── PassTargetResolver.cs          ← resolves intended target position
│   ├── PassTypeProfiles.cs            ← PhysicalProfile factory per PassType
│   ├── PhysicalProfile.cs             ← struct: speed range, spin, launch angle per type
│   ├── PassType.cs                    ← enum: Ground / Driven / Lofted / ThroughBall / AerialThrough / Cross / Chip
│   ├── CrossSubType.cs                ← enum: Flat / Whipped / High
│   ├── SpinType.cs                    ← enum: Topspin / Backspin / Sidespin / Mixed
│   ├── PassOutcome.cs                 ← enum: Initiated / Completed / Cancelled / Invalid
│   ├── PassAgentAttributes.cs         ← agent skill attributes consumed by pass system
│   ├── PassAgentState.cs              ← agent state consumed by pass system
│   ├── IPassAgentQuery.cs             ← interface: query agent attributes and state
│   ├── IPassBallSystem.cs             ← interface: apply kick velocity to ball
│   ├── IPassCollisionQuery.cs         ← interface: interception queries into Collision System
│   ├── EventBusStub.cs                ← wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads
│   └── EventBusRegistrar.cs           ← boot-time RegisterExternalRow<T>() for PassAttemptEvent (0x0C) + PassCancelledEvent (0x0D)
├── shot-mechanics/                    ← Spec #6
│   ├── ShotMechanicsConstants.cs      ← all GT/Fixed/Cross constants (velocity, angle, spin, error, body, weak-foot, timing)
│   ├── ContactZone.cs                 ← enum: Centre / BelowCentre / OffCentre
│   ├── ShotOutcome.cs                 ← enum: Completed / Cancelled / Invalid / Initiated
│   ├── ShotCancelReason.cs            ← enum: TackleInterrupt
│   ├── ShotRequest.cs                 ← input struct from Decision Tree to ShotExecutor
│   ├── ShotResult.cs                  ← output struct returned by ShotExecutor
│   ├── ShotAgentAttributes.cs         ← agent attribute snapshot (Finishing, LongShots, etc.)
│   ├── ShotAgentState.cs              ← agent physical state snapshot (position, velocity, etc.)
│   ├── ShotExecutedEvent.cs           ← struct event published at CONTACT completion
│   ├── ShotCancelledEvent.cs          ← struct event published on WINDUP tackle interrupt
│   ├── ShotAnimationData.cs           ← struct event stub for animation system (Stage 1+)
│   ├── BodyMechanicsResult.cs         ← output struct from BodyMechanicsEvaluator
│   ├── GoalGeometry.cs                ← value struct: goal width/height/line/posts/crossbar
│   ├── IShotVelocityCalculator.cs     ← interface for EC-008 NaN injection seam only
│   ├── IShotBallSystem.cs             ← interface: possession check + ApplyKick
│   ├── IShotAgentQuery.cs             ← interface: read agent attributes and state
│   ├── IShotCollisionQuery.cs         ← interface: tackle flag poll + pressure scalar
│   ├── GoalGeometryProvider.cs        ← static goal geometry access + test override seam (SP-009)
│   ├── ShotVelocityCalculator.cs      ← §3.2 velocity formula; singleton; implements IShotVelocityCalculator
│   ├── ShotLaunchAngleCalculator.cs   ← §3.3 launch angle formula; pure static
│   ├── ShotSpinCalculator.cs          ← §3.4 spin vector assembly; pure static
│   ├── ShotPlacementResolver.cs       ← §3.5 goal-relative placement → world-space aim direction
│   ├── BodyMechanicsEvaluator.cs      ← §3.7 body mechanics score (run-up, plant, velocity, lean)
│   ├── WeakFootPenaltyApplier.cs      ← §3.8 weak-foot error/velocity multipliers
│   ├── ShotErrorCalculator.cs         ← §3.6 deterministic angular error (magnitude + direction)
│   ├── ShotEventEmitter.cs            ← publishes ShotExecutedEvent / ShotCancelledEvent / ShotAnimationData
│   ├── EventBusStub.cs                ← wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads
│   ├── EventBusRegistrar.cs           ← boot-time RegisterExternalRow<T>() for ShotExecutedEvent (0x01) + ShotCancelledEvent (0x0E) + ShotAnimationData (0x0F)
│   ├── ShotExecutor.cs                ← sealed orchestrator: 5-state machine (Idle/Windup/Contact/FollowThrough/Complete)
│   └── Tests/
│       └── NaNVelocityStub.cs         ← #if UNITY_EDITOR||DEVELOPMENT_BUILD; returns NaN for EC-008 FM-05 test
├── perception-system/                 ← Spec #7
│   ├── perception-system.asmdef
│   ├── PerceptionConstants.cs         ← all GT/Fixed/Derived/Cross constants (§3.10); 18 spec constants + sizing constants
│   ├── PerceptionAgentAttributes.cs   ← struct: Decisions/Anticipation/TeamId/IsHalfTurned snapshot (§4.2.2)
│   ├── FilteredView.cs                ← FilteredView, PerceptionDiagnostics, PerceivedAgent, ShoulderCheckAnimData, OcclusionDebugRecord, PerceivedAgentDebug
│   ├── PerceptionEvents.cs            ← PerceptionRefreshEvent struct + RefreshTrigger enum (§4.6.3)
│   ├── EventBusStub.cs                ← wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads
│   ├── EventBusRegistrar.cs           ← boot-time RegisterExternalRow<T>() for PerceptionRefreshEvent (0x10)
│   ├── FovCalculator.cs               ← FoV formula (§3.1) + angular candidacy test + blind-side/peripheral arc predicates
│   ├── OcclusionFilter.cs             ← shadow cone geometry (§3.2.3) + opponent occlusion test; Stage 0: opponents only (OQ-1)
│   ├── PressureEvaluator.cs           ← PressureScalar formula (§3.6); reused verbatim from First Touch #4 §3.5
│   ├── BallPerceptionEvaluator.cs     ← ball range/FoV/occlusion tests + BallStalenessFrames tracking (§3.5); no L_rec (OQ-2)
│   ├── RecognitionLatencyTracker.cs   ← per-(observer,target) latency counters, L_rec formula, half-turn bonus, Wang/Jenkins deterministic hash (§3.3)
│   ├── ShoulderCheckScheduler.cs      ← autonomous shoulder check scheduling + window management + blind-side entity L_rec (§3.4)
│   ├── ViewBuilder.cs                 ← pure field-assembly step: sets scalar/count fields on pre-allocated FilteredView + PerceptionDiagnostics (§3.7)
│   ├── PerceptionSystem.cs            ← 10Hz orchestrator; 7-step pipeline for all 22 agents; forced-refresh handler (§3.0–§3.8, §4.1, §4.6)
│   └── Tests/                         ← (empty — unit tests deferred to Stage 0+1)
├── decision-tree/                     ← Spec #8
│   ├── decision-tree.asmdef           ← AI layer; references agent-movement, perception-system, pass-mechanics, shot-mechanics
│   ├── AssemblyInfo.cs                ← [assembly: InternalsVisibleTo("TacticalDirector.DecisionTree.Tests")]
│   ├── DecisionTree.cs                ← public sealed class: 6-step pipeline orchestrator + state machine (§3.6, §3.7, §4.1)
│   ├── DecisionTreeStateMachine.cs    ← pure state evaluator: IDLE/EVALUATING/EXECUTING/INTERRUPTED transitions (§3.7.2)
│   ├── SnapshotValidator.cs           ← Step 1: validates FilteredView — phase gate, agent identity, ball state (§3.1.1)
│   ├── DecisionContextAssembler.cs    ← Step 2: assembles DecisionContext from all pipeline inputs (§2.2.4, §3.1.1)
│   ├── OptionGenerator.cs             ← Step 3: generates all eligible ActionOption candidates — possession + off-ball branches (§3.1)
│   ├── UtilityScorer.cs               ← Step 4: scores ActionOptions with §3.2 formulas (zone×AM×context×tact×risk)
│   ├── TacticalModifierResolver.cs    ← Step 4 helper: resolves tactical multipliers per action type (§3.4)
│   ├── ActionSelector.cs              ← Step 5: composure noise injection + highest-EffectiveUtility winner (§3.3)
│   ├── ActionDispatcher.cs            ← Step 6: routes selected action to movement controller or physics executor (§3.5)
│   ├── DecisionContext.cs             ← internal struct: all assembled pipeline inputs for one agent-tick (§2.2.4)
│   ├── ActionOption.cs                ← internal struct: one scored candidate with type-specific context fields (§3.1.0)
│   ├── AgentAction.cs                 ← public readonly struct: pipeline output (type, target, pass/shot params, utility) (§2.2.3)
│   ├── DecisionMadeEvent.cs           ← struct event: published after each decision; IEventC (Tier C); wired via EventBusStub (§2.2.7)
│   ├── DtAgentAttributes.cs           ← struct: all DT-consumed player attributes [1–20] raw + CreateDefault factory (§3.1)
│   ├── MatchContext.cs                ← struct: authoritative match state per heartbeat (score, possession, ball, zone) (§2.2.5)
│   ├── TacticalContext.cs             ← struct: pressing mode, passing style, formation slots; Stage0Default factory (§2.2.6)
│   ├── DecisionTreeConstants.cs       ← constants: capacity limits / timing budgets / pipeline invariants (§4.2, §3.7)
│   ├── UtilityWeights.cs              ← constants: all 58+ utility scoring constants (§3.2.11)
│   ├── ComposureWeights.cs            ← constants: NOISE_MAX / COMPOSURE_SUPPRESSION / TIEBREAK_EPSILON (§3.3.3–3.3.5)
│   ├── TacticalWeights.cs             ← constants: tactical multipliers for all action types (§3.4)
│   ├── PitchGeometry.cs               ← static helpers: field zone classification, goal post positions, centre (§3.1.1)
│   ├── IDtMovementController.cs       ← public interface: dispatch boundary to Agent Movement #2 XC-3.5-10 (§3.5)
│   ├── EventBusStub.cs                ← wired to EventBus.Publish (internal; single-sig for DecisionMadeEvent)
│   ├── EventBusRegistrar.cs           ← boot-time RegisterExternalRow<T>() for DecisionMadeEvent (0x11)
│   ├── ActionType.cs                  ← enum: PASS/SHOOT/DRIBBLE/HOLD/MOVE_TO_POSITION/PRESS/INTERCEPT (ordinals are hash inputs)
│   ├── DtState.cs                     ← enum: IDLE / EVALUATING / EXECUTING / INTERRUPTED (§3.7.1)
│   ├── FieldZone.cs                   ← enum: DEFENSIVE / MIDFIELD / ATTACKING — zone boundaries for utility modifiers (§2.2.5)
│   ├── MatchPhase.cs                  ← enum: OPEN_PLAY / SET_PIECE_HOME / SET_PIECE_AWAY / KICK_OFF (§2.2.5)
│   ├── PassingStyle.cs                ← enum: DIRECT / MIXED / SHORT — team passing instruction (§2.2.6)
│   ├── PressingMode.cs                ← enum: HIGH / MEDIUM / LOW — team pressing instruction (§2.2.6)
│   ├── PossessionState.cs             ← enum: HOME_TEAM / AWAY_TEAM / CONTESTED (§2.2.5)
│   └── Tests/
│       ├── decision-tree-tests.asmdef ← EditMode; references decision-tree.asmdef
│       ├── OptionGeneratorTests.cs    ← UT-01..UT-07: OptionGenerator generation gates and candidate logic
│       ├── UtilityScorerTests.cs      ← UT-08..UT-09: UtilityScorer per-action-type utility formulas
│       ├── ActionSelectorTests.cs     ← UT-10..UT-15: ActionSelector composure noise injection + winner selection
│       ├── DispatcherTests.cs         ← UT-16..UT-23: ActionDispatcher movement routing (HOLD/DRIBBLE/MOVE/PRESS/INTERCEPT)
│       └── DecisionTreeIntegrationTests.cs ← UT-24..UT-32: full pipeline state machine + output (public API only)
├── fixed64-math/                      ← Spec #9  (Stage 5+; no runtime code at Stage 0)
├── heading-mechanics/                 ← Spec #10
│   ├── heading-mechanics.asmdef
│   ├── HeadingMechanicsConstants.cs   ← all GT/Fixed/Cross/Derived constants (§3.1)
│   ├── ContactQualityLabel.cs         ← enum: Early / OnTime / Late (telemetry only; KD-2)
│   ├── MistimedDirection.cs           ← enum: None / Early / Late (eligibility output)
│   ├── FailureCause.cs                ← enum: MistimedEarly / MistimedLate / PositionedPoorly / DisturbedInDuel
│   ├── SetPieceContext.cs             ← enum: OpenPlay / Corner / FreeKick (telemetry only)
│   ├── HeadingAgentAttributes.cs      ← struct: Heading/Strength/Balance [1-20], Fatigue [0,1], TeamId
│   ├── HeaderIntent.cs                ← struct: PowerIntent/ContactPointIntent/TargetIntent/AttemptCommittedTick/SetPieceContext (locked at commit; KD-17)
│   ├── HeaderContactState.cs          ← struct: per-attempt mutable state (JumpStartFrame, quality, disturbance, etc.)
│   ├── EligibilityResult.cs           ← struct: IsEligible, PredictedContactFrame, IdealContactFrame, MistimedDirection
│   ├── HeaderExecutedEvent.cs         ← struct: published on successful contact (Tier B event)
│   ├── HeaderAttemptFailedEvent.cs    ← struct: published on failure (Tier C event; no ball-state modification)
│   ├── ContestedDuelContext.cs        ← struct: DuelId, ParticipantCount, WinnerAgentId, BufferStartIndex
│   ├── IHeadingBallSystem.cs          ← interface: GetBallState + ApplyKick
│   ├── IHeadingRngService.cs          ← interface: NextFloat + NextGaussian
│   ├── HeadingRngServiceStub.cs       ← Stage 0 SplitMix64 stub; replace at Stage 1 with #16 wiring
│   ├── EventBusStub.cs                ← wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads
│   ├── EventBusRegistrar.cs           ← boot-time RegisterExternalRow<T>() for HeaderExecutedEvent (0x12 Tier B) + HeaderAttemptFailedEvent (0x13 Tier C)
│   ├── HeadingEligibility.cs          ← pure eligibility predicate (§3.2); no side effects
│   ├── HeadingJumpKinematics.cs       ← FM-010-001 JumpReach + Stage 0 synthetic parabolic Z (KD-18)
│   ├── HeadingContactQuality.cs       ← FM-010-002 contact-quality scalar (asymmetric timing + point error)
│   ├── HeadingPowerAngle.cs           ← FM-010-003 outgoing speed + reflection geometry + own-goal flag (§3.8)
│   ├── HeadingSpinTransfer.cs         ← FM-010-004 head angular-velocity derivation + outgoing spin (§3.6)
│   ├── HeadingDuelResolution.cs       ← FM-010-005 duel scoring; ICollisionEventConsumer; pre-allocated buffers
│   ├── HeadingTelemetry.cs            ← Stage 0 stub; emits §2.4 heading.* trace-pipeline channels at Stage 0+1
│   └── HeadingMechanics.cs            ← 60 Hz orchestrator; two-pass per-frame loop (§4.6)
├── goalkeeper-mechanics/              ← Spec #11
│   ├── goalkeeper-mechanics.asmdef
│   ├── GoalkeeperConstants.cs         ← all GT/Fixed/Cross/Derived constants (§3.4); ~79 constants + 4 draw-site IDs
│   ├── GoalkeeperState.cs             ← enum: GoalkeeperState (11 states)
│   ├── HandlingQualityLabel.cs        ← enum: Caught/Parried/Deflected/Spilled/Missed — telemetry only (KD-2)
│   ├── ReactionLabel.cs               ← enum: Reflexive/Standard/Sluggish — telemetry only (KD-2)
│   ├── FailureCause.cs                ← enum: MissedContact/MistimedDive/WrongDirection/OutOfReach/DisturbedInDuel
│   ├── ClaimType.cs                   ← enum: Cross/Aerial/OneOnOne/ShotCatch — telemetry only
│   ├── RushPhase.cs                   ← enum: Launched/InFlight/Reached/Aborted
│   ├── AbortReason.cs                 ← enum: BallIntercepted/BallCleared/AttackerBeatGK
│   ├── BodyPartEnum.cs                ← enum: Hand/Head/Body/Foot — collision routing (KD-14)
│   ├── HandEnum.cs                    ← enum: Left/Right/Either — KD-1 anatomy lookup carve-out
│   ├── DeliveryKind.cs                ← enum: Throw/Roll/Kick — KD-1 kinematic profile lookup carve-out
│   ├── SaveIntent.cs                  ← struct: TargetHand/ClutchFirmness/DeflectionTarget/AttemptCommittedTick
│   ├── ClaimIntent.cs                 ← struct: TargetContactPoint/ClutchFirmness/AttemptCommittedTick
│   ├── DistributeIntent.cs            ← struct: DeliveryKind/TargetReceiverId/TargetPoint/PowerIntent/SpinIntent
│   ├── RushIntent.cs                  ← struct: RushTarget/CommitmentLevel/AttemptCommittedTick
│   ├── GkContactState.cs             ← struct: per-attempt mutable state (PredictedContactFrame, HandlingQualityScalar, etc.)
│   ├── CrossClaimDuelContext.cs       ← struct: DuelId/ParticipantCount/WinnerAgentId/ContactBodyPart/BufferStartIndex
│   ├── GoalkeeperAgentAttributes.cs   ← struct: all GK attributes [1-20] + Fatigue [0,1] + normalised accessors
│   ├── GoalkeeperPositioningContract.cs ← struct: KD-13 consumer contract; gkBaselineSlot + reactive-radius bounds
│   ├── SaveAttemptedEvent.cs          ← struct event: save attempt (success or failure) with telemetry labels
│   ├── BallClaimedEvent.cs            ← struct event: caught save + releaseTickEarliest (6-second rule)
│   ├── DistributionExecutedEvent.cs   ← struct event: distribution passIntent emitted to Pass Mechanics #5
│   ├── GoalkeeperRushEvent.cs         ← struct event: rush launch/update/abort
│   ├── IGoalkeeperBallSystem.cs       ← interface: GetBallState + ApplyKick + SetPossessor
│   ├── IGoalkeeperRngService.cs       ← interface: NextFloat + NextGaussian (4 registered draw sites)
│   ├── GoalkeeperStateMachine.cs      ← pure state evaluator: EvaluateTacticalTransition + EvaluatePhysicsTransition
│   ├── GoalkeeperReactionPipeline.cs  ← §3.2 ComputeShotDetectedTickMs / ComputeRequiredReactionMs / ComputeReactionWindowAchieved; pure static
│   ├── GoalkeeperDiveKinematics.cs    ← §3.3 Stage 0 synthetic dive: launch impulse, timing jitter, parabolic Z, reach envelope; pure static
│   ├── GoalkeeperHandlingQuality.cs   ← §3.5 handling-quality scalar + parry/deflect/spill velocity helpers; pure static
│   ├── GoalkeeperCrossClaimDuel.cs    ← §3.6 body-part determination + duel arithmetic + tiebreak; ICollisionEventConsumer
│   ├── GoalkeeperRushDispatch.cs      ← §3.7 rush launch impulse + per-frame update; pure static
│   ├── GoalkeeperDistribution.cs      ← §3.8 release-point geometry, windup, accuracy, F-05/F-09 target validation; pure static
│   ├── GoalkeeperTelemetry.cs         ← Stage 0 stub; emits §2.4 gk.* trace-pipeline channels (12 channels)
│   ├── EventBusStub.cs                ← wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads
│   ├── EventBusRegistrar.cs           ← boot-time RegisterExternalRow<T>() for SaveAttemptedEvent (0x14) + BallClaimedEvent (0x15) + DistributionExecutedEvent (0x16) + GoalkeeperRushEvent (0x17)
│   └── GoalkeeperMechanics.cs         ← 10 Hz + 60 Hz orchestrator; constructor-injected; sealed
├── positioning-ai/                    ← Spec #12
│   ├── positioning-ai.asmdef
│   ├── PositioningAIConstants.cs      ← single constant catalogue: pitch/spacing/hysteresis/GK/phase constants, 3 formation tables, pull-factor 13×4 table, lane edges (FR-PA-011/KD-17)
│   ├── Phase.cs                       ← enum: InPoss / OutOfPoss / TransToAtk / TransToDef
│   ├── LineId.cs                      ← enum: Defense / Midfield / Attack
│   ├── LaneId.cs                      ← enum: LW / LH / C / RH / RW (5 × 13.6 m bins)
│   ├── RoleId.cs                      ← enum: 13 roles (GK..ST); row index in 13×4 pull-factor table
│   ├── FormationFamily.cs             ← enum: F442 / F433 / F4231
│   ├── FormationSlotRecord.cs         ← readonly struct: LongPct / LateralPct / Role / DefaultLine / DefaultLane / IsGoalkeeper
│   ├── ContextModifierInputs.cs       ← readonly struct: ScoreDiff / TeamMeanFatigue / TacticalIntensity
│   ├── AgentPositioningData.cs        ← readonly struct: EntityId / SlotIndex / Position / IsActive / Role / IsGoalkeeper
│   ├── AgentHysteresisState.cs        ← struct: CurrentLine / CandidateLine / LineDwellCount / CurrentLane / CandidateLane / LaneDwellCount
│   ├── HysteresisState.cs             ← sealed class: team phase state + AgentHysteresisState[] Agents; SeedFromFormation()
│   ├── PositioningPerceptionSnapshot.cs ← sealed class: pre-allocated tick input (TickIndex / BallPosition / BallVxFiltered / Agents[])
│   ├── PhaseClassifier.cs             ← pure static: ClassifyAndCommit() with PHASE_HYSTERESIS_TICKS dwell; indeterminate → lastCommitted
│   ├── AnchorCalculator.cs            ← pure static: ComputeAnchor / ComputeBallRelativeOffset / ComputeGkSlot (own-half clamp)
│   ├── ContextModifier.cs             ← pure static: ApplyToAll() — lateral + vertical compactness scaling relative to centroid (§3.5)
│   ├── SpacingResolver.cs             ← pure static: EnforceHardSpacing() cost-based displacement up to SPACING_MAX_PASSES (§3.6)
│   ├── ShapeAnalyzer.cs               ← pure static: ResolveAllLines() insertion-sort + LINE_DWELL_TICKS; ResolveAllLanes() LANE_DWELL_TICKS; called AFTER spacing+clamp per AR-S1-03
│   ├── SlotComposer.cs                ← pure static: Compose() 7-step pipeline (anchor→offset→modifiers→spacing→clamp→lines→lanes)
│   ├── PositioningAITick.cs           ← sealed class: 10 Hz orchestrator; zero-alloc hot path; F1 stale detection; GetFormationSlot / GetLine / GetLane / GetPhase
│   └── Tests/
│       ├── positioning-ai-tests.asmdef
│       └── PositioningAITests.cs      ← T-U-001..021 (unit) + T-D-001..002 (determinism) + T-I-001..004 (integration) + T-P-001 (perf) + T-T-001 (tactical)
├── pressing-ai/                       ← Spec #13
│   ├── pressing-ai.asmdef             ← Mechanics layer; references positioning-ai, pass-mechanics
│   ├── PressingAIConstants.cs         ← single constant catalogue: trigger distances/durations, cover-shadow geometry, stamina costs, pitch constants (GT/Fixed/Derived/Cross regions)
│   ├── TriggerFlags.cs                ← [Flags] enum: None / BadTouch / BackwardPass / SidelineTrap / WeakReceiver
│   ├── PressRole.cs                   ← enum: HoldShape / PrimaryPress / CoverShadow
│   ├── CoverShadow.cs                 ← struct: DefenderId, ReceiverId, TargetPosition
│   ├── PressDirective.cs              ← struct: per-tick output (PrimaryPresserId, PrimaryTargetPosition, Shadow0, Shadow1, CoverShadowCount, ActiveTriggers); static Inactive; IsActive property
│   ├── PressAssignment.cs             ← struct: per-agent output (EntityId, Role, TargetPosition)
│   ├── PressTrigger.cs                ← struct: 8 dwell/release counters (4 dwell + 4 release; zero allocation, no arrays)
│   ├── RoleHysteresisState.cs         ← sealed class: LastRole[], RoleDwell[] arrays; Reset()
│   ├── PressingAgentSnapshot.cs       ← struct: per-agent tick input (EntityId, TeamId, Position, BaselineSlot, Fatigue, FirstTouchAttribute, Line, IsGoalkeeper, HasBall, IsActive)
│   ├── PressingSnapshot.cs            ← sealed class: tick input container (TickIndex, BallPosition, BallVelocity, BallCarrierEntityId, AttackingDirection, PossessionTeamId, PressingTeamId, Agents[22])
│   ├── PassEventRing.cs               ← sealed class: ring buffer for BackwardPass trigger (Push, TryGetLatest, Clear)
│   ├── PositioningAIView.cs           ← readonly struct: facade over PositioningAITick (GetFormationSlot, GetLine, GetPhase, IsSentinelSlot)
│   ├── TriggerEvaluator.cs            ← pure static: Evaluate() debounce pipeline for 4 triggers + ComputeGeometricPressure helper
│   ├── PrimaryPressSelector.cs        ← pure static: Select() best presser by cost; ComputeInterceptionPoint(); GetCarrierPosition() helper
│   ├── CoverShadowSelector.cs         ← pure static: Select() up to 2 cover shadows; threat score + greedy defender assignment
│   ├── RoleHysteresis.cs              ← pure static: Commit() dwell guard; ForceAllHoldShape()
│   ├── StaminaAccumulator.cs          ← pure static: Apply() per-role fatigue cost; ApplyAll() batch apply
│   ├── DisengageResolver.cs           ← pure static: Evaluate() disengage conditions (zone exit + timeout); IsInCooldown()
│   ├── InvariantEnforcer.cs           ← pure static: Enforce() three anti-chaos invariants (MaxPressersBallThird, MinBacklineAgents, MaxPressDisplacementM)
│   └── PressingAITick.cs              ← sealed class: 10 Hz orchestrator; 8-step pipeline; pre-allocated buffers; zero-alloc hot path
├── defensive-ai/                      ← Spec #14
│   ├── defensive-ai.asmdef            ← Mechanics layer; references positioning-ai, pressing-ai
│   ├── DefensiveAIConstants.cs        ← single constant catalogue: assignment/hysteresis/offside-trap/tackle/anti-chaos/GK-zone constants (22 [GT] + 4 [CROSS])
│   ├── MarkMode.cs                    ← enum: Zonal / ManMark / InterceptRunner / CoverGkZone (FR-DA-011)
│   ├── TackleMode.cs                  ← enum: Hold / Jockey / Commit
│   ├── MarkDirective.cs               ← struct: per-tick team-level output (TeamId, OffensiveLineDepth, OffsideTrapActive, StepUpTargetDepth, EmergencyFlag)
│   ├── MarkAssignment.cs              ← struct: per-agent assignment (Mode, TargetEntityId, TargetPosition, ValidThroughTick, OverriddenThisTick); MakeZonal factory
│   ├── TackleIntentRequest.cs         ← struct: per-agent tackle intent (Mode, TargetEntityId, ApproachAngle, CoverageDepth)
│   ├── MarkHysteresisState.cs         ← struct: per-agent dwell-lock state (DwellCounter, CandidateMode, CandidateTargetEntityId, HoldTicks)
│   ├── OffsideLineState.cs            ← struct: per-team offside state (CurrentLineDepth, StepUpDwellCounter, CooldownTicksRemaining, CoverGkZoneActiveTicks)
│   ├── DefensiveAgentSnapshot.cs      ← struct: per-agent tick input (EntityId, TeamId, Position, Velocity, IsActive, IsGoalkeeper, HasBall, BaselineSlot, Line, PressRole, PerceivedFirstTouch)
│   ├── DefensiveSnapshot.cs           ← sealed class: tick input container (TickIndex, DefensiveTeamId, BallPosition, BallVelocity, TeamPhase, DefensiveLineDepth, GkEntityId, GkPosition, Agents[22])
│   ├── HoldShapePoolFilter.cs         ← pure static: BuildPool() excludes GK + PrimaryPress/CoverShadow; SnapshotIndexOf() helper
│   ├── LastManDetector.cs             ← pure static: Evaluate() last-man predicate (§3.8) + COVER_GK_ZONE trigger (§3.9); DefendsX0/DistToOwnGoal/DisplacementCost helpers; LastManResult struct
│   ├── MarkHysteresis.cs              ← pure static: PreCheck() dwell-lock gate; ApplyGate() transition accumulator; Reset() for emergency overrides
│   ├── MarkAssigner.cs                ← pure static: Assign() regular assignment loop (§3.3); ThreatScore() §3.5; SelectBestCandidate(); IsBetter() tie-break
│   ├── TackleIntentEvaluator.cs       ← pure static: Evaluate() tackle intent (§3.6); ComputeCoverageDepth(); SelectMode()
│   ├── OffsideTrapController.cs       ← pure static: Update() dwell counter + fire trigger (§3.7); ExecuteStepUp(); ComputeDefenseLineSpread()
│   ├── InvariantEnforcer.cs           ← pure static: Enforce() 3 anti-chaos invariants (§3.10); 3-pass demotion loop; F4 hard-fallback detection
│   └── DefensiveAITick.cs             ← sealed class: 10 Hz orchestrator; 9-step §3.13 pipeline; pre-allocated buffers; GetMarkDirective/GetAssignment/GetTackleIntentRequests
├── attacking-ai/                      ← Spec #15
│   ├── attacking-ai.asmdef            ← Mechanics layer; references positioning-ai, pressing-ai
│   ├── AttackingAIConstants.cs        ← single constant catalogue: pool/role/run-params/support/width/weak-side/overload/invariant/hysteresis/test constants (GT/Derived/Cross regions)
│   ├── AttackRole.cs                  ← enum: HoldWidth / SupportBall / Runner / WeakSide (FR-AT-012)
│   ├── Flank.cs                       ← enum: Left / Right — overload lateral discriminator (§3.8)
│   ├── RunParameters.cs               ← readonly struct: DepthOffsetM / LateralOffsetM / RunTriggerTick (FR-AT-011; exactly 3 fields)
│   ├── AttackHysteresisState.cs       ← struct: per-agent dwell state (CurrentRole, DwellCounter, CandidateRole, CandidateDwell)
│   ├── TransitionHoldState.cs         ← struct: per-team possession-loss countdown + PrevPhase
│   ├── AttackDirective.cs             ← readonly struct: team-level tick output (TeamId, OverloadActive, OverloadFlank, TransitionHoldTick)
│   ├── AttackIntent.cs                ← readonly struct: per-agent tick output (AgentEntityId, Role, RunParameters?, ValidThroughTick)
│   ├── StyleProfile.cs                ← readonly struct: 5 profile multipliers; static factories Possession/Direct/Counter
│   ├── AttackIntentSnapshot.cs        ← readonly struct: read-only zero-copy view over tick output (Directive, Intents[], IntentCount, TickIndex)
│   ├── AttackingAgentSnapshot.cs      ← readonly struct: per-agent tick input (EntityId, TeamId, Position, BaselineSlot, Line, IsGoalkeeper, HasBall, IsActive, Pace, Stamina, Dribbling)
│   ├── AttackingSnapshot.cs           ← sealed class: pre-allocated tick input container (TickIndex, AttackingTeamId, BallPosition, BallCarrierEntityId, BallCarrierPosition, TeamAttackAngle, Agents[22])
│   ├── AttackPoolEntry.cs             ← internal struct: per-agent scratch entry during pipeline (EntityId, Position, LateralPct, Line, AssignedRole, HasRunParams, run-param fields, RunTargetPosition, TargetPosition)
│   ├── AttackingPoolBuilder.cs        ← pure static: Build() filters snapshot → pool, EntityId-ascending insertion sort; returns −1 on F2 sentinel
│   ├── AttackHysteresis.cs            ← pure static: IsStable() / Update() / Reset() — increment-based dwell (CandidateDwell resets on current-role re-preference)
│   ├── SupportHeuristic.cs            ← pure static: IsWithinSupportRadius() / ComputeEffectiveRadius() — floored at MinEffectiveRadiusM
│   ├── RoleAssigner.cs                ← pure static: Assign() two-pass role assignment (pass 1 counts stable roles; pass 2 evaluates non-stable); GenerateRunParams() §3.4
│   ├── WidthHolder.cs                 ← pure static: Enforce() near-touchline width-holding; promotes non-RUNNER agents to HOLD_WIDTH
│   ├── WeakSideController.cs          ← pure static: EnsureWeakSide() post-check; selects max-deviation non-RUNNER for far-side coverage
│   ├── OverloadDetector.cs            ← pure static: Evaluate() counts non-WEAK_SIDE agents in Y-corridor; fires at ≥ OverloadCount
│   ├── TransitionController.cs        ← pure static: Evaluate() SET-then-DECREMENT transition hold; COUNTER profile (0 ticks) → instant empty
│   ├── InvariantEnforcer.cs           ← pure static: Apply() 3 anti-chaos invariants (max runners, min support, no own-half runs); fallback all→HoldWidth
│   └── AttackingAITick.cs             ← sealed class: 10 Hz orchestrator; §3.13 pipeline (phase gate→pool→roles→width→weak-side→overload→invariants→publish); pre-allocated zero-alloc buffers
│
├── deterministic-sim/                 ← Spec #16  (cross-cutting; referenced by all layers)
│   ├── deterministic-sim.asmdef       ← no references (all layers reference this; it references none)
│   ├── DeterministicSimConstants.cs   ← all [FIXED]/[DERIVED]/[GT] constants: tick rates, error codes, domain tags, field widths, RNG params, digest versions
│   ├── PhaseId.cs                     ← enum: Input=0 / Intent=1 / AI=2 / Physics=3 / Resolve=4 / Events=5 / Snapshot=6 (AR-1 H-4: AI_NoOp removed; Events added)
│   ├── DeterminismTier.cs             ← enum: TierA=0 / TierB=1 / TierC=2 (byte)
│   ├── DivergenceClass.cs             ← enum: None / HardDesync / SoftDrift / Cosmetic (byte)
│   ├── SubsystemOrdinals.cs           ← compile-time const ints: BallPhysics=0..GoalkeeperMechanics=7 (Physics 0–19), PositioningAI=20..AttackingAI=23 (Mechanics 20–39), PerceptionSystem=40, DecisionTree=41 (AI 40–59), EventSystem=60
│   ├── ReplayCursor.cs                ← readonly struct: Tick (ulong), PhaseOrdinal (byte), IsAtEndOfSnapshot property, EndOfSnapshot(tick) factory
│   ├── DespawnEntry.cs                ← readonly struct: EntityId, FinalActionOrdinal, FinalRngCursor, DespawnTick (all fields; Tier A tombstone)
│   ├── DespawnLog.cs                  ← pre-allocated tombstone list: Append / ContainsEntity / GetEntry / Clear; capacity = MaxDespawnEntries
│   ├── EnvironmentFingerprint.cs      ← sealed class: 6 readonly fields + Lock() + ValidateAgainst() → ERR_DS_REPLAY_ENV_MISMATCH + CreateStage0Dev() factory
│   ├── RngStreamState.cs              ← mutable struct: StreamKey/RngCursor/ActionOrdinal (ulong), BudgetRemaining/DeclaredBudget/DrawIndex (int), SiteId (string), StreamVersion (ushort), SubsystemOrdinal, EntityId; ClearReservation()
│   ├── MatchClock.cs                  ← sealed class: CurrentTick / CurrentTacticalTick / CurrentMatchTimeMs / IsAiStrideTick; Advance() / RestoreFromSnapshot(tick) — no System.DateTime (FR-CS-042)
│   ├── DeterministicRngService.cs     ← sealed class: HKDF-SHA256 key derivation + SipHash-2-4-64 per-draw hash; RegisterStream / Reserve / DrawReserved / CloseReservation / Skip / RestoreStream; zero-allocation hot path (stackalloc Span<byte>)
│   ├── CanonicalSerializer.cs         ← static class: §3.2.4.1 Write/Read for all wire types; FloatUintUnion explicit-layout struct (AR-1 H-1/H-2: no BitConverter allocs); −0.0→+0.0 normalization; Tier B NaN→0x7FC00000
│   ├── SnapshotHeader.cs              ← sealed class: SchemaVersion / DigestVersion / Tick / PrevSnapshotDigest[32] / CurrentSnapshotDigest[32] / Fingerprint / Cursor; Initialize()
│   ├── SnapshotPayload.cs             ← sealed class: pre-allocated PayloadBytes[MaxSnapshotBytes] / BytesWritten / Reset()
│   ├── SnapshotCodec.cs               ← sealed class: Encode() SHA-256 + digest chain; ValidateHeader() / ValidatePrevDigest() / CommitLoadedDigest(); _prevDigest[32] chain state
│   ├── ReplayEngine.cs                ← sealed class: PrepareReplay() steps 1–7 of §4.2.2; step 6 Stage 0 stub (in-memory RNG state preserved); step 8 delegated to TickOrchestrator
│   ├── SaveManager.cs                 ← sealed class: CommitAtomic() §4.6.1.1 five-step atomic save (temp→fsync→rename-overwrite→dir-fsync); File.Move(overwrite:true) (AR-1 M-2)
│   ├── TickOrchestrator.cs            ← sealed class: RunTick() 7-phase 60 Hz pipeline; AI stride-gated on IsAiStrideTick; System.Action callbacks per phase; 9 ProfilerMarkers; zero-alloc hot path
│   ├── DivergenceDetector.cs          ← static class: CompareDigests / CompareTierAFloat / CompareTierBFloat (AR-1 M-3: one-NaN→SoftDrift) / CompareTierAInt / CompareTierAUlong / Worst()
│   └── tests/
│       ├── deterministic-sim-tests.asmdef  ← EditMode; references deterministic-sim.asmdef
│       └── DeterministicSimTests.cs   ← HKDF RFC 5869 KAT; SipHash-2-4 ref vectors 0–7; canonical serialization (bool/u32/u64/-0.0/DT bits); T-DS-ORDER-001 MatchClock; T-DS-RNG-002 branch cursor parity; T-DS-SNAP-003 u64 round-trip; T-DS-FAULT-009..014; AI stride; DespawnLog
│
├── event-system/                      ← Spec #17  (cross-cutting; referenced by all layers)
│   ├── event-system.asmdef            ← references TacticalDirector.DeterministicSim; autoReferenced true
│   ├── EventSystemConstants.cs        ← all [GT]/[CROSS] constants: queue/dispatch/handler/slot capacities + error codes + DomainTagEventLedger
│   ├── IEventA.cs                     ← marker interface: Tier A (authoritative, digest-included)
│   ├── IEventB.cs                     ← marker interface: Tier B (bounded-authoritative; Stage 5+ only)
│   ├── IEventC.cs                     ← marker interface: Tier C (cosmetic; immediate-dispatch; excluded from digest)
│   ├── EventHandler.cs                ← delegate: void EventHandler<T>(in T evt) where T : struct
│   ├── SubscriptionToken.cs           ← readonly struct: EventTypeOrdinal + SubscriberIndex; zero allocation (FR-EVT-073)
│   ├── EventRegistry.cs               ← Appendix A registry: 11 seeded rows; RegisterRow<T>/RegisterRowRaw/RegisterExternalRow; EventOrdinalCache<T> O(1) lookup
│   ├── EventLedger.cs                 ← ring buffer + typed dispatch; EventSlotMeta (FM-017-002 sort key); EventTypeDispatchBase/EventTypeDispatcher<T>; DrainTick BFS; InsertionSort; SerializeLedger; Subscribe
│   ├── CosmeticChannel.cs             ← Tier C immediate-dispatch; per-ordinal pub-count table; >= maxPerTick drop predicate (FR-EVT-043); stackalloc span dispatch (zero-alloc FR-EVT-048)
│   ├── EventBus.cs                    ← public static API: BeginTick/BeginPhase/DrainTick/SerializeLedger/OnTickBoundary; Publish/Subscribe overloads per tier; debug phase assertion #if UNITY_EDITOR||DEVELOPMENT_BUILD
│   ├── PossessionChangedEvent.cs      ← Tier A 0x04: PreviousHolder/NewHolder/Reason
│   ├── FoulCommittedEvent.cs          ← Tier A 0x05: Offender/Victim/Location(Vector3)/FoulKind
│   ├── CardIssuedEvent.cs             ← Tier A 0x06: Recipient/CardKind/FoulOrdinal(ushort; 0xFFFF=procedural)
│   ├── GoalAwardedEvent.cs            ← Tier A 0x07: Scorer/Assister/ScoringTeam/BallPosition(Vector3)
│   ├── SubstitutionEvent.cs           ← Tier A 0x08: Outgoing/Incoming/Team/SubstitutionReason
│   ├── TickHeartbeatEvent.cs          ← Tier C 0x09: empty payload; MaxPerTick=1; CLR min size 1 byte
│   ├── VfxImpactCue.cs                ← Tier C 0x0A: ImpactPoint(Vector3)/ImpactKind/Intensity; MaxPerTick=64
│   ├── UiNotificationCue.cs           ← Tier C 0x0B: NotificationKind/SubjectEntity; MaxPerTick=32
│   └── tests/
│       └── event-system-tests.asmdef  ← EditMode; references TacticalDirector.EventSystem; autoReferenced false
│
├── performance-optimization/          ← Spec #18  (owns trace pipeline; minimal game-loop code)
│   ├── performance-optimization.asmdef  ← autoReferenced false; references TacticalDirector.DeterministicSim; game-layer assemblies MUST NOT import
│   ├── HotPathAllocExemptAttribute.cs   ← §3.7.5 governance attribute; Justification (required) + SignOffRef (optional) properties
│   ├── ChannelVerbosity.cs              ← F.0 schema enum: Minimal / Standard / Debug / Exhaustive (FR-PO-055)
│   ├── ChannelSamplingRule.cs           ← F.0 schema enum: EveryTick / PerNTicks / EventDriven (FR-PO-056)
│   ├── ChannelDeterminismClass.cs       ← F.0 schema enum: TierA / TierB / TierC (FR-PO-058a)
│   ├── TraceChannelDescriptor.cs        ← F.0 11-field sealed descriptor; constructor enforces SamplingRule/SampleN invariant
│   ├── TraceChannelRegistry.cs          ← Stage 0 anchor rows: PerfBudget / PerfAlloc / PerfTrace
│   ├── PerformanceOptimizationConstants.cs  ← Fixed: HOT_PATH_ALLOC_BUDGET_BYTES=0 / LOOP_TAG_TACTICAL_10HZ / LOOP_TAG_PHYSICS_60HZ; GT: PerPrRegressionFraction / AbsoluteDriftFraction / BaselineSampleCount / MaxFlakeRate / HeadroomMultiplierMin/Max / PromotionToleranceFraction / ReproducibilityToleranceFraction; EST: SamplerDefaultHz / StatisticalSignificanceN / FirstTickWarmupCount
│   ├── LoopTag.cs                        ← enum: TacticalTenHz / PhysicsSixtyHz (KD-8; Appendix A on-disk loop-tag keys)
│   ├── BaselinePassFail.cs               ← enum: Pass / Fail / Advisory (capture-time verdict per Appendix A)
│   ├── HardwareCounterSnapshot.cs        ← readonly struct: CpuModel / CoreCount / ThermalState (§3.3.2 session manifest field)
│   ├── SessionManifest.cs                ← sealed class: all §3.3.2 required fields (GitSha / Seed / EnvironmentFingerprint #16 §4.8 / PlatformPin / ScenarioManifestId / timestamps / HardwareCounters / HarnessVersion); IsComplete() validator
│   ├── BaselineRecord.cs                 ← sealed class: SessionManifest + Loop + P50Ms + P99Ms + PerMethodAllocBytes + PassFail + ThresholdCited (Appendix A / §4.3.2)
│   ├── BudgetRollupEntry.cs              ← readonly struct: SpecId / SubroutineName / Loop / BudgetMs / AllocBudgetBytes / Citation (§4.3.3 / §3.1.3 roll-up columns)
│   ├── HotPathEntry.cs                   ← readonly struct: SpecId / MethodName / Loop / BudgetMs / HasAllocExemption (§3.7.2 union entry)
│   ├── IPerfHarness.cs                   ← interface: BeginSession / RecordTickSample / FinalizeSession (§4.3.1 / §4.4; both sides specified per CLAUDE.md "Interface Design Principle")
│   ├── IBudgetSource.cs                  ← interface: SpecId property + GetEntries() → IReadOnlyList<BudgetRollupEntry> (§4.4; both sides specified)
│   ├── RegressionResult.cs               ← readonly struct: PerPrPassed / AbsoluteDriftPassed / DeltaFraction / MilestoneDriftFraction / AllPassed (FR-PO-031 evaluation output)
│   ├── RegressionGate.cs                 ← static class: PassesPerPrCheck / PassesAbsoluteDriftCheck / Evaluate (FR-PO-031 / §3.5.2 / §3.5.6)
│   ├── ReproducibilityResult.cs          ← readonly struct: IsReproducible / OriginalP50Ms / RecapturedP50Ms / AbsDeltaFraction / ScenarioMatched / SeedMatched (FR-PO-067 audit output)
│   └── BaselineReproducibilityAuditor.cs ← sealed class: Validate(original, recaptured) → ReproducibilityResult (§3.4.4 / §5.4 / FR-PO-067)
├── testing-strategy/                  ← Spec #19  (CI orchestration tooling; no game-loop code)
│   ├── testing-strategy.asmdef        ← autoReferenced false; references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization; game-layer assemblies MUST NOT import
│   ├── TestingStrategyConstants.cs    ← §3.10 governance constants: pyramid bounds / coverage thresholds / quarantine + eviction windows / pre-commit budget / [FIXED] MATCH_LENGTH_MINUTES
│   ├── TestTier.cs                    ← enum: TierA / TierB / TierC (mirror of #16 §1.1.1; consumed not owned per KD-1)
│   ├── TestLayer.cs                   ← enum: Unit / Integration / Simulation / Determinism / EndToEndSoak (§3.1.1 five-layer taxonomy, FR-TS-001)
│   ├── GoldenVectorKind.cs            ← enum: HkdfSha256Kat / SipHash24Kat / CanonicalSerializeCorpus (#16 §9.5 #4 a/b/c)
│   ├── GoldenVectorEntry.cs           ← readonly struct: Kind / Name / SourcePath / Citation — one row in the corpus catalogue
│   ├── GoldenVectorResult.cs          ← readonly struct: Entry / Passed / VectorsExecuted / VectorsFailed / Diagnostic
│   ├── GoldenVectorRunner.cs          ← static class: Catalogue() + Run(in entry) + RunAll(); Stage 0 deferred-status (D1 test-runner pin); Stage 0+1 parses corpus + invokes DeterministicRngService / CanonicalSerializer
│   ├── DeterminismTierKind.cs         ← enum: Unit / Integration / Scenario / Soak (#16 §5 canonical tier order; FR-TS-011 / FR-TS-018)
│   ├── DeterminismTierResult.cs       ← readonly struct: Tier / Passed / TestsExecuted / TestsFailed / Diagnostic
│   ├── DeterminismSuiteResult.cs      ← sealed class: TierResults[] + GoldenVectorResults[] + AllPassed (FR-DS-009-GATE union)
│   ├── DeterminismGate.cs             ← static class: RunTiers() — single integration point per FR-TS-016; aggregates §5 tiers + golden-vector corpus
│   ├── PerfGateReport.cs              ← sealed class: SpecId / LoopTag / ScenarioManifestId / Regression — CI-friendly wrapper over Spec #18 RegressionResult
│   └── PerfGateRunner.cs              ← static class: Run(specId, loopTag, baseline, current, milestoneMs) → PerfGateReport; delegates verdict to #18 RegressionGate.Evaluate (KD-3 boundary)
└── code-standards/                    ← Spec #20  (governance only; no runtime code)
```

**One folder per spec. One `.asmdef` per folder. Folder names match `docs/specs/` exactly.**

> **Note on `.asmdef` coverage:** Every spec folder listed above requires a
> `.asmdef` file (e.g., `pressing-ai/pressing-ai.asmdef`). Only a subset is shown
> in the tree for brevity. See each spec's `§4` (Architecture) file for the exact
> `.asmdef` reference list. GUIDs are blocked on Unity project initialization (see
> "WHAT IS NOT HERE YET").
>
> **Test assemblies:** Every `tests/` subfolder requires its own `.asmdef` with
> `testPlatforms: [EditMode]` (or as specified per Spec #19 §7.5 D2) and a reference
> to the parent spec's `.asmdef`. Test assemblies are excluded from production builds
> via platform filtering. Only the expanded spec folders in the tree above show the
> `.asmdef` entry; all `tests/` subfolders follow the same pattern.

### Assembly Layer Taxonomy

The authoritative layer taxonomy is Spec #20 §3.5.2. The three layers and their
members are reproduced here verbatim — do not infer layer membership from folder
order or spec number.

| Layer | Assemblies |
|---|---|
| **Physics** | ball-physics, agent-movement, collision-system, first-touch, pass-mechanics, shot-mechanics, heading-mechanics, goalkeeper-mechanics |
| **Mechanics** | positioning-ai, pressing-ai, defensive-ai, attacking-ai |
| **AI** | decision-tree, perception-system |
| **UI** | (Stage 1+ — not yet specified) |

The `deterministic-sim` and `event-system` assemblies are cross-cutting foundations
referenced by all layers (not members of any single layer).

The following assemblies are **infrastructure-only** and are NOT members of any
gameplay layer. Game-layer code (Physics / Mechanics / AI) MUST NOT import them
at runtime:

| Assembly | Role |
|---|---|
| `project-constants` | Constants shared across ≥ 2 spec assemblies; read-only by all |
| `performance-optimization` | Trace pipeline only (Spec #18 KD-3); no game-loop types |
| `testing-strategy` | CI orchestration tooling only (Spec #19); no game-loop types |
| `code-standards` | Governance only (Spec #20); no runtime types |

### Reference Direction

**AI depends on Mechanics. Mechanics depends on Physics. Never the reverse.**

```
project-constants  (read-only by all assemblies)

Physics  ←  Mechanics  ←  AI  ←  UI
```

`←` means "is referenced by" — `A ← B` means B depends on A (B imports from A).
The AI assembly imports types from Mechanics, which imports types from Physics.
A Physics assembly MUST NOT import from Mechanics or AI; a Mechanics assembly MUST NOT
import from AI. These prohibited import directions are enforced as build errors via
`.asmdef` reference declarations (FR-CS-046).

For upward event notification (e.g., a physics event consumed by AI), use a struct
event on the event bus — no direct assembly reference (FR-CS-047).

For the specific `.asmdef` references each assembly declares, read that spec's `§4`
(Architecture) file. Do not infer the intra-layer dependency chain from this document.

---

## BUILD AND TEST COMMANDS

> **Note:** The Unity LTS revision, backend (Mono/IL2CPP), and compiler flags are not yet pinned in `docs/tracking/certification-platform.md`. Fill those in before running the first certification gate (`FR-DS-009-GATE`). The commands below are the intended Stage 1 setup; update this section when the project is configured.

**Format check (pre-commit gate):**
```bash
dotnet format --verify-no-changes
```

**Build with warnings-as-errors:**
```bash
dotnet build /p:TreatWarningsAsErrors=true
```

**Run tests:**
```bash
dotnet test
```

**Unity batch-mode test run (CI):**
```
# To be filled in once Unity project is initialized and certification-platform.md is pinned.
```

**Stage 0 verification:** Manual code review against Spec #20 §5.4 checklist (7 categories, 73 FRs). Static analysis tooling (Roslyn analyzers, BannedSymbols.txt, `.editorconfig`) activates at Stage 1.

---

## FILE NAMING

- One public type per file. Filename must match the type name exactly (case-sensitive).
  - `BallState.cs` contains `public struct BallState`
  - `BallPhysicsConstants.cs` contains `public static class BallPhysicsConstants`
- Tests live in a sibling `tests/` folder under the same spec folder.
- No version suffixes in filenames. Git tracks history.

---

## NAMING CONVENTIONS

| Identifier | Convention | Example |
|---|---|---|
| Types, methods, properties, events | PascalCase | `BallState`, `ApplyKick` |
| Local variables, parameters | camelCase | `deltaTime`, `agentId` |
| Private instance fields | `_camelCase` | `_clock`, `_agentCount` |
| Private static fields | `s_camelCase` | `s_updateMarker`, `s_runTickMarker` |
| `[FIXED]` constants | `ALL_CAPS` | `BALL_RADIUS`, `DRAG_COEFFICIENT` |
| All other constants (`[GT]`, `[EST]`, `[DERIVED]`, `[CROSS]`) | PascalCase | `MaxSubsteps`, `TerminalVelocity` |
| Interfaces | `I` prefix + PascalCase | `IEventBus`, `ICollisionConsumer` |
| Assembly names / namespaces | `TacticalDirector.<SpecName>` | `TacticalDirector.BallPhysics` |

No Hungarian notation. No other prefix/suffix schemes (FR-CS-001/002).

**`var` policy (FR-CS-013):** Use `var` only when the type is immediately obvious from the RHS. `var state = new BallState()` is clear. `var result = Compute();` is not — write the explicit type.

---

## STYLE

**Indentation:** 4 spaces. Tabs are prohibited (FR-CS-011). Enforced by `.editorconfig` at Stage 1.

**Brace style:** Allman — opening brace on its own line (FR-CS-012).

```csharp
// COMPLIANT — FR-CS-012
public void Update(ref BallState state)
{
    if (state.IsGrounded)
    {
        ApplyFriction(ref state);
    }
}

// VIOLATION — K&R brace style
public void Update(ref BallState state) {
    if (state.IsGrounded) {
        ApplyFriction(ref state);
    }
}
```

**Explicit access modifiers:** Every type, method, property, field, and event declaration MUST carry an explicit access modifier. Relying on C#'s implicit `private` or `internal` is prohibited (FR-CS-014).

---

## NAMESPACES

One namespace per assembly. Sub-folders do not introduce sub-namespaces (FR-CS-007).

```csharp
// File: src/ball-physics/simulation/DragIntegrator.cs
namespace TacticalDirector.BallPhysics   // flat — no sub-namespace
{
    internal readonly struct DragIntegrator { … }
}

// VIOLATION:
namespace TacticalDirector.BallPhysics.Simulation { … }
```

---

## CONSTANT CATALOGUES

Every constant lives in `<SpecName>Constants.cs`. No literals in formula or system files (FR-CS-016).

**Naming:** PascalCase folder name + `Constants.cs`

| Spec folder | Catalogue file |
|---|---|
| `ball-physics/` | `BallPhysicsConstants.cs` |
| `agent-movement/` | `AgentMovementConstants.cs` |
| `collision-system/` | `CollisionSystemConstants.cs` |
| *(all specs)* | `<SpecName>Constants.cs` |
| *(cross-spec)* | `project-constants/ProjectConstants.cs` |

**Region order inside every catalogue (most-immutable first):**

```csharp
#region Fixed      // [FIXED]   → public const float BALL_RADIUS = 0.11f;
#region Derived    // [DERIVED] → public static readonly float TerminalVelocity = Mathf.Sqrt(GRAVITY / DRAG_COEFFICIENT);
#region Cross      // [CROSS]   → public static readonly float PhysicsTickHz = ProjectConstants.PHYSICS_TICK_HZ;
#region GT         // [GT]      → public static readonly int MaxSubsteps = 8; // TODO: replace with config loader (Stage 1)
#region EST        // [EST]     → public static readonly float LiftCoefficient = 0.35f; // TODO: validate
```

Omit a region entirely if the spec has no constants with that tag. Empty regions are prohibited.

**Region name convention:** The first three region names use Title Case (`Fixed`, `Derived`, `Cross`). `GT` and `EST` match their tag names exactly since those are already **all-caps abbreviations**. Do not use ALL_CAPS (`FIXED`) or lowercase for region names.

**`[DERIVED]` constants:** The XML doc must include the tag, the formula, and the source constants (FR-CS-021). Substitute actual formula references (FM-NNN, §x.y) from the implementing spec:

```csharp
#region Derived
/// <summary>
/// [DERIVED] Terminal velocity (m/s) at which drag force equals gravity.
/// Formula: sqrt(GRAVITY / DRAG_COEFFICIENT). FM-NNN. Ball Physics #1 §3.x.
/// Source constants: BallPhysicsConstants.GRAVITY, BallPhysicsConstants.DRAG_COEFFICIENT.
/// </summary>
public static readonly float TerminalVelocity =
    Mathf.Sqrt(BallPhysicsConstants.GRAVITY / BallPhysicsConstants.DRAG_COEFFICIENT);
```

**`[GT]` loading mechanism:** The exact class and method for loading `[GT]` constants from tunable config at boot (FR-CS-019) is a Stage 1 deliverable — no class named `ConfigLoader` exists in any approved spec. Until the mechanism is defined and documented in this file, use the constant's design-time default directly and mark it with `// TODO: replace with config loader`:

```csharp
#region GT
/// <summary>[GT] Maximum physics substeps per frame. Code Standards #20 §3.2.3.</summary>
public static readonly int MaxSubsteps = 8; // TODO: replace with config loader (Stage 1)
```

**`[EST]` constants:** Every `[EST]` constant requires a `spec-error-log.md` entry (FR-CS-020). The constant must be promoted to `[GT]`, `[FIXED]`, `[DERIVED]`, or `[CROSS]` before the system that consumes it is implemented. If the validated value is derivable via formula, use `[DERIVED]` (document the formula per FR-CS-021). If it already exists authoritatively in another spec, use `[CROSS]` (cite the authoritative spec and section per FR-CS-022).

**`[CROSS]` mirrors — routing rule (Spec #20 §4.2):**
- **Multi-consumer** (constant used by ≥ 2 spec assemblies): declare in `ProjectConstants.cs`; each consuming catalogue mirrors from there.
- **Single-consumer** (constant used by exactly 1 spec assembly, e.g., a domain tag allocated in Spec #16 §3.4 used only by one spec): the consuming catalogue mirrors directly from the source spec's catalogue — not via `ProjectConstants.cs`.

A `[CROSS]` mirror must not diverge from its source. Naming is PascalCase per §3.2.3. Cite the authoritative spec and section:

```csharp
// Multi-consumer mirror: declare in ProjectConstants.cs; each consuming catalogue mirrors from there.
/// <summary>
/// [CROSS] Physics/render loop tick rate (Hz).
/// Authoritative source: ProjectConstants.cs — PHYSICS_TICK_HZ.
/// Ball Physics #1 §1.2. Value: 60 Hz.
/// </summary>
public static readonly float PhysicsTickHz = ProjectConstants.PHYSICS_TICK_HZ;

// Single-consumer mirror: source spec's catalogue directly, NOT via ProjectConstants.cs
/// <summary>
/// [CROSS] Goalkeeper subsystem domain tag.
/// Authoritative source: DeterministicSimConstants.DOMAIN_TAG_GOALKEEPER.
/// Deterministic Simulation #16 §3.4. Value: 0x1D.
/// </summary>
public static readonly uint DomainTagGoalkeeper =
    DeterministicSimConstants.DOMAIN_TAG_GOALKEEPER;
```

> **Note — naming discrepancy in Spec #20 §4.2 (ERR-020-001, resolved):** The §4.2
> worked example originally showed `PHYSICS_TICK_HZ` (ALL_CAPS) for the `[CROSS]`
> *mirror* field in `BallPhysicsConstants.cs`. This contradicts §3.2.3, which is the
> rule-definition section and states PascalCase for `[CROSS]`. §3.2.3 is authoritative —
> use PascalCase for the mirror field name. Spec #20 §4.2 has been patched to show
> `PhysicsTickHz` (PascalCase). Note that the source constant in `ProjectConstants.cs`
> is tagged `[FIXED]` and correctly uses ALL_CAPS (`PHYSICS_TICK_HZ`); the right-hand
> side of the mirror assignment must reference that ALL_CAPS name.

---

## GAME-LOOP RULES (ZERO ALLOCATION)

The 60 Hz physics/render path must produce **zero managed-memory allocations per frame** (FR-CS-066).

**Required patterns:**
- Game-state data in `readonly struct`, not `class`
- State passed by `ref` parameter
- Pre-allocated fixed-size buffers for temp arrays
- Struct-based events on the event bus (not `event Action<T>`)
- `stackalloc` with `Span<T>` for transient buffers with statically bounded size (C# 7.2+; no `unsafe` block required). The pointer form (`int* p = stackalloc int[n]`) requires `unsafe` and therefore lead-developer sign-off per FR-CS-010 — use the `Span<T>` form by default
- `private static readonly ProfilerMarker` field on every system class for profiling (one-time alloc at startup); call `.Auto()` at each entry point to bracket the measurement scope (FR-CS-070)
- **Dependency injection via constructor parameters** — see "Banned Architectural Patterns" below for the full rule and the four anti-patterns it replaces

**Banned constructs on hot paths (FR-CS-027–034):**
- `new` class objects or managed arrays
- Boxing (value type → object cast)
- LINQ (`.Where`, `.Select`, `.ToList`, etc.)
- `params` array parameters
- String formatting (`$"…"`, `string.Format`, `+` concatenation)
- Closures capturing local variables
- `foreach` over any type that does not expose a concrete struct `GetEnumerator()` at the call site — including `List<T>` or `Dictionary<K,V>` via an interface variable (both `List<T>.Enumerator` and `Dictionary.Enumerator` are structs, but both are boxed when the collection variable is typed as an interface); use arrays or `Span<T>` for hot-path iteration
- Reflection

**Banned language features in game-loop and game-state code (FR-CS-010):**
- `dynamic` — bypasses compile-time type safety; introduces non-deterministic dispatch paths
- `async`/`await` in game-loop / game-state-modifying code — breaks deterministic tick ordering; continuations resume on unpredictable frames. Permitted in initialization code, editor tooling, and loading pipelines that do not touch game state.
- `unsafe` without lead-developer sign-off recorded in the PR description
- `try`/`catch` inside per-frame inner loops (FR-CS-069)
- Virtual method calls inside per-frame inner loops (FR-CS-068)

**Banned architectural patterns in game-state assemblies (FR-CS-051–054):**
- **Service locator** (`ServiceLocator.Get<T>()`) — hides dependencies; breaks deterministic testing
- **Ambient context** (`MatchContext.Current`) — hidden state; breaks replay rewind
- **Static mutable singleton** — cannot be reset between deterministic replay ticks
- **Generic DI container on the hot path** (Zenject, VContainer, `Microsoft.Extensions.DependencyInjection`) — reflection-based; allocates; violates zero-alloc budget

The required alternative to all four is **constructor injection**: pass dependencies as constructor parameters.

The `ProfilerMarker` field is `private static readonly`, named per the
`s_<EntryPointName>Marker` convention (see "Profiler Markers" section).

```csharp
// COMPLIANT — sealed instance class; dependencies injected via constructor per FR-CS-051–054
// Note: `state with { … }` requires C# 10+ on readonly structs. Verify the
// Unity LTS + backend in certification-platform.md before using this pattern.
public sealed class BallPhysicsSystem
{
    private readonly MatchClock _clock;
    private static readonly ProfilerMarker s_updateMarker =
        new ProfilerMarker("BallPhysics.Update");

    public BallPhysicsSystem(MatchClock clock)
    {
        _clock = clock;
    }

    public void Update(ref BallState state, float dt)
    {
        using var _ = s_updateMarker.Auto();
        state = state with { Velocity = state.Velocity * (1f - BallPhysicsConstants.DRAG_COEFFICIENT * dt) };
    }
}

// VIOLATION — copies BallState by value; wastes memory bandwidth
public void Update(BallState state, float dt) { … }
```

---

## DETERMINISM RULES

No `System.Random`, no `DateTime.Now`, no `Guid.NewGuid()`, no `Task.Run` or `Parallel.*`, no hardware-intrinsic FMA in game logic (FR-CS-036–040).

| Need | Use | Owning assembly |
|---|---|---|
| Random numbers | `SplitMix64` helper (FR-CS-041) | `deterministic-sim/` (Spec #16) |
| Simulation time | `MatchClock` (injected) (FR-CS-042) | `deterministic-sim/` (Spec #16) |
| Trigonometry / math | Project math helper (FR-CS-043) | `project-constants/` — exact class TBD at Stage 1 |
| Deterministic IDs | Pre-allocated deterministic ID ranges (Spec #16 §3.2.5) | `deterministic-sim/` (Spec #16) |

**Hardware-intrinsic FMA (FR-CS-040):** Fused multiply-add instructions can produce different results from separate multiply + add on different hardware or compiler versions. FMA intrinsics are banned unless the platform is pinned and the lead developer has signed off.

**64-bit multiplication** must use `unchecked { }` with a `// Spec #16 §3.4.4` comment
(FR-CS-044), regardless of which assembly the code lives in. The citation always refers
to Spec #16 §3.4.4 (SplitMix64 state update) — not the local spec's §3.4.4:

```csharp
unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
{
    state += 0x9E3779B97F4A7C15UL;
}
```

**Python tooling** that mirrors C# SplitMix64 constants (FR-CS-045): omit the `UL` suffix and mask intermediates with `& 0xFFFFFFFFFFFFFFFF`. Do not mix `unchecked` into Python (it has no meaning there) or mask operators into C# (that would introduce a different semantic).

---

## NUMERIC TYPES

- `float` everywhere at Stage 0 (FR-CS-071).
- `double` is banned by default; override requires lead-developer sign-off and inline comment.
- `decimal` is always banned.
- Fixed64 migration is a Stage 5+ concern (Spec #9).

---

## INTERFACE DESIGN

Write an interface only when both the producer and consumer are specified. No phantom interfaces for unspecified systems (ERR-001, ERR-004, FR-CS-048/049).

An `interface` file MUST reside in the same assembly as at least one of its specified consumers (FR-CS-048). Access modifier is `public` only if callers cross the assembly boundary; `internal` otherwise (FR-CS-015).

**Event-vs-interface decision tree (FR-CS-050):**
- Same assembly → direct method call
- Cross-assembly, consumer not yet specified → wait; create nothing
- Cross-assembly, consumer specified, multiple implementations → interface (in consumer's assembly)
- Cross-assembly, single implementation, lower→higher layer notification → struct event on event bus
- Cross-assembly, single implementation, same or downward layer → direct method call

---

## FILE HEADER (REQUIRED ON EVERY FILE)

```csharp
// File:     src/ball-physics/BallPhysicsCore.cs
// Created:  2026-05-19
// Modified: 2026-05-19
// Author:   <name or handle>
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Implements core ball physics calculations (gravity, drag, Magnus effect).
//           Does not manage state; all state is passed by ref parameter.

namespace TacticalDirector.BallPhysics
{
    // …
}

#region VersionHistory
// | Version | Date       | Author           | Notes                   |
// | 1.0     | 2026-05-19 | <name or handle> | Initial implementation. |
#endregion
```

**Required fields (FR-CS-056/057):** file path (relative to repo root), created date (ISO), modified date (must match latest version-history row), author, governing specs, purpose (≤ 2 sentences).

Version history lives at the end of the file; rows are appended, never deleted.

When a file is authored or modified by an automated agent with no named individual, use `—` in the Author field.

---

## XML DOC COMMENTS

Every `public` type, method, property, and event requires `/// <summary>`. Every constant (any access level) requires `/// <summary>` that includes its tag (FR-CS-060/061).

```csharp
/// <summary>[FIXED] Ball radius in metres. Ball Physics Spec #1 §2.1.</summary>
public const float BALL_RADIUS = 0.11f;

/// <summary>Applies drag to ball velocity for one physics step.</summary>
/// <param name="velocity">Current velocity vector (m/s).</param>
/// <param name="dt">Time delta in seconds.</param>
public static Vector3 CalculateDrag(Vector3 velocity, float dt) { … }
```

---

## INLINE COMMENTS

Write a comment only when the **why** is non-obvious. Do not comment what the code already says (FR-CS-064).

```csharp
// COMPLIANT — hidden constraint
unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
{ … }

// VIOLATION — states the obvious
int count = agentList.Count;  // Get the number of agents
```

**Commented-out code is prohibited** in any commit to a shared branch (FR-CS-065). Delete disabled code; version control preserves the history.

---

## `using` DIRECTIVE ORDER

System → Unity → Project, each group separated by a blank line (FR-CS-006):

```csharp
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;

using TacticalDirector.BallPhysics;
using TacticalDirector.EventSystem;
```

Alphabetical within each group is recommended but not enforced.

---

## PROFILER MARKERS

Every system entry point (`Update`, `Tick`, `RunStep`, or similarly named method) must be wrapped in a `ProfilerMarker.Auto()`. The marker is a `private static readonly` field (allocated once at startup — zero per-frame cost) (FR-CS-070).

> **Note:** These are custom methods on game system classes — **not** Unity MonoBehaviour
> lifecycle callbacks (`FixedUpdate()` / `Update()` with no parameters). The MonoBehaviour
> / PlayerLoop integration layer is a Stage 1 concern; see "WHAT IS NOT HERE YET" below.

**Field naming convention:** `s_<EntryPointName>Marker` — e.g., `s_updateMarker` for `Update`, `s_runTickMarker` for `RunTick`.

**Marker string format:** `<SpecName>.<MethodName>` (e.g., `"BallPhysics.Update"`, `"DeterministicSim.RunTick"`).

```csharp
using UnityEngine.Profiling;

// Profiler-relevant fields shown; constructor and injected dependencies
// follow the same pattern as the Game-Loop Rules COMPLIANT example above.
public sealed class BallPhysicsSystem
{
    private static readonly ProfilerMarker s_updateMarker =
        new ProfilerMarker("BallPhysics.Update");

    public void Update(ref BallState state, float dt)
    {
        using var _ = s_updateMarker.Auto();
        // …
    }
}
```

---

## STAGE 0 VERIFICATION

No static analysis tooling yet. Verify each file manually against the Spec #20 §5.4 checklist before marking it complete. Roslyn analyzers, `BannedSymbols.txt`, and `.editorconfig` activate at Stage 1 once `certification-platform.md` is fully pinned.

---

## WHAT IS NOT HERE YET

These items are deferred pending Unity project setup and platform pinning:

| Item | Blocked on |
|---|---|
| `.asmdef` content (GUIDs, `allowUnsafeCode`, `autoReferenced`, `testPlatforms`, `versionDefines`) | Unity project initialization |
| Exact Unity LTS revision | `docs/tracking/certification-platform.md` pinned |
| `dotnet test` framework args | Stage 0+1 setup (Spec #19 §7.5 D2 — framework pin deferred to Stage 0+1) |
| Unity batch-mode CI commands | Unity project initialization |
| `.editorconfig` path and contents | Stage 1 setup |
| C# language version pin | `certification-platform.md` pinned |
| `[GT]` config loader class / method | Stage 1 setup — define in this file when resolved; update all `// TODO: replace with config loader` constants |
| Project math helper class name / assembly | Stage 1 setup — update determinism table when defined |
| MonoBehaviour / PlayerLoop integration pattern | Unity project initialization — how Unity's lifecycle loop calls into struct-based game systems; until defined, system entry points are pure C# instance methods named `Update`, `Tick`, or similar |
| `AgentState` as `readonly struct` + `with` expressions | C# language version pin in `certification-platform.md` — `with` on `readonly struct` requires C# 10+. Until pinned, `AgentState` (and equivalent game-state structs) are mutable structs mutated by `ref` parameter; migration to readonly + with is a Stage 0+1 cleanup task once the language version is locked. |

Update this file when those items are resolved.

---

## VERSION HISTORY

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-05-19 | — | Initial creation. All 20 Stage 0 specs approved; coding begins. |
| 1.1 | 2026-05-19 | — | Adversarial review v1.0 fix pass. H-1: layer taxonomy rebuilt from §3.5.2. H-2/H-3: dependency arrows corrected. H-4: Author and Purpose added to file header template. M-1: FMA ban added. M-2: dynamic/async/unsafe bans added. M-3: four architectural anti-patterns added. M-4: phantom TacticalDirector.Shared replaced. M-5: [CROSS] naming contradiction flagged. M-6: Spec #19 blocker resolved to §7.5 D2. L-1: style section added (indentation, Allman braces). L-2: project-constants.asmdef added to tree. L-3: commented-out code ban added. L-4: [EST] spec-error-log requirement added. L-5: var policy added. |
| 1.2 | 2026-05-19 | — | Adversarial review v1.1 fix pass (2H · 7M · 8L). H-1: arrow label corrected to "is referenced by." H-2: ConfigLoader fabrication removed; [GT] loading noted as Stage 1 TBD. M-1: s_fixedUpdateMarker declaration added to game-loop example; field naming convention added. M-2: [CROSS] mirror RHS corrected to ProjectConstants.PHYSICS_TICK_HZ (ALL_CAPS). M-3: tree comment for ProjectConstants.cs: wrong tag and scope fixed. M-4: single vs multi-consumer [CROSS] routing rule documented. M-5: C# 10+ note added to `with {}` example. M-6: infrastructure assembly table added to taxonomy section. M-7: .asmdef coverage note added under tree. L-1: Last Updated header field added. L-2: ProfilerMarker field naming rule added. L-3: `using UnityEngine.Profiling;` added to profiler example. L-4: var policy semicolon fixed. L-5: owning assembly column added to determinism table. L-6: BallCollision.cs vs collision-system/ note added to tree. L-7: [CROSS] XML doc updated to cite spec+section. L-8: foreach ban reworded for technical accuracy. |
| 1.3 | 2026-05-19 | — | Adversarial review v1.2 fix pass (2H · 5M · 4L). H-1: project-constants diagram line fixed; removed broken ← arrow (RHS was prose). H-2: // §3.4.4 → // Spec #16 §3.4.4 in Determinism Rules and Inline Comments sections. M-1: Physics→AI prohibition rewritten in prose (inconsistent arrow direction). M-2: async/await entry scoped to "game-loop / game-state-modifying"; heading updated to match. M-3: tests/ .asmdef entries added to all five expanded spec folders; .asmdef coverage note extended with test-assembly rule. M-4: foreach parenthetical covers both List<T>.Enumerator and Dictionary.Enumerator. M-5: [GT] region comment updated to match actual code pattern (= 8; // TODO:). L-1: — author placeholder documented in File Header section. L-2: .asmdef deferral entry expanded to all unresolved fields. L-3: DI bullet in required-patterns replaced with cross-reference to Banned Architectural Patterns section. L-4: ProfilerMarker naming comment moved outside game-loop code block. |
| 1.4 | 2026-05-22 | — | Adversarial review v1.3 fix pass (1H · 4M · 3L). H-1: Game-Loop COMPLIANT example rewritten as sealed instance class (public void); VIOLATION updated to match. M-1: [EST] promotion targets extended to [GT] / [FIXED] / [DERIVED] / [CROSS] with guidance for each path. M-2: Profiler Markers entry-point list changed from FixedUpdate/Update to Update/Tick/RunStep; MonoBehaviour-not-applicable note added; examples updated (FixedUpdate → Update, s_fixedUpdateMarker → s_updateMarker); WHAT IS NOT HERE YET row added for MonoBehaviour/PlayerLoop integration. M-3: Naming discrepancy note updated with ERR-020-001 reference and confirmation that §4.2 has been patched. M-4: stackalloc Span<T> vs pointer distinction added. L-1: §3.2 → §3.2.3 in [GT] XML doc. L-2: [DERIVED] worked example added; region comment shows formula instead of ellipsis. L-3: #region name convention (Title Case vs acronym) documented. |
| 1.5 | 2026-05-22 | — | Adversarial review v1.4 fix pass (1H · 1M · 5L). H-1+M-1 (combined): Game-Loop COMPLIANT example rewritten to show constructor injection (_clock field + constructor body); method renamed Update, field renamed s_updateMarker, profiler string "BallPhysics.Update"; VIOLATION moved inside class as commented-out method. L-1: "two-letter acronyms" → "all-caps abbreviations" (EST has 3 letters). L-2: VIOLATION was orphaned outside class at file scope (invalid C#); now inside BallPhysicsSystem as commented-out member. L-3: Root CLAUDE.md "Heartbeat Tick Rate" removed from [CROSS] XML doc example (non-spec citation); Ball Physics #1 §1.2 alone is sufficient. L-4: ProfilerMarker required-patterns bullet rewritten to distinguish the field declaration (one-time alloc) from the .Auto() call at entry points. L-5: Single-consumer [CROSS] mirror example added alongside multi-consumer example. |
| 1.6 | 2026-05-22 | — | Adversarial review v1.5 fix pass (0H · 1M · 2L). M-1: Profiler Markers BallPhysicsSystem example gained a note "Profiler-relevant fields shown; constructor and injected dependencies follow Game-Loop Rules COMPLIANT example." L-1: commented-out VIOLATION removed from inside COMPLIANT class body (violated FR-CS-065); restored as standalone labeled snippet outside the class. L-2: private static field naming convention (s_camelCase) added to NAMING CONVENTIONS table. |
| 1.7 | 2026-05-25 | — | Agent Movement adversarial review fix pass (M-A / L-A). M-A: agent-movement/ tree expanded to all 13 implemented files with role annotations. L-A: readonly struct deferral row added to WHAT IS NOT HERE YET; explains why AgentState is mutable pending C# version pin. |
| 1.8 | 2026-05-25 | — | Pass-4 follow-up. Constants tree: PlayerAttributeConstants added to AgentMovementConstants.cs annotation (8 classes). |
| 1.9 | 2026-05-25 | — | Tracking-doc sync. ball-physics/ tree: `BallStateSystem.cs` removed (never implemented); `BallStateMachineTests.cs` added (exists at `src/Core/Physics/Ball/Tests/`); structural deviation warning added (actual path is `src/Core/Physics/Ball/`, not `src/ball-physics/`). |
| 1.10 | 2026-05-27 | — | Tree expanded for Collision System (#3) 18 files with role annotations; First Touch (#4) 15 files with role annotations; Pass Mechanics (#5) 22 files with role annotations. agent-movement/ tests updated: `AgentMovementTests.cs` added (from AR-2/AR-3 pass May 27). |
| 1.11 | 2026-05-27 | — | AR-1 pass-mechanics adversarial review fix pass. L-2: corrected stale enum value descriptions for PassType.cs / CrossSubType.cs / SpinType.cs / PassOutcome.cs. M-3: PassEvents.cs renamed to CancelReason.cs in tree. |
| 1.12 | 2026-05-28 | — | Shot Mechanics (#6) tree expanded: 27 files + Tests/NaNVelocityStub.cs. AR-1 fix pass applied (H-1: BodyMechanicsResult extracted; H-2: GoalGeometry extracted; H-3: underscore GT constants renamed to PascalCase; M-1: unused AdvanceWindup param removed; M-2/M-3/M-4: magic literals promoted to constants). |
| 1.13 | 2026-05-28 | — | Heading Mechanics (#10) tree expanded: 25 files with role annotations (heading-mechanics.asmdef + 23 .cs files). |
| 1.14 | 2026-05-28 | — | Goalkeeper Mechanics (#11) tree expanded: 36 files with role annotations (goalkeeper-mechanics.asmdef + 35 .cs files). AR-1 (5H+1M) + AR-2 (2M) review cycles completed; all findings fixed. |
| 1.15 | 2026-05-29 | — | Perception System (#7) tree expanded: 14 files with role annotations (perception-system.asmdef + 13 .cs files). AR-1 (3M+3L) review cycle completed; all findings fixed. |
| 1.16 | 2026-05-29 | — | Decision Tree (#8) tree expanded: 36 files with role annotations (35 .cs files + 1 asmdef). AR-1 (2H+3M+4L) review cycle completed; all findings fixed: H-1 AssemblyInfo.cs InternalsVisibleTo; H-2 SplitMix64 unchecked{}; H-3 DecisionContextAssembler possession classification bug; M-1 ScoreMove possession source; M-2 OptionGenerator using directive; M-3 DtAgentAttributes.Stamina doc; L-1 magic numbers → named constants; L-2 AttributeNormMin XML doc; L-3 UTILITY_FLOOR/CEILING XML docs; L-4 CrossSubType tautological ternary. AR-2 full sweep: no new findings. |
| 1.17 | 2026-05-29 | — | Positioning AI (#12) tree expanded: 20 files (19 .cs + 1 asmdef) with role annotations. AR-1 (2H+4M+1L) + AR-2 (2M+3L) + AR-3 clean review cycles completed. Key fixes: H-1 entityIdArr zero-alloc (SlotComposer parameter + PositioningAITick field); H-2 squad-size validation; M-1 LANE_DWELL_TICKS constant; M-2 dead _entityIdMap removed; M-3 LaneEdgesM literals → PITCH_WIDTH_M fractions; M-4 dead `slots` variable in ShapeAnalyzer; VersionHistory regions all 20 files. |
| 1.18 | 2026-05-29 | — | Pressing AI (#13) tree expanded: 21 files (20 .cs + 1 asmdef) with role annotations. AR-1 (3H+1M+1L) + AR-2 clean review cycles completed. Key fixes: H-1 IsActive field added to PressingAgentSnapshot + guards in 5 files (TriggerEvaluator, PrimaryPressSelector, CoverShadowSelector, InvariantEnforcer, PressingAITick); H-2 unit mismatch in TriggerEvaluator.EvaluateBackwardPass (len→len*len vs SpacingEpsilonM2); H-3 PrimaryPressSelector eligibility uses carrier position not interception point; M-1 PressingAITick missing using TacticalDirector.PositioningAI; L-1 PressTrigger stale doc comment UpdateDebounce→Evaluate. |
| 1.19 | 2026-05-29 | — | Defensive AI (#14) tree expanded: 19 files (18 .cs + 1 asmdef) with role annotations. AR-1 (2H+1M) + AR-2 clean review cycles completed. Key fixes: H-1 DefensiveAIConstants.SQUAD_SIZE corrected from literal 22 to mirror PressingAIConstants.SQUAD_SIZE (true [CROSS] reference); H-2 MarkAssigner.Assign now refreshes ValidThroughTick during PreCheck dwell lock (external consumers saw stale tick on retained assignments); M-1 LastManDetector.Evaluate guards GkEntityId < 0 before GK-zone distance check (Vector2.zero gave false COVER_GK_ZONE trigger for x=105-defending teams with no GK). |
| 1.20 | 2026-05-29 | — | Attacking AI (#15) tree expanded: 24 files (23 .cs + 1 asmdef) with role annotations. AR-1 (2H+4M) + AR-2 (0H+0M+2L) + AR-3 (1L) review cycles completed; AR-3 clean. Key fixes: H-1 MinEffectiveRadiusM moved from SupportHeuristic local const to AttackingAIConstants catalogue (FR-CS-016); H-2 magic literals 5.0/40.0/±34.0 in GenerateRunParams promoted to MinRunDepthM/MaxRunDepthM/MaxLateralOffsetM constants; M-1 AttackHysteresis.Update resets CandidateDwell when current role re-preferred (prevented premature transitions on interrupted evaluation windows); M-2 WidthHolder promotion loop now skips near-side WeakSide agents (already counted, must not re-promote); M-3 Math.Round(double) → Mathf.RoundToInt in GenerateRunParams; M-4 dead firstLossThisTick branch removed from AttackingAITick; L-1/L-2 doc updates; AR-3 L-1 AttackAngleEpsilon (0.01f) extracted to catalogue. |
| 1.21 | 2026-05-29 | — | Deterministic Simulation (#16) tree expanded: 23 files (21 .cs + 2 asmdef) with role annotations. AR-1 (4H+4M) + AR-2 (1L) + AR-3 (1L) review cycles completed; AR-3 clean. Key fixes: H-1/H-2 FloatUintUnion explicit-layout struct in CanonicalSerializer (zero-alloc SingleToUInt32Bits/UInt32BitsToSingle); H-3 stackalloc Span<byte>[21] in DeterministicRngService.ComputeDrawValue + SipHash24_64 ReadOnlySpan<byte> signature; H-4 PhaseId enum corrected (AI_NoOp ordinal removed; Events=5 added; Physics=3, Resolve=4, Snapshot=6); M-1 AI_PHASE_STRIDE const→static readonly; M-2 File.Move(overwrite:true) in SaveManager; M-3 one-canonical-NaN→SoftDrift in DivergenceDetector; AR-2 L-1 T-DS-FAULT-014 comment "phase 3 (AI)"→"phase 3 (Physics)"; AR-3 L-1 empty for-loop → comment stub in ReplayEngine step 6. |
| 1.22 | 2026-05-30 | — | Event System (#17) tree expanded: 20 files (18 .cs + 2 asmdef) with role annotations. AR-1 (3H+3M+2L) + AR-2 (1L) + AR-3 clean review cycles completed. Key fixes: H-1 drop predicate corrected `>` → `>=` maxPerTick (off-by-one allowed MaxPerTick+1 publishes per tick); H-2 HandlerSecondaryPublishCount reset moved inside per-handler loop (was per-slot, violating FR-EVT-046a per-invocation semantics); H-3 InDrainDispatch flag wrapped in try/finally (handler exception left flag stuck true corrupting subsequent ticks); M-1 AddHandler bounds check added (0x1701 overflow error); M-2 debug phase assertion added (#if UNITY_EDITOR\|\|DEVELOPMENT_BUILD in EventBus.Publish<IEventA>); AR-2 L-1 AddHandler error code corrected to 0x1701 (was 0x1705); L-1 CardIssuedEvent.FoulOrdinal doc corrected (byte cannot hold -1; now says 0xFF); L-2 TickHeartbeatEvent comment corrected (CLR min size 1 byte, not zero). |
| 1.23 | 2026-05-30 | — | Stage 1 event-bus wiring + ball-physics relocation + performance-optimization scaffold. (1) EventBus wired in 6 specs: PassAttemptEvent/PassCancelledEvent (Tier A 0x0C/0x0D), ShotExecutedEvent/ShotCancelledEvent (Tier A 0x01/0x0E), ShotAnimationData (Tier C 0x0F), PerceptionRefreshEvent (Tier C 0x10), DecisionMadeEvent (Tier C 0x11), HeaderExecutedEvent (Tier B 0x12), HeaderAttemptFailedEvent (Tier C 0x13), SaveAttemptedEvent/BallClaimedEvent/DistributionExecutedEvent (Tier A 0x14–0x16), GoalkeeperRushEvent (Tier C 0x17); IEventA/B/C marker interfaces added to all structs; Tier A/B structs gained 12-byte header fields per §3.4 layout. (2) 6 EventBusStub.cs files replaced no-op with 3-tier generic EventBus.Publish forwarding (decision-tree keeps internal single-sig overload). (3) 6 EventBusRegistrar.cs files added with boot-time RegisterExternalRow<T>() calls. (4) EventRegistry.cs 0x0C–0x17 placeholder rows added. (5) 4 .asmdef files gained TacticalDirector.EventSystem reference (heading-mechanics, goalkeeper-mechanics, perception-system, decision-tree). (6) ball-physics/ relocated from src/Core/Physics/Ball/ to src/ball-physics/ via git mv; ball-physics.asmdef + ball-physics-tests.asmdef added; deviation warning removed. (7) performance-optimization/ created: performance-optimization.asmdef (autoReferenced false), HotPathAllocExemptAttribute.cs, TraceChannel.cs (F.0 schema + Stage 0 anchor rows), PerformanceOptimizationConstants.cs. Tree annotations + EventBusStub/EventBusRegistrar annotations updated throughout. |
| 1.24 | 2026-06-01 | — | Performance Optimization (#18) scaffold promoted to real implementation. (1) asmdef gained TacticalDirector.DeterministicSim reference (SessionManifest uses EnvironmentFingerprint #16 §4.8). (2) PerformanceOptimizationConstants.cs v1.1: Fixed region LOOP_TAG_TACTICAL_10HZ + LOOP_TAG_PHYSICS_60HZ added (§3.2.2); GT region PromotionToleranceFraction + ReproducibilityToleranceFraction added (§3.9.1 / FR-PO-067); EST region SamplerDefaultHz + StatisticalSignificanceN + FirstTickWarmupCount added (§3.3.4 / §3.4.3 / §3.9.4); region order corrected to Fixed→GT→EST. (3) 13 new C# files: LoopTag / BaselinePassFail / HardwareCounterSnapshot / SessionManifest / BaselineRecord / BudgetRollupEntry / HotPathEntry / IPerfHarness / IBudgetSource / RegressionResult / RegressionGate / ReproducibilityResult / BaselineReproducibilityAuditor. (4) tools/run-perf-local.sh (FR-PO-070 Stage 0 runbook / Appendix E). (5) tools/budget-auditor.py (§5.3 schema-conformance + §5.5 loop-tag auditor; FR-PO-070). (6) tools/select-seed.py (KD-6 seed selection; Stage 0 fixed seed; Stage 0+1 SHA-derived). (7) tools/perf-harness/run.sh + tools/perf-harness/scenarios/anchor-baseline.manifest.json (§4.1 Stage 0 anchor scenario). (8) docs/specs/performance-optimization/baselines/.gitkeep (Appendix A §A.3 Stage 0 baseline storage). |
| 1.33 | 2026-06-02 | — | Testing Strategy (#19) PR #132 Codex P2 follow-up: `PerfGateRunner.Run` now rejects mismatched perf-baseline pairs via `ArgumentException` before delegating to `RegressionGate.Evaluate`. FR-PO-031 defines the +5% gate only for the same scenario, seed, platform pin, and loop; `RegressionGate.Evaluate` compares only `P50Ms` and would silently green-light a meaningless pair. Run validates `baseline.Loop == current.Loop` unconditionally and `ScenarioManifestId` / `Seed` / `PlatformPin` when both records carry a non-null `SessionManifest` (preserves AR-1 H-1 missing-manifest tolerance). Mismatches throw `ArgumentException` naming the field, the baseline value, the current value, and the FR-PO-031 binding. Files: PerfGateRunner.cs v1.2. |
| 1.39 | 2026-06-03 | — | Ball Physics (#1) AR-6 fix pass: 0M + 4L all fixed against the merged AR-5 state (post-PR #134). Lands on a fresh branch — PR #134 was already merged into main before AR-6 fixes were authored. L-1: `BodyPart` enum XML doc gains the ORDINAL STABILITY paragraph, completing the full-surface sweep (AR-3 L-2 covered `BallEventType`; AR-4 L-1 propagated to `BallStateType` / `SurfaceType` / `RestartType`; AR-5 L-1 added `KickResult`; `BodyPart` was the last public enum without the paragraph). `BodyPart` is cross-spec embedded in `collision-system/AgentBallCollisionData.BodyPart` so the insertion-shifts-ordinals hazard applies. L-2: `BallPhysicsCore.cs` file-header `Modified` annotation refreshed `"(AR-4 fix pass)"` → `"(AR-6 fix pass)"` so a reader scanning headers for the latest-pass anchor lands on the correct review row; date unchanged. L-3: new `EnumOrdinalStabilityTests.cs` mechanically enforces the APPEND-only contract on all six public enums via direct `Assert.AreEqual(N, (int)EnumMember)` assertions (6 tests, one per enum). Converts the AR-3 L-2 / AR-4 L-1 / AR-5 L-1 / AR-6 L-1 documentation into enforced contracts — any future maintainer who inserts a value in the middle of an enum fails the suite immediately. L-4: `BallEventLogger.cs` HISTORICAL NOTE (AR-5 L-3) rephrased — Stage 0 never persisted event logs, so the original "pre-AR-2 serialised event streams are NOT compatible" wrongly implied any existed; reworded conditionally ("if any … had existed it would NOT be compatible"). Files: BodyPart.cs v1.1, BallPhysicsCore.cs v1.3.2, BallEventLogger.cs v1.6; new EnumOrdinalStabilityTests.cs v1.0. Tree row added for the new test file. |
| 1.38 | 2026-06-03 | — | Ball Physics (#1) AR-5 fix pass: 1M + 4L all fixed against the AR-4 v1.3/v1.4/v1.6 state. M-1: file header `Modified` field added to 4 files that lacked it (FR-CS-056): `BodyPart.cs` and `KickResult.cs` (created in AR-2 L-2 split, missed by AR-3); `BodyPartCoefficientsTests.cs` and `SurfacePropertiesTests.cs` (created in AR-4 L-2, same shortcoming). For brand-new files `Modified == Created`. L-1: `KickResult` enum XML doc gains the ORDINAL STABILITY paragraph parallel to AR-3 L-2 / AR-4 L-1 on the four sibling public enums; though KickResult is currently a method-return-only value with no struct embedding, Stage 1+ kick-outcome logs will inherit the insertion-shifts-ordinals hazard so the APPEND-only rule applies pre-emptively. L-2: new `Get_EveryDeclaredBodyPart_HasCatalogueEntry` test in `BodyPartCoefficientsTests` and `EveryDeclaredSurfaceType_HasEntryInAllFourLookups` test in `SurfacePropertiesTests` iterate `Enum.GetValues(typeof(BodyPart))` / `typeof(SurfaceType)` so a future enum extension without a matching catalogue row (or, for SurfaceType, without atomic updates to all four switch arms) fails the test suite immediately — the hardcoded round-trip tests above would silently miss the gap. L-3: `BallEventType` XML doc gains a HISTORICAL NOTE clarifying that the AR-2 L-3 drop of four unused members (`Header` / `Deflection` / `OutOfPlay` / `PossessionChange`) RENUMBERED the remaining ordinals (Bounce 3→2, GoalPostHit 5→3, Goal 7→4) before the AR-3 L-2 APPEND-only rule was established; Stage 0 has no persisted log so the renumbering had no consumer impact, but pre-AR-2 serialised event streams are NOT compatible with the current ordinals. L-4: `BallPhysicsCore.cs` version history gains a 1.3.1 doc-only clarification note recording that the NaN-recovery `LogError` emit is a plain string literal (no `$"…"` interpolation), so its AR-4 M-1 gating was for surface symmetry with the three sibling clamp emits, not direct FR-CS-031 necessity. Files: BodyPart.cs v1.0.1, KickResult.cs v1.1, BallEventLogger.cs v1.5, BallPhysicsCore.cs v1.3.1, BodyPartCoefficientsTests.cs v1.1, SurfacePropertiesTests.cs v1.1. |
| 1.37 | 2026-06-03 | — | Ball Physics (#1) AR-4 fix pass: 1M + 4L all fixed against the AR-3 v1.4/v1.5 state. M-1: the four `Debug.LogError` / `Debug.LogWarning` emit blocks in `BallPhysicsCore.ValidatePhysicsState` (NaN-recovery, velocity clamp, spin clamp, height clamp) gated behind `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` — same FR-CS-031 carve-out pattern AR-3 L-3 applied to `BallCollision.ApplyKick`. ValidatePhysicsState runs at 60 Hz inside `UpdateBallPhysics` so the gating concern is stronger here than for once-per-kick emits; AR-3 only finished half the job and the AR-4 fix restores symmetry. L-1: ordinal-stability XML doc paragraphs added to `BallStateType` / `SurfaceType` / `RestartType` parallel to the AR-3 L-2 paragraph on `BallEventType` — all four enums are embedded in serialised data (`BallState.State`, `BallEvent.ResultingState` / `BallEvent.Surface`, `BallCollision.CheckBoundaries` return, persisted match config) and share the same insertion-shifts-ordinals hazard for replay / save / analytics consumers + FR-DS-009 digest compatibility. L-2: two new test files lock the AR-1 L-4 throw-on-unknown contracts plus the AR-3 M-1 catalogue round-trips. `BodyPartCoefficientsTests.cs` covers `Get(BodyPart)` known-value round-trip across all 6 body parts (asserting the catalogue value reaches the consumer) + `(BodyPart)100` cast-from-int throws `ArgumentOutOfRangeException`. `SurfacePropertiesTests.cs` covers all four `Get*(SurfaceType)` methods with 5-known-surface round-trips + `(SurfaceType)100` throw branches — 8 tests total. L-3: `BodyPartCoefficients.s_coefficients` field retyped `Dictionary<…>` → `IReadOnlyDictionary<…>` (backed by the same Dictionary instance) so future code inside the class cannot mutate the lookup contents through the field reference; matches the AR-2 / Testing Strategy `IReadOnlyList<T>` pattern. L-4: `BodyPartRetention` constant XML docs use "factor" instead of "multiplier" for terminology consistency with the consuming `BodyPartCoefficients` class (which uses retention / coefficient / factor vocabulary). Files: BallPhysicsCore.cs v1.3, BallState.cs v1.4, SurfaceProperties.cs v1.4, RestartType.cs v1.1, BodyPartCoefficients.cs v1.2, BallPhysicsConstants.cs v1.6; new BodyPartCoefficientsTests.cs v1.0, SurfacePropertiesTests.cs v1.0. Tree updated with the 2 new test files. |
| 1.36 | 2026-06-03 | — | Ball Physics (#1) AR-3 fix pass: 1M + 5L all fixed against the AR-2 v1.3/v1.4 state. M-1: 12 per-body-part `(speedRetention, spinRetention)` literals inside `BodyPartCoefficients.cs` (which AR-2 L-2 lifted verbatim out of `BallCollision.cs`) replaced with references to a new `BallPhysicsConstants.BodyPartRetention` nested class catalogue (`FootSpeed`/`FootSpin`/`ShinSpeed`/`ShinSpin`/…/`ArmSpeed`/`ArmSpin`, 12 `[GT]` constants tagged for the Stage 1 config-loader migration) — FR-CS-016 ("No magic numbers anywhere else in the ball-physics assembly") is now satisfied. L-1: the AR-2 L-1 inline `//` comments at the home-goal and away-goal branches of `BallCollision.CheckBoundaries` carried `&lt;` / `&gt;` XML entity escapes left over from a draft XML doc; replaced with raw `<` / `>` (the comments are plain source, not XML). L-2: `BallEventType` XML doc gains an explicit ORDINAL STABILITY paragraph instructing future maintainers to APPEND new members at the end of the enum — inserting in the middle shifts ordinals 2/3/4 and breaks both Stage 1+ analytics pipelines and FR-DS-009 digest compatibility on any serialised event stream. L-3: the four `Debug.LogError` / `Debug.LogWarning` emit blocks in `BallCollision.ApplyKick` gated behind `#if UNITY_EDITOR || DEVELOPMENT_BUILD` (mirrors the event-system EventBus debug-assertion pattern); the functional rejection return `KickResult.RejectedNonFiniteVelocity` and the velocity/spin clamping stay outside the gates, so production builds emit no `$"…"` interpolation per FR-CS-031 while editor and development builds keep the diagnostic surface. L-4: parallel `Debug.LogWarning` added for spin clamping in `ApplyKick` so the two symmetric clamp operations (velocity at `Limits.MaxVelocity`, spin at `Limits.MaxSpin`) now have symmetric editor-gated diagnostics. L-5: root `CLAUDE.md` OPEN ISSUES gains a new entry `Possession.ControlHeight ↔ GroundControlHeight cross-spec routing` (since 2026-06-03) tracking the deferred Spec #20 §4.2 routing decision; `Possession.ControlHeight` XML doc back-references the entry by title so the deferral has a discoverable anchor. Files: BallPhysicsConstants.cs v1.5, BodyPartCoefficients.cs v1.1, BallCollision.cs v1.4, BallEventLogger.cs v1.4; root `CLAUDE.md` OPEN ISSUES entry added. |
| 1.35 | 2026-06-03 | — | Ball Physics (#1) AR-2 fix pass: 2M + 5L all fixed against the AR-1 v1.2/v1.3 state. M-1: `BallEventLogger._lastSnapshotTime` initialised to `float.NegativeInfinity` instead of the `NeverSnapshotted = -1f` sentinel + float-equality branch — `matchTime − NegativeInfinity == +Infinity ≥ SnapshotInterval` for any finite interval, so the first snapshot always emits without the explicit equality test, and the magic literal + named const both vanish. Survives future `SnapshotInterval` tuning that would have broken the `-1f` sentinel (any interval > 1.0 s suppressed the first call). M-2: `LogAssert.Expect` added to `BallPhysicsCoreTests.Validation_DetectsNaN_AndRecovers` and `Validation_DetectsInfinity_AndRecovers` so the `Debug.LogError("[BallPhysics] NaN/Infinity detected …")` emitted by `ValidatePhysicsState`'s recovery path does not fail the Unity NUnit runner (parallels the AR-1 H-1 follow-on already applied on the two integration-test counterparts). L-1: `BallCollision.IsInHomeGoal` / `IsInAwayGoal` zero-information wrappers folded — `CheckBoundaries` now calls `IsBetweenPostsUnderCrossbar` directly with `// Home goal` / `// Away goal` inline comments at the call sites; the two redundant one-line indirections introduced by AR-1 M-3 are gone. L-2: `BallCollision.cs` (5 public types) split into single-type files per `src/CLAUDE.md` FILE NAMING — `BodyPart.cs` / `RestartType.cs` / `KickResult.cs` / `BodyPartCoefficients.cs` / `BallCollision.cs`. `BodyPartCoefficients` retains the AR-1 M-2 (`s_coefficients`) + AR-1 L-4 (throw on unknown enum) fixes verbatim. Unused `using System.Collections.Generic;` dropped from BallCollision.cs and the redundant `UnityEngine.` prefix dropped from `Debug` / `Vector2` (already in scope via the file's `using UnityEngine;`). L-3: dead `BallEventType` members `Header` / `Deflection` / `OutOfPlay` / `PossessionChange` dropped — no producer existed in `BallEventLogger`. Enum XML doc records that Stage 1+ re-additions MUST land atomically with the producing `Log*` method to prevent silent default-valued events leaking to consumers. L-4: `BallEvent` XML doc gains a per-`Type` validity map listing which detail fields each event type populates (`PositionSnapshot` header-only, `Kick`→ResultingState, `Bounce`→Surface/RestitutionUsed/VnBefore/VnAfter, `GoalPostHit`→ContactPoint, `Goal`→TeamID); consumers MUST switch on `Type` before reading any detail field. `FormatDetail` default arm now throws `ArgumentOutOfRangeException` (consistent with the closed enum) and `PositionSnapshot` gets an explicit empty-string arm so silent zero leakage is impossible. L-5: `BallPhysicsConstants.Possession.ControlHeight` XML doc records a cross-spec-drift warning naming `FirstTouchConstants.GroundControlHeight` as a parallel `[GT]` declaration at the same value (0.50 m); per Spec #20 §4.2 routing one of the two specs must become the authority and the other a `[CROSS]` mirror — routing decision deferred to a dedicated cross-spec pass. Tree: 4 new file rows added under `ball-physics/`. Files: BallEventLogger.cs v1.3, BallCollision.cs v1.3, BallPhysicsCoreTests.cs v1.4, BallPhysicsConstants.cs v1.4; new: BodyPart.cs v1.0, RestartType.cs v1.0, KickResult.cs v1.0, BodyPartCoefficients.cs v1.0. |
| 1.34 | 2026-06-02 | — | Ball Physics (#1) AR-1 fix pass: 3H + 7M + 6L all fixed across the 8 implementation files + 3 tests. H-1: `BallEventLogger` per-event string interpolation in `LogBounce` / `LogKick` / `LogGoalPostHit` removed (FR-CS-031 hot-path violation; logger is called from `BallPhysicsCore.UpdateBallPhysics` at 60 Hz). `BallEvent` now carries typed `Surface` / `RestitutionUsed` / `VnBefore` / `VnAfter` / `ContactPoint` / `ResultingState` / `TeamID` / `AngularVelocity` fields; `LogKick` signature changed from `(…, string kickType, …)` → `(…, BallStateType resultingState, …)`; `FormatDetail(in BallEvent)` is an off-hot-path string helper for diagnostic export. H-2: all 8 source files + 3 test files had stale `// File: src/Core/Physics/Ball/<Name>.cs` headers from before the v1.23 git-mv; updated to `src/ball-physics/…`. H-3: `BallStateMachine.IsOutOfBounds` now applies the same `z < Ball.Diameter` Stage-0 gate as `BallCollision.CheckBoundaries` — a high-flying ball over the touchline is no longer silently classified as OutOfPlay by the state machine while CheckBoundaries returns `(false, None)`; the two predicates now agree. New unit test `OutOfBounds_HighAboveTouchline_ReturnsFalse` locks the gate. M-1: `BallCollision.cs` + `BallEventLogger.cs` using directives reordered `System → Unity` per FR-CS-006. M-2: `BodyPartCoefficients._coefficients` → `s_coefficients` per FR-CS-002 (private static naming). M-3: dead `isHomeGoal` parameter on the private `IsInGoal` helper removed — replaced with `IsInHomeGoal` / `IsInAwayGoal` wrappers over a shared `IsBetweenPostsUnderCrossbar` (both goals have identical Y/Z gates; the caller already validates X). M-4: `BallStateType`, `BallEventType`, `RestartType`, `SurfaceType` members renamed ALL_CAPS_SNAKE → PascalCase per FR-CS-001 / Spec #20 §3.2.3 (`STATIONARY → Stationary`, `OUT_OF_PLAY → OutOfPlay`, `GRASS_DRY → GrassDry`, `THROW_IN → ThrowIn`, `KICKOFF → KickOff`, etc.); the four enums are not consumed by member name outside ball-physics so the rename is contained. M-5: `BallCollision.ApplyKick` signature changed `void → KickResult { Applied, RejectedNonFiniteVelocity }` so callers can detect non-finite-velocity rejection without scraping `Debug.LogError`; new `Possession_NaNKick_RejectedWithFeedback_BallStateUnchanged` test locks the contract. M-6: `BallEventLogger` is now `sealed` (FR-CS-068 virtual-dispatch avoidance). M-7: `_lastSnapshotTime = -999f` magic literal replaced with `private const float NeverSnapshotted = -1f` plus an explicit "first call always emits" sentinel branch. L-1: `UpdateSpinDecay` XML doc records the rationale for the hybrid empirical-linear + analytical-aerodynamic-torque decay model (§3.1.7.1 calibration term vs §3.1.7.2 textbook torque). L-2: `CheckBoundaries` XML doc records the corner-region precedence Stage-0 simplification (touchline check wins on simultaneous goal+touch crossing) and points at the `BallStateMachine.IsOutOfBounds` z-gate alignment. L-3: `ExportEvents` XML doc warns that it allocates a `List<BallEvent>` per call and MUST be invoked off the hot path; new `Count` property exposes the size without copying. L-4: `BodyPartCoefficients.Get` and all four `SurfaceProperties.Get…` switch expressions throw `ArgumentOutOfRangeException` for unknown enum values instead of silently returning a default tuple / `GrassDry` — fails fast on cast-from-int callers. L-5: `BallState` XML doc records the cross-spec consumers (`HeaderExecutedEvent` in heading-mechanics, `SaveAttemptedEvent` in goalkeeper-mechanics) that embed it via `MemoryMarshal.Write`. L-6: `BallPhysicsCore.UpdateBallPhysics` default-branch comment now states explicitly that `ValidatePhysicsState` is skipped on the Stationary / Controlled / OutOfPlay branches and points at the caller-side validation responsibility. Files: BallEventLogger.cs v1.2, BallCollision.cs v1.2, BallStateMachine.cs v1.2, BallPhysicsCore.cs v1.2, BallState.cs v1.3, SurfaceProperties.cs v1.3, BallGroundInteraction.cs v1.2, BallPhysicsConstants.cs v1.3, tests/BallStateMachineTests.cs v1.3, tests/BallPhysicsCoreTests.cs v1.3, tests/BallIntegrationTests.cs v1.3. |
| 1.32 | 2026-06-02 | — | Testing Strategy (#19) AR-5 fix pass: 2L all fixed against the AR-4 v1.4/v1.3 state. L-1: `DeterminismGate.RunTiers` order snapshot promoted from heap-allocated `DeterminismTierKind[]` to `stackalloc Span<DeterminismTierKind>`, eliminating the per-call heap allocation while preserving the AR-4 L-1 dispatch-elimination benefit; matches the deterministic-sim `CanonicalSerializer` Span pattern (src/CLAUDE.md v1.21 AR-1 H-3). `using System;` added for the `Span<T>` ref-struct; no `unsafe` block required (Span form, C# 7.2+). L-2: `PerfGateReport` XML doc on the constructor split the empty-string rationale per parameter — empty `loopTag` is the existing "SHOULD" advisory (runner does not enforce the `LOOP_TAG_*` allowlist), empty `scenarioManifestId` is the missing-manifest sentinel produced by `PerfGateRunner.Run` when `current.Manifest` is null. Doc-only. Files: DeterminismGate.cs v1.5, PerfGateReport.cs v1.2. |
| 1.31 | 2026-06-02 | — | Testing Strategy (#19) AR-4 fix pass: 1M + 1L all fixed against the AR-3 v1.3 state. M-1: `DeterminismSuiteResult` drops the AR-3 L-4 fast-path bypass (skip wrap if already `ReadOnlyCollection<T>`) — that path did NOT achieve defensive copy because `ReadOnlyCollection<T>` wraps an `IList<T>` that a caller can mutate via a retained reference. Constructor now always copies inputs into fresh `DeterminismTierResult[]` / `GoldenVectorResult[]` arrays before wrapping. Pass-loop iterates the copied arrays so `AllPassed` and the property surface are computed against the same snapshot. Supersedes the AR-2 L-1 producer-side wrap in `DeterminismGate.RunTiers` (reverted to plain array pass-through — the suite's defensive copy is now the authoritative read-only-contract boundary; the producer wrap would have been a double-copy). L-1: `DeterminismGate.RunTiers` snapshots `s_canonicalTierOrder` into a local `DeterminismTierKind[] order` once so the result-construction loop reads plain array elements instead of re-dispatching `IReadOnlyList<T>.this[int]` four times per call. Files: DeterminismSuiteResult.cs v1.3, DeterminismGate.cs v1.4. |
| 1.30 | 2026-06-02 | — | Testing Strategy (#19) AR-3 fix pass: 1M + 4L all fixed against the AR-2 v1.2 state. M-1: `PerfGateReport` constructor null-checks `loopTag` + `scenarioManifestId` with explicit `ArgumentNullException` (parallel to AR-1/AR-2 invariant push on the other result/entry types); empty strings remain valid — `PerfGateRunner.Run` uses `string.Empty` as the missing-manifest sentinel. L-1: null-vs-empty string checks split across `GoldenVectorEntry` / `GoldenVectorResult` / `DeterminismTierResult` — `ArgumentNullException` for null, `ArgumentException` for empty (idiomatic .NET BCL convention; parallels the AR-2 L-3 range-vs-relation split). L-2: `DeterminismGate.RunTiers` caches `s_canonicalTierOrder.Count` into a local `tierCount` once (interface-property dispatch after the AR-2 retype). L-3: `DeterminismTierKind.cs` VersionHistory header row widened to match body-row column widths (was shorter, breaking markdown table alignment). L-4: `DeterminismSuiteResult` constructor re-wraps input lists in `ReadOnlyCollection<T>` unless already wrapped, closing the AR-2 L-1 contract leak for external direct constructions that bypass `DeterminismGate.RunTiers`. Files: PerfGateReport.cs v1.1, GoldenVectorEntry.cs v1.3, GoldenVectorResult.cs v1.3, DeterminismTierResult.cs v1.3, DeterminismGate.cs v1.3, DeterminismTierKind.cs v1.2, DeterminismSuiteResult.cs v1.2. |
| 1.29 | 2026-06-02 | — | Testing Strategy (#19) AR-2 fix pass: 1M + 5L all fixed against the AR-1 v1.1 state. M-1: `GoldenVectorEntry` constructor now enforces non-empty `name` / `sourcePath` / `citation` via `ArgumentException` (parallel to AR-1 L-2/L-3 invariant guards already on `GoldenVectorResult` + `DeterminismTierResult`). L-1: `GoldenVectorRunner.Catalogue()` + `RunAll()` wrap their backing arrays in `ReadOnlyCollection<T>` so callers cannot cast `IReadOnlyList<T>` back to mutable `T[]`; `DeterminismGate.RunTiers` applies the same wrap to the local `tierResults` array before passing to `DeterminismSuiteResult`. L-2: `DeterminismGate.s_canonicalTierOrder` retyped `private static readonly IReadOnlyList<DeterminismTierKind>` backed by `ReadOnlyCollection<T>` (elements no longer reassignable through the field reference); loop in `RunTiers` switched to `.Count`. L-3: range-style violations now throw `ArgumentOutOfRangeException` with explicit `actualValue` argument — `vectorsExecuted < 0`, `vectorsFailed < 0 \|\| vectorsFailed > vectorsExecuted`, `testsExecuted < 0`, `testsFailed < 0 \|\| testsFailed > testsExecuted`; pass invariant + null/empty diagnostic stay `ArgumentException`. L-4: KD-5 "Stage-gated" annotation extended to remaining GT rows (UnitWallTimeBoundMs, PreCommitWallTimeBoundSeconds, QuarantineExpiryDays, EvictionQuarantineCount, EvictionWindowDays) — §3.7 preamble explicitly tags Stage-gated per KD-5. L-5: maintainer notes added to `DeterminismTierKind` enum and `DeterminismGate.s_canonicalTierOrder` reminding contribs to extend atomically when a new tier is added (FR-TS-018 currently forbids new tier categories without a #16 §5 revision, but a missed entry would silently exclude the new tier from `RunTiers`). L-6: XML doc on `GoldenVectorEntry` / `GoldenVectorResult` / `DeterminismTierResult` notes default-value bypass — C# struct default-value semantics skip constructor invariant checks; consumers SHOULD treat default-valued instances as malformed. Files: GoldenVectorEntry.cs v1.2, GoldenVectorResult.cs v1.2, GoldenVectorRunner.cs v1.2, DeterminismTierKind.cs v1.1, DeterminismTierResult.cs v1.2, DeterminismGate.cs v1.2, TestingStrategyConstants.cs v1.2. |
| 1.28 | 2026-06-02 | — | Testing Strategy (#19) AR-1 fix pass: 1H + 3M + 6L all fixed. H-1: `PerfGateRunner.Run` misleading null-conditional removed — `current?.` chain dropped (RegressionGate.Evaluate NPEs first on null current); explicit `ArgumentNullException` guards added on `baseline` + `current` with inner `Manifest?.` kept for the malformed-record diagnostic path only. M-1: `GoldenVectorRunner` private const `GoldenVectorRootRelPath` + `Stage0DeferredDiagnostic` gained XML `<summary>` docs per FR-CS-061. M-2: `DeterminismGate.Stage0DeferredDiagnostic` private const gained XML `<summary>`. M-3: `DeterminismSuiteResult.AllPassed` now fails closed on empty tier or golden-vector lists (previously returned true via empty early-exit loops, which would silently green-light FR-DS-009-GATE for a degenerate caller). L-1: `GoldenVectorEntry` property declaration order reordered to `Kind / Name / SourcePath / Citation` matching the constructor parameter order. L-2: `GoldenVectorResult` + `DeterminismTierResult` constructors now enforce non-empty diagnostic via `ArgumentException`. L-3: same constructors enforce `0 ≤ failed ≤ executed` and `passed == (executed > 0 && failed == 0)` so callers cannot publish "passed with failures", "failed with no failures", or "passed with zero executions" records; the `(passed=false, executed=0, failed=0)` deferred-status shape remains valid. L-4: `DeterminismSuiteResult` constructor null-checks both list arguments. L-5: `TestingStrategyConstants` pyramid-bound + per-tier coverage XML docs gained "Stage-gated per KD-5" annotation. L-6: `DeterminismGate.RunTiers` tier-order array promoted from per-call local to `private static readonly s_canonicalTierOrder`. Files: PerfGateRunner.cs v1.1, GoldenVectorRunner.cs v1.1, DeterminismGate.cs v1.1, DeterminismSuiteResult.cs v1.1, GoldenVectorEntry.cs v1.1, GoldenVectorResult.cs v1.1, DeterminismTierResult.cs v1.1, TestingStrategyConstants.cs v1.1. |
| 1.27 | 2026-06-02 | — | Testing Strategy (#19) scaffold initiated. 14 new files under `src/testing-strategy/`: (1) `testing-strategy.asmdef` (autoReferenced false; references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization). (2) `TestingStrategyConstants.cs` — §3.10 governance: Fixed `MATCH_LENGTH_MINUTES=90` + GT pyramid bounds / coverage thresholds (Tier A/B line+branch) / `UnitWallTimeBoundMs=1.0` / `PreCommitWallTimeBoundSeconds=60.0` / quarantine + eviction windows. (3) `TestTier.cs` enum (TierA/B/C mirrors #16 §1.1.1 per KD-1). (4) `TestLayer.cs` enum (Unit/Integration/Simulation/Determinism/EndToEndSoak per §3.1.1 / FR-TS-001). Golden-vector harness (4 files): `GoldenVectorKind` discriminates #16 §9.5 #4 a/b/c corpora; `GoldenVectorEntry` carries kind/name/source-path/citation; `GoldenVectorResult` carries pass/fail + vectors-executed + diagnostic; `GoldenVectorRunner.Catalogue() / Run(in entry) / RunAll()` — Stage 0 deferred-status (D1 test-runner pin); Stage 0+1 parses corpus and invokes DeterministicRngService / CanonicalSerializer. Determinism gate (4 files): `DeterminismTierKind` enum (canonical #16 §5 tier order: Unit/Integration/Scenario/Soak per FR-TS-011 / FR-TS-018); `DeterminismTierResult` + `DeterminismSuiteResult`; `DeterminismGate.RunTiers()` — single integration point per FR-TS-016, aggregates §5 tiers + golden-vector corpus into one FR-DS-009-GATE signal. Perf-gate runner (2 files): `PerfGateRunner.Run(specId, loopTag, baseline, current, milestoneMs)` delegates verdict to #18 `RegressionGate.Evaluate` (KD-3 boundary, §3.9.2 pointer) and wraps the result in `PerfGateReport` carrying spec/loop/scenario context for FR-PO-036 message formatting. |
| 1.26 | 2026-06-02 | — | Performance Optimization (#18) AR-2 fix pass (2M+2L). M-1: RegressionGate helper/orchestrator semantic divergence on NaN milestone — PassesAbsoluteDriftCheck now returns true for float.NaN milestone (skip-drift signal), aligning with Evaluate's documented semantics; Evaluate delegates drift verdict entirely to the helper. M-2: TraceChannelDescriptor public constructor gained XML <summary> doc (FR-CS-060 — every public method requires a summary; missed in AR-1 H-1 split). L-1: SessionManifest.IsComplete() now also validates HardwareCounters.CpuModel and HardwareCounters.ThermalState — default-constructed HardwareCounterSnapshot struct (null strings) previously slipped through despite §3.3.2 listing hardware counters as required. L-2: TraceChannelDescriptor constructor enforces the symmetric F.0 invariant InsideTickPipeline ⇒ SignOffLogRef non-empty (AR-1 L-1 fixed the SamplingRule/SampleN side; this completes the descriptor structural-invariant set). Files: RegressionGate.cs v1.2, TraceChannelDescriptor.cs v1.2, SessionManifest.cs v1.1. |
| 1.25 | 2026-06-02 | — | Performance Optimization (#18) AR-1 fix pass (2H+3M+3L). H-1: TraceChannel.cs (5 public types) split into 5 single-type files — ChannelVerbosity.cs / ChannelSamplingRule.cs / ChannelDeterminismClass.cs / TraceChannelDescriptor.cs / TraceChannelRegistry.cs — per CLAUDE.md FILE NAMING. H-2: PerformanceOptimizationConstants.HotPathAllocBudgetBytes → HOT_PATH_ALLOC_BUDGET_BYTES ([FIXED] constants are ALL_CAPS per FR-CS-001). M-1: RegressionGate.Evaluate dedupes via PassesPerPrCheck/PassesAbsoluteDriftCheck rather than reimplementing the formula. M-2: RegressionGate degenerate baseline or milestone (≤0 or NaN) now fails closed instead of silently passing the gate; float.NaN milestone still skips the drift check (explicit opt-in). M-3: BaselineReproducibilityAuditor degenerate origP50 (≤0 or NaN) fails closed instead of silently returning isReproducible=true. L-1: TraceChannelDescriptor constructor enforces the documented SamplingRule=PerNTicks ⇒ SampleN>0 invariant (throws ArgumentException). L-2: BaselineReproducibilityAuditor sealed class → static class (stateless validator; matches RegressionGate style). L-3: IBudgetSource.GetEntries() return type changed from BudgetRollupEntry[] to IReadOnlyList<BudgetRollupEntry> to match the documented "callers must not mutate" contract. L-4: PerformanceOptimizationConstants.PromotionToleranceFraction XML doc — removed stale "also used as the reproducibility confidence-interval tolerance" sentence; auditor consumes ReproducibilityToleranceFraction. |
| 1.44 | 2026-06-04 | — | Agent Movement (#2) regression-test roster initiated. (1) New `docs/specs/agent-movement/test-plan.md` v0.1 catalogues T-AM-NNN test IDs grouped by file under test (dwell formula / pipeline collision / safety override / OscillationGuard), each row anchored to an AR finding; documents that Spec #2 §5 is *performance* (not a test plan) and tombstones the fictitious "§5.1 / 85 tests" anchor in the prior placeholder. (2) `src/agent-movement/Tests/AgentMovementTests.cs` placeholder → 18 NUnit `[Test]`s across 4 `[TestFixture]` classes: `AgentStateMachineDwellTests` (T-AM-001..006), `AgentMovementSystemCollisionTests` (T-AM-010..018), `AgentMovementSystemSafetyOverrideTests` (T-AM-030..033), `OscillationGuardTests` (T-AM-040..043). Primary regression locks: T-AM-011 (AR-9 M-1 — GROUNDED dwell uses cached entry-force, not incoming this-frame force), T-AM-013 (AR-5 M-2 second-hit refresh), T-AM-016 (AR-6 M-2 OscillationGuard collision bypass), T-AM-030 (AR-5 M-1 safety-override `LastValid*` preservation). (3) New `src/agent-movement/AssemblyInfo.cs` adds `[InternalsVisibleTo("TacticalDirector.AgentMovement.Tests")]` so T-AM-030..032 can call the internal `MovementCommand.ToolingOverrideOnly_NaNInjection` factory (added in MovementCommand.cs v1.2; production game logic MUST NOT call this factory — it disables Step 10 safety validation). Files: AgentMovementTests.cs v2.0 (replacement); new: AssemblyInfo.cs v1.0; MovementCommand.cs v1.2; new: docs/specs/agent-movement/test-plan.md v0.1. Tree row updated for the new AssemblyInfo + Tests/ folder casing. |
| 1.45 | 2026-06-04 | — | Agent Movement (#2) pure-function unit coverage expansion. New `src/agent-movement/Tests/AgentMovementUnitTests.cs` v1.0 adds 59 NUnit `[Test]`s across 7 `[TestFixture]` classes: `AgentStateMachineStumbleTests` (T-AM-007..009 dwell + T-AM-019..023 `ShouldStumble`), `AgentSafetySystemUnitTests` (T-AM-034..039 `HasInvalidValues` / `ClampVelocity` / `ClampToPitch` / `Validate`), `OscillationGuardEdgeTests` (T-AM-044..047 window-expiry / buffer-wrap / sparse-burst / `Initialize`-after-lock), `PerformanceContextUnitTests` (T-AM-050..052 neutral / partial-modifier / out-of-range clamp), `AgentLocomotionUnitTests` (T-AM-070..083 — endpoints + clamping for `CalculateBaseTopSpeed` / `CalculateBaseAccelK` / `ApplyAcceleration` / `ApplyDeceleration` / `CalculateStoppingDistance` / `CalculateAerobicModifier`), `AgentDirectionalMovementUnitTests` (T-AM-084..099 — endpoints + zone-blend + degenerate guards for `LateralMultiplier` / `BackwardMultiplier` / `CalculateDirectionalMultiplier` / `ApplyDirectionalToAccelK` / `MovementAngleDeg` / `RotateFacingToward`), `AgentTurningUnitTests` (T-AM-100..107 — speed-dependence + clamps for `CalculateMaxTurnRate` / `MinimumTurnRadius` / `CalculateLeanAngle`). Note T-AM-021: at peak stress (speed=MAX_SPEED, turn=180°) the formula deliberately produces `stumbleRisk = 1.5 > max resistance = 1.0`; the test discriminates at moderate stress (8 m/s, 120°) where the attribute axis actually decides. `docs/specs/agent-movement/test-plan.md` bumped to v0.2 — non-coverage section rewritten (locomotion / turning / directional / safety dropped; `EvaluateFromX` private-branch carve-out, fatigue table, UpdateAllAgents asserts, and the `RotateVelocityToward` both-degenerate `Debug.Assert(false)` branch remain explicitly non-covered). Files: new AgentMovementUnitTests.cs v1.0; test-plan.md v0.2; tree row updated. |
| 1.46 | 2026-06-07 | — | AR-hardening sweep — Ball Physics #1 AR-1..AR-6 complete (final AR-6: 0M+4L); Collision System #3 AR-1..AR-6 (final 3L); First Touch #4 AR-5..AR-6 (final 3L); Pass Mechanics #5 AR-2..AR-5 (final 1M+3L cycle-stop); Shot Mechanics #6 AR-2..AR-4 (final 3L). |
| 1.47 | 2026-06-07 | — | Event System #17 AR-8..AR-11. AR-8 (2M+1L): OnTickBoundary resets CurrentPhase to (PhaseId)0xFF + CurrentTick to uint.MaxValue so stale Publish between OnTickBoundary and next BeginTick/BeginPhase trips IORE rather than silently corrupting digest; EventRegistry.RegisterRowRaw throws ERR_EVT_ORDINAL_COLLISION on already-fully-initialised row; RegisterRow asserts sizeof(T) >= EventHeaderBytes for Tier A/B. AR-9 (1M+1L): DrainTick + SerializeLedger stackallocs switched to compile-time MAX_QUEUE_SORT_INTS=2048 (8 KB cap) + static-ctor invariant; CosmeticChannel.ResetForTests added. AR-10 (1M+2L): EventBus.PublishAuthoritative phase validity guard now precedes QueueCount++ reservation (AR-8 M-2 sentinel previously fired via IORE after slot reservation); EventLedger.SerializeLedger dst.Length >= 5 bound check; version-history rows reordered. AR-11: whole-surface sweep clean — no findings. |
| 1.48 | 2026-06-07 | — | Agent Movement #2 AR-10..AR-11. AR-10 (1M+2L): SanitiseCollisionForce maps NaN/±Inf → 0 before Clamp01 (Unity's Clamp01 does not filter NaN); currentTime assert tightened to IsFinite; dead MIN_TURN_RATE_DIVISOR constant removed. AR-11 (2L): EvaluateState collisionForce/groundedReason defaults dropped parallel to AR-9 L-2 on CalculateGroundedDwell; OscillationGuard.WriteTime gains Debug.Assert(false) default arm parallel to ReadTime's NegativeInfinity default. |
| 1.49 | 2026-06-07 | — | Performance Optimization #18 AR-3..AR-4. AR-3 (3M+5L) + AR-3 full-surface (3M+3L): RegressionGate.Evaluate now distinguishes skipped-drift (NaN milestone) from degenerate-milestone (+Infinity) in MilestoneDriftFraction; PassesPerPrCheck + PassesAbsoluteDriftCheck fail-closed on non-finite/negative currentMs; RECORD_FORMAT_VERSION_STAGE0_PENDING [FIXED] sentinel constant added (replaces 3 identical placeholders in TraceChannelRegistry). AR-4 (2L): enum ordinal-stability + registry placeholder doc rows. All tracking docs (root CLAUDE.md, this file, PROGRESS.md, file-manifest.md, README.md) refreshed in the same commit. |
| 1.50 | 2026-06-08 | — | Cross-spec routing close-out: `Possession.ControlHeight` ↔ `GroundControlHeight` resolved (closes the June 3, 2026 root-CLAUDE.md OPEN ISSUE). Ball Physics #1 §3.1.11 `BallPhysicsConstants.Possession.ControlHeight` declared the AUTHORITY for ground-control height; First Touch #4 `FirstTouchConstants.GroundControlHeight` relocated from `#region GT` to `#region Cross`, retagged `[CROSS]`, RHS now `BallPhysicsConstants.Possession.ControlHeight` (single-consumer mirror per Spec #20 §4.2). Rationale: ControlHeight is physical possession geometry, not a First-Touch-specific knob; lives next to the three sibling thresholds (ControlRadius / ControlVelocity / ChallengeRadius). Ball Physics XML drift warning on Possession.ControlHeight replaced with an AUTHORITY / consumer pointer naming the mirror. Sibling-hazard sweep cleared — grep across `docs/specs/` confirms no other spec re-declares `ControlRadius` / `ControlVelocity` / `ChallengeRadius`; those remain single-spec `[GT]` in Ball Physics #1 §3.1.11 with no parallel decls. Heading Mechanics #10 forward note: when #10's eligibility gate begins consuming the value at Stage 0+1, mirror from `BallPhysicsConstants.Possession.ControlHeight` (do NOT re-declare locally). Files: FirstTouchConstants.cs v1.7, BallPhysicsConstants.cs v1.7, docs/specs/first-touch/section-4-4-to-4-6.md §3.4.3 constant block, root CLAUDE.md OPEN ISSUES entry flipped OPEN → RESOLVED. |
