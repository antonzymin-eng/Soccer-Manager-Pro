// File:     src/agent-movement/OscillationGuardState.cs
// Created:  2026-06-16
// Author:   —
// Spec:     Agent Movement #2 §3.1.7; Match Engine design note §2.6 (Phase B step B0); Code Standards #20
// Purpose:  Plain-data snapshot of an OscillationGuard's internal ring-buffer state, exposing the
//           private fields so the match-engine snapshot layer can serialize it canonically for
//           deterministic replay (parallel to DeterministicSim RngStreamState). Pure data: no
//           behaviour, no engine dependency.

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Immutable snapshot of <see cref="OscillationGuard"/>'s cross-tick state — the eight ring-buffer
    /// transition timestamps, the write cursor, and the lock state. Produced by
    /// <see cref="OscillationGuard.GetState"/> and consumed by <see cref="OscillationGuard.RestoreState"/>.
    ///
    /// This is the serialization seam required by Match Engine design note §2.6: the guard lives
    /// inside <see cref="AgentState"/>, so the snapshot payload cannot be assembled canonically
    /// (field-by-field via <c>CanonicalSerializer</c>) without read access to these otherwise-private
    /// fields. Parallel to <c>DeterministicSim.RngStreamState</c> ↔ <c>GetStreamState</c>/<c>RestoreStream</c>.
    ///
    /// <see cref="TimestampSlots"/> MUST equal <c>OscillationGuardConstants.BufferSize</c>; the guard's
    /// <see cref="OscillationGuard.GetState"/>/<see cref="OscillationGuard.RestoreState"/> assert this so a
    /// future BufferSize bump fails fast (the same protection <see cref="OscillationGuard"/>'s switch arms carry).
    /// </summary>
    public readonly struct OscillationGuardState
    {
        /// <summary>Number of timestamp slots carried by this snapshot. Must match OscillationGuardConstants.BufferSize.</summary>
        public const int TimestampSlots = 8;

        /// <summary>Ring-buffer transition timestamps (seconds, MatchClock). float.NegativeInfinity = empty slot sentinel.</summary>
        public readonly float T0, T1, T2, T3, T4, T5, T6, T7;

        /// <summary>Next write position in the ring buffer, range [0, BufferSize).</summary>
        public readonly int WriteIndex;

        /// <summary>True while the guard is in its post-oscillation lock-out window.</summary>
        public readonly bool IsLocked;

        /// <summary>MatchClock time (seconds) at which the lock-out expires. float.NegativeInfinity when unlocked.</summary>
        public readonly float LockUntilTime;

        public OscillationGuardState(
            float t0, float t1, float t2, float t3, float t4, float t5, float t6, float t7,
            int writeIndex, bool isLocked, float lockUntilTime)
        {
            T0 = t0; T1 = t1; T2 = t2; T3 = t3; T4 = t4; T5 = t5; T6 = t6; T7 = t7;
            WriteIndex    = writeIndex;
            IsLocked      = isLocked;
            LockUntilTime = lockUntilTime;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-16 | —      | Initial implementation — Phase B step B0 serialization seam for OscillationGuard. |
#endregion
