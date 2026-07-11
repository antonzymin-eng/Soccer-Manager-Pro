// File:     src/positioning-ai/RotationPairState.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Positional Rotations #25 §2.2.2, Code Standards #20
// Purpose:  Per-adjacency-row, per-team persistent rotation state (dwell / rotated / hold).
//           Zero-init is the valid "not rotated" state (KD-8 discipline).

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Per-pair rotation state (#25 §2.2.2), persistent across heartbeats. Zero-init is the valid
    /// "not rotated" state. Serialized per FR-RO-013 at wiring (table-row order, #25 Appendix B);
    /// restore validates coherence per F6 (Rotated byte ∈ {0,1}; HoldTicksRemaining ≤
    /// ROTATION_HOLD_TICKS and 0 while not Rotated; TriggerDwellTicks ≥ 0 — no upper bound, the
    /// per-tick commit cap can legitimately defer a commit past the threshold).
    /// </summary>
    public struct RotationPairState
    {
        /// <summary>Consecutive heartbeats the (commit or revert) predicate has held (#25 §3.2).</summary>
        public int TriggerDwellTicks;

        /// <summary>Whether the pair is currently swapped.</summary>
        public bool Rotated;

        /// <summary>Countdown before revert becomes evaluable; 0 when idle (#25 §3.2).</summary>
        public int HoldTicksRemaining;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#25 T0 scaffolding). |
#endregion
