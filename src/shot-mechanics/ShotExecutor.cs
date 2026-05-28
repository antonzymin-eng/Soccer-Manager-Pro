// File:     src/shot-mechanics/ShotExecutor.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §3.9, §4.1, §4.2, §4.3, §4.4, Code Standards #20
// Purpose:  Sealed instance orchestrator for the seven-state shot execution state machine:
//           IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE (±STUMBLING).
//           Validates ShotRequest, coordinates sub-system calls, calls Ball.ApplyKick() at
//           CONTACT exactly once, publishes events. Dependencies constructor-injected (FR-CS-051–054).

using UnityEngine;
using UnityEngine.Profiling;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Orchestrates shot execution across the seven-state lifecycle.
    /// IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE (optional STUMBLING branch).
    /// Shot Mechanics #6 §3.9, §4.1.
    /// </summary>
    public sealed class ShotExecutor
    {
        // ── Dependencies ─────────────────────────────────────────────────────────────

        private readonly IShotBallSystem         _ballSystem;
        private readonly IShotAgentQuery         _agentQuery;
        private readonly IShotCollisionQuery     _collisionQuery;
        private readonly IShotVelocityCalculator _velocityCalculator;

        // ── Profiler Markers ─────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_executeMarker =
            new ProfilerMarker("ShotMechanics.Execute");

        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("ShotMechanics.Update");

        // ── State Machine ────────────────────────────────────────────────────────────

        private enum ShotExecutionState
        {
            Idle,
            Windup,
            Contact,
            FollowThrough,
            Complete
        }

        private ShotExecutionState _state = ShotExecutionState.Idle;

        // ── Values Captured at INITIATING ────────────────────────────────────────────

        private ShotRequest  _request;
        private float        _kickSpeed;
        private float        _launchAngleDeg;
        private Vector3      _spinVector;
        private Vector3      _intendedAimDirection;
        private BodyMechanicsResult _bodyMechanics;
        private float        _weakFootErrorMultiplier;
        private int          _windupFrames; // total windup duration, for animation data

        // Cached agent state inputs (frozen at INITIATING for determinism; NFR-07)
        private Vector3 _cachedAgentPosition;
        private float   _cachedFinishing;
        private float   _cachedLongShots;
        private float   _cachedComposure;
        private float   _cachedFatigue;

        // ── Windup / Follow-Through Timers ───────────────────────────────────────────

        private int _windupFramesRemaining;
        private int _followThroughFramesRemaining;

        // ── Result Storage ───────────────────────────────────────────────────────────

        private ShotResult _lastResult;

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>True when no shot is in progress and the executor is ready.</summary>
        public bool IsIdle => _state == ShotExecutionState.Idle;

        /// <summary>
        /// Result of the most recently completed (or cancelled/invalid) shot.
        /// Only meaningful after IsIdle returns true following a shot that was started.
        /// </summary>
        public ShotResult LastResult => _lastResult;

        // ── Constructor ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a ShotExecutor. Production callers pass null for velocityCalculator to use
        /// the singleton default. EC-008 tests pass NaNVelocityStub. §4.1.2.
        /// </summary>
        public ShotExecutor(
            IShotBallSystem         ballSystem,
            IShotAgentQuery         agentQuery,
            IShotCollisionQuery     collisionQuery,
            IShotVelocityCalculator velocityCalculator = null)
        {
            _ballSystem          = ballSystem;
            _agentQuery          = agentQuery;
            _collisionQuery      = collisionQuery;
            _velocityCalculator  = velocityCalculator ?? ShotVelocityCalculator.Instance;
        }

        // ── Execute — INITIATING State ───────────────────────────────────────────────

        /// <summary>
        /// Initiates a shot. Performs INITIATING validation and pre-computation.
        /// Returns ShotOutcome.Invalid synchronously on any validation failure.
        /// Returns ShotOutcome.Initiated when WINDUP has begun — call Update() each 60Hz
        /// frame until IsIdle, then read LastResult. §3.9, §4.1.
        /// </summary>
        public ShotResult Execute(in ShotRequest request)
        {
            using var _ = s_executeMarker.Auto();

            // Guard: reject if a shot is already executing
            if (_state != ShotExecutionState.Idle)
            {
                Debug.LogError($"[ShotExecutor] Execute() called while shot in progress (state={_state}). Agent={request.AgentId} Frame={request.FrameNumber}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §3.1 VR-01: Agent possession check ───────────────────────────────────
            if (!_ballSystem.IsBallPossessedBy(request.AgentId))
            {
                Debug.LogWarning($"[ShotExecutor] FM-01: Agent {request.AgentId} does not have possession. Frame={request.FrameNumber}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §3.1 VR-02: PowerIntent range ────────────────────────────────────────
            if (request.PowerIntent < 0.0f || request.PowerIntent > 1.0f)
            {
                Debug.LogError($"[ShotExecutor] VR-02: PowerIntent={request.PowerIntent} out of [0,1]. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §3.1 VR-03: SpinIntent range ─────────────────────────────────────────
            if (request.SpinIntent < 0.0f || request.SpinIntent > 1.0f)
            {
                Debug.LogError($"[ShotExecutor] VR-03: SpinIntent={request.SpinIntent} out of [0,1]. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §3.1 VR-04: ContactZone validity ─────────────────────────────────────
            if ((int)request.ContactZone < 0 || (int)request.ContactZone > (int)ContactZone.OffCentre)
            {
                Debug.LogError($"[ShotExecutor] VR-04: ContactZone={request.ContactZone} invalid. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §3.1 VR-05: PlacementTarget.u range ──────────────────────────────────
            if (request.PlacementTarget.x < 0.0f || request.PlacementTarget.x > 1.0f)
            {
                Debug.LogError($"[ShotExecutor] VR-05: PlacementTarget.u={request.PlacementTarget.x} out of [0,1]. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §3.1 VR-06: PlacementTarget.v range ──────────────────────────────────
            if (request.PlacementTarget.y < 0.0f || request.PlacementTarget.y > 1.0f)
            {
                Debug.LogError($"[ShotExecutor] VR-06: PlacementTarget.v={request.PlacementTarget.y} out of [0,1]. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §3.1 VR-07: DistanceToGoal > 0 ──────────────────────────────────────
            if (request.DistanceToGoal <= 0.0f)
            {
                Debug.LogError($"[ShotExecutor] VR-07: DistanceToGoal={request.DistanceToGoal} ≤ 0. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §3.1 VR-08: FrameNumber > 0 ──────────────────────────────────────────
            if (request.FrameNumber <= 0)
            {
                Debug.LogError($"[ShotExecutor] VR-08: FrameNumber={request.FrameNumber} ≤ 0. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── §4.3.2: CurrentState validation ──────────────────────────────────────
            ShotAgentState agentState = _agentQuery.GetState(request.AgentId);
            if (!IsApprovedShotState(agentState.CurrentState))
            {
                Debug.LogWarning($"[ShotExecutor] Agent {request.AgentId} in non-shootable state {agentState.CurrentState}.");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            // ── Read attributes (captured and frozen; NFR-07) ─────────────────────────
            ShotAgentAttributes attrs = _agentQuery.GetAttributes(request.AgentId);

            _request = request;

            // ── §3.7 — Body mechanics (evaluated before velocity; BMS feeds CQM) ───────
            Vector3 toGoalDir = ShotPlacementResolver.ComputeAimDirection(
                new Vector2(0.5f, 0.5f), agentState.Position); // centre of goal for BMS
            _bodyMechanics = BodyMechanicsEvaluator.Evaluate(
                agentState.Velocity,
                agentState.Position,
                agentState.Position, // ball at agent foot at INITIATING; exact position irrelevant
                toGoalDir,
                request.PowerIntent);

            // ── §3.8 — Weak foot velocity penalty ────────────────────────────────────
            float wfVelocityMultiplier = WeakFootPenaltyApplier.ComputeVelocityMultiplier(
                request.IsWeakFoot, attrs.WeakFootRating);
            _weakFootErrorMultiplier = WeakFootPenaltyApplier.ComputeErrorMultiplier(
                request.IsWeakFoot, attrs.WeakFootRating);

            // ── FM-05: Guard against NaN from velocity calculator ─────────────────────
            float rawKickSpeed = _velocityCalculator.Calculate(
                request.PowerIntent,
                request.DistanceToGoal,
                attrs.Finishing,
                attrs.LongShots,
                attrs.KickPower,
                request.ContactZone,
                request.SpinIntent,
                attrs.Fatigue,
                _bodyMechanics.ContactQualityModifier);

            if (float.IsNaN(rawKickSpeed) || float.IsInfinity(rawKickSpeed))
            {
                Debug.LogError($"[ShotExecutor] FM-05: NaN/Infinity from velocity calculator. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            _kickSpeed = rawKickSpeed * wfVelocityMultiplier;

            // ── §3.3 — Launch angle ───────────────────────────────────────────────────
            float bodyLean = ShotLaunchAngleCalculator.DeriveBodyLeanAngle(agentState.Velocity.magnitude);
            _launchAngleDeg = ShotLaunchAngleCalculator.Compute(
                request.ContactZone,
                request.PowerIntent,
                request.SpinIntent,
                bodyLean,
                _bodyMechanics.Score);

            // ── §3.4 — Spin vector ────────────────────────────────────────────────────
            _spinVector = ShotSpinCalculator.Compute(
                request.ContactZone,
                request.SpinIntent,
                request.PowerIntent,
                attrs.Technique,
                agentState.FacingDirection);

            // ── §3.5 — Pre-error aim direction ───────────────────────────────────────
            _intendedAimDirection = ShotPlacementResolver.ComputeAimDirection(
                request.PlacementTarget, agentState.Position);

            // Cache inputs needed at CONTACT for error recalculation
            _cachedAgentPosition = agentState.Position;
            _cachedFinishing     = attrs.Finishing;
            _cachedLongShots     = attrs.LongShots;
            _cachedComposure     = attrs.Composure;
            _cachedFatigue       = attrs.Fatigue;

            // ── §3.9 — Windup duration ────────────────────────────────────────────────
            _windupFrames              = ShotMechanicsConstants.ComputeWindupFrames(
                                             request.PowerIntent, request.SpinIntent);
            _windupFramesRemaining     = _windupFrames;
            _followThroughFramesRemaining = ShotMechanicsConstants.FollowThroughFrames;

            _state = ShotExecutionState.Windup;

            return new ShotResult { Outcome = ShotOutcome.Initiated, ContactFrame = -1 };
        }

        // ── Update — Per-Frame State Machine ─────────────────────────────────────────

        /// <summary>
        /// Advances the shot execution state machine by one 60Hz frame.
        /// Call once per frame while a shot is in progress. §3.9.
        /// </summary>
        /// <param name="matchTime">Current match time (seconds from kickoff).</param>
        /// <param name="frameNumber">Current simulation frame number.</param>
        /// <param name="ball">Ball state — modified by ApplyKick at CONTACT.</param>
        public void Update(float matchTime, int frameNumber, ref BallState ball)
        {
            using var _ = s_updateMarker.Auto();

            switch (_state)
            {
                case ShotExecutionState.Idle:
                    break;

                case ShotExecutionState.Windup:
                    AdvanceWindup(frameNumber);
                    break;

                case ShotExecutionState.Contact:
                    ExecuteContact(matchTime, frameNumber, ref ball);
                    break;

                case ShotExecutionState.FollowThrough:
                    _followThroughFramesRemaining--;
                    if (_followThroughFramesRemaining <= 0)
                        _state = ShotExecutionState.Complete;
                    break;

                case ShotExecutionState.Complete:
                    _state = ShotExecutionState.Idle;
                    break;
            }
        }

        // ── WINDUP State ─────────────────────────────────────────────────────────────

        private void AdvanceWindup(int frameNumber)
        {
            // §4.4.2: Poll tackle interrupt first
            if (_collisionQuery.GetAndClearTackleFlag(_request.AgentId))
            {
                _lastResult = new ShotResult
                {
                    Outcome      = ShotOutcome.Cancelled,
                    ContactFrame = -1
                };

                ShotEventEmitter.PublishShotCancelled(in _request, frameNumber);

                _state = ShotExecutionState.Idle;
                return;
            }

            _windupFramesRemaining--;
            if (_windupFramesRemaining <= 0)
                _state = ShotExecutionState.Contact;
        }

        // ── CONTACT State ─────────────────────────────────────────────────────────────

        private void ExecuteContact(float matchTime, int frameNumber, ref BallState ball)
        {
            // §4.4.1: Re-sample pressure at CONTACT (captures pressure at moment of kick)
            float pressureScalar = _collisionQuery.ComputePressureScalar(
                _cachedAgentPosition, _request.TeamId);

            // §3.6: Compute error magnitude with fresh pressure
            float errorMag = ShotErrorCalculator.ComputeErrorMagnitude(
                _cachedFinishing,
                _cachedLongShots,
                _cachedComposure,
                _request.DistanceToGoal,
                _request.PowerIntent,
                pressureScalar,
                _cachedFatigue,
                _bodyMechanics.Score,
                _weakFootErrorMultiplier);

            // §3.6.9: Deterministic error direction
            Vector2 errorDir    = ShotErrorCalculator.ComputeErrorDirection(
                0, _request.AgentId, _request.FrameNumber); // matchSeed=0 at Stage 0 (§1.6)
            Vector2 errorOffset = ShotErrorCalculator.ComputeErrorOffset(errorMag, errorDir);

            // §3.6.9: Apply error to aim direction
            Vector3 finalDirection = ShotPlacementResolver.ApplyErrorOffset(
                _intendedAimDirection, errorOffset, _cachedAgentPosition);

            // §4.2.1: Construct final velocity vector — horizontal speed + launch angle
            float   launchRad     = _launchAngleDeg * Mathf.Deg2Rad;
            float   horizontalSpd = _kickSpeed * Mathf.Cos(launchRad);
            float   verticalSpd   = _kickSpeed * Mathf.Sin(launchRad);
            // Encode launch angle into Z while preserving XY direction from finalDirection
            Vector2 aimXY         = new Vector2(finalDirection.x, finalDirection.y).normalized;
            Vector3 finalVelocity = new Vector3(
                aimXY.x * horizontalSpd,
                aimXY.y * horizontalSpd,
                verticalSpd);

            // FM-04: Guard against NaN in velocity before calling ApplyKick
            if (float.IsNaN(finalVelocity.x) || float.IsNaN(finalVelocity.y) || float.IsNaN(finalVelocity.z))
            {
                Debug.LogError($"[ShotExecutor] FM-04: NaN in finalVelocity. Shot cancelled. Agent={_request.AgentId}");
                _lastResult = new ShotResult { Outcome = ShotOutcome.Cancelled, ContactFrame = -1 };
                _state = ShotExecutionState.Idle;
                return;
            }

            // FM-03: Re-check possession immediately before ApplyKick — §4.2.4
            if (!_ballSystem.IsBallPossessedBy(_request.AgentId))
            {
                Debug.LogError($"[ShotExecutor] FM-03: Agent {_request.AgentId} lost possession before CONTACT.");
                _lastResult = new ShotResult { Outcome = ShotOutcome.Cancelled, ContactFrame = -1 };
                _state = ShotExecutionState.Idle;
                return;
            }

            // §4.2.1: Exactly one ApplyKick() call per shot execution
            _ballSystem.ApplyKick(ref ball, finalVelocity, _spinVector, _request.AgentId, matchTime);

            // Populate ShotResult — §2.4.2
            _lastResult = new ShotResult
            {
                Outcome            = ShotOutcome.Completed,
                FinalVelocity      = finalVelocity,
                FinalSpin          = _spinVector,
                IntendedDirection  = _intendedAimDirection,
                FinalDirection     = finalDirection,
                ErrorOffset        = errorOffset,
                BodyMechanicsScore = _bodyMechanics.Score,
                PowerPenaltyApplied = 1.0f + ShotMechanicsConstants.PowerPenaltyCoefficient
                                             * _request.PowerIntent * _request.PowerIntent,
                KickSpeed          = _kickSpeed,
                LaunchAngleDeg     = _launchAngleDeg,
                StumbleTriggered   = _bodyMechanics.StumbleTriggered,
                ContactFrame       = frameNumber
            };

            // §3.10: Publish events — Ball.ApplyKick() first, then events (§3.10.2 ordering)
            ShotEventEmitter.PublishShotExecuted(in _request, in _lastResult, matchTime);
            ShotEventEmitter.PublishAnimationData(in _request, _bodyMechanics.Score, _windupFrames);

            _state = ShotExecutionState.FollowThrough;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static ShotResult MakeInvalidResult()
        {
            return new ShotResult { Outcome = ShotOutcome.Invalid, ContactFrame = -1 };
        }

        /// <summary>
        /// Returns true if the agent's movement state permits shot execution. §4.3.2.
        /// Approved: Idle, Walking, Jogging, Sprinting, Decelerating.
        /// Rejected: Grounded, Stumbling.
        /// </summary>
        private static bool IsApprovedShotState(AgentMovement.AgentMovementState state)
        {
            switch (state)
            {
                case AgentMovement.AgentMovementState.Idle:
                case AgentMovement.AgentMovementState.Walking:
                case AgentMovement.AgentMovementState.Jogging:
                case AgentMovement.AgentMovementState.Sprinting:
                case AgentMovement.AgentMovementState.Decelerating:
                    return true;
                default:
                    return false;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                            |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                                          |
// | 1.1     | 2026-05-28 | —      | M-1: Removed unused matchTime param from AdvanceWindup. L-1: var→Vector3 explicit. |
#endregion
