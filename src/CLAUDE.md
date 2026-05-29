# src/CLAUDE.md — Tactical Director Coding Guide

> **Created:** May 19, 2026
> **Last Updated:** May 29, 2026 (v1.17 — positioning-ai/ 20 files tree expanded + AR-1+AR-2+AR-3 fix cycles noted; v1.16 decision-tree/ 36 files + AR-1 2H+3M+4L fixes; v1.15 perception-system/ 14 files; v1.14 goalkeeper-mechanics/ 36 files; v1.13 heading-mechanics/ 25 files)
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
├── ball-physics/                      ← Spec #1 (spec-canonical target path)
│   │   ⚠️  STRUCTURAL DEVIATION: actual files are at src/Core/Physics/Ball/ —
│   │       fix pass will relocate them here. See root CLAUDE.md REPO STRUCTURE note.
│   ├── ball-physics.asmdef            ← pending Unity project init
│   ├── BallPhysicsConstants.cs
│   ├── BallState.cs
│   ├── BallPhysicsCore.cs
│   ├── BallStateMachine.cs
│   ├── BallGroundInteraction.cs
│   ├── BallCollision.cs               ← ball-specific collision response; detection geometry lives in collision-system/
│   ├── BallEventLogger.cs
│   ├── SurfaceProperties.cs
│   └── tests/
│       ├── ball-physics-tests.asmdef  ← EditMode; references ball-physics.asmdef
│       ├── BallPhysicsCoreTests.cs
│       ├── BallIntegrationTests.cs
│       └── BallStateMachineTests.cs
│
├── agent-movement/                    ← Spec #2
│   ├── agent-movement.asmdef
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
│   ├── MovementCommand.cs
│   ├── AgentMovementSystem.cs         ← 12-step 60 Hz pipeline orchestrator
│   ├── AgentStateMachine.cs           ← pure state evaluator (no side effects)
│   ├── OscillationGuard.cs            ← ring-buffer anti-oscillation guard
│   ├── AgentLocomotion.cs             ← acceleration / deceleration formulas
│   ├── AgentTurning.cs                ← turn rate / lean angle / stumble probability
│   ├── AgentDirectionalMovement.cs    ← directional multipliers / facing update
│   ├── AgentSafetySystem.cs           ← NaN detection / speed clamp / pitch boundary
│   └── tests/
│       ├── agent-movement-tests.asmdef  ← EditMode; references agent-movement.asmdef
│       └── AgentMovementTests.cs
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
│   └── EventBusStub.cs                ← stub pending Event System #17 wiring (Stage 1)
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
│   ├── EventBusStub.cs                ← Stage 0 no-op event bus; replace at Stage 1 with Event System #17
│   ├── ShotExecutor.cs                ← sealed orchestrator: 5-state machine (Idle/Windup/Contact/FollowThrough/Complete)
│   └── Tests/
│       └── NaNVelocityStub.cs         ← #if UNITY_EDITOR||DEVELOPMENT_BUILD; returns NaN for EC-008 FM-05 test
├── perception-system/                 ← Spec #7
│   ├── perception-system.asmdef
│   ├── PerceptionConstants.cs         ← all GT/Fixed/Derived/Cross constants (§3.10); 18 spec constants + sizing constants
│   ├── PerceptionAgentAttributes.cs   ← struct: Decisions/Anticipation/TeamId/IsHalfTurned snapshot (§4.2.2)
│   ├── FilteredView.cs                ← FilteredView, PerceptionDiagnostics, PerceivedAgent, ShoulderCheckAnimData, OcclusionDebugRecord, PerceivedAgentDebug
│   ├── PerceptionEvents.cs            ← PerceptionRefreshEvent struct + RefreshTrigger enum (§4.6.3)
│   ├── EventBusStub.cs                ← Stage 0 no-op event bus; replace at Stage 1 with Event System #17
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
│   ├── DecisionMadeEvent.cs           ← struct event: published after each decision; EventBusStub no-op at Stage 0 (§2.2.7)
│   ├── DtAgentAttributes.cs           ← struct: all DT-consumed player attributes [1–20] raw + CreateDefault factory (§3.1)
│   ├── MatchContext.cs                ← struct: authoritative match state per heartbeat (score, possession, ball, zone) (§2.2.5)
│   ├── TacticalContext.cs             ← struct: pressing mode, passing style, formation slots; Stage0Default factory (§2.2.6)
│   ├── DecisionTreeConstants.cs       ← constants: capacity limits / timing budgets / pipeline invariants (§4.2, §3.7)
│   ├── UtilityWeights.cs              ← constants: all 58+ utility scoring constants (§3.2.11)
│   ├── ComposureWeights.cs            ← constants: NOISE_MAX / COMPOSURE_SUPPRESSION / TIEBREAK_EPSILON (§3.3.3–3.3.5)
│   ├── TacticalWeights.cs             ← constants: tactical multipliers for all action types (§3.4)
│   ├── PitchGeometry.cs               ← static helpers: field zone classification, goal post positions, centre (§3.1.1)
│   ├── IDtMovementController.cs       ← public interface: dispatch boundary to Agent Movement #2 XC-3.5-10 (§3.5)
│   ├── EventBusStub.cs                ← Stage 0 no-op event bus; replace at Stage 1 with Event System #17 wiring
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
│   ├── EventBusStub.cs                ← Stage 0 no-op event bus; replace at Stage 1 with Event System #17
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
│   ├── EventBusStub.cs                ← Stage 0 no-op event bus; replace at Stage 1 with Event System #17
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
├── attacking-ai/                      ← Spec #15
│
├── deterministic-sim/                 ← Spec #16  (cross-cutting; referenced by all layers)
│   ├── deterministic-sim.asmdef
│   ├── DeterministicSimConstants.cs
│   ├── TickOrchestrator.cs
│   ├── SnapshotCodec.cs
│   └── tests/
│       └── deterministic-sim-tests.asmdef  ← EditMode; references deterministic-sim.asmdef
│
├── event-system/                      ← Spec #17  (cross-cutting; referenced by all layers)
│   ├── event-system.asmdef
│   ├── EventSystemConstants.cs
│   ├── EventBus.cs
│   ├── EventLedger.cs
│   ├── CosmeticChannel.cs
│   ├── EventRegistry.cs
│   └── tests/
│       └── event-system-tests.asmdef  ← EditMode; references event-system.asmdef
│
├── performance-optimization/          ← Spec #18  (owns trace pipeline; minimal game-loop code)
├── testing-strategy/                  ← Spec #19  (CI orchestration tooling; no game-loop code)
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
