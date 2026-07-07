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

        public const float RestDefenseRiskMult = 0.85f; // [GT] PASS/SHOOT/DRIBBLE dampener when Positioning AI #12 rest-defense coverage is insufficient

        // ── Half-Spaces (cheap-item addition, new §3.2/§7.8) ────────────────

        /// <summary>
        /// [GT] Per-<c>LaneId</c> PASS utility multiplier (LW=0, LH=1, C=2, RH=3, RW=4). Half-space
        /// lanes (LH/RH) carry a bonus reflecting their combination-play value; wide (LW/RW) and
        /// central (C) lanes are neutral (×1.0 — also the zero-value <c>LaneId</c> default, LW, so an
        /// un-set lane is numerically harmless even though <c>TacticalContext.Stage0Default</c> seeds
        /// the semantically-correct <c>C</c>).
        /// </summary>
        public static readonly float[] LaneMult = { 1.00f, 1.10f, 1.00f, 1.10f, 1.00f };
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
#endregion
