// File:     src/goalkeeper-mechanics/GoalkeeperMechanics.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.1–§3.8, §4.6, KD-9, KD-12, KD-13, KD-15, KD-16, Code Standards #20
// Purpose:  Main 10 Hz + 60 Hz orchestrator. Manages per-GK state, dive kinematics, reaction pipeline,
//           handling quality, cross-claim duels, rush dispatch, and distribution. Constructor-injected.

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// 10 Hz and 60 Hz orchestrator for Goalkeeper Mechanics #11.
    /// Dispatches §3.2–§3.8 sub-systems per GK per frame. Resolves cross-claim duels.
    /// Publishes SaveAttemptedEvent / BallClaimedEvent / DistributionExecutedEvent / GoalkeeperRushEvent.
    /// Constructor-injected dependencies (FR-CS-051–054). Zero heap allocation on hot path.
    /// Goalkeeper Mechanics #11 §4.6.
    /// </summary>
    public sealed class GoalkeeperMechanics
    {
        // ── Dependencies ─────────────────────────────────────────────────────────────

        private readonly IGoalkeeperBallSystem _ballSystem;
        private readonly IGoalkeeperRngService _rng;
        private readonly GoalkeeperCrossClaimDuel _crossClaimDuel;
        private readonly GoalkeeperTelemetry   _telemetry;

        // ── Per-GK state arrays (pre-allocated; indexed by [0, MaxGkAgents)) ────────

        private readonly GoalkeeperState[]             _states;
        private readonly GoalkeeperAgentAttributes[]   _attrs;
        private readonly GkContactState[]              _contactStates;
        private readonly SaveIntent[]                  _saveIntents;
        private readonly bool[]                        _saveIntentActive;
        private readonly RushIntent[]                  _rushIntents;
        private readonly bool[]                        _rushIntentActive;
        private readonly DistributeIntent[]            _distributeIntents;
        private readonly bool[]                        _distributeIntentActive;
        private readonly GoalkeeperPositioningContract[] _positioningContracts;

        // Dive state
        private readonly int[]   _diveLaunchFrames;
        private readonly int[]   _diveDurationFrames;
        private readonly float[] _divePeakHandZ;
        private readonly float[] _diveDirectionX;
        private readonly float[] _rushLaunchMps;

        // Shot reaction state
        private readonly float[] _shotDetectedTickMs;
        private readonly float[] _requiredReactionMs;
        private readonly bool[]  _shotEventPending;

        // Hold-rule state
        private readonly int[]  _claimTick;
        private readonly int[]  _releaseTickEarliest;
        private readonly int[]  _recoveryCooldownEndTick;

        // ── Profiler Markers ─────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_tacticalTickMarker =
            new ProfilerMarker("GoalkeeperMechanics.TacticalTick");

        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("GoalkeeperMechanics.Update");

        // ── Constructor ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Allocates all per-GK buffers and wires dependencies.
        /// No allocation occurs during TacticalTick or Update calls after this point.
        /// Goalkeeper Mechanics #11 §4.6.
        /// </summary>
        public GoalkeeperMechanics(
            IGoalkeeperBallSystem ballSystem,
            IGoalkeeperRngService rng)
        {
            _ballSystem     = ballSystem;
            _rng            = rng;
            _crossClaimDuel = new GoalkeeperCrossClaimDuel();
            _telemetry      = new GoalkeeperTelemetry();

            int maxGks = GoalkeeperConstants.MaxGkAgents;

            _states                 = new GoalkeeperState[maxGks];
            _attrs                  = new GoalkeeperAgentAttributes[maxGks];
            _contactStates          = new GkContactState[maxGks];
            _saveIntents            = new SaveIntent[maxGks];
            _saveIntentActive       = new bool[maxGks];
            _rushIntents            = new RushIntent[maxGks];
            _rushIntentActive       = new bool[maxGks];
            _distributeIntents      = new DistributeIntent[maxGks];
            _distributeIntentActive = new bool[maxGks];
            _positioningContracts   = new GoalkeeperPositioningContract[maxGks];

            _diveLaunchFrames       = new int[maxGks];
            _diveDurationFrames     = new int[maxGks];
            _divePeakHandZ          = new float[maxGks];
            _diveDirectionX         = new float[maxGks];
            _rushLaunchMps          = new float[maxGks];

            _shotDetectedTickMs     = new float[maxGks];
            _requiredReactionMs     = new float[maxGks];
            _shotEventPending       = new bool[maxGks];

            _claimTick              = new int[maxGks];
            _releaseTickEarliest    = new int[maxGks];
            _recoveryCooldownEndTick = new int[maxGks];

            // Sentinel init
            for (int i = 0; i < maxGks; i++)
            {
                _diveLaunchFrames[i]        = -1;
                _claimTick[i]               = -1;
                _releaseTickEarliest[i]      = int.MaxValue;
                _recoveryCooldownEndTick[i]  = 0;
                _contactStates[i]           = GkContactState.CreateNew();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the Positioning AI #12 baseline slot for the specified GK.
        /// Called once per 10 Hz tick per §4.6.1 / KD-13.
        /// </summary>
        public void UpdateBaselineSlot(int gkIndex, Vector2 gkBaselineSlot)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _positioningContracts[gkIndex].GkBaselineSlot = gkBaselineSlot;
        }

        /// <summary>
        /// Notifies that a ShotExecutedEvent has been consumed for the specified GK.
        /// Called by the event subscription handler; sets the pending-shot flag for the next 60 Hz tick.
        /// §3.2 / §4.6.2.
        /// </summary>
        public void OnShotExecutedEvent(int gkIndex, float shotMatchTimeMs, float ballSpeedMps)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _shotEventPending[gkIndex]   = true;
            _shotDetectedTickMs[gkIndex] =
                GoalkeeperReactionPipeline.ComputeShotDetectedTickMs(shotMatchTimeMs, _attrs[gkIndex]);
            _requiredReactionMs[gkIndex] =
                GoalkeeperReactionPipeline.ComputeRequiredReactionMs(_attrs[gkIndex], ballSpeedMps, _states[gkIndex]);
        }

        /// <summary>
        /// Commits a SaveIntent for the specified GK from the 10 Hz Decision Tree output.
        /// §3.1 / §4.6.1.
        /// </summary>
        public void CommitSaveIntent(int gkIndex, SaveIntent intent, GoalkeeperAgentAttributes attrs)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _saveIntents[gkIndex]      = intent;
            _saveIntentActive[gkIndex] = true;
            _attrs[gkIndex]            = attrs;
            _contactStates[gkIndex]    = GkContactState.CreateNew();
            _contactStates[gkIndex].HandChoice       = intent.TargetHand;
            _contactStates[gkIndex].ClutchFirmness   = intent.ClutchFirmness;
        }

        /// <summary>
        /// Commits a RushIntent for the specified GK from the 10 Hz Decision Tree output.
        /// §3.7 / §4.6.1.
        /// </summary>
        public void CommitRushIntent(int gkIndex, RushIntent intent, GoalkeeperAgentAttributes attrs)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _rushIntents[gkIndex]      = intent;
            _rushIntentActive[gkIndex] = true;
            _attrs[gkIndex]            = attrs;
            _rushLaunchMps[gkIndex]    = GoalkeeperRushDispatch.ComputeRushLaunchMps(attrs);
        }

        /// <summary>
        /// Commits a DistributeIntent for the specified GK from the 10 Hz Decision Tree output.
        /// §3.8 / §4.6.1.
        /// </summary>
        public void CommitDistributeIntent(int gkIndex, DistributeIntent intent)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _distributeIntents[gkIndex]      = intent;
            _distributeIntentActive[gkIndex] = true;
        }

        // ── 10 Hz tactical loop ──────────────────────────────────────────────────────

        /// <summary>
        /// 10 Hz tactical tick entry point per §4.6.1.
        /// Evaluates state-machine transitions, applies positioning micro-update,
        /// and updates hold-rule counters.
        /// Called once per GK in #16 §3.2 entity order. §3.1.2.
        /// </summary>
        /// <param name="currentTick">Current 10 Hz tactical tick index.</param>
        /// <param name="agentStates">AM #2 agent state array indexed by agentId.</param>
        /// <param name="ballState">Current BallState snapshot from Ball Physics #1.</param>
        /// <param name="gkAgentIds">Array of GK agent IDs, indexed by gkIndex (length = MaxGkAgents).</param>
        public void TacticalTick(
            int currentTick,
            AgentState[] agentStates,
            BallState ballState,
            int[] gkAgentIds)
        {
            using var _ = s_tacticalTickMarker.Auto();

            for (int gkIndex = 0; gkIndex < GoalkeeperConstants.MaxGkAgents; gkIndex++)
            {
                int agentId = gkAgentIds[gkIndex];
                if ((uint)agentId >= (uint)agentStates.Length)
                {
                    continue;
                }

                AgentState agentState = agentStates[agentId];
                GoalkeeperAgentAttributes attrs = _attrs[gkIndex];
                GoalkeeperPositioningContract positioning = _positioningContracts[gkIndex];

                // Ball in attacking third from this GK's perspective:
                // Team 0 (defends X=0): attacking third is X > BallAttackingThirdXM (≈70m).
                // Team 1 (defends X=105): attacking third is X < (PitchLengthM - BallAttackingThirdXM) (≈35m).
                // Simplified Stage 0: attacker-controlled possession approximated by ball position only.
                bool ballInAttackingThird = attrs.TeamId == 0
                    ? ballState.Position.x >= GoalkeeperConstants.BallAttackingThirdXM
                    : ballState.Position.x <= GoalkeeperConstants.PitchLengthM - GoalkeeperConstants.BallAttackingThirdXM;

                // Ball in defensive third heuristic for GK team.
                // Defensive third boundary = PitchLengthM - BallAttackingThirdXM for team 0;
                // BallAttackingThirdXM for team 1 (mirror). BallAttackingThirdXM = 2/3 * PitchLengthM.
                float defensiveThirdBoundary = GoalkeeperConstants.PitchLengthM
                                             - GoalkeeperConstants.BallAttackingThirdXM;
                bool ballInDefensiveThird = (attrs.TeamId == 0)
                    ? ballState.Position.x < defensiveThirdBoundary
                    : ballState.Position.x > GoalkeeperConstants.BallAttackingThirdXM;

                // Anticipation score from Decision Tree — Stage 0 approximation based on ball proximity
                // Full Decision Tree integration is a Stage 1 concern per §4.6.1
                // Stage 0 stub: full Decision Tree anticipation score at Stage 1 (§4.6.1)
                float anticipationScore = ballInAttackingThird
                    ? GoalkeeperConstants.Stage0AnticipationScoreActive
                    : 0.0f;

                float rushCommitmentLevel = _rushIntentActive[gkIndex]
                    ? _rushIntents[gkIndex].CommitmentLevel : 0.0f;

                Vector2 gkXY = new Vector2(agentState.Position.x, agentState.Position.y);

                GoalkeeperState newState = GoalkeeperStateMachine.EvaluateTacticalTransition(
                    currentState:           _states[gkIndex],
                    ballState:              ballState,
                    hasSaveIntent:          _saveIntentActive[gkIndex],
                    hasRushIntent:          _rushIntentActive[gkIndex],
                    hasDistributeIntent:    _distributeIntentActive[gkIndex],
                    anticipationScore:      anticipationScore,
                    rushCommitmentLevel:    rushCommitmentLevel,
                    currentTick:            currentTick,
                    claimTick:              _claimTick[gkIndex],
                    releaseTickEarliest:    _releaseTickEarliest[gkIndex],
                    recoveryCooldownEndTick: _recoveryCooldownEndTick[gkIndex],
                    gkPosition:             gkXY,
                    gkBaselineSlot:         positioning.GkBaselineSlot,
                    ballInAttackingThird:   ballInAttackingThird,
                    ballInDefensiveThird:   ballInDefensiveThird);

                // Forced release telemetry
                if (_states[gkIndex] == GoalkeeperState.HandsOnBall &&
                    newState == GoalkeeperState.Distributing &&
                    _claimTick[gkIndex] >= 0 &&
                    (currentTick - _claimTick[gkIndex]) >= GoalkeeperConstants.GK_HOLD_MAX_TICKS)
                {
                    _telemetry.RecordForcedRelease(agentId);
                }

                _states[gkIndex] = newState;
            }
        }

        // ── 60 Hz physics loop ───────────────────────────────────────────────────────

        /// <summary>
        /// 60 Hz physics tick entry point per §4.6.2.
        /// Advances dive kinematics, resolves hand-ball contacts, handles rush updates,
        /// manages distribution release, and publishes events.
        /// Called once per GK per physics frame. §3.1.2 / §4.6.2.
        /// </summary>
        /// <param name="currentFrame">Current 60 Hz physics frame index.</param>
        /// <param name="currentMatchTimeMs">Current match time (ms) from kickoff.</param>
        /// <param name="agentStates">AM #2 agent state array indexed by agentId.</param>
        /// <param name="ballState">Current BallState snapshot from Ball Physics #1.</param>
        /// <param name="gkAgentIds">Array of GK agent IDs, indexed by gkIndex (length = MaxGkAgents).</param>
        public void Update(
            int currentFrame,
            float currentMatchTimeMs,
            AgentState[] agentStates,
            BallState ballState,
            int[] gkAgentIds)
        {
            using var _ = s_updateMarker.Auto();

            _crossClaimDuel.ClearFrameBuffer();

            for (int gkIndex = 0; gkIndex < GoalkeeperConstants.MaxGkAgents; gkIndex++)
            {
                int agentId = gkAgentIds[gkIndex];
                if ((uint)agentId >= (uint)agentStates.Length)
                {
                    continue;
                }

                AgentState agentState = agentStates[agentId];
                GoalkeeperState gkState = _states[gkIndex];

                // ── 60 Hz shot detection (Anticipate early trigger via ShotExecutedEvent) ──
                bool shotEventDetected = _shotEventPending[gkIndex];
                _shotEventPending[gkIndex] = false;

                // ── Per-frame reaction window update ──────────────────────────────────
                float reactionWindowAchieved = 0.0f;
                if (gkState == GoalkeeperState.Anticipate || gkState == GoalkeeperState.Diving || gkState == GoalkeeperState.Airborne)
                {
                    if (_shotDetectedTickMs[gkIndex] > 0.0f)
                    {
                        float elapsed = currentMatchTimeMs - _shotDetectedTickMs[gkIndex];
                        reactionWindowAchieved = GoalkeeperReactionPipeline.ComputeReactionWindowAchieved(
                            elapsed, _requiredReactionMs[gkIndex]);

                        _contactStates[gkIndex].ReactionWindowAchieved = reactionWindowAchieved;

                        ReactionLabel reactionLabel = GoalkeeperReactionPipeline.ComputeReactionLabel(reactionWindowAchieved);
                        _telemetry.RecordSaveReactionWindow(reactionWindowAchieved, reactionLabel);
                    }
                }

                // ── Dive kinematics (Diving / Airborne) ──────────────────────────────
                bool handBallContactOccurred = false;
                float handlingQualityScalar  = 0.0f;
                bool groundReEntry           = false;

                if (gkState == GoalkeeperState.Diving)
                {
                    // Transition Diving → Airborne: launch impulse applied this frame
                    if (_diveLaunchFrames[gkIndex] < 0)
                    {
                        _diveLaunchFrames[gkIndex]   = currentFrame;
                        _diveDurationFrames[gkIndex] = GoalkeeperDiveKinematics.ComputeDiveDurationFrames();

                        // Compute timing jitter via draw-site
                        float jitterGaussian = _rng.NextGaussian(
                            GoalkeeperConstants.DrawSiteDiveTimingJitter,
                            GoalkeeperConstants.DomainTagGoalkeeper);
                        float jitterMs = GoalkeeperConstants.DiveTimingJitterSigmaMs * jitterGaussian;

                        _divePeakHandZ[gkIndex] = GoalkeeperDiveKinematics.ComputePeakHandZ(_attrs[gkIndex], jitterMs);

                        // Dive direction from save intent
                        _diveDirectionX[gkIndex] = _saveIntentActive[gkIndex]
                            ? ComputeDiveDirectionX(_saveIntents[gkIndex], agentState)
                            : 0.0f;

                        // Emit rush event (Launched phase) if applicable — not for save dives
                    }
                }

                if (gkState == GoalkeeperState.Airborne)
                {
                    int launchFrame    = _diveLaunchFrames[gkIndex];
                    int durationFrames = _diveDurationFrames[gkIndex];

                    float handZ = GoalkeeperDiveKinematics.ComputeHandPathZ(
                        currentFrame, launchFrame, durationFrames, _divePeakHandZ[gkIndex]);

                    // Ground re-entry check: end of dive phase without contact
                    int frameOffset = currentFrame - launchFrame;
                    groundReEntry = frameOffset >= durationFrames;

                    if (!groundReEntry)
                    {
                        // Check if ball is within reach envelope
                        float reachRadius = GoalkeeperDiveKinematics.ComputeReachRadius(_attrs[gkIndex]);
                        Vector3 gkPos3    = agentState.Position;
                        Vector3 reachCenter = GoalkeeperDiveKinematics.ComputeReachCenter(
                            gkPos3, currentFrame, launchFrame, durationFrames,
                            _diveDirectionX[gkIndex], handZ);

                        float distToBallSq = (ballState.Position - reachCenter).sqrMagnitude;

                        if (distToBallSq <= reachRadius * reachRadius)
                        {
                            handBallContactOccurred = true;

                            // Compute handling quality
                            // Raw unit-variance Gaussian draws; GoalkeeperHandlingQuality.Compute
                            // scales handlingScaleNoise by HandlingNoiseSigma internally (§3.5.1).
                            float handlingNoiseRaw = _rng.NextGaussian(
                                GoalkeeperConstants.DrawSiteHandlingNoise,
                                GoalkeeperConstants.DomainTagGoalkeeper);

                            float pointNoiseRaw = _rng.NextGaussian(
                                GoalkeeperConstants.DrawSiteHandlingPointNoise,
                                GoalkeeperConstants.DomainTagGoalkeeper);

                            float pointNoise = GoalkeeperConstants.HandlingPointErrorSigmaM * pointNoiseRaw;

                            handlingQualityScalar = GoalkeeperHandlingQuality.Compute(
                                attrs:                  _attrs[gkIndex],
                                handContactActual:      reachCenter,
                                targetHandContact:      ballState.Position,
                                ballSpeedMps:           ballState.Velocity.magnitude,
                                reactionWindowAchieved: _contactStates[gkIndex].ReactionWindowAchieved,
                                state:                  gkState,
                                handlingScaleNoise:     handlingNoiseRaw,
                                pointErrorNoise:        pointNoise,
                                handlingLabel:          out HandlingQualityLabel handlingLabel);

                            _contactStates[gkIndex].HandlingQualityScalar = handlingQualityScalar;
                            _contactStates[gkIndex].ActualContactFrame     = currentFrame;

                            _telemetry.RecordSaveHandlingQuality(handlingQualityScalar, handlingLabel);
                            _telemetry.RecordSaveOutcome(handlingLabel);

                            // Publish SaveAttemptedEvent
                            ReactionLabel rLabel = GoalkeeperReactionPipeline.ComputeReactionLabel(
                                _contactStates[gkIndex].ReactionWindowAchieved);

                            SaveAttemptedEvent saveEvt = new SaveAttemptedEvent
                            {
                                AgentId                = agentId,
                                MatchTimeMs            = currentMatchTimeMs,
                                HandlingQualityScalar  = handlingQualityScalar,
                                HandlingLabel          = handlingLabel,
                                ReactionWindowAchieved = _contactStates[gkIndex].ReactionWindowAchieved,
                                ReactionLabel          = rLabel,
                                IncomingBallState      = ballState,
                                ContactPointError      = _contactStates[gkIndex].ContactPointError,
                                FailureCause           = handlingLabel == HandlingQualityLabel.Missed
                                                            ? FailureCause.MissedContact
                                                            : default,
                                HandContactPosition    = reachCenter,
                                HandUsed               = _contactStates[gkIndex].HandChoice,
                                ContactBodyPart        = BodyPartEnum.Hand
                            };

                            EventBusStub.Publish(in saveEvt);

                            // If caught: Ball.SetPossessor + BallClaimedEvent
                            if (handlingQualityScalar >= GoalkeeperConstants.CatchThreshold)
                            {
                                _ballSystem.SetPossessor(agentId);
                                _claimTick[gkIndex]            = currentFrame / GoalkeeperConstants.FramesPerTacticalTick;
                                _releaseTickEarliest[gkIndex]   = _claimTick[gkIndex] + 1;

                                BallClaimedEvent claimEvt = new BallClaimedEvent
                                {
                                    AgentId               = agentId,
                                    MatchTimeMs           = currentMatchTimeMs,
                                    HandlingQualityScalar = handlingQualityScalar,
                                    ClaimType             = ClaimType.ShotCatch,
                                    ClaimPosition         = reachCenter,
                                    ContactBodyPart       = BodyPartEnum.Hand,
                                    ContestedDuelId       = -1
                                };

                                EventBusStub.Publish(in claimEvt);
                                _telemetry.RecordBallClaim(ClaimType.ShotCatch);
                            }
                            else if (handlingQualityScalar >= GoalkeeperConstants.ParryThreshold)
                            {
                                Vector3 parryVel = GoalkeeperHandlingQuality.ComputeParryVelocity(
                                    ballState.Velocity, handlingQualityScalar,
                                    _contactStates[gkIndex].ClutchFirmness);

                                _ballSystem.ApplyKick(parryVel, ballState.AngularVelocity, agentId, currentMatchTimeMs);
                            }
                            else if (handlingQualityScalar >= GoalkeeperConstants.DeflectThreshold)
                            {
                                Vector3 deflectionTarget = _saveIntentActive[gkIndex] && _saveIntents[gkIndex].DeflectionTarget.HasValue
                                    ? _saveIntents[gkIndex].DeflectionTarget.Value
                                    : ballState.Position + ballState.Velocity.normalized * GoalkeeperConstants.Stage0DeflectFallbackProjectionM;

                                Vector3 deflectVel = GoalkeeperHandlingQuality.ComputeDeflectVelocity(
                                    ballState.Velocity, handlingQualityScalar, deflectionTarget, reachCenter);

                                _ballSystem.ApplyKick(deflectVel, ballState.AngularVelocity, agentId, currentMatchTimeMs);
                            }
                            else if (handlingQualityScalar >= GoalkeeperConstants.MinHandlingQuality)
                            {
                                Vector3 spillVel = GoalkeeperHandlingQuality.ComputeSpillVelocity(
                                    ballState.Velocity, handlingQualityScalar);

                                _ballSystem.ApplyKick(spillVel, ballState.AngularVelocity, agentId, currentMatchTimeMs);
                            }
                        }
                    }
                }

                // ── Rush update ───────────────────────────────────────────────────────
                bool rushBallIntercepted            = false;
                bool attackerWithinOneVsOneRadius   = false;
                bool gkWithinSmotherRadius          = false;

                if (gkState == GoalkeeperState.Rushing)
                {
                    Vector3 gkMutablePos = agentState.Position;
                    GoalkeeperRushDispatch.UpdateRushFrame(
                        ref gkMutablePos,
                        _rushIntents[gkIndex].RushTarget,
                        _rushLaunchMps[gkIndex]);

                    // F-08 ball-interception check (KD-15)
                    int ballPossessorId = _ballSystem.GetBallPossessorId();
                    int gkId           = agentId;
                    rushBallIntercepted = ballPossessorId >= 0
                                       && ballPossessorId != gkId
                                       && ballPossessorId != GetInitialAttackerTargetId(gkIndex);

                    if (rushBallIntercepted)
                    {
                        GoalkeeperRushEvent rushEvt = new GoalkeeperRushEvent
                        {
                            AgentId        = agentId,
                            MatchTimeMs    = currentMatchTimeMs,
                            RushPhase      = RushPhase.Aborted,
                            AbortReason    = AbortReason.BallIntercepted,
                            RushTarget     = _rushIntents[gkIndex].RushTarget,
                            GkPosition     = agentState.Position,
                            RushLaunchMps  = _rushLaunchMps[gkIndex]
                        };
                        EventBusStub.Publish(in rushEvt);
                        _telemetry.RecordRushAbort(AbortReason.BallIntercepted);
                        _rushIntentActive[gkIndex] = false;
                    }
                    else
                    {
                        // Check 1v1 trigger and smother trigger
                        attackerWithinOneVsOneRadius = CheckAttackerWithinRadius(
                            agentState.Position, ballState, GoalkeeperConstants.OneVsOneTriggerRadiusM);

                        gkWithinSmotherRadius = CheckAttackerWithinRadius(
                            agentState.Position, ballState, GoalkeeperConstants.SmotherTriggerRadiusM);

                        if (gkWithinSmotherRadius)
                        {
                            // Transition will happen in physics transition evaluation
                        }
                    }
                }

                // ── Distribution release ──────────────────────────────────────────────
                bool distributionReleaseReached = false;

                if (gkState == GoalkeeperState.Distributing && _distributeIntentActive[gkIndex])
                {
                    ref DistributeIntent distIntent = ref _distributeIntents[gkIndex];

                    GoalkeeperDistribution.ValidateTarget(
                        ref distIntent,
                        agentRosterContains: true, // Stage 0 stub: always true pending roster query
                        receiverMissingWarningEmitted: out bool receiverWarning,
                        targetPointClamped: out bool targetClamped);

                    if (receiverWarning) _telemetry.WarnDistributionTargetReceiverMissing(agentId);
                    if (targetClamped)   _telemetry.WarnDistributionTargetPointClamped(agentId);

                    float windupMs      = GoalkeeperDistribution.ComputeWindupMs(distIntent.DeliveryKind);
                    float accuracyCoeff = GoalkeeperDistribution.ComputeAccuracyCoeff(distIntent.DeliveryKind, _attrs[gkIndex]);
                    Vector3 releasePoint = GoalkeeperDistribution.ComputeReleasePoint(agentState.Position, distIntent.DeliveryKind);

                    // Stage 0 approximation: release on the frame the state was entered (windup is tracked externally)
                    distributionReleaseReached = true;

                    float emittedPower = distIntent.PowerIntent * accuracyCoeff;

                    DistributionExecutedEvent distEvt = new DistributionExecutedEvent
                    {
                        AgentId          = agentId,
                        MatchTimeMs      = currentMatchTimeMs,
                        DeliveryKind     = distIntent.DeliveryKind,
                        ReleasePoint     = releasePoint,
                        // DistributeIntent.TargetReceiverId is int? (null = zone-targeted);
                        // the event field is int with sentinel -1 for zone (v1.2 AR-2 row).
                        TargetReceiverId = distIntent.TargetReceiverId ?? -1,
                        TargetPoint      = distIntent.TargetPoint,
                        EmittedPowerIntent = emittedPower,
                        WindupMs         = windupMs
                    };

                    EventBusStub.Publish(in distEvt);
                    _telemetry.RecordDistribution(distIntent.DeliveryKind);
                    _distributeIntentActive[gkIndex] = false;
                }

                // ── Physics state transition ──────────────────────────────────────────
                _states[gkIndex] = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                    currentState:                 gkState,
                    handBallContactOccurred:      handBallContactOccurred,
                    handlingQualityScalar:         handlingQualityScalar,
                    groundReEntry:                groundReEntry,
                    distributionReleaseReached:    distributionReleaseReached,
                    rushBallIntercepted:           rushBallIntercepted,
                    attackerWithinOneVsOneRadius:  attackerWithinOneVsOneRadius,
                    gkWithinSmotherRadius:         gkWithinSmotherRadius,
                    shotEventDetected:             shotEventDetected);

                // ── Recovery cooldown tracking ────────────────────────────────────────
                if (_states[gkIndex] == GoalkeeperState.Recovering &&
                    gkState != GoalkeeperState.Recovering)
                {
                    int currentTacticalTick = currentFrame / GoalkeeperConstants.FramesPerTacticalTick;
                    _recoveryCooldownEndTick[gkIndex] = currentTacticalTick + GoalkeeperConstants.RecoveryCooldownTicks;
                }

                // ── Reset dive launch frame when no longer in dive states ─────────────
                GoalkeeperState newState = _states[gkIndex];
                if (newState != GoalkeeperState.Diving && newState != GoalkeeperState.Airborne)
                {
                    _diveLaunchFrames[gkIndex] = -1;
                }

                // ── Clear save intent once the save attempt is fully resolved ─────────
                // Airborne → HandsOnBall: caught. Airborne → Recovering: parry/deflect/spill/miss.
                // Smothered → HandsOnBall or Recovering also clears the active flag.
                if (_saveIntentActive[gkIndex])
                {
                    bool saveResolved = (gkState == GoalkeeperState.Airborne || gkState == GoalkeeperState.Smothered)
                                    && (newState == GoalkeeperState.HandsOnBall || newState == GoalkeeperState.Recovering);
                    if (saveResolved)
                    {
                        _saveIntentActive[gkIndex] = false;
                    }
                }

                // ── Clear rush intent once the rush is fully resolved ─────────────────
                // Rushing → Smothered: contact made. Rushing → Recovering: ball intercepted (cleared above).
                // Rushing → HandsOnBall path via Smothered → HandsOnBall handled transitively next frame.
                if (_rushIntentActive[gkIndex] && gkState == GoalkeeperState.Rushing)
                {
                    if (newState == GoalkeeperState.Smothered || newState == GoalkeeperState.Recovering)
                    {
                        _rushIntentActive[gkIndex] = false;
                    }
                }
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static float ComputeDiveDirectionX(SaveIntent intent, AgentState agentState)
        {
            if (intent.DeflectionTarget.HasValue)
            {
                float dx = intent.DeflectionTarget.Value.x - agentState.Position.x;
                return dx > 0.0f ? 1.0f : dx < 0.0f ? -1.0f : 0.0f;
            }
            return 0.0f;
        }

        private bool CheckAttackerWithinRadius(Vector3 gkPosition, BallState ballState, float radius)
        {
            // Stage 0: use ball position as attacker proxy when ball is possessed
            if (_ballSystem.GetBallPossessorId() >= 0)
            {
                float distSq = (ballState.Position - gkPosition).sqrMagnitude;
                return distSq <= radius * radius;
            }
            return false;
        }

        private int GetInitialAttackerTargetId(int gkIndex)
        {
            // Stage 0 stub: return -1 (no stored attacker target tracking yet)
            // Full attacker-target tracking is a Stage 1 concern per §3.7 / KD-15
            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-28 | —      | AR-1: H-5 _rushLaunchMps set in CommitRushIntent; M-2 intent clear. |
// | 1.2     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling ->          |
// |         |            |        | Unity.Profiling. ProfilerMarker's actual namespace is               |
// |         |            |        | Unity.Profiling; the old using was CS0246 under Unity and the Linux |
// |         |            |        | compile gate alike, so this assembly could not have compiled        |
// |         |            |        | in-engine. No functional change.                                    |
// | 1.3     | 2026-06-12 | —      | Build fix (dotnet CI gate):                                         |
// |         |            |        | DistributionExecutedEvent.TargetReceiverId assignment passed        |
// |         |            |        | DistributeIntent.TargetReceiverId (int?) into the int event field - |
// |         |            |        | CS0266 everywhere; assembly never compiled. Now coalesces null ->   |
// |         |            |        | -1, the zone-target sentinel the event field documented in its v1.2 |
// |         |            |        | AR-2 row. No behaviour change for receiver-targeted distributions.  |
#endregion
