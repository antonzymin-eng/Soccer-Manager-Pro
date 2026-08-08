// File:     src/pressing-ai/PressingTickState.cs
// Created:  2026-06-27
// Modified: 2026-06-27
// Author:   —
// Spec:     Pressing AI #13 §3.2, §3.6; Match Engine design note §2.6 (Phase D D4 follow-up); Code Standards #20
// Purpose:  Read-only view bundling a PressingAITick's cross-tick state (role hysteresis, trigger
//           debounce counters, disengage/cooldown dwell, accumulated press fatigue) so a host
//           snapshot layer can serialize it canonically for deterministic save/restore.

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Immutable view over a <see cref="PressingAITick"/>'s cross-tick state — the role
    /// <see cref="RoleHysteresisState"/>, the <see cref="PressTrigger"/> debounce counters, the
    /// disengage/cooldown dwell counters, and the per-agent accumulated press-fatigue array. Produced by
    /// <see cref="PressingAITick.CaptureState"/> for the Match Engine snapshot layer (design note §2.6),
    /// the pressing analogue of the Positioning <see cref="PositioningAI.HysteresisState"/> seam and the
    /// DecisionTree D0 / executor C0 / OscillationGuard B0 seams.
    ///
    /// <see cref="Roles"/> and <see cref="PressFatigue"/> are references to the live, allocated-once-per-
    /// match containers (not copies); the host reads them for serialization and MUST NOT mutate them
    /// outside a sanctioned restore path. <see cref="Roles"/>.<see cref="RoleHysteresisState.Capacity"/>
    /// equals <see cref="PressFatigue"/>.Length (both sized to the EntityId space).
    /// </summary>
    public readonly struct PressingTickState
    {
        /// <summary>Per-agent role hysteresis (last/pending role + dwell), keyed by EntityId.</summary>
        public readonly RoleHysteresisState Roles;

        /// <summary>The four-trigger debounce dwell/release counters.</summary>
        public readonly PressTrigger Trigger;

        /// <summary>Consecutive ticks the press has been disengaging (§3.2 disengage dwell).</summary>
        public readonly int DisengageDwell;

        /// <summary>Remaining post-disengage cooldown ticks (§3.2).</summary>
        public readonly int CooldownTicks;

        /// <summary>Per-agent accumulated press fatigue [0,1], keyed by EntityId. Length = Roles.Capacity.</summary>
        public readonly float[] PressFatigue;

        /// <summary>Bundles the live cross-tick state references/values into an immutable view.</summary>
        public PressingTickState(
            RoleHysteresisState roles,
            in PressTrigger trigger,
            int disengageDwell,
            int cooldownTicks,
            float[] pressFatigue)
        {
            Roles          = roles;
            Trigger        = trigger;
            DisengageDwell = disengageDwell;
            CooldownTicks  = cooldownTicks;
            PressFatigue   = pressFatigue;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-27 | —      | Initial implementation — Match Engine Phase D D4 follow-up      |
// |         |            |        | pressing-hysteresis snapshot view (parallel to Positioning     |
// |         |            |        | HysteresisState / DecisionTree D0 / executor C0 seams).        |
#endregion
