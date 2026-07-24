// File:     src/player-progression/TrainingInput.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §2.2 / §4.5 (the #29 training seam); Code Standards #20
// Purpose:  The per-player growth contribution the #29 Training System writes (KD-2). A value type, not
//           an interface against the absent #29 producer (FR-PG-009 / FR-LW-031 — no phantom interface).

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// The per-player training contribution the daily step reads (KD-2 / FR-PG-008 — training is an
    /// input, never a parallel mutation of the same attributes). <see cref="Neutral"/> == no training:
    /// it MUST leave the daily step byte-identical to no training input (FR-PG-009). At Stage 0 (T0)
    /// the struct is empty — the Stage-3 #29 fields (focus / intensity / coach quality) APPEND here and
    /// the daily step reads them once the curve lands (T3).
    /// </summary>
    public readonly struct TrainingInput
    {
        /// <summary>The identity contribution — all-zero, byte-identical to no training input (FR-PG-009).</summary>
        public static TrainingInput Neutral => default;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-24 | —      | Initial implementation. |
#endregion
