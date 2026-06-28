// File:     src/decision-tree/TacticTranslation.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Tactical Instructions #21 §3.1, §3.2, FR-TI-004 / FR-TI-025; Decision Tree #8 §2.2.6
// Purpose:  Consumer-side (T2) enum-translation seam: maps #21 Tactic* inputs onto the
//           #8-local PressingMode / PassingStyle enums and resolves the Mentality
//           risk/line outputs. Pure functions, translate-once (FR-TI-025); invoked by
//           the match-engine Phase-D writer on a tactic change, never on the hot path.

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// #21 → #8 translation maps (§3.1 / §3.2). Lives in the consuming assembly per KD-2 —
    /// the #21 data layer never references #8. Mapping is by aggression/forward rank, NOT raw
    /// ordinal: the #21 enums order ascending (Low/Short = 0) while the #8 enums order
    /// descending (HIGH/DIRECT = 0), so a raw cast would invert the instruction. The F5 clamp
    /// (§3.1) maps a Stage-1 widening (an appended bolder value with no #8 peer) to the nearest
    /// existing peer.
    /// </summary>
    internal static class TacticTranslation
    {
        // Rank order = ascending intensity, matching the #21 enum ordinals (Low=0…High=2).
        // One-time static allocation; not a per-tick surface (translate-once, FR-TI-025).
        private static readonly PressingMode[] s_pressingByRank =
            { PressingMode.LOW, PressingMode.MEDIUM, PressingMode.HIGH };

        private static readonly PassingStyle[] s_passingByRank =
            { PassingStyle.SHORT, PassingStyle.MIXED, PassingStyle.DIRECT };

        /// <summary>
        /// §3.1: TacticPressing → #8 PressingMode (Low→LOW, Medium→MEDIUM, High→HIGH).
        /// F5: a widened value (ordinal &gt; High) clamps to the boldest peer, HIGH.
        /// </summary>
        internal static PressingMode ToPressingMode(TacticPressing pressing)
            => s_pressingByRank[ClampRank((int)pressing, s_pressingByRank.Length)];

        /// <summary>
        /// §3.1: TacticPassing → #8 PassingStyle (Short→SHORT, Mixed→MIXED, Direct→DIRECT).
        /// F5: a widened value (ordinal &gt; Direct) clamps to the most-direct peer, DIRECT.
        /// </summary>
        internal static PassingStyle ToPassingStyle(TacticPassing passing)
            => s_passingByRank[ClampRank((int)passing, s_passingByRank.Length)];

        /// <summary>
        /// §3.2: per-Mentality utility multiplier (×#8 utility, before clamp). Balanced ⇒ 1.0
        /// (identity, FR-TI-031). A widened Mentality clamps to the table bounds.
        /// </summary>
        internal static float MentalityRiskMultiplier(Mentality mentality)
            => TacticalInstructionsConstants.MentalityRiskMult[ClampMentality(mentality)];

        /// <summary>
        /// §3.2: per-Mentality additive defensive-line bias (+TeamTactic.DefensiveLine, then
        /// Clamp01 by the caller). Balanced ⇒ 0.0 (identity, FR-TI-031). #12 remains the depth
        /// authority; this is the §3.4 single-source bias the Phase-D layer adds to the input dial.
        /// </summary>
        internal static float MentalityLineBias(Mentality mentality)
            => TacticalInstructionsConstants.MentalityLineBias[ClampMentality(mentality)];

        private static int ClampRank(int rank, int count)
        {
            if (rank < 0) return 0;
            return rank >= count ? count - 1 : rank;
        }

        private static int ClampMentality(Mentality mentality)
            => ClampRank((int)mentality, TacticalInstructionsConstants.MENTALITY_LEVELS);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-06-28 | —      | Initial T2 consumer seam: TacticPressing/TacticPassing → #8 enums |
// |         |            |        |   (rank-mapped, F5 clamp) + Mentality risk/line resolvers.        |
#endregion
