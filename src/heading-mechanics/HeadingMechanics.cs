// File:     src/heading-mechanics/HeadingMechanics.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §3.2–§3.9 dispatch, §4.6, KD-9, KD-17, KD-18, Code Standards #20
// Purpose:  60 Hz physics-tick orchestrator. Manages per-agent intent tracking, jump kinematics,
//           contact resolution, duel dispatch, event publication, and failed-attempt handling.

using UnityEngine;
using UnityEngine.Profiling;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// 60 Hz physics-tick orchestrator for Heading Mechanics #10.
    /// Dispatches §3.2–§3.9 sub-systems per-agent per-frame, resolves contested duels, and
    /// publishes HeaderExecutedEvent / HeaderAttemptFailedEvent to the event bus.
    /// Constructor-injected dependencies (FR-CS-051–054). Zero heap allocation on hot path.
    /// Heading Mechanics #10 §4.6.
    /// </summary>
    public sealed class HeadingMechanics
    {
        // ── Dependencies ─────────────────────────────────────────────────────────────

        private readonly IHeadingBallSystem  _ballSystem;
        private readonly IHeadingRngService  _rng;
        private readonly HeadingDuelResolution _duelResolution;
        private readonly HeadingTelemetry    _telemetry;

        // ── Pre-allocated per-agent state ────────────────────────────────────────────

        private readonly HeaderIntent[]      _intents;
        private readonly HeaderContactState[] _contactStates;
        private readonly bool[]              _intentActive;
        private readonly int[]               _ballSnapshotFrames;
        private readonly HeadingAgentAttributes[] _agentAttrs;

        // ── Profiler Markers ─────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("HeadingMechanics.Update");

        // ── Constructor ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Allocates per-agent buffers and wires all dependencies.
        /// No allocation occurs during 60 Hz Update calls after this point.
        /// </summary>
        public HeadingMechanics(
            IHeadingBallSystem ballSystem,
            IHeadingRngService rng)
        {
            _ballSystem    = ballSystem;
            _rng           = rng;
            _duelResolution = new HeadingDuelResolution();
            _telemetry     = new HeadingTelemetry();

            int maxAgents = HeadingMechanicsConstants.MaxAgents;
            _intents           = new HeaderIntent[maxAgents];
            _contactStates     = new HeaderContactState[maxAgents];
            _intentActive      = new bool[maxAgents];
            _ballSnapshotFrames = new int[maxAgents];
            _agentAttrs        = new HeadingAgentAttributes[maxAgents];
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Commits a HeaderIntent for an agent (called from the 10 Hz tactical loop).
        /// targetIntent is clamped to the pitch bounding box per FR-HE-029.
        /// contactPointIntent is clamped to the head-local envelope per FR-HE-030.
        /// </summary>
        public void CommitIntent(
            int agentId,
            HeaderIntent intent,
            HeadingAgentAttributes attrs,
            BallState currentBall,
            int currentFrame)
        {
            if ((uint)agentId >= (uint)HeadingMechanicsConstants.MaxAgents)
            {
                return;
            }

            // FR-HE-029: clamp targetIntent to pitch bounding box.
            intent.TargetIntent = ClampToPitch(intent.TargetIntent, agentId);

            // FR-HE-030: clamp contactPointIntent to head-local envelope.
            intent.ContactPointIntent = ClampToHeadEnvelope(intent.ContactPointIntent);

            _intents[agentId]           = intent;
            _agentAttrs[agentId]        = attrs;
            _intentActive[agentId]      = true;
            _ballSnapshotFrames[agentId] = currentFrame;
            _contactStates[agentId]     = HeaderContactState.CreateNew();
        }

        /// <summary>
        /// Cancels any active HeaderIntent for an agent (e.g. tackle interrupt).
        /// </summary>
        public void CancelIntent(int agentId)
        {
            if ((uint)agentId >= (uint)HeadingMechanicsConstants.MaxAgents)
            {
                return;
            }

            _intentActive[agentId] = false;
        }

        /// <summary>
        /// 60 Hz physics tick entry point.
        /// Processes all agents with active intents: advances jump kinematics, evaluates eligibility,
        /// accumulates duel candidates, resolves duels, and publishes events.
        /// All agent states must be passed in their current frame snapshot.
        /// Heading Mechanics #10 §4.6.
        /// </summary>
        /// <param name="agentStates">Agent kinematic states indexed by agentId (AM #2 §3.5.1).</param>
        /// <param name="currentBall">Current BallState snapshot from Ball Physics #1.</param>
        /// <param name="currentFrame">Current 60 Hz physics frame index.</param>
        /// <param name="currentMatchTime">Current match time in seconds from kickoff.</param>
        public void Update(
            AgentState[] agentStates,
            BallState currentBall,
            int currentFrame,
            float currentMatchTime)
        {
            using var _ = s_updateMarker.Auto();

            _duelResolution.ClearFrameBuffer();

            // Pass 1: per-agent eligibility check, jump kinematics, contact-frame detection.
            // Agents are iterated in index order (deterministic per #16 §3.2 entity ordering).
            for (int agentId = 0; agentId < HeadingMechanicsConstants.MaxAgents; agentId++)
            {
                if (!_intentActive[agentId])
                {
                    continue;
                }

                ref HeaderContactState contactState = ref _contactStates[agentId];
                ref HeaderIntent       intent       = ref _intents[agentId];
                HeadingAgentAttributes attrs        = _agentAttrs[agentId];
                AgentState             agentState   = agentStates[agentId];

                // Set jumpStartFrame on the first eligible frame after commit (§3.3 / §4.6 / v0.2 M-3).
                if (contactState.JumpStartFrame < 0)
                {
                    if (currentFrame >= intent.AttemptCommittedTick * HeadingMechanicsConstants.FramesPerTacticalTick &&
                        agentState.CurrentState != AgentMovementState.GROUNDED &&
                        agentState.CurrentState != AgentMovementState.STUMBLING)
                    {
                        contactState.JumpStartFrame      = currentFrame;
                        contactState.JumpReachM          = HeadingJumpKinematics.ComputeJumpReach(attrs);
                        contactState.PrevFrameFacingDirection = agentState.FacingDirection;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Aerial-phase exit: if agent has landed, deactivate intent.
                int landingFrame = HeadingJumpKinematics.ComputeLandingFrame(contactState.JumpStartFrame);
                if (currentFrame > landingFrame)
                {
                    _intentActive[agentId] = false;
                    continue;
                }

                // Compute current head Z from synthetic trajectory.
                float agentHeadZ = HeadingJumpKinematics.ComputeHeadZ(
                    contactState.JumpStartFrame,
                    contactState.JumpReachM,
                    currentFrame);

                // Evaluate eligibility.
                EligibilityResult eligibility = HeadingEligibility.Evaluate(
                    agentState,
                    agentId,
                    agentHeadZ,
                    currentBall,
                    _ballSnapshotFrames[agentId],
                    intent,
                    in contactState,
                    currentFrame,
                    currentMatchTime,
                    _ballSystem,
                    out BallState freshBall);

                // Update snapshot frame if ball was re-queried.
                currentBall                   = freshBall;
                _ballSnapshotFrames[agentId]  = currentFrame;

                if (!eligibility.IsEligible)
                {
                    // Emit failed event per §3.9 / v0.2 M-2.
                    if (eligibility.MistimedDirection != MistimedDirection.None)
                    {
                        FailureCause cause = eligibility.MistimedDirection == MistimedDirection.Early
                            ? FailureCause.MistimedEarly
                            : FailureCause.MistimedLate;

                        EmitFailedAttempt(agentId, cause, currentBall, currentMatchTime, contactState.TimingOffsetMs, agentStates);
                        _intentActive[agentId] = false;
                    }
                    else if (eligibility.PredictedContactFrame < 0)
                    {
                        // Ball never enters contact volume — PositionedPoorly if past apex.
                        if (currentFrame > eligibility.IdealContactFrame)
                        {
                            EmitFailedAttempt(agentId, FailureCause.PositionedPoorly, currentBall, currentMatchTime, 0.0f, agentStates);
                            _intentActive[agentId] = false;
                        }
                    }

                    // Update per-frame tracking even when not eligible this tick.
                    contactState.IdealContactFrame    = eligibility.IdealContactFrame;
                    contactState.PrevFrameFacingDirection = agentState.FacingDirection;
                    continue;
                }

                // Update contact state with fresh prediction.
                contactState.PredictedContactFrame = eligibility.PredictedContactFrame;
                contactState.IdealContactFrame     = eligibility.IdealContactFrame;

                // Contact frame reached?
                if (currentFrame == eligibility.PredictedContactFrame)
                {
                    contactState.ActualContactFrame = currentFrame;  // v0.2 M-4

                    // Compute contact point actual from ball-to-head geometry.
                    Vector3 headCentre_ws = new Vector3(
                        agentState.Position.x,
                        agentState.Position.y,
                        agentHeadZ);

                    Vector2 contactPointActual_headLocal = ComputeContactPointHeadLocal(
                        freshBall.Position, headCentre_ws, agentState.FacingDirection);

                    // Compute contact quality.
                    float qualityScalar = HeadingContactQuality.Compute(
                        contactState.ActualContactFrame,
                        contactState.IdealContactFrame,
                        contactPointActual_headLocal,
                        intent.ContactPointIntent,
                        attrs,
                        _rng,
                        out float timingOffsetMs,
                        out Vector2 contactPointError,
                        out ContactQualityLabel qualityLabel);

                    contactState.TimingOffsetMs    = timingOffsetMs;
                    contactState.ContactPointError = contactPointError;
                    contactState.ContactQualityScalar = qualityScalar;

                    // Register with duel resolution (always; uncontested agents will be solo-resolved).
                    float baseScore = HeadingDuelResolution.ComputeBaseScore(attrs);
                    _duelResolution.RegisterDuelCandidate(agentId, currentMatchTime, baseScore);

                    // Stash for use in Pass 2.
                    _contactStates[agentId] = contactState;
                }

                // Do not update PrevFrameFacingDirection on the contact frame; Pass 2 needs the
                // previous frame's value for DeriveHeadAngularVelocity finite-difference (§3.6 FR-HE-032).
                if (currentFrame != eligibility.PredictedContactFrame)
                {
                    contactState.PrevFrameFacingDirection = agentState.FacingDirection;
                }
            }

            // Pass 2: resolve duels and emit events.
            _duelResolution.ResolveAll(_rng);

            for (int agentId = 0; agentId < HeadingMechanicsConstants.MaxAgents; agentId++)
            {
                if (!_intentActive[agentId])
                {
                    continue;
                }

                ref HeaderContactState contactState = ref _contactStates[agentId];
                if (contactState.ActualContactFrame != currentFrame)
                {
                    continue;
                }

                HeadingAgentAttributes attrs      = _agentAttrs[agentId];
                ref HeaderIntent       intent     = ref _intents[agentId];
                AgentState             agentState = agentStates[agentId];
                float agentHeadZ = HeadingJumpKinematics.ComputeHeadZ(
                    contactState.JumpStartFrame,
                    contactState.JumpReachM,
                    currentFrame);

                Vector3 headCentre_ws = new Vector3(
                    agentState.Position.x,
                    agentState.Position.y,
                    agentHeadZ);

                Vector2 contactPointActual_headLocal = ComputeContactPointHeadLocal(
                    currentBall.Position, headCentre_ws, agentState.FacingDirection);

                Vector3 contactPointActual_ws = headCentre_ws
                    + new Vector3(
                        agentState.FacingDirection.x * contactPointActual_headLocal.x,
                        agentState.FacingDirection.y * contactPointActual_headLocal.x,
                        contactPointActual_headLocal.y);

                // Find this agent's duel result.
                int  duelId          = -1;
                bool isWinner        = true;
                float disturbance    = 0.0f;

                for (int d = 0; d < _duelResolution.DuelCount; d++)
                {
                    ContestedDuelContext duel = _duelResolution.GetDuel(d);
                    for (int p = 0; p < duel.ParticipantCount; p++)
                    {
                        if (_duelResolution.GetParticipantAgentId(duel.BufferStartIndex, p) == agentId)
                        {
                            duelId     = duel.ParticipantCount > 1 ? duel.DuelId : HeaderExecutedEvent.UncontestedDuelId;
                            isWinner   = duel.WinnerAgentId == agentId;
                            disturbance = _duelResolution.GetDisturbanceFactor(duel.BufferStartIndex, p);
                            break;
                        }
                    }
                }

                float effectiveQuality = contactState.ContactQualityScalar * (1.0f - disturbance);

                if (!isWinner && effectiveQuality < HeadingMechanicsConstants.MinContactQuality)
                {
                    // FR-HE-026: loser below threshold emits failed event.
                    EmitFailedAttempt(agentId, FailureCause.DisturbedInDuel, currentBall, currentMatchTime, contactState.TimingOffsetMs, agentStates);
                    _telemetry.RecordDuelOutcome(false, false);
                    _intentActive[agentId] = false;
                    continue;
                }

                // Compute outgoing velocity.
                Vector3 outgoingVelocity = HeadingPowerAngle.ComputeOutgoingVelocity(
                    attrs,
                    intent,
                    effectiveQuality,
                    contactPointActual_ws,
                    headCentre_ws,
                    currentBall.Velocity);

                // Compute outgoing spin.
                Vector3 outgoingSpin = HeadingSpinTransfer.ComputeOutgoingSpin(
                    currentBall.AngularVelocity,
                    contactPointActual_headLocal,
                    agentState.FacingDirection,
                    contactState.PrevFrameFacingDirection);

                // Own-goal flag.
                bool ownGoalFlag = HeadingPowerAngle.ComputeOwnGoalFlag(
                    outgoingVelocity,
                    currentBall.Position,
                    attrs.TeamId);

                // Re-compute quality label for the effective quality (winner may be unchanged; loser may be disturbed).
                float timingOff  = contactState.TimingOffsetMs;
                ContactQualityLabel qualLabel;
                if (timingOff < -HeadingMechanicsConstants.EarlyLabelThresholdMs)
                    qualLabel = ContactQualityLabel.Early;
                else if (timingOff > HeadingMechanicsConstants.LateLabelThresholdMs)
                    qualLabel = ContactQualityLabel.Late;
                else
                    qualLabel = ContactQualityLabel.OnTime;

                // Apply kick to ball (winner only, or undisturbed loser — both paths reach here).
                _ballSystem.ApplyKick(outgoingVelocity, outgoingSpin, agentId, currentMatchTime);

                // Publish HeaderExecutedEvent.
                HeaderExecutedEvent evt = new HeaderExecutedEvent
                {
                    AgentId                 = agentId,
                    MatchTime               = currentMatchTime,
                    ContactQualityScalar    = effectiveQuality,
                    ContactQualityLabel     = qualLabel,
                    ContactPoint            = contactPointActual_headLocal,
                    IncomingBallState       = currentBall,
                    OutgoingVelocity        = outgoingVelocity,
                    OutgoingSpin            = outgoingSpin,
                    ContestedDuelId         = duelId,
                    OwnGoalShapedTrajectory = ownGoalFlag,
                    SetPieceContext         = intent.SetPieceContext
                };

                EventBusStub.Publish(in evt);

                _telemetry.RecordContactQuality(effectiveQuality, qualLabel);
                _telemetry.RecordDuelOutcome(isWinner, !isWinner);
                if (ownGoalFlag) _telemetry.RecordOwnGoalFlag();

                // Landing: set GROUNDED with DIVING_HEADER if appropriate (AM #2 §3.1.2).
                // Stage 0: aerial exit is managed externally; #10 just deactivates intent.
                _intentActive[agentId] = false;
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private void EmitFailedAttempt(
            int agentId,
            FailureCause cause,
            BallState ball,
            float matchTime,
            float timingOffsetMs,
            AgentState[] agentStates)
        {
            float missDistance = ComputeClosestApproach(ball, agentStates, agentId);

            HeaderAttemptFailedEvent evt = new HeaderAttemptFailedEvent
            {
                AgentId       = agentId,
                MatchTime     = matchTime,
                MissDistanceM = missDistance,
                TimingOffsetMs = timingOffsetMs,
                FailureCause  = cause
            };

            EventBusStub.Publish(in evt);
            _telemetry.RecordFailedAttempt(cause);
        }

        private float ComputeClosestApproach(BallState ball, AgentState[] agentStates, int agentId)
        {
            // Stage 0 approximation: return ball distance from last-known head position.
            // Full attempt-window sweep is a Stage 0+1 refinement.
            if (agentStates == null || (uint)agentId >= (uint)agentStates.Length)
            {
                return float.MaxValue;
            }

            ref HeaderContactState contactState = ref _contactStates[agentId];
            float headZ = HeadingJumpKinematics.ComputeHeadZ(
                contactState.JumpStartFrame,
                contactState.JumpReachM,
                contactState.PredictedContactFrame >= 0 ? contactState.PredictedContactFrame : 0);

            Vector3 headPos = new Vector3(
                agentStates[agentId].Position.x,
                agentStates[agentId].Position.y,
                headZ);

            return Vector3.Distance(ball.Position, headPos);
        }

        /// <summary>
        /// Converts world-space ball position to head-local 2-D contact point.
        /// Head-local: origin = headCentre, +x = agent.facing forward, +y = agent-left lateral.
        /// </summary>
        private static Vector2 ComputeContactPointHeadLocal(
            Vector3 ballPos,
            Vector3 headCentre_ws,
            Vector2 facingDir)
        {
            Vector3 delta = ballPos - headCentre_ws;

            // Forward axis (head-local +x) = agent.facing projected into world XY, then extended to 3D.
            Vector3 fwd = new Vector3(facingDir.x, facingDir.y, 0.0f);

            // Left axis (head-local +y) = perpendicular in XY plane (CCW from forward).
            Vector3 left = new Vector3(-facingDir.y, facingDir.x, 0.0f);

            float localX = Vector3.Dot(delta, fwd);
            float localY = Vector3.Dot(delta, left);

            // Clamp to head contact radius.
            float radius = HeadingMechanicsConstants.HeadContactVolumeRadiusM;
            Vector2 local = new Vector2(localX, localY);
            if (local.sqrMagnitude > radius * radius)
            {
                local = local.normalized * radius;
            }

            return local;
        }

        private Vector3 ClampToPitch(Vector3 pos, int agentId)
        {
            float pitchX = HeadingMechanicsConstants.PitchLengthM;
            float pitchY = HeadingMechanicsConstants.PitchWidthM;

            bool clamped = pos.x < 0.0f || pos.x > pitchX ||
                           pos.y < 0.0f || pos.y > pitchY;

            if (clamped)
            {
                _telemetry.WarnTargetIntentClamped(agentId);
            }

            return new Vector3(
                Mathf.Clamp(pos.x, 0.0f, pitchX),
                Mathf.Clamp(pos.y, 0.0f, pitchY),
                pos.z);
        }

        private static Vector2 ClampToHeadEnvelope(Vector2 contactPoint)
        {
            float radius = HeadingMechanicsConstants.HeadContactVolumeRadiusM;
            if (contactPoint.sqrMagnitude > radius * radius)
            {
                return contactPoint.normalized * radius;
            }

            return contactPoint;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-28 | —      | AR-1 H-1: EmitFailedAttempt gains AgentState[] agentStates parameter; all three  |
// |         |            |        | call sites updated so ComputeClosestApproach receives real agent positions.      |
// | 1.2     | 2026-05-28 | —      | AR-2 M: PrevFrameFacingDirection no longer overwritten on the contact frame;      |
// |         |            |        | fixes zero headAngularVelocity in spin finite-difference (§3.6 FR-HE-032).       |
#endregion
