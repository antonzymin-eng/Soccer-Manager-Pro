// File:     src/decision-tree/TacticalWeights.cs
// Created:  2026-05-29
// Modified: 2026-07-07
// Author:   —
// Spec:     Decision Tree #8 §3.4.7, new §3.2/§7.7, Code Standards #20
// Purpose:  All tactical context modifier constants (§3.4) and dispatch constants (§3.5).
//           22 constants total: 16 tactical + 6 dispatch/movement. All [GT].
//           (AR-2 L: the spec §3.4.7 table claims 23 but lists 22 rows — the v1.1
//           change note counted PRESS_URGENCY_FACTOR in both groups; ERR-008-010.)

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Tactical and dispatch constants for TacticalModifierResolver and ActionDispatcher.
    /// All 22 §3.4–3.5 constants reside here. Decision Tree #8 §3.4.7.
    /// </summary>
    public static class TacticalWeights
    {
        // ── PressingMode Multipliers (§3.4.3) ────────────────────────────────

        public const float PressingHighPressMod     = 1.4f; // [GT] PRESS utility under HIGH pressing
        public const float PressingLowPressMod      = 0.6f; // [GT] PRESS utility under LOW pressing
        public const float PressingHighInterceptMod = 1.2f; // [GT] INTERCEPT utility under HIGH pressing
        public const float PressingLowInterceptMod  = 0.9f; // [GT] INTERCEPT utility under LOW pressing
        public const float PressingHighHoldMod      = 0.7f; // [GT] HOLD suppression under HIGH pressing
        public const float PressingLowHoldMod       = 1.2f; // [GT] HOLD boost under LOW pressing
        public const float PressingHighDribbleMod   = 0.9f; // [GT] mild DRIBBLE suppression under HIGH pressing

        // ── PassingStyle Multipliers (§3.4.4) ────────────────────────────────

        public const float PassingDirectLongMod   = 1.3f; // [GT] long pass boost under DIRECT style
        public const float PassingDirectShortMod  = 0.9f; // [GT] short pass suppression under DIRECT style
        public const float PassingDirectHoldMod   = 0.7f; // [GT] HOLD suppression under DIRECT style
        public const float PassingShortLongMod    = 0.6f; // [GT] long pass suppression under SHORT style
        public const float PassingShortShortMod   = 1.3f; // [GT] short pass boost under SHORT style
        public const float PassingShortHoldMod    = 1.2f; // [GT] HOLD boost under SHORT style

        // ── Range Threshold ───────────────────────────────────────────────────

        public const float PassLongShortThreshold  = 20.0f; // [GT] m; long/short pass classification boundary
        public const float DefensiveLineDepthRange = 30.0f; // [GT] m; formation slot Y adjustment range

        // ── Possession-Phase Pressing Urgency (§3.4.6) ───────────────────────

        public const float PressUrgencyFactor = 1.2f; // [GT] extra PRESS multiplier under opponent possession

        // ── Dispatch Constants (§3.5) ─────────────────────────────────────────

        public const float UrgencyPressureScale    = 1.0f; // [GT] PressureScalar → PassRequest.Urgency
        public const float SpinIntentBelowCentre   = 0.6f; // [GT] default SpinIntent for BelowCentre ContactZone
        public const float SpinIntentOffCentre     = 0.8f; // [GT] default SpinIntent for OffCentre ContactZone
        public const float PlacementCornerOffset   = 0.1f; // [GT] inward nudge from post/bar for PlacementTarget
        public const float MoveSprintThreshold     = 15.0f; // [GT] m; distance above which agent sprints
        public const float MoveJogThreshold        = 6.0f;  // [GT] m; distance above which agent jogs

        // ── Rest Defense (cheap-item addition, new §3.2/§7.7) ───────────────

        public const float RestDefenseRiskMult = 0.85f; // [GT] PASS/SHOOT/DRIBBLE dampener when Positioning AI #12 rest-defense coverage is insufficient AND the ball carrier is aware of it (Lerp'd by carrier Decisions/Anticipation in UtilityScorer)

        // ── Marked-Pass-Target Penalty (Dismarking #23 §3.4 / FM-DM-03, #8 §3.2.2.1) ──

        /// <summary>
        /// [GT] Floor of the marked-pass-target utility multiplier: a fully aware passer sees ×this
        /// on a PASS to a teammate with a perceived opponent at 0 m, ×1.0 on a free teammate
        /// (Lerp'd by targetProximity01 × passer awareness in UtilityScorer). #23 §3.4/§3.5;
        /// magnitude illustrative pending the #23 balance pass (#21 G2 precedent).
        /// </summary>
        public const float TargetMarkedUtilityMult = 0.7f;

        /// <summary>
        /// [CROSS] Marking radius (m) for the §3.4 target-proximity term.
        /// Authoritative source: PositioningAIConstants.MARKING_RADIUS_M (Dismarking #23 §3.1/§3.5 —
        /// shared by design: one definition of "tight" couples §3.1 and §3.4, #23 Appendix D).
        /// Value: 3.0 m. Single-consumer mirror per Spec #20 §4.2.
        /// </summary>
        public static readonly float MarkedPassRadiusM =
            TacticalDirector.PositioningAI.PositioningAIConstants.MARKING_RADIUS_M;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-11 | —      | Audit AR-2 L: constant tally corrected 23 → 22 (16 tactical + 6 dispatch);  |
// |         |            |        |   the spec §3.4.7 22-row table claiming 23 is filed as ERR-008-010.          |
// | 1.2     | 2026-07-07 | —      | Cheap-item addition: + RestDefenseRiskMult (new §3.2/§7.7, rest-defense    |
// |         |            |        |   dampener on PASS/SHOOT/DRIBBLE).                                          |
// | 1.3     | 2026-07-07 | —      | Cheap-item addition: + LaneMult[5] (new §3.2/§7.8 half-spaces, PASS bonus  |
// |         |            |        |   for LH/RH lanes).                                                         |
// | 1.4     | 2026-07-07 | —      | Reverted after user review: LaneMult[5] half-spaces bonus REMOVED (an       |
// |         |            |        |   exploitable spatial gap needs tactical/player instructions, not a flat    |
// |         |            |        |   bonus). RestDefenseRiskMult doc updated for the awareness-gated consumer. |
// | 1.5     | 2026-07-11 | —      | #23 §3.4 wiring: + TargetMarkedUtilityMult [GT] + MarkedPassRadiusM [CROSS] |
// |         |            |        |   mirror of PositioningAIConstants.MARKING_RADIUS_M (FM-DM-03 consumer in   |
// |         |            |        |   UtilityScorer; decision-tree.asmdef gains the PositioningAI reference —   |
// |         |            |        |   a valid AI→Mechanics direction per the layer taxonomy).                   |
#endregion
