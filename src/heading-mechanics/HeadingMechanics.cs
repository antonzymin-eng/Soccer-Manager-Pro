// File:     src/heading-mechanics/HeadingMechanics.cs
// Created:  2026-05-28
// Modified: 2026-06-14
// Modified: 2026-07-23 (GK/Heading engine-integration Phase 2: CaptureState/RestoreState snapshot seam over
//           the per-agent cross-tick arrays, for the Match Engine v18 save/restore path)
// Modified: 2026-08-09 (ERR-010-002: contact-geometry rewritten around a single ResolveContactGeometry
//           owner read by both Update passes, carrying the 3-D contact point directly; see §3.5.1 / HeadingAim.cs)
// Author:   —
// Spec:     Heading Mechanics #10 §3.2–§3.9 dispatch, §4.6, KD-9, KD-17, KD-18, Code Standards #20
// Purpose:  60 Hz physics-tick orchestrator. Manages per-agent intent tracking, jump kinematics,
//           contact resolution, duel dispatch, event publication, and failed-attempt handling.

using System;

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.CollisionSystem;

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
        /// Exposes the internal ICollisionEventConsumer so Collision System #3 can push
        /// AGENT_BALL events into the duel resolution buffer each frame (§4.2.1 / KD-8).
        /// </summary>
        public ICollisionEventConsumer CollisionConsumer => _duelResolution;

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

            // FR-HE-030: clamp contactPointIntent to head-local envelope. ERR-010-002: this validates
            // the W9 DT-supplied override; Stage-0 geometry does not read ContactPointIntent at all
            // (§3.5.1 derives the contact point from TargetIntent at the contact frame instead — see
            // the matching note at MatchEngine.cs's HeaderIntent commit site).
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

        // ── Snapshot seam (design note §2.6 / GK-Heading Phase 2) ─────────────────────

        /// <summary>
        /// Snapshot seam: bundles this orchestrator's full per-agent cross-tick state (intents + active
        /// latches, per-frame contact state, ball-snapshot frames, attributes) into a
        /// <see cref="HeadingTickState"/> view so a host snapshot layer can serialize it canonically for
        /// deterministic save/restore (parallel to the Pressing
        /// <see cref="TacticalDirector.PressingAI.PressingTickState"/> seam). The bundled arrays are the
        /// live, allocated-once instances (read-only serialization use only).
        /// </summary>
        public HeadingTickState CaptureState() =>
            new HeadingTickState(_intents, _contactStates, _intentActive, _ballSnapshotFrames, _agentAttrs);

        /// <summary>
        /// Restores this orchestrator's cross-tick state from a snapshot produced by
        /// <see cref="CaptureState"/> (deterministic save/restore — the heading analogue of the Pressing /
        /// Defensive <c>RestoreState</c> seams). Each array in <paramref name="state"/> is copied element-wise
        /// into the live, allocated-once container (the caller supplies a freshly-built view with matching
        /// <c>MaxAgents</c> length — the internal containers stay the authoritative instances). No per-tick
        /// output buffers exist outside these arrays, so forward replay from the restored tick is
        /// byte-identical.
        /// </summary>
        public void RestoreState(in HeadingTickState state)
        {
            Array.Copy(state.Intents,            _intents,            _intents.Length);
            Array.Copy(state.ContactStates,      _contactStates,      _contactStates.Length);
            Array.Copy(state.IntentActive,       _intentActive,       _intentActive.Length);
            Array.Copy(state.BallSnapshotFrames, _ballSnapshotFrames, _ballSnapshotFrames.Length);
            Array.Copy(state.AgentAttrs,         _agentAttrs,         _agentAttrs.Length);
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
                if (currentFrame - _ballSnapshotFrames[agentId] > 1)
                {
                    _telemetry.WarnBallStateStale(agentId);
                }
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

                    // §3.5.1 (ERR-010-002): the aimed contact geometry. contactPointIntent is DERIVED
                    // here from TargetIntent rather than read from intent.ContactPointIntent — the
                    // half-vector that realizes an aim depends on the incoming velocity at contact,
                    // which no producer can know at commit time, and #10 KD-4 locks the intent at commit.
                    ResolveContactGeometry(
                        in intent,
                        in attrs,
                        freshBall.Position,
                        freshBall.Velocity,
                        headCentre_ws,
                        agentState.FacingDirection,
                        out Vector2 contactPointActual_headLocal,
                        out Vector2 contactPointIntent_headLocal,
                        out Vector3 unusedContactPoint_ws);

                    // Compute contact quality.
                    float qualityScalar = HeadingContactQuality.Compute(
                        contactState.ActualContactFrame,
                        contactState.IdealContactFrame,
                        contactPointActual_headLocal,
                        contactPointIntent_headLocal,
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

                // §3.5.1 (ERR-010-002): one owner for the contact geometry, shared with Pass 1 above.
                //
                // Pass 1 reassigns currentBall from each agent's own FR-HE-033 re-query, so by the time
                // Pass 2 runs it holds the last-evaluated agent's freshBall. That is the same physical
                // ball every agent saw this frame — the re-query reads one ball system at one frame —
                // so both passes resolve identical geometry from identical inputs. The pre-fix code
                // achieved that only by having written the same expression twice.
                //
                // The 3-D contact point is taken directly and NOT rebuilt from the 2-D head-local
                // projection. Rebuilding it (the pre-fix Pass 2) pinned contactPointActual_ws.z to the
                // head centre's z, so the §3.5 reflection normal was always horizontal and
                // reflected.z == v̂_in.z — a dropping cross was headed further DOWN and no header
                // could lift the ball. AR-3 M-1's fix, which stopped the lateral offset being injected
                // as height, is preserved: the lateral term still maps to the agent-left axis.
                ResolveContactGeometry(
                    in intent,
                    in attrs,
                    currentBall.Position,
                    currentBall.Velocity,
                    headCentre_ws,
                    agentState.FacingDirection,
                    out Vector2 contactPointActual_headLocal,
                    out Vector2 unusedAimHeadLocal,
                    out Vector3 contactPointActual_ws);

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
        /// §3.5.1 (ERR-010-002) — resolves the aimed contact geometry for one contact frame.
        ///
        /// <para>Single owner of the contact point, called from BOTH passes of <see cref="Update"/>.
        /// Before ERR-010-002 each pass computed it independently from ball-vs-head geometry — the
        /// parallel-surface shape this project keeps filing against itself — and neither read
        /// <c>TargetIntent</c>, so the header was a passive mirror.</para>
        ///
        /// <para>Three outputs, because the spec needs the contact point in two frames at once:
        /// <paramref name="actual_ws"/> is the full 3-D point the §3.5 reflection normal is taken
        /// from, while <paramref name="actualHeadLocal"/> is its 2-D head-local projection, which is
        /// all §3.4's <c>pointError</c> and §3.6's spin transfer are defined over (Appendix D pins that
        /// frame as 2-D: +x facing-forward, +y agent-left). Reconstructing the world point FROM the
        /// 2-D projection — what the pre-fix Pass 2 did — forces
        /// <c>actual_ws.z == headCentre.z</c>, hence a permanently horizontal normal, hence
        /// <c>reflected.z == v̂_in.z</c>: a descending ball stayed descending and no header could ever
        /// lift the ball. The 3-D point is therefore carried directly rather than round-tripped.</para>
        ///
        /// <para>Radial magnitude is preserved from the geometric contact exactly as before; only the
        /// DIRECTION is steered. That keeps §3.6's axial-offset input and §3.4's error scale on their
        /// existing footing so this landing changes one thing.</para>
        /// </summary>
        private static void ResolveContactGeometry(
            in HeaderIntent intent,
            in HeadingAgentAttributes attrs,
            Vector3 ballPos,
            Vector3 ballVelocity,
            Vector3 headCentre_ws,
            Vector2 facingDir,
            out Vector2 actualHeadLocal,
            out Vector2 aimHeadLocal,
            out Vector3 actual_ws)
        {
            Vector3 delta = ballPos - headCentre_ws;
            float radius = HeadingMechanicsConstants.HeadContactVolumeRadiusM;

            // Radial magnitude of the geometric contact, clamped to the head surface (pre-fix behaviour).
            float magnitude = Mathf.Min(delta.magnitude, radius);

            Vector3 geometricNormal = delta.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ
                ? Vector3.zero
                : delta.normalized;

            // The aim is solved at the NOMINAL outgoing speed — the speed this header would carry on a
            // perfect contact. Solving it at the achieved speed would be circular: achieved speed comes
            // from contact quality, which comes from the error between aim and achieved. A player aims
            // for the target expecting to strike it well, and execution then degrades what he gets.
            float nominalSpeed = HeadingPowerAngle.ComputeOutgoingSpeed(
                attrs, intent, HeadingMechanicsConstants.PERFECT_CONTACT_QUALITY);

            Vector3 aimNormal = Vector3.zero;
            if (ballVelocity.sqrMagnitude >= HeadingMechanicsConstants.DEGENERACY_EPSILON_SQ &&
                IsFiniteVector(ballVelocity))
            {
                Vector3 incident = -ballVelocity.normalized;
                Vector3 aimDirection = HeadingAim.ComputeAimDirection(ballPos, intent.TargetIntent, nominalSpeed);
                aimNormal = HeadingAim.ComputeAimNormal(incident, aimDirection);
            }

            Vector3 achievedNormal = HeadingAim.ComputeAchievedNormal(
                geometricNormal, aimNormal, NormalisedHeading(attrs));

            actual_ws = headCentre_ws + achievedNormal * magnitude;
            actualHeadLocal = ProjectToHeadLocal(achievedNormal * magnitude, facingDir);

            // A degenerate aim leaves intent equal to achieved, so pointError is zero and §3.4 charges
            // nothing for an aim that was never expressible.
            aimHeadLocal = aimNormal.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ
                ? actualHeadLocal
                : ProjectToHeadLocal(aimNormal * magnitude, facingDir);
        }

        /// <summary>Normalised Heading attribute [0, 1] — §3.5.1's steer authority (ERR-010-002).</summary>
        private static float NormalisedHeading(in HeadingAgentAttributes attrs)
        {
            float clamped = Mathf.Clamp(
                attrs.Heading,
                HeadingMechanicsConstants.ATTR_MIN,
                HeadingMechanicsConstants.ATTR_MAX);
            return clamped / HeadingMechanicsConstants.ATTR_MAX;
        }

        private static bool IsFiniteVector(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }

        /// <summary>
        /// Projects a world-space head-surface offset into the 2-D head-local frame
        /// (origin = head centre, +x = agent.facing forward, +y = agent-left lateral; Appendix D).
        /// The vertical component is dropped BY DEFINITION of that frame — §3.4 and §3.6 are the only
        /// consumers and both are defined over it. The reflection uses the 3-D point instead.
        /// </summary>
        private static Vector2 ProjectToHeadLocal(Vector3 offset, Vector2 facingDir)
        {
            Vector3 fwd  = new Vector3(facingDir.x, facingDir.y, 0.0f);
            Vector3 left = new Vector3(-facingDir.y, facingDir.x, 0.0f);

            return new Vector2(Vector3.Dot(offset, fwd), Vector3.Dot(offset, left));
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
// | 1.3     | 2026-05-28 | —      | AR-2 H-2: CollisionConsumer property + using CollisionSystem added.               |
// |         |            |        | AR-2 M-5: WarnBallStateStale telemetry call added on stale BallState snapshot.   |
// |         |            |        | AR-2 L-3: Redundant self-assignment _contactStates[agentId]=contactState removed. |
// | 1.4     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling -> Unity.Profiling.       |
// |         |            |        | ProfilerMarker's actual namespace is Unity.Profiling; the old using was CS0246    |
// |         |            |        | under Unity and the Linux compile gate alike, so this assembly could not have     |
// |         |            |        | compiled in-engine. No functional change.                                         |
// | 1.5     | 2026-06-14 | —      | AR-3 M-1: Pass-2 head-local→world contact-point reconstruction mapped the         |
// |         |            |        | lateral (+y) head-local offset onto world Z, tilting the reflection normal        |
// |         |            |        | vertically for off-centre headers; now reconstructed on the (-facing.y,facing.x)  |
// |         |            |        | left axis, exactly inverting ComputeContactPointHeadLocal. Centred headers        |
// |         |            |        | unaffected (why unit tests passed).                                               |
// | 1.6     | 2026-07-23 | —      | GK/Heading engine-integration Phase 2: CaptureState() bundles the per-agent       |
// |         |            |        | cross-tick arrays into a HeadingTickState view; RestoreState(in) copies them      |
// |         |            |        | back into the live containers (the Match Engine v18 snapshot seam).               |
// | 1.7     | 2026-08-09 | —      | ERR-010-002: the header aim had no owner (delegated to Decision Tree #8, which    |
// |         |            |        | cannot emit a header at all). New ResolveContactGeometry is the single owner of   |
// |         |            |        | contact-point derivation, read by both Update passes (was two independent,       |
// |         |            |        | only-by-coincidence-agreeing derivations); the 3-D contact point is carried       |
// |         |            |        | directly instead of round-tripped through its 2-D head-local projection, which    |
// |         |            |        | had pinned contactPointActual.z to the head centre and made every reflection      |
// |         |            |        | normal horizontal. Calls the new §3.5.1 / HeadingAim.cs three-step aim solve.     |
// |         |            |        | Retroactive version-history row (adversarial review of the landing, Finding 4) —  |
// |         |            |        | no further logic change from this row itself.                                     |
#endregion
