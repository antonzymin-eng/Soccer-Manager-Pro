// File:     src/shot-mechanics/ShotExecutor.cs
// Created:  2026-05-27
// Modified: 2026-07-27  [v1.10]
// Author:   —
// Spec:     Shot Mechanics #6 §3.9, §4.1, §4.2, §4.3, §4.4, Code Standards #20
// Purpose:  Sealed instance orchestrator for the five-state shot execution state machine:
//           IDLE → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE (StumbleTriggered boolean on result).
//           Validates ShotRequest, coordinates sub-system calls, calls Ball.ApplyKick() at
//           CONTACT exactly once, publishes events. Dependencies constructor-injected (FR-CS-051–054).

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Orchestrates shot execution across the five-state lifecycle.
    /// IDLE → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE. INITIATING is synchronous in Execute();
    /// StumbleTriggered is a boolean flag on the result, not a distinct state.
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

        // ORDINAL STABILITY (Match Engine Phase C C0): these ordinals are captured into
        // ShotExecutorState.State and become digest-load-bearing once the C5 snapshot serializes
        // them. APPEND-only — never reorder or insert in the middle, or persisted snapshots /
        // replays desync on the executor state field.
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
        private int          _windupFrames; // total windup duration (frames); copied to animation data at CONTACT, immutable after INITIATING

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
            _velocityCalculator  = velocityCalculator ?? ShotVelocityCalculator.Instance; // EC-008 tests inject NaNVelocityStub; production uses singleton
        }

        // ── Snapshot seam — Match Engine Phase C step C0 ─────────────────────────────

        /// <summary>
        /// Captures the executor's cross-tick state-machine + in-flight fields as a plain-data
        /// snapshot for canonical serialization / deterministic replay (Match Engine design note
        /// §2.6). Allocation-free (returns a value type). Parallel to the Pass executor seam and
        /// to <see cref="AgentMovement.OscillationGuard.GetState"/>. Named Capture (NOT Get) to
        /// avoid colliding with the Shot agent-query <c>GetState</c> surface.
        /// </summary>
        public ShotExecutorState CaptureState()
        {
            return new ShotExecutorState(
                (int)_state,
                in _request,
                _kickSpeed,
                _launchAngleDeg,
                _spinVector,
                _intendedAimDirection,
                in _bodyMechanics,
                _weakFootErrorMultiplier,
                _windupFrames,
                _cachedAgentPosition,
                _cachedFinishing,
                _cachedLongShots,
                _cachedComposure,
                _cachedFatigue,
                _windupFramesRemaining,
                _followThroughFramesRemaining,
                in _lastResult);
        }

        /// <summary>
        /// Restores the executor's cross-tick state from a snapshot produced by
        /// <see cref="CaptureState"/> (replay / save-load). Parallel to the Pass executor seam.
        /// Every in-flight field is carried directly — Shot has no internal recompute-on-restore
        /// exclusion (unlike Pass's PhysicalProfile).
        /// </summary>
        public void RestoreState(in ShotExecutorState state)
        {
            _state                        = (ShotExecutionState)state.State;
            _request                      = state.Request;
            _kickSpeed                    = state.KickSpeed;
            _launchAngleDeg               = state.LaunchAngleDeg;
            _spinVector                   = state.SpinVector;
            _intendedAimDirection         = state.IntendedAimDirection;
            _bodyMechanics                = state.BodyMechanics;
            _weakFootErrorMultiplier      = state.WeakFootErrorMultiplier;
            _windupFrames                 = state.WindupFrames;
            _cachedAgentPosition          = state.CachedAgentPosition;
            _cachedFinishing              = state.CachedFinishing;
            _cachedLongShots              = state.CachedLongShots;
            _cachedComposure              = state.CachedComposure;
            _cachedFatigue                = state.CachedFatigue;
            _windupFramesRemaining        = state.WindupFramesRemaining;
            _followThroughFramesRemaining = state.FollowThroughFramesRemaining;
            _lastResult                   = state.LastResult;
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

            // Guard: reject if a shot is already executing.
            // Report the rejection via the return value ONLY — must NOT stomp _lastResult, which
            // may hold the committed Completed record (ContactFrame replay-sync data) of a shot
            // still in FollowThrough/Complete. Mirrors Pass Mechanics AR-9 M-1.
            if (_state != ShotExecutionState.Idle)
            {
                Debug.LogError($"[ShotExecutor] Execute() called while shot in progress (state={_state}). Agent={request.AgentId} Frame={request.FrameNumber}");
                return MakeInvalidResult();
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
            // Project NaN-gate idiom: `<= 0f` passes NaN (NaN comparisons are false), so test the
            // positive case and reject non-finite distances explicitly. Mirrors Pass Mechanics AR-9 M-2.
            if (!(request.DistanceToGoal > 0.0f) || float.IsInfinity(request.DistanceToGoal))
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

            // ── §3.1 VR-09: Fatigue range ─────────────────────────────────────────────
            if (attrs.Fatigue < 0.0f || attrs.Fatigue > 1.0f)
            {
                Debug.LogWarning($"[ShotExecutor] VR-09: Fatigue={attrs.Fatigue} out of [0, 1]. Agent={request.AgentId}");
                _lastResult = MakeInvalidResult();
                return _lastResult;
            }

            _request = request;

            // ── §3.7 — Body mechanics (evaluated before velocity; BMS feeds CQM) ───────
            // Body mechanics scores "approaching" posture toward goal centre (general readiness),
            // not toward the specific PlacementTarget. §3.7: BMS measures stance quality, not aim fidelity.
            Vector3 toGoalDir = ShotPlacementResolver.ComputeAimDirection(
                new Vector2(ShotMechanicsConstants.GoalCentreU, ShotMechanicsConstants.GoalCentreV),
                agentState.Position);
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
            // §3.5.6 composition (ERR-006-002 / shot-outcome KD-2): the horizontal unit toward the
            // u target tilted by the §3.3 launch angle — the vertical comes from the launch model,
            // not from the geometric line to the (u, v) point (which the former assembly discarded
            // anyway, leaving the vertical half of the placement/error model inert).
            _intendedAimDirection = ShotPlacementResolver.ComputeAimDirectionWithLaunchAngle(
                request.PlacementTarget, agentState.Position, _launchAngleDeg);

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

            // §3.8.5 freshness: drain (discard) any stale tackle flag before WINDUP begins.
            // The flag is otherwise only polled during AdvanceWindup, so a tackle registered while
            // idle / in FollowThrough (even with no possession) would survive and cancel THIS shot
            // on its first WINDUP frame. Mirrors Pass Mechanics AR-9 M-3.
            _collisionQuery.GetAndClearTackleFlag(request.AgentId);

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
                ShotMechanicsConstants.ErrorDirectionMatchSeed, _request.AgentId, _request.FrameNumber);
            Vector2 errorOffset = ShotErrorCalculator.ComputeErrorOffset(errorMag, errorDir);

            // §3.6.9: Apply error to aim direction
            Vector3 finalDirection = ShotPlacementResolver.ApplyErrorOffset(
                _intendedAimDirection, errorOffset, _cachedAgentPosition);

            // §3.5.7 / §3.9 step 9: finalVelocity = finalDirection × kickSpeed. The launch angle is
            // already encoded in finalDirection's Z by the §3.5.6 composition at INITIATING, and the
            // error model's vertical half rides finalDirection.z — the former cos/sin re-derivation
            // discarded that Z and made vertical placement/error inert (ERR-006-002, shot-outcome
            // design KD-1).
            // FM-04a: Guard against degenerate (near-zero) XY magnitude — happens only if the
            // shooter is exactly on the goal line (ApplyErrorOffset returned a delta with ~zero XY).
            // Route through Invalid outcome rather than producing NaN and relying on the FM-04
            // post-hoc trap below.
            Vector2 aimXY2 = new Vector2(finalDirection.x, finalDirection.y);
            if (aimXY2.sqrMagnitude < ShotMechanicsConstants.AimDirectionEpsilon
                                       * ShotMechanicsConstants.AimDirectionEpsilon)
            {
                Debug.LogError($"[ShotExecutor] FM-04a: degenerate XY aim (shooter on goal line). Shot invalid. Agent={_request.AgentId}");
                _lastResult = new ShotResult { Outcome = ShotOutcome.Invalid, ContactFrame = -1 };
                _state = ShotExecutionState.Idle;
                return;
            }
            Vector3 finalVelocity = finalDirection * _kickSpeed;

            // FM-04: Guard against NaN in assembled finalVelocity (direction × speed encoding).
            // Detects formula/arithmetic errors in lines above — not bad input (FM-05 catches NaN from
            // velocity calculator earlier). Programming error → Invalid outcome, no event.
            if (float.IsNaN(finalVelocity.x) || float.IsNaN(finalVelocity.y) || float.IsNaN(finalVelocity.z))
            {
                Debug.LogError($"[ShotExecutor] FM-04: NaN in finalVelocity. Shot invalid. Agent={_request.AgentId}");
                _lastResult = new ShotResult { Outcome = ShotOutcome.Invalid, ContactFrame = -1 };
                _state = ShotExecutionState.Idle;
                return;
            }

            // FM-03: Re-check possession immediately before ApplyKick — §4.2.4.
            // ShotEventEmitter.PublishShotCancelled() is NOT called here.
            // §4.7.1 permanently restricts ShotCancelledEvent to WINDUP tackle interrupts only.
            // FM-03 is CONTACT-phase possession loss; ShotCancelReason must NOT get PossessionLost.
            // Stage 1: if notification is needed, add a separate PossessionLostEvent channel.
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

            // Commit the state transition BEFORE publishing. The ball is already kicked (ApplyKick
            // above); if a Publish throws (queue overflow / registry mismatch) the executor must not
            // remain in Contact, or the next Update would re-enter ExecuteContact and call ApplyKick
            // a second time. FM-03's possession recheck currently guards the double-kick, but ordering
            // the transition first removes the dependence on that recovery seam. Mirrors Pass AR-8 L-2.
            _state = ShotExecutionState.FollowThrough;

            // §3.10: Publish events — Ball.ApplyKick() first, then events (§3.10.2 ordering)
            ShotEventEmitter.PublishShotExecuted(in _request, in _lastResult, matchTime);
            ShotEventEmitter.PublishAnimationData(in _request, _bodyMechanics.Score, _windupFrames);
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
                case AgentMovement.AgentMovementState.IDLE:
                case AgentMovement.AgentMovementState.WALKING:
                case AgentMovement.AgentMovementState.JOGGING:
                case AgentMovement.AgentMovementState.SPRINTING:
                case AgentMovement.AgentMovementState.DECELERATING:
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
// | 1.2     | 2026-05-28 | —      | H-2: FM-03 possession-loss: §4.7.1 restricts ShotCancelledEvent to WINDUP          |
// |         |            |        |   tackle interrupts; no ShotCancelReason.PossessionLost at Stage 0.                |
// |         |            |        |   AR-1 incorrectly added PublishShotCancelled; reverted. Comment + Stage 1 TODO    |
// |         |            |        |   added. H-3: FM-04 (NaN velocity) outcome Cancelled→Invalid (programming error).  |
// |         |            |        |   M-1: Header/XML corrected seven-state→five-state.                                |
// |         |            |        |   M-5: Hardcoded 0.5f/0.5f replaced with GoalCentreU/GoalCentreV.                 |
// | 1.3     | 2026-05-28 | —      | M-1: FM-03 TODO rephrased: §4.7.1 permanently bans ShotCancelledEvent for FM-03;    |
// |         |            |        |   Stage 1 must use a separate PossessionLostEvent channel.                         |
// |         |            |        |   M-2: FM-04 comment clarified: detects formula errors in velocity assembly,       |
// |         |            |        |   not input validation (FM-05 covers velocity-calculator NaN earlier).             |
// |         |            |        |   L-1: Body mechanics comment: explains why goal centre, not PlacementTarget.      |
// |         |            |        |   L-2: _windupFrames and velocityCalculator field comments improved.               |
// | 1.4     | 2026-05-28 | —      | M-1: VR-09: Fatigue [0,1] range validation added (out-of-range = Invalid).          |
// |         |            |        |   M-2: matchSeed literal 0 → ErrorDirectionMatchSeed constant.                     |
// |         |            |        |   M-3: FM-03 comment: explicit "PublishShotCancelled NOT called here".             |
// | 1.5     | 2026-06-01 | —      | AR-2 H-1: explicit FM-04a guard against degenerate XY aim before normalize         |
// |         |            |        |   (routes to Invalid outcome rather than relying on FM-04 post-hoc NaN trap).      |
// |         |            |        |   L-3: header doc — "±STUMBLING flag"→"StumbleTriggered boolean" (clarity).        |
// | 1.6     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling -> Unity.Profiling.        |
// |         |            |        | ProfilerMarker's actual namespace is Unity.Profiling; the old using was CS0246     |
// |         |            |        | under Unity and the Linux compile gate alike, so this assembly could not have      |
// |         |            |        | compiled in-engine. No functional change.                                          |
// | 1.7     | 2026-06-12 | —      | Build fix (dotnet CI gate): IsApprovedShotState referenced AgentMovementState      |
// |         |            |        | members by PascalCase names (Idle/Walking/Jogging/Sprinting/Decelerating) but the  |
// |         |            |        | #2 enum declares ALL_CAPS members (IDLE..DECELERATING) - CS0117 under Unity and    |
// |         |            |        | the Linux gate alike; this assembly never compiled. Members corrected;             |
// |         |            |        | approved/rejected set unchanged.                                                   |
// | 1.8     | 2026-06-13 | —      | AR fix pass — four defect classes ported from the sibling PassExecutor that were   |
// |         |            |        | never propagated here. M-1 (Pass AR-9 M-3): Execute() drains the stale tackle flag |
// |         |            |        | at INITIATING; a flag set while idle/FollowThrough no longer cancels the NEXT      |
// |         |            |        | shot's first WINDUP frame. M-2 (Pass AR-9 M-1): in-progress Execute() guard no      |
// |         |            |        | longer stomps _lastResult (was destroying a committed Completed record); reports    |
// |         |            |        | rejection via return value only. L-1 (Pass AR-8 L-2): _state=FollowThrough          |
// |         |            |        | hoisted above the Publish calls (a throwing Publish previously left the executor    |
// |         |            |        | in Contact → ApplyKick double-kick on re-entry). L-2 (Pass AR-9 M-2): VR-07 gate    |
// |         |            |        | `<= 0f` passed NaN; now `!(d > 0f) || IsInfinity(d)` per the project NaN-gate idiom.|
// | 1.9     | 2026-06-19 | —      | Match Engine Phase C step C0: CaptureState()/RestoreState(in ShotExecutorState)    |
// |         |            |        | snapshot seam added (parallel to PassExecutor C0 + OscillationGuard B0) so the      |
// |         |            |        | match-engine snapshot can serialize the executor's cross-tick state-machine +      |
// |         |            |        | in-flight fields for deterministic replay (design note §2.6). Full field set       |
// |         |            |        | carried directly (no internal recompute exclusion). No change to Execute/Update.   |
// | 1.9.1   | 2026-06-19 | —      | C0 AR-1 (L-1): ShotExecutionState gains an ORDINAL STABILITY note — its ordinals    |
// |         |            |        | are captured into ShotExecutorState.State and become digest-load-bearing at C5      |
// |         |            |        | (APPEND-only). Doc-only.                                                            |
// | 1.10    | 2026-07-27 | —      | ERR-006-002 (shot-outcome design KD-1/KD-2): the intended aim now uses the §3.5.6   |
// |         |            |        | composition (ComputeAimDirectionWithLaunchAngle — vertical from the launch model),  |
// |         |            |        | and CONTACT assembles finalVelocity = finalDirection × kickSpeed per §3.5.7/§3.9    |
// |         |            |        | step 9 — the former cos/sin re-derivation discarded finalDirection.z, leaving the   |
// |         |            |        | vertical half of the placement/error model inert. FM-04a/FM-04 guards unchanged.    |
#endregion
