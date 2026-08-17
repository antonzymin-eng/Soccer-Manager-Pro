// File:     src/pass-mechanics/PassExecutor.cs
// Created:  2026-05-26
// Modified: 2026-08-12 (W2: FM-08's CONTACT-time possession loss downgraded LogError -> LogWarning; a tackle now makes it ordinary)
// Author:   —
// Spec:     Pass Mechanics #5 §3.8, §3.9, §4.1, Code Standards #20
// Purpose:  Sealed instance orchestrator for the six-state pass execution state
//           machine. Validates PassRequest, coordinates sub-system calls, calls
//           Ball.ApplyKick() at CONTACT, and publishes events. Dependencies are
//           constructor-injected (FR-CS-051–054).

using UnityEngine;
using Unity.Profiling;

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

        // ORDINAL STABILITY (Match Engine Phase C C0): these ordinals are captured into
        // PassExecutorState.State and become digest-load-bearing once the C5 snapshot serializes
        // them. APPEND-only — never reorder or insert in the middle, or persisted snapshots /
        // replays desync on the executor state field.
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
        private CrossSubType _cachedEffectiveSubType; // cross: actual sub-type; all others: Flat

        // ── Windup / Follow-Through Timers ───────────────────────────────────────────

        private int _windupFramesRemaining;
        private int _followThroughFramesRemaining;

        // ── Result Storage ───────────────────────────────────────────────────────────

        private PassResult _lastResult;

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>True when no pass is in progress and the executor is ready.</summary>
        public bool IsIdle => _state == PassExecutionState.Idle;

        /// <summary>
        /// The team-mate this executor's CURRENT pass was aimed at (<c>PassRequest.TargetAgentId</c>).
        /// <para>
        /// Only meaningful at the moment of the CONTACT kick, which is the sole caller: <c>_request</c>
        /// is never cleared on the return to Idle, so between passes this reports a stale last-pass
        /// target forever. Read it at the kick or not at all — the engine's own pass-in-flight latch
        /// (ERR-012-011) exists precisely because this value is not self-dating.
        /// </para>
        /// <para>
        /// Not a second derivation of anything: this and <c>CaptureState().Request</c> return the same
        /// <c>_request</c> field, one for observation at the kick and one for serialization.
        /// </para>
        /// </summary>
        public int InFlightTargetAgentId => _request.TargetAgentId;

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

        // ── Snapshot seam — Match Engine Phase C step C0 ─────────────────────────────

        /// <summary>
        /// Captures the executor's cross-tick state-machine + in-flight fields as a plain-data
        /// snapshot for canonical serialization / deterministic replay (Match Engine design note
        /// §2.6). Allocation-free (returns a value type). Parallel to
        /// <see cref="AgentMovement.OscillationGuard.GetState"/> and DeterministicSim's
        /// RngStreamState. Named Capture (NOT Get) to avoid colliding with
        /// <see cref="IPassAgentQuery.GetState"/>.
        /// </summary>
        public PassExecutorState CaptureState()
        {
            return new PassExecutorState(
                (int)_state,
                in _request,
                _cachedEffectiveSubType,
                _kickSpeed,
                _launchAngleDeg,
                _spinVector,
                _baseKickDirection,
                _aimPoint,
                _leadDistance,
                _cachedPassing,
                _cachedFatigue,
                _cachedBodyAngleDeg,
                _cachedIsWeakFoot,
                _cachedWeakFootRating,
                _windupFramesRemaining,
                _followThroughFramesRemaining,
                in _lastResult);
        }

        /// <summary>
        /// Restores the executor's cross-tick state from a snapshot produced by
        /// <see cref="CaptureState"/> (replay / save-load). Parallel to
        /// <see cref="AgentMovement.OscillationGuard.RestoreState"/>. The internal
        /// <see cref="PhysicalProfile"/> is NOT serialized — it is a pure function of
        /// (<see cref="PassRequest.PassType"/>, effective sub-type), so it is recomputed here
        /// (design note §2.6 "fully recomputed before its first read" exclusion).
        /// </summary>
        public void RestoreState(in PassExecutorState state)
        {
            _state                  = (PassExecutionState)state.State;
            _request                = state.Request;
            _cachedEffectiveSubType = state.EffectiveSubType;

            // Recompute the internal profile (pure function of PassType + effective sub-type;
            // never serialized — §2.6 recompute exclusion). NOTE: restoring a captured-Idle state
            // recomputes a real profile (e.g. GetProfile(Ground, Flat)) rather than the
            // default(PhysicalProfile) a freshly-constructed executor holds; this is benign — Idle
            // Update is a no-op, _profile is excluded from the digest, and the next Execute()
            // overwrites it before any read.
            _profile = PassTypeProfiles.GetProfile(state.Request.PassType, state.EffectiveSubType);

            _kickSpeed                    = state.KickSpeed;
            _launchAngleDeg               = state.LaunchAngleDeg;
            _spinVector                   = state.SpinVector;
            _baseKickDirection            = state.BaseKickDirection;
            _aimPoint                     = state.AimPoint;
            _leadDistance                 = state.LeadDistance;
            _cachedPassing                = state.CachedPassing;
            _cachedFatigue                = state.CachedFatigue;
            _cachedBodyAngleDeg           = state.CachedBodyAngleDeg;
            _cachedIsWeakFoot             = state.CachedIsWeakFoot;
            _cachedWeakFootRating         = state.CachedWeakFootRating;
            _windupFramesRemaining        = state.WindupFramesRemaining;
            _followThroughFramesRemaining = state.FollowThroughFramesRemaining;
            _lastResult                   = state.LastResult;
        }

        // ── Execute — INITIATING State ───────────────────────────────────────────────

        /// <summary>
        /// Initiates a pass. Performs all INITIATING validation and pre-computation
        /// (§3.8.4). If validation fails, returns Invalid immediately and state stays
        /// Idle. If valid, transitions to WINDUP — call Update() each frame until
        /// IsIdle is true, then read LastResult. Pass Mechanics #5 §3.8.4.
        /// </summary>
        /// <param name="request">Pass request from Decision Tree #8.</param>
        /// <returns>
        /// PassResult with Outcome=Invalid for synchronous rejection.
        /// PassResult with Outcome=Initiated means windup has begun;
        /// poll IsIdle and read LastResult to obtain the final outcome.
        /// </returns>
        public PassResult Execute(in PassRequest request)
        {
            using var _ = s_executeMarker.Auto();

            // ── Guard: reject if already executing a pass ────────────────────────────
            if (_state != PassExecutionState.Idle)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[PassExecutor] Execute() called while pass is in progress (state={_state}). Agent={request.AgentId}. Frame={request.FrameNumber}");
#endif
                // AR-9 M-1: do NOT write _lastResult here. A pass is in flight and owns
                // that slot — in FollowThrough/Complete it already holds the committed
                // Completed record (ContactFrame is replay-sync data), and nothing would
                // rewrite it before IsIdle turns true. The rejection is reported to the
                // offending caller via the return value only.
                return MakeInvalidResult(request.PassType);
            }

            // ── FM-01: Possession check ──────────────────────────────────────────────
            if (!_ballSystem.IsBallPossessedBy(request.AgentId))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[PassExecutor] FM-01: Agent {request.AgentId} does not have possession. Frame={request.FrameNumber}");
#endif
                _lastResult = MakeInvalidResult(request.PassType);
                return _lastResult;
            }

            // ── FM-01: PassType validation ───────────────────────────────────────────
            // Explicit-switch validity check (no Enum.IsDefined — reflection is banned
            // per FR-CS-027–034). Maintainers MUST add a new case here when appending
            // a member to the PassType enum, or FM-01 will close it as Invalid.
            if (!IsValidPassType(request.PassType))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[PassExecutor] FM-01: Invalid PassType={request.PassType}. Frame={request.FrameNumber}");
#endif
                _lastResult = MakeInvalidResult(request.PassType);
                return _lastResult;
            }

            // ── FM-07: Distance validation ───────────────────────────────────────────
            // Negated-comparison form: NaN fails (d > 0) so non-finite distance is
            // rejected here rather than slipping past `d <= 0f` (NaN compares false on
            // both sides) and being silently sanitised to a 0.001 m pass downstream by
            // Mathf.Max argument ordering in ComputeKickSpeed. AR-9 M-2; parallels the
            // First Touch AR-8 M-1 / Agent Movement AR-10 NaN-gate pattern.
            if (!(request.IntendedDistance > 0f) || float.IsInfinity(request.IntendedDistance))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[PassExecutor] FM-07: IntendedDistance={request.IntendedDistance} is not a positive finite value. Frame={request.FrameNumber}");
#endif
                _lastResult = MakeInvalidResult(request.PassType);
                return _lastResult;
            }

            // ── Profile lookup ───────────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (request.PassType != PassType.Cross && request.CrossSubType != CrossSubType.Flat)
                Debug.LogWarning($"[PassExecutor] CrossSubType={request.CrossSubType} ignored for non-Cross PassType={request.PassType}.");
#endif

            CrossSubType effectiveSubType = (request.PassType == PassType.Cross)
                ? request.CrossSubType
                : CrossSubType.Flat;

            _request = request;
            _profile = PassTypeProfiles.GetProfile(request.PassType, effectiveSubType);

            // ── FM-11: Player-targeted pass must have a valid target agent ────────────
            if (!_profile.IsSpaceTargeted && request.TargetAgentId == PassMechanicsConstants.AGENT_ID_NONE)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[PassExecutor] FM-11: Player-targeted pass type {request.PassType} has TargetAgentId=-1. Frame={request.FrameNumber}");
#endif
                _lastResult = MakeInvalidResult(request.PassType);
                return _lastResult;
            }

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
            // Note: ResolveAimPoint runs BEFORE the cache block below — its only
            // PassRequest reads are TargetAgentId / TargetPosition (request-locals), so
            // it does not depend on _cached* fields. If a future extension consumes
            // cached values, move the cache block ABOVE this call (X-1 trap).
            // §3.6.5 Stage-0 lead projection uses receiver position-at-windup-start;
            // staleness across WINDUP frames is a Stage 1 upgrade (KD-4, §7.1).
            _aimPoint = ResolveAimPoint(request, agentState.Position, out _leadDistance);
            _aimPoint = PassTargetResolver.ClampToPitchBounds(_aimPoint);

            // ── Base kick direction (pre-error) ──────────────────────────────────────
            _baseKickDirection = PassTargetResolver.ComputeKickDirection(agentState.Position, _aimPoint);

            // ── Cache error-chain inputs ──────────────────────────────────────────────
            _cachedPassing            = attrs.Passing;
            _cachedFatigue            = attrs.Fatigue;
            _cachedBodyAngleDeg       = PassTargetResolver.ComputeBodyAngle(agentState.FacingDirection, _baseKickDirection);
            _cachedIsWeakFoot         = request.IsWeakFoot;
            _cachedWeakFootRating     = attrs.WeakFootRating;
            _cachedEffectiveSubType   = effectiveSubType;

            // ── §3.8.8 — Windup duration ─────────────────────────────────────────────
            int baseWindup  = PassMechanicsConstants.GetWindupFrames(request.PassType, effectiveSubType);
            float reduction = 1.0f - Mathf.Clamp01(request.Urgency) * PassMechanicsConstants.UrgencyWindupReduction;
            _windupFramesRemaining = Mathf.Max(
                PassMechanicsConstants.MinWindupFrames,
                Mathf.RoundToInt(baseWindup * reduction));

            _followThroughFramesRemaining = PassMechanicsConstants.GetFollowThroughFrames(
                request.PassType, effectiveSubType);

            // §3.8.5 flag freshness (AR-9 M-3): the tackle flag is cleared only by
            // polling, and polling happens only during WINDUP frames — so a flag set
            // during FollowThrough/Idle (possibly while this agent did not even have
            // possession) would survive until now and spuriously cancel THIS pass on
            // its first WINDUP frame. Drain and discard it so WINDUP polls observe
            // only tackles registered after the pass began.
            _collisionQuery.GetAndClearTackleFlag(request.AgentId);

            _state = PassExecutionState.Windup;

            return new PassResult
            {
                Outcome      = PassOutcome.Initiated,
                PassType     = request.PassType,
                AimPoint     = _aimPoint,
                LeadDistance = _leadDistance,
                ContactFrame = -1
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
                    UpdateWindup(matchTime, frameNumber);
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

        private void UpdateWindup(float matchTime, int frameNumber)
        {
            // Poll tackle interrupt first — §3.8.5
            if (_collisionQuery.GetAndClearTackleFlag(_request.AgentId))
            {
                EmitPassCancelled(matchTime, frameNumber, CancelReason.TackleInterrupt);
                return;
            }

            _windupFramesRemaining--;
            if (_windupFramesRemaining <= 0)
                _state = PassExecutionState.Contact;
        }

        // ── CONTACT State ─────────────────────────────────────────────────────────────

        private void ExecuteContact(float matchTime, int frameNumber, ref BallState ball)
        {
            // Step 1: Re-sample pressure at CONTACT — §3.8.6, §4.4.1.
            // Position is re-queried fresh (AR-9 L-1): §3.8.6's intent is contact-time
            // pressure, and the INITIATING-time position is up to ~15 frames stale for
            // a pass on the run. Body angle stays INITIATING-cached by design — only
            // pressure is specified for CONTACT re-sampling.
            Vector2 passerPositionNow = _agentQuery.GetState(_request.AgentId).Position;
            float pressureScalar = _collisionQuery.ComputePressureScalar(passerPositionNow, _request.TeamId);

            // Step 2: Recompute error angle with fresh pressure — §3.8.6
            float errorAngleDeg = PassErrorCalculator.ComputeErrorAngle(
                _request.PassType,
                _cachedEffectiveSubType,
                _cachedPassing,
                pressureScalar,
                _cachedFatigue,
                _cachedBodyAngleDeg,
                _request.Urgency,
                _cachedIsWeakFoot,
                _cachedWeakFootRating);

            // Step 3: Compute deterministic error direction — §3.5.7
            // Returns a signed fraction in [-1, +1) (uniform distribution; upper bound
            // is open because the 24-bit mantissa quantisation never yields +1.0f).
            float errorDirectionFraction = PassErrorCalculator.ComputeErrorDirection(
                _request.AgentId,
                _request.FrameNumber,
                (int)_request.PassType);

            // Step 4: Apply error to kick direction — §3.6.7
            Vector3 finalKickDirection = PassTargetResolver.ApplyErrorToDirection(
                _baseKickDirection, errorAngleDeg, errorDirectionFraction);

            // Step 5: Construct final velocity Vector3 — §3.3.6
            Vector3 finalVelocity = PassVelocityCalculator.ConstructKickVelocity(
                _kickSpeed, finalKickDirection, _launchAngleDeg);

            // FM-04: Guard against NaN in velocity
            if (float.IsNaN(finalVelocity.x) || float.IsNaN(finalVelocity.y) || float.IsNaN(finalVelocity.z))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[PassExecutor] FM-04: NaN in finalVelocity. Pass cancelled. Agent={_request.AgentId}");
#endif
                EmitPassCancelled(matchTime, frameNumber, CancelReason.InvalidVelocity);
                return;
            }

            // FM-08: Re-check possession before ApplyKick — §4.2.4
            if (!_ballSystem.IsBallPossessedBy(_request.AgentId))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Downgraded from LogError, and the wording corrected, at wiring backlog W2. "Race
                // condition" was accurate while the ONLY way to lose the ball mid-windup was an
                // ordering accident between systems — which is what FM-08 was written to catch. Since
                // #14 §3.6.5 gave the engine a tackle, this is an ORDINARY football event: a defender
                // took the ball off the passer before he struck it. Leaving it at error level would
                // put a red line in the log for every successful tackle on a passer, which both buries
                // real errors and fails any suite that treats an unexpected LogError as a failure.
                Debug.LogWarning($"[PassExecutor] FM-08: Agent {_request.AgentId} lost possession before CONTACT — pass cancelled.");
#endif
                EmitPassCancelled(matchTime, frameNumber, CancelReason.PossessionLost);
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

            // State transition must precede Publish: if EventBusStub.Publish throws
            // (queue overflow / registry mismatch), the ball has already been kicked
            // (Step 6) and _state must not stay in Contact — otherwise next frame's
            // Update re-enters ExecuteContact and calls ApplyKick a second time.
            // The FM-08 possession re-check would normally catch the re-entry path,
            // but defensive ordering removes the dependence on that recovery seam.
            // AR-8 L-2.
            _state = PassExecutionState.FollowThrough;

            // Step 8: Publish PassAttemptEvent — §3.9.2
            EventBusStub.Publish(new PassAttemptEvent
            {
                AgentId       = _request.AgentId,
                TeamId        = _request.TeamId,
                PassType      = _request.PassType,
                CrossSubType  = _cachedEffectiveSubType,
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
        }

        // ── Aim Point Resolution ──────────────────────────────────────────────────────

        private Vector3 ResolveAimPoint(
            in PassRequest request,
            Vector2 passerPosition,
            out float leadDistance)
        {
            PhysicalProfile prof = _profile;
            leadDistance = 0f;

            if (!prof.IsSpaceTargeted)
            {
                // Player-targeted: aim at current receiver position — §3.6.3
                // TargetAgentId != -1 guaranteed by FM-11 check in Execute()
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
            return new PassResult { Outcome = PassOutcome.Invalid, PassType = passType, ContactFrame = -1 };
        }

        // Explicit-switch validity predicate — reflection-free (FR-CS-027–034).
        // MAINTAINERS: when appending a new PassType member, ADD a new case here in the
        // same commit. Coverage mirrors PassTypeProfiles.GetProfile / GetBaseError.
        private static bool IsValidPassType(PassType passType)
        {
            switch (passType)
            {
                case PassType.Ground:
                case PassType.Driven:
                case PassType.Lofted:
                case PassType.ThroughBall:
                case PassType.AerialThrough:
                case PassType.Cross:
                case PassType.Chip:
                    return true;
                default:
                    return false;
            }
        }

        // Shared cancellation path: sets _lastResult, publishes PassCancelledEvent, returns to Idle.
        // Used by WINDUP tackle interrupt (§3.8.5), CONTACT-time FM-04 (NaN velocity), and
        // CONTACT-time FM-08 (lost possession) — all paths terminating before Ball.ApplyKick().
        // PRECONDITION: _request has been populated by Execute()'s cache block. Synchronous
        // Execute()-level Invalid rejections — FM-01 (unknown PassType), FM-07 (distance ≤ 0),
        // FM-11 (player-targeted with TargetAgentId=-1) — return via MakeInvalidResult BEFORE
        // _request is assigned, so they must NOT route through this helper; otherwise the
        // event would publish default(PassType)=Ground instead of the actually-requested type.
        private void EmitPassCancelled(float matchTime, int frameNumber, CancelReason reason)
        {
            _lastResult = new PassResult
            {
                Outcome      = PassOutcome.Cancelled,
                PassType     = _request.PassType,
                ContactFrame = -1
            };

            EventBusStub.Publish(new PassCancelledEvent
            {
                AgentId   = _request.AgentId,
                TeamId    = _request.TeamId,
                Reason    = reason,
                PassType  = _request.PassType,
                Frame     = frameNumber,
                MatchTime = matchTime
            });

            _state = PassExecutionState.Idle;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                        |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                                      |
// | 1.1     | 2026-05-26 | —      | H1: Idle guard added to Execute() (prevent in-progress overwrite).           |
// |         |            |        |     M3: Update() gains frameNumber param; ContactFrame/Frame set accurately. |
// | 1.2     | 2026-05-27 | —      | AR-1 H-2: Execute() sentinel changed Completed → Initiated (Completed       |
// |         |            |        |     semantics are "ball kicked"; Initiated is "windup started").             |
// |         |            |        | AR-1 H-3: UpdateWindup gains frameNumber param; PassCancelledEvent.Frame     |
// |         |            |        |     now records cancellation frame, not initiation frame (§3.9.3).          |
// |         |            |        | AR-1 round-2 L-A: all non-Completed PassResult paths set ContactFrame=-1    |
// |         |            |        |     (previously 0, ambiguous with frame 0 at start of match).               |
// | 1.3     | 2026-05-27 | —      | AR-1 round-3 L-C: added _cachedEffectiveSubType field; set in Execute()    |
// |         |            |        |     cache block; ExecuteContact uses cached value instead of recomputing.   |
// |         |            |        | AR-1 round-3 M-C: removed unused CrossSubType param from ResolveAimPoint;  |
// |         |            |        |     PassAttemptEvent.CrossSubType uses _cachedEffectiveSubType.             |
// | 1.4     | 2026-05-27 | —      | AR-1 round-4 M-A: FM-11 check moved from ResolveAimPoint to Execute();    |
// |         |            |        |     player-targeted pass with TargetAgentId=-1 now returns Invalid (was    |
// |         |            |        |     logging error but returning Initiated with fallback aim point).         |
// | 1.5     | 2026-05-27 | —      | AR-1 round-5 M-B: removed unused ref BallState ball param from Execute(); |
// |         |            |        |     possession check uses _ballSystem, not BallState directly.            |
// |         |            |        | (AR-8 L-3 forward-reference: the "[-1, +1]" characterisation in the      |
// |         |            |        |  AR-2 M-2 line below was superseded by AR-6 L-1 — the contract is        |
// |         |            |        |  actually [-1, +1) because the 24-bit mantissa never produces +1.0f.)    |
// | 1.6     | 2026-06-06 | —      | AR-2 H-1/H-2: ExecuteContact FM-04 (NaN velocity) and FM-08 (lost          |
// |         |            |        |     possession) silent-cancel paths now publish PassCancelledEvent via the |
// |         |            |        |     new EmitPassCancelled helper using CancelReason.InvalidVelocity /     |
// |         |            |        |     PossessionLost respectively. WINDUP TackleInterrupt path migrated to   |
// |         |            |        |     the same helper. §3.9.3 telemetry surface now complete.                |
// |         |            |        | AR-2 M-2: callsite renamed errorDirectionRad → errorDirectionFraction to   |
// |         |            |        |     match PassErrorCalculator's new [-1, +1] uniform-fraction contract.    |
// |         |            |        | AR-2 L-6: FM-01 PassType validation replaced the `> (int)PassType.Chip`    |
// |         |            |        |     upper-bound check with an explicit-switch IsValidPassType helper       |
// |         |            |        |     (reflection-free; FR-CS-027–034 compliant). Future enum appends now    |
// |         |            |        |     fail-closed until a case is added, prompting the maintainer.           |
// |         |            |        | AR-2 L-7: explanatory comment added documenting ResolveAimPoint vs cache-  |
// |         |            |        |     block ordering invariant (no _cached* read; safe today).               |
// |         |            |        | AR-2 X-1: Stage-0 receiver-position staleness across WINDUP frames noted   |
// |         |            |        |     at the resolution site (KD-4 / §7.1 upgrade point).                    |
// |         |            |        |     PassCancelledEvent.CancelReason field renamed to .Reason (L-8 follow). |
// | 1.7     | 2026-06-06 | —      | AR-3 L-6: helper renamed EmitCancelAtContact → EmitPassCancelled (the   |
// |         |            |        |     prior name was misleading after WINDUP tackle-interrupt migrated to    |
// |         |            |        |     the same helper — not at CONTACT).                                    |
// | 1.8     | 2026-06-06 | —      | AR-4 L-2: EmitPassCancelled gains PRECONDITION comment naming the         |
// |         |            |        |     _request dependency. Synchronous FM-01 / FM-07 / FM-11 rejections in   |
// |         |            |        |     Execute() return Invalid via MakeInvalidResult BEFORE _request is     |
// |         |            |        |     assigned and MUST NOT route through this helper.                      |
// | 1.9     | 2026-06-06 | —      | AR-5 L-1: PRECONDITION comment expanded — FM-01 (unknown PassType) /     |
// |         |            |        |     FM-07 (distance ≤ 0) / FM-11 (player-targeted with TargetAgentId=-1)   |
// |         |            |        |     spelled out so the reader does not need to grep for them.            |
// | 1.10    | 2026-06-08 | —      | AR-7 M-1: all 8 Debug.LogError / Debug.LogWarning emits with $"…"        |
// |         |            |        |     interpolation gated behind #if UNITY_EDITOR || DEVELOPMENT_BUILD     |
// |         |            |        |     (FR-CS-031 hot-path carve-out). Brings the file in line with the     |
// |         |            |        |     precedent set by PassMechanicsConstants v1.2 AR-2 L-13. Emits        |
// |         |            |        |     cover: Idle-guard, FM-01 possession check, FM-01 PassType validity,  |
// |         |            |        |     FM-07 distance validity, CrossSubType ignore warning, FM-11 player-  |
// |         |            |        |     targeted no-receiver, FM-04 NaN velocity, FM-08 lost possession.    |
// |         |            |        | AR-7 L-1: ExecuteContact Step 3 callsite comment corrected               |
// |         |            |        |     [-1, +1] → [-1, +1) to match the PassErrorCalculator AR-6 L-1       |
// |         |            |        |     producer-side correction (24-bit mantissa never yields +1.0f).      |
// | 1.11    | 2026-06-08 | —      | AR-8 fix pass (0M + 3L).                                                |
// |         |            |        | L-1: CrossSubType-ignore warning gate hoisted from if-body to wrap      |
// |         |            |        |     the entire if-statement — the diagnostic has no functional follow-  |
// |         |            |        |     up, so production builds no longer carry an empty `if (cond) { }`   |
// |         |            |        |     stylistic artifact. (The other 7 AR-7-gated emits MUST keep the    |
// |         |            |        |     body-gate form because their if-bodies contain _lastResult / return.)|
// |         |            |        | L-2: ExecuteContact state transition (_state = FollowThrough) hoisted   |
// |         |            |        |     above Step 8 EventBusStub.Publish. If Publish throws (queue          |
// |         |            |        |     overflow / registry mismatch), the ball was already kicked at Step  |
// |         |            |        |     6 and the executor must not stay in Contact — re-entry next frame  |
// |         |            |        |     would re-run ApplyKick. The FM-08 possession recheck currently      |
// |         |            |        |     guards against the re-entry double-kick, but defensive ordering     |
// |         |            |        |     removes the dependence on that recovery seam.                       |
// |         |            |        | L-3: forward-reference note inserted above the AR-2 M-2 v1.6 history    |
// |         |            |        |     row — the "[-1, +1]" characterisation there is the AR-2-era       |
// |         |            |        |     contract, superseded by AR-6 L-1 to [-1, +1). The AR-2 row remains  |
// |         |            |        |     verbatim to preserve historical record.                            |
// | 1.12    | 2026-06-11 | —      | AR-9 fix pass (1H+3M+5L across the spec; this file: 3M+1L).             |
// |         |            |        | M-1: Idle-guard rejection no longer writes _lastResult — an Execute()   |
// |         |            |        |     during FollowThrough/Complete previously destroyed the committed    |
// |         |            |        |     Completed record (ContactFrame replay-sync data) and surfaced       |
// |         |            |        |     Invalid at the next IsIdle. Rejection now reported via return only. |
// |         |            |        | M-2: FM-07 gate rewritten `d <= 0f` → `!(d > 0f) || IsInfinity(d)` so   |
// |         |            |        |     NaN/±Inf distance is rejected instead of slipping the gate (NaN     |
// |         |            |        |     compares false) and being silently sanitised to a 0.001 m pass by   |
// |         |            |        |     Mathf.Max ordering in ComputeKickSpeed (project NaN-gate pattern).  |
// |         |            |        | M-3: stale tackle flag drained (discarded) at INITIATING just before   |
// |         |            |        |     the WINDUP transition — the flag is cleared only by polling and     |
// |         |            |        |     polling happens only in WINDUP, so a tackle registered during       |
// |         |            |        |     FollowThrough/Idle (even while not in possession) would otherwise   |
// |         |            |        |     cancel the NEXT pass on its first WINDUP frame (§3.8.5 freshness).  |
// |         |            |        | L-1: CONTACT pressure re-sample now queries the passer position fresh  |
// |         |            |        |     via _agentQuery instead of the INITIATING-cached _passerPosition    |
// |         |            |        |     (field removed) — §3.8.6 contact-time pressure intent; position    |
// |         |            |        |     was up to ~15 frames stale for a pass on the run.                   |
// |         |            |        | AR-10 L-1 (same commit): FM-07 log wording "≤ 0" → "not a positive     |
// |         |            |        |     finite value" to match the widened M-2 gate.                        |
// | 1.13    | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling ->              |
// |         |            |        | Unity.Profiling. ProfilerMarker's actual namespace is Unity.Profiling;  |
// |         |            |        | the old using was CS0246 under Unity and the Linux compile gate alike,  |
// |         |            |        | so this assembly could not have compiled in-engine. No functional       |
// |         |            |        | change.                                                                 |
// | 1.14    | 2026-06-19 | —      | Match Engine Phase C step C0: CaptureState()/RestoreState(in            |
// |         |            |        | PassExecutorState) snapshot seam added (parallel to OscillationGuard    |
// |         |            |        | GetState/RestoreState B0 seam) so the match-engine snapshot can         |
// |         |            |        | serialize the executor's cross-tick state-machine + in-flight fields    |
// |         |            |        | for deterministic replay (design note §2.6). The internal              |
// |         |            |        | PhysicalProfile is recomputed on restore, not serialized. No change to  |
// |         |            |        | the Execute/Update execution paths.                                     |
// | 1.14.1  | 2026-06-19 | —      | C0 AR-1 (L-1/L-2): PassExecutionState gains an ORDINAL STABILITY note   |
// |         |            |        | (its ordinals are captured into PassExecutorState.State and become      |
// |         |            |        | digest-load-bearing at C5 — APPEND-only); RestoreState documents the    |
// |         |            |        | benign restored-Idle profile recompute. Doc-only.                       |
// | 1.15    | 2026-08-08 | —      | ERR-012-011: + InFlightTargetAgentId, an observation read of the        |
// |         |            |        | PassRequest.TargetAgentId this executor already holds, so the engine    |
// |         |            |        | can latch a pass's intended receiver at the CONTACT kick. Same field    |
// |         |            |        | CaptureState().Request serializes — one source, two readers, not a      |
// |         |            |        | second derivation. No execution-path change.                            |
// | 1.16    | 2026-08-12 | —      | Wiring backlog W2: FM-08's CONTACT-time possession-loss log goes      |
// |         |            |        | LogError -> LogWarning and drops "Race condition". That wording was  |
// |         |            |        | correct while an ordering accident was the ONLY way to lose the ball |
// |         |            |        | mid-windup; #14 §3.6.5 made it an ordinary football event, and an    |
// |         |            |        | error line per successful tackle buries real errors and fails any    |
// |         |            |        | suite treating an unexpected LogError as a failure. NOT "text only" |
// |         |            |        | (AR-1 L-7): the SEVERITY change alters LogAssert behaviour in every  |
// |         |            |        | suite, which is the whole reason for making it. No formula changed.  |
#endregion
