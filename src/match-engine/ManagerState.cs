// File:     src/match-engine/ManagerState.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Tactical Presets #26 §2.2.4 (FR-TP-012/013, KD-4), Appendix C; Code Standards #20
// Purpose:  Per-team persistent manager-AI state (mode, profile ref, current preset ordinal,
//           hold countdown, last-decision tick). Serialized in Appendix C field order at
//           SNAPSHOT_SCHEMA_VERSION v13; zero-init = Human mode = inert (KD-4 discipline).

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Per-team persistent manager-AI state (#26 §2.2.4). Zero-init is the inert identity:
    /// <see cref="Mode"/> = <see cref="ManagerMode.Human"/> means no field here is ever read or
    /// written by the decision loop, so a default match is byte-identical to pre-#26 (KD-4).
    /// Serialized per team in Appendix C order (Mode u8, ProfileOrdinal u8, CurrentPresetOrdinal u8,
    /// HoldIntervalsRemaining i32, LastDecisionTick i32) — FR-TP-012, v13.
    /// </summary>
    public struct ManagerState
    {
        /// <summary>Manager mode; <see cref="ManagerMode.Human"/> (zero-value) is inert.</summary>
        public ManagerMode Mode;

        /// <summary>Appendix A.2 archetype ordinal backing this team's <see cref="ManagerProfile"/>.</summary>
        public byte ProfileOrdinal;

        /// <summary>
        /// The team's current preset ordinal on the A.1 ladder (meaningful only in AI mode; the
        /// kickoff boot path seeds it, the adaptation ladder walks it one rung per switch).
        /// </summary>
        public byte CurrentPresetOrdinal;

        /// <summary>
        /// Fired decision points remaining before another switch is evaluable (FR-TP-011). Set to
        /// <c>ManagerSwitchHoldIntervals × PatienceIntervals</c> on a switch; decremented once per
        /// fired decision point while positive.
        /// </summary>
        public int HoldIntervalsRemaining;

        /// <summary>
        /// The 60 Hz tick of the last fired decision (the §3.2 interval anchor). Negative =
        /// "no decision fired yet" — the next stride-gate evaluation fires as the kickoff decision.
        /// The boot kickoff path stamps 0, so the tick-0 in-engine gate does not double-fire.
        /// </summary>
        public int LastDecisionTick;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#26 §2.2.4). |
#endregion
