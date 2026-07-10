// File:     src/positioning-ai/MarkingDwellState.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Dismarking AI #23 §2.2.1, Code Standards #20
// Purpose:  Per-agent persistent marking-dwell state consumed by MarkingPressureEvaluator.
//           Zero-init is the valid "unmarked" state, so no ctor seeding is required (KD-4).

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Per-agent marking-dwell state (#23 §2.2.1), persistent across 10 Hz heartbeats. Zero-init
    /// (<c>DwellTicks = 0</c>, <c>LastMarkerId</c> defaulting to 0 rather than −1) is corrected by
    /// <see cref="Unmarked"/>; use that factory for fresh agents — a default-valued instance carries
    /// marker id 0, which is a real agent id. Serialized per FR-DM-014 at wiring (field order
    /// pinned in #23 Appendix B); restore validates coherence per F2 (DwellTicks ∈ [0, cap];
    /// LastMarkerId ∈ {−1} ∪ [0, roster); DwellTicks &gt; 0 ⇒ LastMarkerId ≠ −1).
    /// </summary>
    public struct MarkingDwellState
    {
        /// <summary>Sentinel for "no current marker" (#23 §2.2.1).</summary>
        public const int NoMarker = -1;

        /// <summary>
        /// Heartbeats a perceived opponent has stayed within
        /// <see cref="PositioningAIConstants.MARKING_RADIUS_M"/>; capped at
        /// <see cref="PositioningAIConstants.MARKING_DWELL_FULL_TICKS"/>.
        /// </summary>
        public int DwellTicks;

        /// <summary>
        /// AgentId of the nearest qualifying perceived opponent last tick; <see cref="NoMarker"/> = none.
        /// Marker identity switches do NOT reset dwell (#23 §3.2) — this simply tracks the current nearest.
        /// </summary>
        public int LastMarkerId;

        /// <summary>The valid "unmarked" state: zero dwell, <see cref="NoMarker"/> marker id.</summary>
        public static MarkingDwellState Unmarked => new MarkingDwellState { DwellTicks = 0, LastMarkerId = NoMarker };
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#23 T0 scaffolding). |
#endregion
