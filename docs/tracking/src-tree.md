# src/ File Tree

> **Created:** July 31, 2026
> **Purpose:** The annotated `src/` tree formerly inlined in `src/CLAUDE.md`. Useful
> for orientation; **not authoritative**. `docs/tracking/file-manifest.md` is the
> authoritative file inventory, and the per-spec `§4` (Architecture) files are
> authoritative for `.asmdef` reference lists.
> Split out of `src/CLAUDE.md` on July 31, 2026. Content is **verbatim** — moved, never edited or reordered.

**A hand-maintained tree drifts.** Prefer `ls`/`git ls-files` over trusting this
file, and treat any annotation here as a hint to verify rather than a fact.

---

## UNITY PROJECT STRUCTURE

```
src/
├── CLAUDE.md                          ← You are here
│
├── project-constants/                 ← infrastructure; read-only by all (bottom of the reference graph)
│   ├── project-constants.asmdef       ← one assembly per folder (FR-CS-055); references: [] (autoReferenced)
│   ├── GameplayConfig.cs              ← FR-CS-019: immutable boot-time [GT] key/value store (GetFloat/Int/Bool/String + fallback; fail-loud on malformed)
│   ├── GameplayConfigFileLoader.cs    ← FR-CS-019: on-disk [section] key = value text → GameplayConfig (parser swap; fail-loud)
│   ├── (ProjectConstants.cs)          ← source-of-truth for multi-consumer constants (Spec #20 §4.2) — not yet needed; add when a multi-consumer [CROSS] constant exists
│   └── tests/
│       ├── project-constants-tests.asmdef
│       ├── GameplayConfigTests.cs            ← getter / fallback / fail-loud / case-insensitive / ctor-guard locks
│       └── GameplayConfigFileLoaderTests.cs  ← grammar round-trip + empty→Empty + fail-loud cases
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
│       ├── ball-physics-tests.asmdef  ← EditMode; references ball-physics.asmdef + testing-strategy.asmdef
│       ├── BallPhysicsCoreTests.cs
│       ├── BallIntegrationTests.cs
│       ├── BallStateMachineTests.cs
│       ├── BodyPartCoefficientsTests.cs ← AR-4 L-2 throw-on-unknown + catalogue round-trip
│       ├── SurfacePropertiesTests.cs    ← AR-4 L-2 throw-on-unknown + catalogue round-trip (4 Get* methods)
│       ├── EnumOrdinalStabilityTests.cs ← AR-6 L-3 locks int ordinals for all 6 public enums
│       ├── BallPhysicsScenarios.cs      ← Spec #1 closed-loop scenario corpus on the #19 ScenarioRunner:
│       │                                │   drop-and-rebound (AR-7 H-1 / ERR-001-001 lock) +
│       │                                │   fast-descent-grounds-out (AR-7 H-2 hover-deadlock lock)
│       └── BallPhysicsScenarioTests.cs  ← sim_<scenario> Simulation-layer tests running the corpus through ScenarioRunner
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
│       ├── agent-movement-tests.asmdef  ← EditMode; references agent-movement.asmdef + testing-strategy.asmdef
│       ├── AgentMovementTests.cs        ← T-AM-001..018 / T-AM-030..033 / T-AM-040..043 regression roster
│       ├── AgentMovementScenarios.cs    ← T-AM-110..115 closed-loop scenario corpus: bodies + #19 A.1 manifests
│       │                                │   (migrated from the AR-12/AR-13 in-fixture form June 10, 2026)
│       ├── AgentMovementScenarioTests.cs ← sim_<scenario> Simulation-layer tests running the corpus through #19 ScenarioRunner
│       └── AgentMovementUnitTests.cs    ← T-AM-007..009 / T-AM-019..023 / T-AM-034..039 /
│                                       │   T-AM-044..047 / T-AM-050..052 / T-AM-070..109
│                                       │   pure-function coverage (test-plan.md v0.4)
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
│   ├── IFirstTouchSystem.cs           ← public interface for First Touch consumers
│   ├── AssemblyInfo.cs                ← [InternalsVisibleTo("TacticalDirector.FirstTouch.Tests")]
│   ├── first-touch.asmdef             ← references BallPhysics, AgentMovement, CollisionSystem
│   └── Tests/
│       ├── first-touch-tests.asmdef   ← EditMode; references first-touch.asmdef + testing-strategy.asmdef
│       ├── FirstTouchTests.cs         ← CQ/TR/PR/OR/PO/EC/BD/VS + invariant suite + IT-001..008 stubs
│       ├── FirstTouchScenarios.cs     ← Spec #4 closed-loop scenario corpus on the #19 ScenarioRunner:
│       │                              │   heavy-touch-runs-on (AR-7 H-1 / ERR-004-003 coherence lock) +
│       │                              │   interception-chain-anchors-at-displaced-ball (ERR-004-004 +
│       │                              │   §3.4.5 redirect + Frame N+1 chain via real PressureEvaluator)
│       └── FirstTouchScenarioTests.cs ← sim_<scenario> Simulation-layer tests running the corpus through ScenarioRunner
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
│   └── Tests/
│       ├── perception-system-tests.asmdef ← EditMode; + references TacticalDirector.TestingStrategy (scenario corpus)
│       ├── PerceptionSystemTests.cs   ← 68 unit tests (Fov / Occlusion / RecognitionLatency / ShoulderCheck / BallPerception / Snapshot); §5.11 multi-agent integration = Play-Mode Assert.Ignore stubs
│       ├── PerceptionScenarios.cs     ← Spec #7 closed-loop corpus on the #19 ScenarioRunner: drives the real 22-agent OnHeartbeat (full-heartbeat-all-snapshots / awareness-cold-start-builds / two-instance-determinism)
│       └── PerceptionScenarioTests.cs ← sim_<scenario> Simulation-layer tests running the corpus through ScenarioRunner
├── decision-tree/                     ← Spec #8
│   ├── decision-tree.asmdef           ← AI layer; references agent-movement, perception-system, pass-mechanics, shot-mechanics, deterministic-sim (+4 more)
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
│       ├── DecisionTreeIntegrationTests.cs ← UT-24..UT-35: full pipeline state machine + output (UT-33..35: AR-2 H-3 locks)
│       └── DecisionContextAssemblerTests.cs ← AR-2 H-2 (team-relative BallZone) + M-1 (OpponentHasBall) locks
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
│   ├── MarkingDwellState.cs           ← #23 §2.2.1 per-agent dwell state (T0; serialized at #23 wiring)
│   ├── MarkingPressureEvaluator.cs    ← #23 pure static: FM-DM-01 pressure + §3.2 dwell machine + FM-DM-02 offset (primitive-span inputs per the layering note)
│   ├── BuildUpZone.cs                 ← #24 enum: OwnThird / MiddleThird / FinalThird
│   ├── BuildUpZoneState.cs            ← #24 §2.2.2 per-team committed zone + suppression countdown (T0)
│   ├── BuildUpZoneClassifier.cs       ← #24 pure static: FM-BU-01 hysteresis classifier + FM-BU-03 suppression arithmetic
│   ├── BuildUpOverlayCatalogue.cs     ← #24 Appendix A [GT] overlay tables (row keys per ERR-024-001)
│   ├── RotationPair.cs                ← #25 §2.2.3 normalized adjacency pair (GK-refusing ctor)
│   ├── RotationPairState.cs           ← #25 §2.2.2 per-pair dwell/rotated/hold state (serialized at v12)
│   ├── RotationAdjacencyCatalogue.cs  ← #25 Appendix A [GT] adjacency tables (F442/F433/F4231)
│   ├── RotationController.cs          ← #25 §3.1–§3.4 controller: FM-RO-01/02 on the serialized LastComposedTarget cache; atomic SlotIndex swap + partner lock; F2/F5/F6 seams; sole post-seed SlotIndex writer (ERR-012-009)
│   ├── RestDefenseEvaluator.cs        ← §3.5/§7.13 rest-defense coverage check (cheap-item addition)
│   ├── SlotComposer.cs                ← pure static: Compose() pipeline (anchor→offset→modifiers→#24 build-up overlay→spacing→#23 dismark offset→clamp→lines→lanes; ERR-012-007/008 stage insertions)
│   ├── PositioningAITick.cs           ← sealed class: 10 Hz orchestrator; zero-alloc hot path; F1 stale detection; #25 RotationController before compose (§4.2); GetFormationSlot / GetLine / GetLane / GetPhase / CaptureRotationState
│   └── Tests/
│       ├── positioning-ai-tests.asmdef
│       ├── PositioningAITests.cs      ← T-U-001..021 (unit) + T-D-001..002 (determinism) + T-I-001..004 (integration) + T-P-001 (perf) + T-T-001 (tactical)
│       ├── RestDefenseEvaluatorTests.cs   ← §3.5/§7.13 rest-defense coverage locks
│       ├── TacticTranslationTests.cs      ← #21 T2 width-scalar seam locks
│       ├── MarkingPressureEvaluatorTests.cs ← #23 T0: FM-DM-01/02 worked examples + dwell machine + gates
│       ├── BuildUpStructureTests.cs       ← #24 T0: FM-BU-01/03 + catalogue bound/identities + ERR-024-001 regression
│       ├── RotationCatalogueTests.cs      ← #25 T0: F1 invariants + pinned Appendix A rows + FR-RO-007 bound
│       ├── SlotComposerStageTests.cs      ← #23/#24 stage-insertion locks (exact Δ/offset, identities, gates, spacing invariant, clamp bounds)
│       └── RotationControllerTests.cs     ← #25 controller locks (dwell/commit/revert/hold, partner lock, cap, phase freeze, Off identity, F2/F5/F6 gates)
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
│   ├── EnvironmentFingerprint.cs      ← sealed class: 6 readonly fields + Lock() + ValidateAgainst() → ERR_DS_REPLAY_ENV_MISMATCH + CreateStage0Dev() placeholder/IsDevPlaceholder + CreateStage0MonoCertified() real-hash factory (ERR-016-006)
│   ├── FloatFlagTuple.cs              ← readonly struct: §4.8.3 11-field float-flag tuple + ComputeHash() live-host floatModelHash hasher (ERR-016-006 Option A)
│   ├── RngStreamState.cs              ← mutable struct: StreamKey/RngCursor/ActionOrdinal (ulong), BudgetRemaining/DeclaredBudget/DrawIndex (int), SiteId (string), StreamVersion (ushort), SubsystemOrdinal, EntityId; ClearReservation()
│   ├── MatchClock.cs                  ← sealed class: CurrentTick / CurrentTacticalTick / CurrentMatchTimeMs / IsAiStrideTick; Advance() / RestoreFromSnapshot(tick) — no System.DateTime (FR-CS-042)
│   ├── DeterministicRngService.cs     ← sealed class: HKDF-SHA256 key derivation + SipHash-2-4-64 per-draw hash; RegisterStream / Reserve / DrawReserved / CloseReservation / Skip / RestoreStream; zero-allocation hot path (stackalloc Span<byte>)
│   ├── SaveBlobFramingHelpers.cs      ← static class: the framing helpers #29's and #41's sub-blob codecs share — CanonicalOrder (ascending keys over a COPY, duplicates throw), RequireAscending, ReadCount (bound in ELEMENTS, not an overflowable byte product), Require (overflow-safe). Hoisted at the T1 AR pass; the three older codecs keep their own copies
│   ├── CanonicalSerializer.cs         ← static class: §3.2.4.1 Write/Read for all wire types; FloatUintUnion explicit-layout struct (AR-1 H-1/H-2: no BitConverter allocs); −0.0→+0.0 normalization; Tier B NaN→0x7FC00000
│   ├── SnapshotHeader.cs              ← sealed class: SchemaVersion / DigestVersion / Tick / PrevSnapshotDigest[32] / CurrentSnapshotDigest[32] / Fingerprint / Cursor; Initialize()
│   ├── SnapshotPayload.cs             ← sealed class: pre-allocated PayloadBytes[MaxSnapshotBytes] / BytesWritten / Reset()
│   ├── SnapshotCodec.cs               ← sealed class: Encode() SHA-256 + digest chain; ValidateHeader() / ValidatePrevDigest() / CommitLoadedDigest(); _prevDigest[32] chain state
│   ├── ReplayEngine.cs                ← sealed class: PrepareReplay() steps 1–7 of §4.2.2; step 6 Stage 0 stub (in-memory RNG state preserved); step 8 delegated to TickOrchestrator
│   ├── SaveManager.cs                 ← sealed class: CommitAtomic() §4.6.1.1 five-step atomic save (temp→fsync→rename-overwrite→dir-fsync); File.Move(overwrite:true) (AR-1 M-2)
│   ├── TickOrchestrator.cs            ← sealed class: RunTick() 7-phase 60 Hz pipeline; AI stride-gated on IsAiStrideTick; System.Action callbacks per phase; 9 ProfilerMarkers; zero-alloc hot path
│   ├── DivergenceDetector.cs          ← static class: CompareDigests / CompareTierAFloat / CompareTierBFloat (AR-1 M-3: one-NaN→SoftDrift) / CompareTierAInt / CompareTierAUlong / Worst()
│   ├── AssemblyInfo.cs                ← [InternalsVisibleTo("TacticalDirector.DeterministicSim.Tests")] (golden-vector pass — internal Hkdf*/SipHash24_64 calls need it)
│   └── tests/
│       ├── deterministic-sim-tests.asmdef  ← EditMode; references deterministic-sim.asmdef
│       ├── DeterministicSimTests.cs   ← HKDF RFC 5869 KAT; SipHash-2-4 ref vectors 0–7; canonical serialization (bool/u32/u64/-0.0/DT bits); T-DS-ORDER-001 MatchClock; T-DS-RNG-002 branch cursor parity; T-DS-SNAP-003 u64 round-trip; T-DS-FAULT-009..014; AI stride; DespawnLog
│       ├── HkdfSha256KatTests.cs      ← §9.5 #4(a) full KAT: RFC 5869 A.1–A.3 PRK + full OKM (L=42/82) + pinned project Test Case 4 (RNG_KDF → (k0,k1))
│       ├── SipHash24KatTests.cs       ← §9.5 #4(b) full KAT: all 64 Appendix A vectors + pinned project RNG_STREAM_HASH draw-preimage case
│       └── SerializeCanonicalCorpusTests.cs ← §9.5 #4(c) full corpus: all 41 serialize-canonical-corpus.md entries, bytes + SHA-256 each (incl. chained SnapshotDigest D-07)
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
│       ├── event-system-tests.asmdef  ← EditMode; autoReferenced false; references TacticalDirector.EventSystem + DeterministicSim + 6 production spec assemblies (Pass / Shot / Perception / Decision / Heading / Goalkeeper) for the boot-wiring smoke test
│       └── EventBusWiringSmokeTests.cs ← SMOKE-EVT-WIRING-001 boot→publish→drain→digest smoke test across 6 currently-wired EventBusRegistrars; AM #2 carved out as [CROSS-PENDING]; golden digest pinned via Assert.Inconclusive until AM lands
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
│   ├── TestingStrategyConstants.cs    ← §3.10 governance constants: pyramid bounds / coverage thresholds / quarantine + eviction windows / pre-commit budget / [FIXED] MATCH_LENGTH_MINUTES + SCENARIO_MANIFEST_FORMAT_VERSION
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
│   ├── PerfGateRunner.cs              ← static class: Run(specId, loopTag, baseline, current, milestoneMs) → PerfGateReport; delegates verdict to #18 RegressionGate.Evaluate (KD-3 boundary)
│   ├── ScenarioStatus.cs              ← enum: Passed / Failed / Quarantined (§3.3.3; Quarantined is Stage 0+1 flake-layer only)
│   ├── ScenarioResult.cs              ← sealed class: Status / Diagnostics (key=value lines) / DurationMs / #16 §4.8 Fingerprint (§3.3.3 contract value)
│   ├── ScenarioManifest.cs            ← sealed class: Appendix A.1 entry (name / owning_spec_ids / seed / tier / fixture_refs / format_version) + load-time field validation; Stage 0 in-code authoring (D1 pins on-disk encoding at Stage 0+1)
│   ├── ScenarioEnvelope.cs            ← sealed class: executable expected_outcome_envelope — CheckTrue / CheckEquals / CheckInRange; zero predicates ⇒ Failed (FR-TS-030); NaN fails in_range
│   ├── ScenarioContext.cs             ← sealed class + ScenarioBody delegate: manifest / verbatim run seed / KD-7-seeded DeterministicRngService / envelope
│   ├── IScenario.cs                   ← interface (§4.4.1): single method ScenarioResult Run(ulong seed)
│   ├── ClosedLoopScenario.cs          ← sealed IScenario: fresh RNG + context per run (hermetic, FR-TS-023); body drives a real subsystem loop; exceptions → Failed with diagnostic
│   ├── ScenarioIndex.cs               ← sealed class: immutable in-memory root manifest; duplicate paths AND names rejected (A.1); runner refuses unindexed scenarios (§3.3.6 / FR-TS-028)
│   ├── ScenarioIndexEntry.cs          ← one index row (path + manifest + scenario); AR-1 M-1 guard — a ClosedLoopScenario must be registered under the manifest instance it executes
│   ├── ScenarioRunner.cs              ← sealed class: §3.3.3 single entry point Run(manifestPath, seed) — FR-TS-070 format version first, then A.1 fields / §3.3.5 path↔name / cross-spec arity / fixture_refs refusal (§3.3.4) → IScenario.Run
│   └── Tests/
│       ├── testing-strategy-tests.asmdef ← EditMode; references testing-strategy + deterministic-sim + ball-physics + pass-mechanics + event-system (cross-spec corpus)
│       ├── ScenarioRunnerTests.cs     ← runner contract tests: index refusal / format-version + kebab-case rejection / implicit-pass (FR-TS-030) / NaN / exception capture / KD-7 seed plumbing / hermeticity
│       ├── CrossSpecScenarios.cs      ← cross-spec scenario corpus (KD-8; tests/scenarios/cross-spec/ paths, ≥2 owning specs):
│       │                              │   lofted-pass-kick-bounce-roll — PassExecutor (#5) → IPassBallSystem seam → BallPhysicsCore (#1)
│       │                              │   with #17 boot wiring + tick lifecycle around the CONTACT publish; owning specs {1, 5}
│       └── CrossSpecScenarioTests.cs  ← sim_<scenario> Simulation-layer tests running the cross-spec corpus through ScenarioRunner
├── match-analytics/                   ← Match Analytics & Statistics #37 T0 (APPROVED spec; roadmap B2)
│   │                                  │   Value types + the pure xG model. NOT engine-wired: the T1 ledger
│   │                                  │   tap is roadmap B3, and the xG model has no live consumer because
│   │                                  │   the ledger carries no shot origin (ERR-037-001, filed not worked around).
│   ├── match-analytics.asmdef         ← references EventSystem + MatchEngine + BallPhysics
│   ├── MatchAnalyticsConstants.cs     ← Appendix A: xG [GT] coefficients; pitch + TEAM_COUNT [CROSS] mirrors; heatmap grid; sample stride
│   ├── XgLocationModel.cs             ← §3.3 pure model: distance + subtended-goal-angle logit, overflow-safe logistic, [0,1] clamp, F1/F2 gates on all three entry points
│   ├── StatPoint.cs                   ← one recorded event location + team (the aggregator's input unit)
│   ├── MatchStatline.cs               ← immutable per-team basic statline; _hasValue discriminator so default(T) is not a real 0-0 line
│   ├── AdvancedStatline.cs            ← territorial share + copied heatmap bins; same discriminator
│   ├── MatchAnalyticsResult.cs        ← the per-match result (both statlines); refuses a default-constructed statline
│   └── Tests/
│       ├── match-analytics-tests.asmdef
│       ├── XgLocationModelTests.cs    ← the three §3.3 worked examples + the shape a refit must preserve (monotonicity, home/away mirror, totality, purity) + the gates
│       └── MatchAnalyticsValueTypeTests.cs ← copy-not-wrap on heatmap bins, unset-vs-zero, every fail-loud gate
├── match-viewer/                      ← Presentation tooling (not a numbered spec; observes the match-engine composition root)
│   ├── match-viewer.asmdef            ← references match-engine + deterministic-sim + ball-physics + agent-movement
│   ├── MatchViewerConstants.cs        ← IFAB pitch-marking geometry [FIXED] + canvas/recording presentation [GT]
│   ├── ReplayFrame.cs                 ← one sampled frame: tick / ball / possession / agent positions (value copies)
│   ├── MatchReplay.cs                 ← immutable frame sequence + roster/pitch/cadence metadata
│   ├── MatchReplayRecorder.cs         ← ticks a MatchEngine, sampling its public observation surface (observer-neutral)
│   ├── HtmlReplayExporter.cs          ← self-contained HTML canvas replay: play/pause/scrub/speed; fail-loud non-finite gate
│   ├── AssemblyInfo.cs                ← [InternalsVisibleTo("TacticalDirector.MatchViewer.Tests")] (LiveMatchStreamer.TickOnce seam)
│   ├── LiveMatchFrame.cs              ← interactive match view: one live-captured frame (tick/ball/possession/positions/Scoreline/matchEnded + the P1 cues / substitution counts / period / RestartBanner)
│   ├── LiveAgentCue.cs                ← P1: per-agent HUD cue (yellow cards / sent-off / active bench slot; IsSubstitute derived from the slot)
│   ├── Scoreline.cs                   ← P1 AR-1 M-6: the home/away score pair as one carrier; owns the non-negative gate
│   ├── RestartBanner.cs               ← P1 AR-1 M-6: the latched restart; team + tick DERIVED from the cue, so default(RestartBanner) is a correct "no restart"
│   ├── LiveMatchStreamer.cs           ← interactive match view: real-time-paced MatchEngine tick loop; lock-protected latest-frame surface; pause/resume/speed; full-time auto-pause; owns the P1 cross-tick restart latch (KD-P1-3 — kept OUT of the engine so it never reaches the snapshot)
│   ├── LiveMatchServer.cs             ← interactive match view: loopback-only hand-rolled HTTP server (TcpListener) — GET / (viewer page) / /frame (polled JSON) / /control (playback-only)
│   └── tests/
│       ├── match-viewer-tests.asmdef
│       ├── MatchViewerTests.cs        ← cadence / on-pitch / bitwise determinism / observer-neutral digest / exporter locks
│       ├── LiveMatchStreamerTests.cs  ← latest-frame handoff / observer-neutrality digest / auto-pause / speed guards / lifecycle
│       ├── LiveMatchFrameCueTests.cs ← P1: cue lockstep with positions / substitution counts / derived period / the restart latch (+ its non-vacuity guard)
│       └── LiveMatchServerTests.cs    ← real-loopback-socket routing / control / error-path / abuse-guard / shutdown locks
├── living-world/                      ← Spec #22  (off-pitch layer; engine-free, noEngineReferences; references DeterministicSim)
│   ├── living-world.asmdef            ← references TacticalDirector.DeterministicSim (slice 3: world.text stream + §4.6 serializer)
│   ├── LivingWorldConstants.cs        ← Appendix A catalogue; Fixed (world.text stream ids + WORLD_SNAPSHOT_FORMAT_VERSION) / Cross (DomainTagLivingWorld) / GT
│   ├── LivingWorldMath.cs             ← §3.1 ApplyEvent/ApplyDecay + FR-LW-021 CompareEvictability
│   ├── RelationshipLayer.cs / EventKind.cs / ArcKind.cs / InteractionIntent.cs   ← byte enums, APPEND-only
│   ├── RelationshipEdge.cs / MemoryEpisode.cs / SpawnCause.cs / Arc.cs / ColdSummary.cs  ← value types
│   ├── InteractionSlots.cs            ← §3.3 slot-fact carrier: match-engine facts + optional cited episode (FR-LW-013)
│   ├── WorldClock.cs                  ← season-calendar clock (KD-4): worldTick = calendar day; never the match loops
│   ├── WorldLoop.cs                   ← §4.2 per-tick orchestrator (phases 3/4/6 live; 1/2/5 documented seams); phase-4 arc-trigger evaluate (5th nullable seam)
│   ├── MemoryStore.cs                 ← deep-tier store: canonical-order edges, §3.2 episode evict/decay, FR-LW-018 pins
│   ├── ColdStore.cs                   ← cold tier + §3.5 Compress/Rehydrate (Residue-A v1 schema recorded)
│   ├── ArcEngine.cs                   ← §3.4 arc spawn (atomic pinning)/state/resolve + §6.2 expiry
│   ├── ArcCanonSource.cs              ← arc-triggers KD-1/KD-2: concrete nullable canon-input seam + nested Builder (Stage-0 producer)
│   ├── ArcTrigger.cs / ArcTriggerCatalogue.cs ← arc-triggers KD-3: catalogue row + the APPEND-only Stage-0 trigger table
│   ├── ArcTriggerEvaluator.cs         ← arc-triggers KD-3..KD-7: world.arcs stream (distinct key) + FR-LW-017/021 walk + rising-edge latch + SpawnArc
│   ├── ActiveSetMembership.cs         ← §3.5 entry / LRU demotion at the cap / own-club Depart (FR-LW-023/025)
│   ├── InteractionTextCorpus.cs       ← §3.3 Stage-0 in-code authored templates (per-intent) + episode clauses (per EventKind); APPEND-only order
│   ├── InteractionTextGenerator.cs    ← §3.3 deterministic text: world.text sub-stream draw + slot expansion + §3.2 citation gate (FR-LW-011/012/013/020)
│   ├── WorldStateSerializer.cs        ← §4.6 canonical block (Appendix B order) via #16 CanonicalSerializer; rebuild-through-seams deserialize
│   ├── WorldStore.cs                  ← KD-10 season composition root: owns/wires the 6 services + DeterministicRngService + InteractionTextGenerator (world.text) + ArcTriggerEvaluator (world.arcs) + nullable ArcCanonSource; AdvanceDay drives the loop; Snapshot/Restore = §4.6 block + managerId + world.text + world.arcs (cursor+latch, E2 v3) + FR-LW-022 roster
│   └── Tests/
│       ├── living-world-tests.asmdef  ← + references TacticalDirector.DeterministicSim (RngStreamState/CanonicalSerializer)
│       ├── LivingWorldTests.cs        ← T0: enum ordinals, §3.1 worked examples, eviction tiebreak
│       ├── SeasonWorldLoopTests.cs    ← slice 1: clock/loop/memory/cold-store contracts + determinism
│       ├── ArcMembershipTests.cs      ← slice 2: arc lifecycle/pin-rollback/expiry + membership LRU/depart/promotion
│       ├── WorldTextSnapshotTests.cs  ← slice 3: text determinism (T-LW-DET-003/004) + §3.2 citation gate + §4.6 round-trip/fail-loud
│       ├── WorldStoreTests.cs         ← KD-10: construction/wiring, AdvanceDay phases (decay/arc-expiry), Snapshot/Restore round-trip + determinism + fail-loud gates
│       └── ArcTriggerTests.cs         ← arc-triggers E1+E2: distinct stream key, flag-off round-trip, stub-canon spawn + KD-7 single-fire/re-arm, FR-LW-017/021 order, E2 save@N→restore→advance + latch serialization + re-fire-after-restore lock
├── season-save/                       ← Unified season save-file root (not a numbered spec; the ONLY assembly above BOTH match-engine + living-world — resolves FR-LW-003; unified-season-save-design.md)
│   │                                     Also hosts Season & Competition Loop #30 (T0 value types, T1 codec)
│   │                                     and the league bootstrap (league-bootstrap-design.md) — same layer,
│   │                                     no new assembly.
│   ├── season-save.asmdef             ← references match-engine + living-world + deterministic-sim + player-database + project-constants
│   ├── SeasonLoopConstants.cs         ← #30 Appendix A: points, SEASON_STATE_FORMAT_VERSION, identity-permutation seed
│   ├── Fixture.cs / FixtureScheduler.cs         ← #30 T0: the concrete schedule + the pure round-robin generator (local SplitMix64)
│   ├── LeagueTableRow.cs / LeagueTable.cs       ← #30 T0: ApplyResult + FR-SN-007 tie-break OrderedView
│   ├── SeasonCalendar.cs / BoardObjective.cs / BoardState.cs / MatchResult.cs ← #30 T0
│   ├── SeasonState.cs / SeasonViewModel.cs      ← #30 T0: the serialized season surface (KD-7 internal mutators) + the read-only view
│   ├── SeasonRollOutcome.cs           ← #30 T3: the boundary-roll producer record (board verdict + what the next season starts from)
│   ├── SeasonStateCodec.cs            ← #30 T1: the season sub-blob codec (Appendix B layout; decode through the validating ctors)
│   ├── RoundResolutionMode.cs         ← #30 T2: the §3.4.1 dial (ManagedThroughEngine / QuickSimAll / FullEngine)
│   ├── RoundResolutionModel.cs        ← #30 T2: the keyed quick-sim — FixtureKey folds DOMAIN_TAG_SEASON_LOOP (first draw site, ERR-030-001), exp lambdas + inverse-CDF Poisson
│   ├── SeasonLoop.cs                  ← #30 T2: the composition root — KD-2 day advance, whole-round resolution, FR-SN-016 producer record, Snapshot/Restore
│   ├── LeagueBootstrapConstants.cs    ← A3: seed domains [FIXED], club bounds / strength spread / calendar / position template [GT]
│   ├── ClubNameCatalogue.cs           ← A3: APPEND-only club names, assigned by ClubId (KD-3 — drawn from no stream)
│   ├── Club.cs                        ← A3: club identity (ClubId / Name / StrengthDelta); not serialized
│   ├── League.cs                      ← A3: the immutable bootstrap product; IS the ISquadProvider; CreateSeason → SeasonState (KD-9)
│   ├── LeagueBootstrap.cs             ← A3: worldSeed → N clubs × 25 position-coherent players (KD-4 derivations, KD-5 strength ramp, KD-6 template)
│   ├── SeasonSaveConstants.cs         ← [FIXED] SEASON_SAVE_FORMAT_VERSION = 2 (a fourth format version; KD-4)
│   ├── TrainingBlock.cs               ← typed handle on the #29 sub-blob's bytes at the frame boundary (ERR-029-005): the two blocks are byte-shape-identical, so transposing them in Encode's five byte[] had no compile-time signal
│   ├── MedicalBlock.cs                ← the #41 counterpart (ERR-041-009)
│   ├── SeasonSaveBlobs.cs             ← deframe result: World + Season + Training + Medical (all always) + MatchBlob (null if no match) — five opaque sub-blobs (KD-2/KD-3)
│   ├── SeasonSaveCodec.cs             ← pure static: Encode(world, season, training, medical, matchOrNull) / Decode → v3 frame + matchPresent flag + 5 length-prefixed opaque blocks; overflow-safe bounds + fail-loud (KD-7/KD-8)
│   ├── SeasonSaveContents.cs          ← Load result: reconstructed WorldStore (never null) + nullable MatchEngine
│   ├── SeasonSaveManager.cs           ← static: Save(world, matchOrNull, path) (capture both → Encode → atomic temp→fsync→rename) / Load(path, ISquadProvider = null) → SeasonSaveContents (KD-1/KD-5/KD-6/KD-8)
│   └── tests/
│       ├── season-save-tests.asmdef   ← EditMode; references season-save + match-engine + living-world + deterministic-sim + player-database
│       ├── SeasonSaveManagerTests.cs  ← disk round-trip determinism (no-match / neutral+distinct-squad match via ISquadProvider) + SeasonSaveCodec round-trip/fail-loud + manager fail-loud paths
│       ├── SeasonStateTests.cs        ← #30 T0 value-type + aggregate-field-count coupling guards
│       ├── SeasonStateCodecTests.cs   ← #30 T1 round-trip / pinned-offset layout lock / FR-SN-023 fail-loud gates
│       ├── SeasonLoopTests.cs         ← #30 T2: day advance + FR-SN-026 floor, round completeness, order-independence, F4/F5/F6, mid-sequence restore, 380-fixture season
│       ├── SeasonRollTests.cs         ← #30 T3: pure roll helpers, F5 + cursor gates and their atomicity, FR-SN-029 restartability, and a rolled season played to completion (ERR-030-015)
│       ├── RoundResolutionModelTests.cs ← #30 T2: keyed determinism, per-input key sensitivity, lambda clamps, inverse-CDF endpoints/cap/mean
│       ├── SeasonLoopScenarios.cs / SeasonLoopScenarioTests.cs ← #30 §5.7 season-multi-fixture capstone on the #19 ScenarioRunner (one real engine match)
│       ├── RoundResolutionCalibrationHarness.cs / ...Tests.cs ← A4a corpus harness + env-gated KD-8 Step 0 pilot / corpus driver (ERR-030-014 blocks the fit)
│       ├── EngineScoringDiagnosticTests.cs ← env-gated ERR-030-014 characterisation (asserts nothing about scoring — pinning a defect would make it a contract)
│       └── LeagueBootstrapTests.cs    ← A3: determinism, league-size independence, strength ramp, position coherence (incl. a real ConfigureSquads run), F1–F6, CreateSeason → codec round-trip
├── code-standards/                    ← Spec #20  (governance only; no runtime code)
├── player-database/                    ← Squad/Player Data Layer (T0+T1/T2; candidate spec #27, design-supplement stage)
│   │                                    │   References only DeterministicSim (roster-generation RNG stream).
│   │                                    │   Wired into MatchEngine since T1 (July 17, 2026) via
│   │                                    │   match-engine/PlayerAttributeProjection.cs + ConfigureSquads
│   │                                    │   (player-attribute-projection-design.md; T3 restore fidelity open).
│   ├── player-database.asmdef
│   ├── PlayerDatabaseConstants.cs       ← Fixed/Derived/GT catalogue: attribute bounds, generation tuning, [4][31] position-bias table
│   ├── AttrIdx.cs                       ← ordinal mapping for the 31 int[1,20] attribute fields, shared by ToArray/FromArray/RosterGenerator/SquadFileLoader
│   ├── PlayerAttributes.cs              ← canonical 31-field [1,20] record + WeakFootRating [1,5]; reconciles all 7 existing per-spec attribute structs
│   ├── PlayerPosition.cs                ← enum: Goalkeeper/Defender/Midfielder/Forward (coarse; NOT positioning-ai's 13-value RoleId)
│   ├── PlayerRecord.cs                  ← one player: club-scoped PlayerId + name/age/position + PlayerAttributes
│   ├── Squad.cs                         ← one club's roster (≤ CLUB_SQUAD_SIZE=25 players)
│   ├── NameCatalogue.cs                 ← Stage-0 in-code first/last name pools (32 each)
│   ├── RosterGenerator.cs               ← deterministic generation over DeterministicRngService; stateless, caller registers the stream
│   ├── PlayerGenerationRng.cs           ← shared DrawBounded (biased-but-accepted generation mapping) + Clamp, used by RosterGenerator (#27) + RegenGenerator (#28)
│   ├── SquadFileLoader.cs               ← Stage-0 human-authoring text import (mirrors TeamTacticFileLoader's grammar)
│   └── tests/
│       ├── player-database-tests.asmdef
│       ├── PlayerAttributesTests.cs     ← clamp/array round-trip, identity defaults, position-bias table exact-value locks
│       ├── RosterGeneratorTests.cs      ← determinism, club-scoped PlayerId uniqueness, bounds, exact RNG-budget-per-player lock
│       └── SquadFileLoaderTests.cs      ← grammar round-trip, empty-file default squad, every fail-loud gate
│
├── player-progression/                ← Player Progression & Lifecycle #28 (T0; APPROVED spec)
│   │                                    │   References PlayerDatabase + DeterministicSim only (§4.1). World-tick,
│   │                                    │   draw-free aging core + the pure single-player regen generator.
│   │                                    │   T1 (save codec) / T2 (ProgressionEngine + regen stream) deferred.
│   ├── player-progression.asmdef
│   ├── PlayerProgressionConstants.cs    ← Appendix A catalogue (Fixed/Derived/Cross/GT); the 0x20/82 RNG mirrors land at T2 (KD-B)
│   ├── PlayerLifecycle.cs               ← per-player overlay value type (§2.2): PA / CA cache / long GrowthCursor / BirthWorldDay / retirement
│   ├── TrainingInput.cs                 ← the #29 seam value type (Neutral identity, §4.5 — no phantom interface)
│   ├── AbilityModel.cs                  ← pure: ComputeCA + ClassifyAgeBand + weighted spend/drain + the AgeBand enum (§3.1.2/§3.2)
│   ├── GrowthProjection.cs              ← pure per-player daily step (§3.1; sole attribute-mutation path; curve-off KD-8 identity)
│   ├── RegenGenerator.cs                ← pure single-player generation (§3.3; fixed PROGRESSION_REGEN_FIELDS budget; fresh id)
│   └── tests/
│       ├── player-progression-tests.asmdef
│       ├── PlayerProgressionConstantsTests.cs ← balance-pass invariant locks (POINT_COST==DAYS_PER_YEAR, band order, regen-field derivation)
│       ├── AbilityModelTests.cs         ← T-PG-CA-001/002/003: CA determinism, F1 PA ceiling, weighted spend/drain order
│       ├── GrowthProjectionTests.cs     ← T-PG-DET-001/002 + ID-001/002: byte-exact growth/decline, value-copy save, age gap-independence
│       └── RegenGeneratorTests.cs       ← T-PG-REG-001/003: regen determinism, exact budget, bounds, CA≤PA room-to-grow (test-local ordinal, KD-B)
│
├── training-system/                   ← Training System #29 (T0 Aug 5 2026, T1 Aug 6 2026; APPROVED spec)
│   │                                    │   References PlayerProgression + PlayerDatabase + DeterministicSim + ProjectConstants (§4.1).
│   │                                    │   World-tick conditioning/fatigue; DRAW-FREE (FR-TR-008) — 0x21/83 stay reserved.
│   │                                    │   T1 landed (save codec, composed into #30's frame). Nothing PRODUCES state yet: T2 (#30 slot wiring) deferred.
│   ├── training-system.asmdef
│   ├── AssemblyInfo.cs                  ← InternalsVisibleTo the test assembly (the own-attribute terms are internal)
│   ├── TrainingSystemConstants.cs       ← Appendix A catalogue; no RNG constant (KD-6)
│   ├── TrainingFocus.cs                 ← the six-value focus enum (APPEND-only ordinals — indexed + persisted)
│   ├── TrainingState.cs                 ← §2.2 per-player state + Create (never-advanced sentinel; default is NOT valid)
│   ├── TrainingSchedule.cs              ← the FR-TR-003 read-only VIEW over per-player focus; stores nothing, never serialized
│   ├── CoachingModifier.cs              ← KD-3 staff seam (empty at T0, so Identity is safely default)
│   ├── InjuryRiskContribution.cs        ← KD-5 read-only scalar #41 consumes (FR-TR-017)
│   ├── TrainingViewModel.cs             ← KD-7 value-copy observer for #31/#38
│   ├── ClubTrainingStates.cs            ← one club's persisted block: club id + the parallel id/state arrays, bound once
│   ├── TrainingSaveCodec.cs             ← §4.4.1 TRAINING_SAVE_FORMAT_VERSION sub-blob (T1); canonical ascending keys, fail-loud both ways
│   ├── TrainingStep.cs                  ← §3.1 AdvanceTrainingDay / §3.2 ComputeTrainingInput / §3.3 ProjectMatchEntryFatigue / §3.4 ComputeInjuryRisk / FR-TR-023 SetFocus
│   └── tests/
│       ├── training-system-tests.asmdef
│       ├── TrainingStepTests.cs         ← Appendix B day by day + T-TR-DET/NEU/FAT/CON/COA/INJ
│       ├── TrainingScheduleTests.cs     ← view-not-copy, parallel-array guard, T-TR-FAIL-003
│       ├── TrainingSaveCodecTests.cs     ← round-trip field identity, every focus ordinal, order-independence, T-TR-FAIL-001
│       └── TrainingSystemConstantsTests.cs ← catalogue invariants (table coverage, bound order, Rest nets negative)
│
├── injuries-medical/                  ← Injuries & Medical #41 (T0 Aug 5 2026, T1 Aug 6 2026; APPROVED spec)
│   │                                    │   References TrainingSystem + PlayerDatabase + DeterministicSim + ProjectConstants (§4.1).
│   │                                    │   ONE keyed draw, no registered stream (KD-1 / ERR-041-002) ⇒ nothing but InjuryState persists.
│   │                                    │   T1 landed (save codec, composed into #30's frame). Nothing PRODUCES state yet: T2 (#30 slot + availability read) deferred.
│   ├── injuries-medical.asmdef
│   ├── AssemblyInfo.cs                  ← InternalsVisibleTo the test assembly (the keyed draw is internal)
│   ├── InjuriesMedicalConstants.cs      ← Appendix A catalogue; [CROSS] DomainTagInjuriesMedical = 0x2A (no SubsystemOrdinal — no stream)
│   ├── InjurySeverity.cs                ← Stage-2 tiers; None = 0 = healthy (APPEND-only ordinals — persisted as a byte)
│   ├── InjuryState.cs                   ← §2.2 per-player state + Create (never-advanced sentinel; default is NOT valid)
│   ├── MatchLoad.cs                     ← FR-MD-010 caller-supplied input; HardContacts is deep-tier, weighted 0 at Stage 2
│   ├── MedicalModifier.cs               ← KD-5 staff seam, per-mille ints with an EXPLICIT Identity (default fails loud, FR-MD-016)
│   ├── MedicalViewModel.cs              ← KD-8 value-copy observer; Available derives through MedicalStep.IsAvailable
│   ├── ClubInjuryStates.cs              ← one club's persisted block, on #29's ClubTrainingStates terms
│   ├── MedicalSaveCodec.cs              ← §4.4 MEDICAL_SAVE_FORMAT_VERSION sub-blob (T1); ClubId written (ERR-041-008), F1 gate on BOTH sides
│   ├── MedicalStep.cs                   ← §3.1 AdvanceMedicalDay (recovery THEN draw, KD-6 entry gate) / DeriveActionOrdinal / DrawOccurrence / §3.2 ClassifySeverityFromDraw / §3.4 AssembleRiskScore / IsAvailable
│   └── tests/
│       ├── injuries-medical-tests.asmdef
│       ├── MedicalStepTests.cs          ← §3.6 term by term + T-MD-DET/ORD/SEV/REC/MOD/NEU/AVAIL/FAIL
│       ├── MedicalSaveCodecTests.cs      ← round-trip field identity, every severity tier, the no-RNG-cursor block-size lock, F1/F3/F4/F5
│       └── InjuriesMedicalConstantsTests.cs ← catalogue invariants, the [CROSS] tag mirror, the #29/#41 shared-scale lock
│
└── tactical-instructions/             ← Spec #21  (T0 — bottom-of-graph data assembly; behaviour-neutral)
    │                                  │   References only project-constants (FR-TI-002); empty asmdef refs until that assembly exists.
    │                                  │   Seams into #8/#11–#15 land at T2–T3 (gated on match-engine Phase C/D + [GT] loader).
    ├── tactical-instructions.asmdef
    ├── Mentality.cs / Tempo.cs / TacticWidth.cs / TacticDefWidth.cs / LineOfEngagement.cs
    ├── TransitionPlan.cs / GkDistributionPolicy.cs / FocusPlay.cs / TacticPassing.cs / TacticPressing.cs
    ├── TacticTriggerMask.cs ([Flags]) / TacticFormation.cs / Duty.cs / PlayerRole.cs / InstrBias.cs / SetPieceDutyFlags.cs ([Flags])
    ├── MarkingOrientation.cs / DismarkIntensity.cs / BuildUpStructure.cs / RotationFreedom.cs  ← appended dials (cheap-item + ERR-021-005/006/007 back-props; zero/seeded identities)
    ├── TeamTactic.cs / PlayerInstructions.cs / PlayerTactic.cs   ← readonly-struct carriers + identity factories (Balanced / Default)
    ├── TacticPreset.cs / TacticPresetLibrary.cs                  ← #26 T0: named #21-space points + the pinned A.1 ladder catalogue
    ├── TacticalPresetsConstants.cs                               ← #26 §3.5 + A.2/A.3 catalogue (manager-decision cadence/adaptation scalars + archetype/affinity [GT] tables)
    ├── TacticalInstructionsConstants.cs                          ← Appendix A catalogue (Fixed/Derived/GT); identity rows exact
    └── Tests/
        ├── tactical-instructions-tests.asmdef
        ├── EnumOrdinalStabilityTests.cs   ← FR-TI-007 ordinal / bit-position / byte-backing locks (19 enums)
        ├── FactoryIdentityTests.cs        ← FR-TI-031 identity-factory + catalogue identity-row locks
        └── TacticPresetLibraryTests.cs    ← #26 T0: pinned ladder order + A.1 compositions + KD-7 identity discipline
```
