// File:     src/decision-tree/TacticalWeights.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.4.7, Code Standards #20
// Purpose:  All tactical context modifier constants (§3.4) and dispatch constants (§3.5).
//           23 constants total: 16 tactical + 7 dispatch/movement. All [GT].

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Tactical and dispatch constants for TacticalModifierResolver and ActionDispatcher.
    /// All 23 §3.4–3.5 constants reside here. Decision Tree #8 §3.4.7.
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
