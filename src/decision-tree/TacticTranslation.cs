// File:     src/decision-tree/TacticTranslation.cs
// Created:  2026-06-28
// Modified: 2026-06-30 (#21 §5.6 / G2 balance pass — doc reframed illustrative → pinned)
// Author:   —
// Spec:     Tactical Instructions #21 §3.1, §3.2, §3.3, FR-TI-004 / FR-TI-005 / FR-TI-014 / FR-TI-025; Decision Tree #8 §2.2.6
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
    public static class TacticTranslation
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
        public static PressingMode ToPressingMode(TacticPressing pressing)
            => s_pressingByRank[ClampRank((int)pressing, s_pressingByRank.Length)];

        /// <summary>
        /// §3.1: TacticPassing → #8 PassingStyle (Short→SHORT, Mixed→MIXED, Direct→DIRECT).
        /// F5: a widened value (ordinal &gt; Direct) clamps to the most-direct peer, DIRECT.
        /// </summary>
        public static PassingStyle ToPassingStyle(TacticPassing passing)
            => s_passingByRank[ClampRank((int)passing, s_passingByRank.Length)];

        /// <summary>
        /// §3.2: per-Mentality utility multiplier (×#8 utility, before clamp). Balanced ⇒ 1.0
        /// (identity, FR-TI-031). A widened Mentality clamps to the table bounds.
        /// </summary>
        public static float MentalityRiskMultiplier(Mentality mentality)
            => TacticalInstructionsConstants.MentalityRiskMult[ClampMentality(mentality)];

        /// <summary>
        /// §3.2: per-Mentality additive defensive-line bias (+TeamTactic.DefensiveLine, then
        /// Clamp01 by the caller). Balanced ⇒ 0.0 (identity, FR-TI-031). #12 remains the depth
        /// authority; this is the §3.4 single-source bias the Phase-D layer adds to the input dial.
        /// </summary>
        public static float MentalityLineBias(Mentality mentality)
            => TacticalInstructionsConstants.MentalityLineBias[ClampMentality(mentality)];

        /// <summary>
        /// §3.3 per-agent utility product for one option's <see cref="ActionType"/>:
        /// <c>RoleWeightModifiers × dutyBias × instrBias × tempoActionBias</c>. The identity per-agent
        /// tactic (<see cref="PlayerRole.Default"/> / <see cref="Duty.Support"/> / every
        /// <see cref="InstrBias.Default"/>) at <see cref="Tempo.Standard"/> resolves to exactly ×1.0
        /// (FR-TI-031), so a default-tactic match is behaviour-neutral. <see cref="Tempo"/> is the
        /// team-level forward-vs-retain dial (carried alongside the per-agent tactic). The magnitudes are
        /// the §5.6 / G2 balance-pass-pinned defaults (June 30, 2026); the directional shapes remain the
        /// normative contract (<c>BalancePassInvariantsTests</c> locks both). Pure; one static table read
        /// per factor (translate-once, FR-TI-025) — invoked per scored option, never allocates.
        /// </summary>
        public static float PlayerTacticActionMultiplier(in PlayerTactic tactic, Tempo tempo, ActionType action)
        {
            int a = (int)action;

            // Role weight (§3.3 / A.4): per-(role, action) table; the Default row is all 1.0.
            float[][] roleRows = TacticalInstructionsConstants.RoleWeightModifiers;
            float role = roleRows[ClampRank((int)tactic.Role, roleRows.Length)][a];

            // Team tempo forward-vs-retain weighting (§3.3); the Standard row is all 1.0.
            float[][] tempoRows = TacticalInstructionsConstants.TempoActionBias;
            float tempoFactor = tempoRows[ClampRank((int)tempo, tempoRows.Length)][a];

            // Individual instruction bias (§3.4): each bias modulates exactly its named action; Default = 1.0.
            float instr = TacticalInstructionsConstants.InstrBiasMult[
                (int)InstructionBiasForAction(tactic.Instructions, action)];

            // Duty aggression (§3.4): the additive aggression bias folded into a multiplier on the on-ball
            // attacking actions (PASS/SHOOT/DRIBBLE). Support (identity) ⇒ +0.0 ⇒ ×1.0; the action set and
            // the additive→multiplicative fold are the §5.6 / G2-pinned shapes (June 30, 2026).
            float duty = 1.0f;
            if (action == ActionType.PASS || action == ActionType.SHOOT || action == ActionType.DRIBBLE)
            {
                duty = 1.0f + TacticalInstructionsConstants.DutyAggressionBias[(int)tactic.Duty];
            }

            return role * tempoFactor * instr * duty;
        }

        /// <summary>
        /// §3.4: the <see cref="PlayerInstructions"/> bias that modulates a given <see cref="ActionType"/>
        /// (each individual instruction maps to exactly one #8 action term). HOLD and INTERCEPT have no
        /// individual hook, so they follow the team default (<see cref="InstrBias.Default"/>).
        /// </summary>
        private static InstrBias InstructionBiasForAction(PlayerInstructions instr, ActionType action)
        {
            switch (action)
            {
                case ActionType.PASS:             return instr.RiskyPasses;
                case ActionType.SHOOT:            return instr.ShootTendency;
                case ActionType.DRIBBLE:          return instr.DribbleTendency;
                case ActionType.MOVE_TO_POSITION: return instr.PositioningFreedom;
                case ActionType.PRESS:            return instr.CloseDown;
                default:                          return InstrBias.Default; // HOLD / INTERCEPT
            }
        }

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
// | 1.1     | 2026-06-28 | —      | Runtime activation: class + maps promoted internal→public — the   |
// |         |            |        |   match-engine Phase-D writer is the intended caller (#21 §3.1).   |
// | 1.2     | 2026-06-29 | —      | #21 §3.3: PlayerTacticActionMultiplier — per-agent role/duty/instr |
// |         |            |        |   × team tempo utility product (identity ⇒ ×1.0, FR-TI-031);       |
// |         |            |        |   consumed per option in UtilityScorer. Magnitudes illustrative    |
// |         |            |        |   (G2). InstructionBiasForAction helper added.                     |
// | 1.3     | 2026-06-30 | —      | §5.6 / G2 balance pass: doc reframed illustrative → pinned         |
// |         |            |        |   (magnitudes unchanged; identity ⇒ ×1.0 invariant preserved).     |
#endregion
