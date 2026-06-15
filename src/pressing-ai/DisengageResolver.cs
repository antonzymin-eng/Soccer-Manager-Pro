// File:     src/pressing-ai/DisengageResolver.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.8, Code Standards #20
// Purpose:  Pure static class: evaluates disengage conditions (timeout and zone exit)
//           and returns whether the press should collapse to all-HOLD_SHAPE.

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Evaluates the two disengage conditions from §3.8:
    /// (a) Timeout — no committed trigger for DisengageTimeoutTicks consecutive ticks.
    /// (b) Zone exit — ball X outside [PressZoneXMin, PressZoneXMax].
    /// When either condition fires, all agents revert to HoldShape and the
    /// reset-latency cooldown is applied. Pressing AI #13 §3.8.
    /// </summary>
    public static class DisengageResolver
    {
        /// <summary>
        /// Evaluates disengage conditions and returns true if the press should collapse.
        /// Mutates <paramref name="disengageDwell"/> and <paramref name="cooldownTicks"/> in place.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="committed">Committed trigger flags this tick.</param>
        /// <param name="disengageDwell">
        /// Running count of ticks with no committed trigger. Reset to 0 when trigger is active.
        /// </param>
        /// <param name="cooldownTicks">
        /// Remaining ticks in reset-latency cooldown. 0 = press eligible again.
        /// Set to ResetLatencyTicks on disengage; decremented each tick otherwise.
        /// </param>
        /// <returns>True when the press collapses to all-HOLD_SHAPE this tick.</returns>
        public static bool Evaluate(
            PressingSnapshot snapshot,
            TriggerFlags committed,
            ref int disengageDwell,
            ref int cooldownTicks)
        {
            // (b) Zone check — immediate collapse regardless of trigger state.
            // §3.8 BallOutsidePressZone is attack-direction-relative: PressZoneXMin/Max are
            // measured up the pitch from the pressing team's OWN goal line (0 m = own goal),
            // so they must be applied in attack-relative coordinates, not absolute pitch X.
            // Without this, a team attacking −X presses in the wrong half (AR-2 H-1).
            float ballX = PitchOrientation.AttackRelativeX(
                snapshot.BallPosition.x, snapshot.AttackingDirection.x);
            if (ballX < PressingAIConstants.PressZoneXMin || ballX > PressingAIConstants.PressZoneXMax)
            {
                disengageDwell = 0;
                cooldownTicks  = PressingAIConstants.ResetLatencyTicks;
                return true;
            }

            // (a) Timeout check.
            if (committed == TriggerFlags.None)
            {
                disengageDwell++;
                if (disengageDwell >= PressingAIConstants.DisengageTimeoutTicks)
                {
                    disengageDwell = 0;
                    cooldownTicks  = PressingAIConstants.ResetLatencyTicks;
                    return true;
                }
            }
            else
            {
                // Trigger active — reset dwell.
                disengageDwell = 0;
            }

            // Decrement cooldown.
            if (cooldownTicks > 0)
                cooldownTicks--;

            return false;
        }

        /// <summary>Returns true when the press is currently in the reset-latency cooldown period.</summary>
        /// <param name="cooldownTicks">Remaining reset-latency ticks.</param>
        public static bool IsInCooldown(int cooldownTicks) => cooldownTicks > 0;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-15 | —      | AR-2 H-1: press-zone check is now attack-direction-relative (PitchOrientation.AttackRelativeX) so a team attacking −X presses in the correct half. |
#endregion
