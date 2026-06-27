// File:     src/defensive-ai/DefensiveTickState.cs
// Created:  2026-06-27
// Author:   —
// Spec:     Defensive AI #14 §3.3, §3.7; Match Engine design note §2.6 (Phase D D4 follow-up); Code Standards #20
// Purpose:  Read-only view bundling a DefensiveAITick's cross-tick state (per-agent mark hysteresis,
//           per-agent last committed assignment, per-team offside-line state) so a host snapshot layer
//           can serialize it canonically for deterministic save/restore.

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Immutable view over a <see cref="DefensiveAITick"/>'s cross-tick state — the per-agent
    /// <see cref="MarkHysteresisState"/> dwell-lock ledger, the per-agent last committed
    /// <see cref="MarkAssignment"/> (read next tick by the hysteresis gate), and the per-team
    /// <see cref="OffsideLineState"/>. Produced by <see cref="DefensiveAITick.CaptureState"/> for the
    /// Match Engine snapshot layer (design note §2.6), the defensive analogue of the Positioning
    /// <see cref="PositioningAI.HysteresisState"/> / Pressing <see cref="PressingAI.PressingTickState"/>
    /// seams.
    ///
    /// <see cref="Hysteresis"/> and <see cref="PrevAssignments"/> are references to the live, allocated-
    /// once-per-match arrays keyed by EntityId (not copies); the host reads them for serialization and
    /// MUST NOT mutate them outside a sanctioned restore path. Both have the same EntityId-space length.
    /// </summary>
    public readonly struct DefensiveTickState
    {
        /// <summary>Per-agent mark hysteresis (dwell counter, candidate mode/target, hold ticks), by EntityId.</summary>
        public readonly MarkHysteresisState[] Hysteresis;

        /// <summary>Per-agent last committed mark assignment (read next tick by the dwell gate), by EntityId.</summary>
        public readonly MarkAssignment[] PrevAssignments;

        /// <summary>Per-team offside-line state (current depth, step-up dwell, cooldown, GK-zone ticks).</summary>
        public readonly OffsideLineState Offside;

        /// <summary>Bundles the live cross-tick state references/values into an immutable view.</summary>
        public DefensiveTickState(
            MarkHysteresisState[] hysteresis,
            MarkAssignment[] prevAssignments,
            in OffsideLineState offside)
        {
            Hysteresis      = hysteresis;
            PrevAssignments = prevAssignments;
            Offside         = offside;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-27 | —      | Initial implementation — Match Engine Phase D D4 follow-up      |
// |         |            |        | defensive-hysteresis snapshot view (parallel to Positioning    |
// |         |            |        | HysteresisState / Pressing PressingTickState seams).          |
#endregion
