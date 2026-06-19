// File:     src/pass-mechanics/PassExecutorState.cs
// Created:  2026-06-19
// Author:   —
// Spec:     Pass Mechanics #5 §3.8; Match Engine design note §2.6 (Phase C step C0); Code Standards #20
// Purpose:  Plain-data snapshot of a PassExecutor's cross-tick state-machine + in-flight fields,
//           exposing them so the match-engine snapshot layer can serialize executor state
//           canonically for deterministic replay (parallel to DeterministicSim RngStreamState and
//           AgentMovement OscillationGuardState). Pure data: no behaviour, no engine dependency.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Immutable snapshot of <see cref="PassExecutor"/>'s cross-tick execution state — the state-machine
    /// ordinal plus every value captured at INITIATING that survives across WINDUP/CONTACT frames (the
    /// pass is initiated on one tick and the cached values are read several ticks later at CONTACT, so
    /// they are simulation state, not scratch). Produced by <see cref="PassExecutor.CaptureState"/> and
    /// consumed by <see cref="PassExecutor.RestoreState"/>.
    ///
    /// This is the executor analogue of the Phase B0 <see cref="AgentMovement.OscillationGuardState"/>
    /// seam: the executor holds this state in private fields with no get/restore accessor, so the
    /// Match Engine snapshot payload (design note §2.6) cannot serialize it canonically without it.
    /// Named Capture/Restore (NOT Get) to avoid colliding with <see cref="IPassAgentQuery.GetState"/>.
    ///
    /// EXCLUDED FIELD — the internal <c>PhysicalProfile</c>: it is NOT carried here. The profile is a
    /// pure function of (<see cref="PassRequest.PassType"/>, <see cref="EffectiveSubType"/>) via
    /// <c>PassTypeProfiles.GetProfile</c>, so <see cref="PassExecutor.RestoreState"/> recomputes it
    /// rather than serializing it — the design note §2.6 "fully recomputed before its first read"
    /// exclusion. (It is also an <c>internal</c> type and so cannot be a public field on this DTO.)
    /// </summary>
    public readonly struct PassExecutorState
    {
        /// <summary>State-machine ordinal: 0=Idle, 1=Windup, 2=Contact, 3=FollowThrough, 4=Complete.</summary>
        public readonly int State;

        /// <summary>The PassRequest captured at INITIATING and held for the execution lifecycle.</summary>
        public readonly PassRequest Request;

        /// <summary>Effective cross sub-type (Flat for non-Cross types); the third hash input to error direction.</summary>
        public readonly CrossSubType EffectiveSubType;

        /// <summary>Kick speed (m/s) computed at INITIATING (§3.2).</summary>
        public readonly float KickSpeed;

        /// <summary>Launch angle (degrees) computed at INITIATING (§3.3).</summary>
        public readonly float LaunchAngleDeg;

        /// <summary>Spin vector (rad/s) computed at INITIATING (§3.4).</summary>
        public readonly UnityEngine.Vector3 SpinVector;

        /// <summary>Pre-error kick direction toward the aim point (§3.6).</summary>
        public readonly UnityEngine.Vector3 BaseKickDirection;

        /// <summary>Resolved aim point (world space, pitch-clamped) (§3.6).</summary>
        public readonly UnityEngine.Vector3 AimPoint;

        /// <summary>Through-ball lead distance (metres); 0 for player-targeted / stationary receivers.</summary>
        public readonly float LeadDistance;

        /// <summary>Cached Passing attribute (error-chain input).</summary>
        public readonly float CachedPassing;

        /// <summary>Cached Fatigue scalar [0,1] (error-chain input).</summary>
        public readonly float CachedFatigue;

        /// <summary>Cached body angle (degrees) between facing and base kick direction.</summary>
        public readonly float CachedBodyAngleDeg;

        /// <summary>Cached weak-foot flag.</summary>
        public readonly bool CachedIsWeakFoot;

        /// <summary>Cached weak-foot rating [1–5].</summary>
        public readonly int CachedWeakFootRating;

        /// <summary>Windup frames remaining.</summary>
        public readonly int WindupFramesRemaining;

        /// <summary>Follow-through frames remaining.</summary>
        public readonly int FollowThroughFramesRemaining;

        /// <summary>The most recently committed result (read via <see cref="PassExecutor.LastResult"/> after IsIdle).</summary>
        public readonly PassResult LastResult;

        public PassExecutorState(
            int state,
            in PassRequest request,
            CrossSubType effectiveSubType,
            float kickSpeed,
            float launchAngleDeg,
            UnityEngine.Vector3 spinVector,
            UnityEngine.Vector3 baseKickDirection,
            UnityEngine.Vector3 aimPoint,
            float leadDistance,
            float cachedPassing,
            float cachedFatigue,
            float cachedBodyAngleDeg,
            bool cachedIsWeakFoot,
            int cachedWeakFootRating,
            int windupFramesRemaining,
            int followThroughFramesRemaining,
            in PassResult lastResult)
        {
            State                        = state;
            Request                      = request;
            EffectiveSubType             = effectiveSubType;
            KickSpeed                    = kickSpeed;
            LaunchAngleDeg               = launchAngleDeg;
            SpinVector                   = spinVector;
            BaseKickDirection            = baseKickDirection;
            AimPoint                     = aimPoint;
            LeadDistance                 = leadDistance;
            CachedPassing                = cachedPassing;
            CachedFatigue                = cachedFatigue;
            CachedBodyAngleDeg           = cachedBodyAngleDeg;
            CachedIsWeakFoot             = cachedIsWeakFoot;
            CachedWeakFootRating         = cachedWeakFootRating;
            WindupFramesRemaining        = windupFramesRemaining;
            FollowThroughFramesRemaining = followThroughFramesRemaining;
            LastResult                   = lastResult;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-19 | —      | Initial implementation — Match Engine Phase C step C0 executor |
// |         |            |        | snapshot seam DTO (parallel to OscillationGuardState / Rng-    |
// |         |            |        | StreamState). PhysicalProfile excluded (recomputed on restore).|
#endregion
