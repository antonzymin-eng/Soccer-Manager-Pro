// File:     src/shot-mechanics/ShotExecutorState.cs
// Created:  2026-06-19
// Author:   —
// Spec:     Shot Mechanics #6 §3.9; Match Engine design note §2.6 (Phase C step C0); Code Standards #20
// Purpose:  Plain-data snapshot of a ShotExecutor's cross-tick state-machine + in-flight fields,
//           exposing them so the match-engine snapshot layer can serialize executor state
//           canonically for deterministic replay (parallel to DeterministicSim RngStreamState and
//           AgentMovement OscillationGuardState). Pure data: no behaviour, no engine dependency.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Immutable snapshot of <see cref="ShotExecutor"/>'s cross-tick execution state — the state-machine
    /// ordinal plus every value captured/frozen at INITIATING that survives across WINDUP/CONTACT frames
    /// (a shot is initiated on one tick and the cached values are read several ticks later at CONTACT,
    /// so they are simulation state, not scratch). Produced by <see cref="ShotExecutor.CaptureState"/>
    /// and consumed by <see cref="ShotExecutor.RestoreState"/>.
    ///
    /// This is the executor analogue of the Phase B0 <see cref="AgentMovement.OscillationGuardState"/>
    /// seam: the executor holds this state in private fields with no get/restore accessor, so the
    /// Match Engine snapshot payload (design note §2.6) cannot serialize it canonically without it.
    /// Named Capture/Restore (NOT Get) for symmetry with the Pass executor seam (the Shot agent-query
    /// surface likewise owns a <c>GetState</c>).
    ///
    /// Unlike Pass, every in-flight field here is a public type (no internal profile), so the full
    /// set is carried directly with no recompute-on-restore exclusion.
    /// </summary>
    public readonly struct ShotExecutorState
    {
        /// <summary>State-machine ordinal: 0=Idle, 1=Windup, 2=Contact, 3=FollowThrough, 4=Complete.
        /// ORDINAL STABILITY: this value mirrors the private <c>ShotExecutor.ShotExecutionState</c> enum
        /// and is digest-load-bearing once the C5 snapshot serializes it — the enum is APPEND-only
        /// (reordering it silently breaks replay compatibility for persisted snapshots).</summary>
        public readonly int State;

        /// <summary>The ShotRequest captured at INITIATING and held for the execution lifecycle.</summary>
        public readonly ShotRequest Request;

        /// <summary>Kick speed (m/s) after the weak-foot velocity multiplier (§3.2, §3.8).</summary>
        public readonly float KickSpeed;

        /// <summary>Launch angle (degrees) computed at INITIATING (§3.3).</summary>
        public readonly float LaunchAngleDeg;

        /// <summary>Spin vector (rad/s) computed at INITIATING (§3.4).</summary>
        public readonly UnityEngine.Vector3 SpinVector;

        /// <summary>Pre-error aim direction (§3.5).</summary>
        public readonly UnityEngine.Vector3 IntendedAimDirection;

        /// <summary>Body-mechanics result frozen at INITIATING (§3.7).</summary>
        public readonly BodyMechanicsResult BodyMechanics;

        /// <summary>Weak-foot error multiplier applied at CONTACT (§3.8).</summary>
        public readonly float WeakFootErrorMultiplier;

        /// <summary>Total windup duration (frames); copied to animation data at CONTACT.</summary>
        public readonly int WindupFrames;

        /// <summary>Agent position cached at INITIATING (pressure-query origin + aim basis).</summary>
        public readonly UnityEngine.Vector3 CachedAgentPosition;

        /// <summary>Cached Finishing attribute (error-chain input).</summary>
        public readonly float CachedFinishing;

        /// <summary>Cached LongShots attribute (error-chain input).</summary>
        public readonly float CachedLongShots;

        /// <summary>Cached Composure attribute (error-chain input).</summary>
        public readonly float CachedComposure;

        /// <summary>Cached Fatigue scalar [0,1] (error-chain input).</summary>
        public readonly float CachedFatigue;

        /// <summary>Windup frames remaining.</summary>
        public readonly int WindupFramesRemaining;

        /// <summary>Follow-through frames remaining.</summary>
        public readonly int FollowThroughFramesRemaining;

        /// <summary>The most recently committed result (read via <see cref="ShotExecutor.LastResult"/> after IsIdle).</summary>
        public readonly ShotResult LastResult;

        /// <summary>Constructs an immutable executor-state snapshot from the captured field set.</summary>
        public ShotExecutorState(
            int state,
            in ShotRequest request,
            float kickSpeed,
            float launchAngleDeg,
            UnityEngine.Vector3 spinVector,
            UnityEngine.Vector3 intendedAimDirection,
            in BodyMechanicsResult bodyMechanics,
            float weakFootErrorMultiplier,
            int windupFrames,
            UnityEngine.Vector3 cachedAgentPosition,
            float cachedFinishing,
            float cachedLongShots,
            float cachedComposure,
            float cachedFatigue,
            int windupFramesRemaining,
            int followThroughFramesRemaining,
            in ShotResult lastResult)
        {
            State                        = state;
            Request                      = request;
            KickSpeed                    = kickSpeed;
            LaunchAngleDeg               = launchAngleDeg;
            SpinVector                   = spinVector;
            IntendedAimDirection         = intendedAimDirection;
            BodyMechanics                = bodyMechanics;
            WeakFootErrorMultiplier      = weakFootErrorMultiplier;
            WindupFrames                 = windupFrames;
            CachedAgentPosition          = cachedAgentPosition;
            CachedFinishing              = cachedFinishing;
            CachedLongShots              = cachedLongShots;
            CachedComposure              = cachedComposure;
            CachedFatigue                = cachedFatigue;
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
// |         |            |        | StreamState). Full in-flight field set (no internal profile).  |
// | 1.0.1   | 2026-06-19 | —      | C0 AR-1 (L-1/L-3): State field gains an ORDINAL STABILITY note |
// |         |            |        | (digest-load-bearing at C5); public constructor gains a        |
// |         |            |        | <summary> (FR-CS-060). Doc-only.                               |
#endregion
