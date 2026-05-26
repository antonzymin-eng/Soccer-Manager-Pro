// File:     src/pass-mechanics/PassExecutor.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.8, §3.9, §4.1, Code Standards #20
// Purpose:  Sealed instance orchestrator for the six-state pass execution state
//           machine. Validates PassRequest, coordinates sub-system calls, calls
//           Ball.ApplyKick() at CONTACT, and publishes events. Dependencies are
//           constructor-injected (FR-CS-051–054).

using UnityEngine;
using UnityEngine.Profiling;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Orchestrates pass execution across the six-state lifecycle:
    /// IDLE → INITIATING (inside Execute) → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE.
    /// All §3.x subsystems are coordinated from here. Pass Mechanics #5 §3.8, §4.1.
    /// </summary>
    public sealed class PassExecutor
    {
        // ── Dependencies ─────────────────────────────────────────────────────────────

        private readonly IPassBallSystem     _ballSystem;
        private readonly IPassAgentQuery     _agentQuery;
        private readonly IPassCollisionQuery _collisionQuery;

        // ── Profiler Markers ─────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_executeMarker =
            new ProfilerMarker("PassMechanics.Execute");

        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("PassMechanics.Update");

        // ── State Machine ────────────────────────────────────────────────────────────

        private enum PassExecutionState
        {
            Idle,
            Windup,
            Contact,
            FollowThrough,
            Complete
        }

        private PassExecutionState _state = PassExecutionState.Idle;

        // ── Values Captured at INITIATING ────────────────────────────────────────────

        private PassRequest  _request;
        private PhysicalProfile _profile;

        private float   _kickSpeed;
        private float   _launchAngleDeg;
        private Vector3 _spinVector;
        private Vector3 _baseKickDirection;  // pre-error, toward aimPoint
        private Vector3 _aimPoint;
        private float   _leadDistance;

        // Cached error-chain inputs (from agent attributes, captured at INITIATING)
        private float _cachedPassing;
        private float _cachedFatigue;
        private float _cachedBodyAngleDeg;
        private bool  _cachedIsWeakFoot;
        private int   _cachedWeakFootRating;
        private Vector2 _passerPosition; // for pressure re-query at CONTACT

        // ── Windup / Follow-Through Timers ───────────────────────────────────────────

        private int _windupFramesRemaining;
        private int _followThroughFramesRemaining;

        // ── Result Storage ───────────────────────────────────────────────────────────

        private PassResult _lastResult;

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>True when no pass is in progress and the executor is ready.</summary>
        public bool IsIdle => _state == PassExecutionState.Idle;

        /// <summary>
        /// The result of the most recently completed (or cancelled/invalid) pass.
        /// Only meaningful after IsIdle returns true following a pass that was started.
        /// </summary>
        public PassResult LastResult => _lastResult;

        // ── Constructor ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a PassExecutor with the required system dependencies.
        /// Pass Mechanics #5 §4.1.
        /// </summary>
        public PassExecutor(
            IPassBallSystem     ballSystem,
            IPassAgentQuery     agentQuery,
            IPassCollisionQuery collisionQuery)
        {
            _ballSystem     = ballSystem;
            _agentQuery     = agentQuery;
            _collisionQuery = collisionQuery;
        }

        // ── Execute — INITIATING State ───────────────────────────────────────────────

        /// <summary>
        /// Initiates a pass. Performs all INITIATING validation and pre-computation
        /// (§3.8.4). If validation fails, returns Invalid immediately and state stays
        /// Idle. If valid, transitions to WINDUP — call Update() each frame until
        /// IsIdle is true, then read LastResult. Pass Mechanics #5 §3.8.4.
        /// </summary>
        /// <param name="request">Pass request from Decision Tree #8.</param>
        /// <param name="ball">Current ball state for possession check.</param>
        /// <returns>
        /// PassResult with Outcome=Invalid for synchronous rejection.
        /// PassResult with Outcome=Completed and ContactFrame=0 means pass started;
        /// the real result is in LastResult once IsIdle is true.
        /// </returns>
        public PassResult Execute(in PassRequest request, ref BallState ball)
        {
            using var _ = s_executeMarker.Auto();

            // ── Guard: reject if already executing a pass ────────────────────────────
            if (_state != PassExecutionState.Idle)
            {
                Debug.LogError($"[PassExecutor] Execute() called while pass is in progress (state={_state}). Agent={request.AgentId}. Frame={request.FrameNumber}");
                _lastResult = MakeInvalidResult(request.PassType);
                return _lastResult;
            }

            // ── FM-01: Possession check ──────────────────────────────────────────────
            if (!_ballSystem.IsBallPossessedBy(request.AgentId))
            {
                Debug.LogWarning($"[PassExecutor] FM-01: Agent {request.AgentId} does not have possession. Frame={request.FrameNumber}");
                _lastResult = MakeInvalidResult(request.PassType);
                return _lastResult;
            }

            // ── FM-01: PassType validation ───────────────────────────────────────────
            if ((int)request.PassType < 0 || (int)request.PassType > (int)PassType.Chip)
            {
                Debug.LogError($"[PassExecutor] FM-01: Invalid PassType={request.PassType}. Frame={request.FrameNumber}");
                _lastResult = MakeInvalidResult(request.PassType);
                return _lastResult;
            }

            // ── FM-07: Distance validation ───────────────────────────────────────────
            if (request.IntendedDistance <= 0f)
            {
                Debug.LogError($"[PassExecutor] FM-07: IntendedDistance={request.IntendedDistance} ≤ 0. Frame={request.FrameNumber}");
                _lastResult = MakeInvalidResult(request.PassType);
                return _lastResult;
            }

            // ── Profile lookup ───────────────────────────────────────────────────────
            if (request.PassType != PassType.Cross && request.CrossSubType != CrossSubType.Flat)
                Debug.LogWarning($"[PassExecutor] CrossSubType={request.CrossSubType} ignored for non-Cross PassType={request.PassType}.");

            CrossSubType effectiveSubType = (request.PassType == PassType.Cross)
                ? request.CrossSubType
                : CrossSubType.Flat;

            _request = request;
            _profile = PassTypeProfiles.GetProfile(request.PassType, effectiveSubType);

            // ── Read agent attributes and state ──────────────────────────────────────
            PassAgentAttributes attrs = _agentQuery.GetAttributes(request.AgentId);
            PassAgentState agentState = _agentQuery.GetState(request.AgentId);

            // ── §3.7 — Weak foot power penalty ───────────────────────────────────────
            float weakFootPowerPenalty = PassErrorCalculator.ComputeWeakFootPowerPenalty(
                request.IsWeakFoot, attrs.WeakFootRating);

            // ── §3.2 — Kick speed ────────────────────────────────────────────────────
            _kickSpeed = PassVelocityCalculator.ComputeKickSpeed(
                request.IntendedDistance,
                attrs.KickPower,
                attrs.Fatigue,
                weakFootPowerPenalty,
                _profile);

            // ── §3.3 — Launch angle ──────────────────────────────────────────────────
            _launchAngleDeg = PassVelocityCalculator.ComputeLaunchAngle(
                request.PassType, effectiveSubType, request.IntendedDistance, _profile);

            // ── §3.4 — Spin vector ───────────────────────────────────────────────────
            _spinVector = PassVelocityCalculator.ComputeSpinVector(
                request.PassType, effectiveSubType, attrs.Technique, request.IsWeakFoot, _profile);

            // ── §3.6 — Target resolution and aim point ───────────────────────────────
            _aimPoint = ResolveAimPoint(request, effectiveSubType, agentState.Position, out _leadDistance);
            _aimPoint = PassTargetResolver.ClampToPitchBounds(_aimPoint);

            // ── Base kick direction (pre-error) ──────────────────────────────────────
            _baseKickDirection = PassTargetResolver.ComputeKickDirection(agentState.Position, _aimPoint);

            // ── Cache error-chain inputs ──────────────────────────────────────────────
            _cachedPassing       = attrs.Passing;
            _cachedFatigue       = attrs.Fatigue;
            _cachedBodyAngleDeg  = PassTargetResolver.ComputeBodyAngle(agentState.FacingDirection, _baseKickDirection);
            _cachedIsWeakFoot    = request.IsWeakFoot;
            _cachedWeakFootRating = attrs.WeakFootRating;
            _passerPosition      = agentState.Position;

            // ── §3.8.8 — Windup duration ─────────────────────────────────────────────
            int baseWindup  = PassMechanicsConstants.GetWindupFrames(request.PassType, effectiveSubType);
            float reduction = 1.0f - Mathf.Clamp01(request.Urgency) * PassMechanicsConstants.UrgencyWindupReduction;
            _windupFramesRemaining = Mathf.Max(
                PassMechanicsConstants.MinWindupFrames,
                Mathf.RoundToInt(baseWindup * reduction));

            _followThroughFramesRemaining = PassMechanicsConstants.GetFollowThroughFrames(
                request.PassType, effectiveSubType);

            _state = PassExecutionState.Windup;

            // Return a sentinel indicating pass started successfully
            return new PassResult
            {
                Outcome   = PassOutcome.Completed,
                PassType  = request.PassType,
                AimPoint  = _aimPoint,
                LeadDistance = _leadDistance
            };
        }

        // ── Update — Per-Frame State Machine ─────────────────────────────────────────

        /// <summary>
        /// Advances the pass execution state machine by one frame. Call once per
        /// 60 Hz physics frame while a pass is in progress. Pass Mechanics #5 §3.8.9.
        /// </summary>
        /// <param name="matchTime">Current match time in seconds.</param>
        /// <param name="frameNumber">Current simulation frame number (for ContactFrame tracking).</param>
        /// <param name="ball">Ball state — modified by ApplyKick at CONTACT.</param>
        public void Update(float matchTime, int frameNumber, ref BallState ball)
        {
            using var _ = s_updateMarker.Auto();

            switch (_state)
            {
                case PassExecutionState.Idle:
                    break;

                case PassExecutionState.Windup:
                    UpdateWindup(matchTime);
                    break;

                case PassExecutionState.Contact:
                    ExecuteContact(matchTime, frameNumber, ref ball);
                    break;

                case PassExecutionState.FollowThrough:
                    _followThroughFramesRemaining--;
                    if (_followThroughFramesRemaining <= 0)
                        _state = PassExecutionState.Complete;
                    break;

                case PassExecutionState.Complete:
                    _state = PassExecutionState.Idle;
                    break;
            }
        }

        // ── WINDUP State ─────────────────────────────────────────────────────────────

        private void UpdateWindup(float matchTime)
        {
            // Poll tackle interrupt first — §3.8.5
            if (_collisionQuery.GetAndClearTackleFlag(_request.AgentId))
            {
                _lastResult = new PassResult
                {
                    Outcome  = PassOutcome.Cancelled,
                    PassType = _request.PassType
                };

                EventBusStub.Publish(new PassCancelledEvent
                {
                    AgentId      = _request.AgentId,
                    TeamId       = _request.TeamId,
                    CancelReason = CancelReason.TackleInterrupt,
                    PassType     = _request.PassType,
                    Frame        = _request.FrameNumber,
                    MatchTime    = matchTime
                });

                _state = PassExecutionState.Idle;
                return;
            }

            _windupFramesRemaining--;
            if (_windupFramesRemaining <= 0)
                _state = PassExecutionState.Contact;
        }

        // ── CONTACT State ─────────────────────────────────────────────────────────────

        private void ExecuteContact(float matchTime, int frameNumber, ref BallState ball)
        {
            // Step 1: Re-sample pressure at CONTACT — §3.8.6, §4.4.1
            float pressureScalar = _collisionQuery.ComputePressureScalar(_passerPosition, _request.TeamId);

            // Step 2: Recompute error angle with fresh pressure — §3.8.6
            CrossSubType effectiveSubType = (_request.PassType == PassType.Cross)
                ? _request.CrossSubType
                : CrossSubType.Flat;

            float errorAngleDeg = PassErrorCalculator.ComputeErrorAngle(
                _request.PassType,
                effectiveSubType,
                _cachedPassing,
                pressureScalar,
                _cachedFatigue,
                _cachedBodyAngleDeg,
                _request.Urgency,
                _cachedIsWeakFoot,
                _cachedWeakFootRating);

            // Step 3: Compute deterministic error direction — §3.5.7
            float errorDirectionRad = PassErrorCalculator.ComputeErrorDirection(
                _request.AgentId,
                _request.FrameNumber,
                (int)_request.PassType);

            // Step 4: Apply error to kick direction — §3.6.7
            Vector3 finalKickDirection = PassTargetResolver.ApplyErrorToDirection(
                _baseKickDirection, errorAngleDeg, errorDirectionRad);

            // Step 5: Construct final velocity Vector3 — §3.3.6
            Vector3 finalVelocity = PassVelocityCalculator.ConstructKickVelocity(
                _kickSpeed, finalKickDirection, _launchAngleDeg);

            // FM-04: Guard against NaN in velocity
            if (float.IsNaN(finalVelocity.x) || float.IsNaN(finalVelocity.y) || float.IsNaN(finalVelocity.z))
            {
                Debug.LogError($"[PassExecutor] FM-04: NaN in finalVelocity. Pass cancelled. Agent={_request.AgentId}");
                _lastResult = new PassResult { Outcome = PassOutcome.Cancelled, PassType = _request.PassType };
                _state = PassExecutionState.Idle;
                return;
            }

            // FM-08: Re-check possession before ApplyKick — §4.2.4
            if (!_ballSystem.IsBallPossessedBy(_request.AgentId))
            {
                Debug.LogError($"[PassExecutor] FM-08: Agent {_request.AgentId} lost possession before CONTACT. Race condition.");
                _lastResult = new PassResult { Outcome = PassOutcome.Cancelled, PassType = _request.PassType };
                _state = PassExecutionState.Idle;
                return;
            }

            // Step 6: Call Ball.ApplyKick() — exactly once per pass execution §4.2.1
            _ballSystem.ApplyKick(ref ball, finalVelocity, _spinVector, _request.AgentId, matchTime);

            // Step 7: Populate PassResult — §2.4.2
            _lastResult = new PassResult
            {
                Outcome        = PassOutcome.Completed,
                FinalVelocity  = finalVelocity,
                FinalSpin      = _spinVector,
                AimPoint       = _aimPoint,
                ErrorAngleDeg  = errorAngleDeg,
                LeadDistance   = _leadDistance,
                PassType       = _request.PassType,
                ContactFrame   = frameNumber,
                ContactMatchTime = matchTime
            };

            // Step 8: Publish PassAttemptEvent — §3.9.2
            EventBusStub.Publish(new PassAttemptEvent
            {
                AgentId       = _request.AgentId,
                TeamId        = _request.TeamId,
                PassType      = _request.PassType,
                CrossSubType  = effectiveSubType,
                TargetPosition = _aimPoint,
                FinalVelocity = finalVelocity,
                FinalSpin     = _spinVector,
                ErrorAngleDeg = errorAngleDeg,
                KickSpeed     = _kickSpeed,
                LeadDistance  = _leadDistance,
                IsWeakFoot    = _request.IsWeakFoot,
                TargetAgentId = _request.TargetAgentId,
                Frame         = frameNumber,
                MatchTime     = matchTime
            });

            _state = PassExecutionState.FollowThrough;
        }

        // ── Aim Point Resolution ──────────────────────────────────────────────────────

        private Vector3 ResolveAimPoint(
            in PassRequest request,
            CrossSubType effectiveSubType,
            Vector2 passerPosition,
            out float leadDistance)
        {
            PhysicalProfile prof = _profile;
            leadDistance = 0f;

            if (!prof.IsSpaceTargeted)
            {
                // Player-targeted: aim at current receiver position — §3.6.3
                if (request.TargetAgentId == PassMechanicsConstants.AGENT_ID_NONE)
                {
                    // FM-11: no valid target for player-targeted pass type
                    Debug.LogError($"[PassExecutor] FM-11: Player-targeted pass type {request.PassType} has TargetAgentId=-1.");
                    return PassTargetResolver.ResolvePlayerTargetedAimPoint(
                        passerPosition + Vector2.right * request.IntendedDistance);
                }

                PassAgentState receiverState = _agentQuery.GetState(request.TargetAgentId);
                return PassTargetResolver.ResolvePlayerTargetedAimPoint(receiverState.Position);
            }

            // Space-targeted pass — §3.6.4
            if (request.TargetAgentId == PassMechanicsConstants.AGENT_ID_NONE)
            {
                // Path A: explicit space target from Decision Tree
                leadDistance = 0f;
                return PassTargetResolver.ResolveSpaceTargetedAimPoint(request.TargetPosition);
            }

            // Path B: agent-based space target — compute through-ball lead
            PassAgentState runner = _agentQuery.GetState(request.TargetAgentId);
            return PassTargetResolver.ComputeThroughBallAimPoint(
                passerPosition,
                runner.Position,
                runner.Velocity,
                _kickSpeed,
                out leadDistance);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static PassResult MakeInvalidResult(PassType passType)
        {
            return new PassResult { Outcome = PassOutcome.Invalid, PassType = passType };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                        |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                                      |
// | 1.1     | 2026-05-26 | —      | H1: Idle guard added to Execute() (prevent in-progress overwrite).           |
// |         |            |        |     M3: Update() gains frameNumber param; ContactFrame/Frame set accurately. |
#endregion
