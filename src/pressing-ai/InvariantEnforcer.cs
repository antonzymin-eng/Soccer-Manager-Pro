// File:     src/pressing-ai/InvariantEnforcer.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.9, Code Standards #20
// Purpose:  Pure static class: checks the three anti-chaos invariants post-assignment
//           and demotes excess pressers or triggers F5 (all-HOLD_SHAPE) as required.

using UnityEngine;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Enforces the three anti-chaos invariants after role assignment (§3.9):
    /// (1) MaxPressersBallThird — no more than 3 active pressers in the ball-side third.
    /// (2) MinBacklineAgents — at least 3 Defense-line agents in the own defensive third.
    /// (3) MaxPressDisplacementM — cover-shadow target within 25 m of baseline slot.
    /// Any invariant violation that cannot be resolved by demotion triggers F5
    /// (all agents → HoldShape). Pressing AI #13 §3.9.
    /// </summary>
    public static class InvariantEnforcer
    {
        /// <summary>
        /// Enforces all three invariants against <paramref name="directive"/> in place.
        /// Returns false and sets directive to Inactive when F5 is required.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="directive">
        /// Press directive to validate and potentially modify.
        /// CoverShadow fields may be demoted; PrimaryPress is preserved if possible.
        /// </param>
        /// <returns>True when the directive survives; false on F5.</returns>
        public static bool Enforce(PressingSnapshot snapshot, ref PressDirective directive)
        {
            // Invariant (3): cover-shadow displacement check first (cheapest filter).
            if (!EnforceMaxDisplacement(snapshot, ref directive))
            {
                directive = PressDirective.Inactive;
                return false;
            }

            // Invariant (1): maximum active pressers in ball-side pitch third.
            if (!EnforceMaxPressersBallThird(snapshot, ref directive))
            {
                directive = PressDirective.Inactive;
                return false;
            }

            // Invariant (2): minimum backline agents in own defensive third.
            if (!EnforceMinBackline(snapshot, ref directive))
            {
                directive = PressDirective.Inactive;
                return false;
            }

            return true;
        }

        // ── Invariant (1): MaxPressersBallThird ──────────────────────────────

        private static bool EnforceMaxPressersBallThird(
            PressingSnapshot snapshot,
            ref PressDirective directive)
        {
            float ballX      = snapshot.BallPosition.x;
            float thirdWidth = PressingAIConstants.PITCH_THIRD_M;

            // Determine ball-side third X range.
            float thirdMin, thirdMax;
            if (ballX < thirdWidth)
            {
                thirdMin = 0f;
                thirdMax = thirdWidth;
            }
            else if (ballX < thirdWidth * 2f)
            {
                thirdMin = thirdWidth;
                thirdMax = thirdWidth * 2f;
            }
            else
            {
                thirdMin = thirdWidth * 2f;
                thirdMax = PressingAIConstants.PITCH_LENGTH_M;
            }

            // Count active pressers in the ball-side third.
            int presserCount = 0;

            if (directive.PrimaryPresserId >= 0)
            {
                Vector2 pos = GetAgentPosition(snapshot, directive.PrimaryPresserId);
                if (pos.x >= thirdMin && pos.x <= thirdMax)
                    presserCount++;
            }

            if (directive.CoverShadowCount >= 1)
            {
                Vector2 pos = GetAgentPosition(snapshot, directive.Shadow0.DefenderId);
                if (pos.x >= thirdMin && pos.x <= thirdMax)
                    presserCount++;
            }

            if (directive.CoverShadowCount >= 2)
            {
                Vector2 pos = GetAgentPosition(snapshot, directive.Shadow1.DefenderId);
                if (pos.x >= thirdMin && pos.x <= thirdMax)
                    presserCount++;
            }

            if (presserCount <= PressingAIConstants.MaxPressersBallThird)
                return true;

            // Over limit — demote shadow(s) from highest cost downward.
            // Try demoting Shadow1 first, then Shadow0.
            if (directive.CoverShadowCount == 2)
            {
                Vector2 pos1 = GetAgentPosition(snapshot, directive.Shadow1.DefenderId);
                if (pos1.x >= thirdMin && pos1.x <= thirdMax)
                {
                    directive.Shadow1        = default;
                    directive.CoverShadowCount = 1;
                    presserCount--;
                }
            }

            if (presserCount > PressingAIConstants.MaxPressersBallThird && directive.CoverShadowCount >= 1)
            {
                Vector2 pos0 = GetAgentPosition(snapshot, directive.Shadow0.DefenderId);
                if (pos0.x >= thirdMin && pos0.x <= thirdMax)
                {
                    directive.Shadow0          = default;
                    directive.CoverShadowCount = 0;
                    presserCount--;
                }
            }

            // If still over (would require demoting PrimaryPress) → F5.
            if (presserCount > PressingAIConstants.MaxPressersBallThird)
                return false;

            return true;
        }

        // ── Invariant (2): MinBacklineAgents ────────────────────────────────

        private static bool EnforceMinBackline(
            PressingSnapshot snapshot,
            ref PressDirective directive)
        {
            float defensiveThirdMax = PressingAIConstants.PITCH_THIRD_M;
            int   pressingTeamId   = snapshot.PressingTeamId;

            // Count own Defense-line agents in own defensive third (X < defensiveThirdMax).
            // Agents with active press roles still occupy their physical position —
            // we count by position, not by role assignment, per §3.9 invariant (2).
            int backlineCount = 0;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.TeamId != pressingTeamId)
                    continue;
                if (!a.IsActive)
                    continue;
                if (a.IsGoalkeeper)
                    continue;
                if (a.Line != LineId.Defense)
                    continue;
                if (a.Position.x < defensiveThirdMax)
                    backlineCount++;
            }

            if (backlineCount >= PressingAIConstants.MinBacklineAgents)
                return true;

            // Under threshold — F5 immediately per §3.9.
            return false;
        }

        // ── Invariant (3): MaxPressDisplacementM ────────────────────────────

        private static bool EnforceMaxDisplacement(
            PressingSnapshot snapshot,
            ref PressDirective directive)
        {
            if (directive.CoverShadowCount == 0)
                return true;

            if (directive.CoverShadowCount >= 1)
            {
                if (!CheckShadowDisplacement(snapshot, ref directive.Shadow0))
                {
                    // Demote Shadow0; shift Shadow1 into slot 0 if present.
                    if (directive.CoverShadowCount == 2)
                    {
                        directive.Shadow0          = directive.Shadow1;
                        directive.Shadow1          = default;
                        directive.CoverShadowCount = 1;
                    }
                    else
                    {
                        directive.Shadow0          = default;
                        directive.CoverShadowCount = 0;
                    }
                }
            }

            if (directive.CoverShadowCount == 2)
            {
                if (!CheckShadowDisplacement(snapshot, ref directive.Shadow1))
                {
                    directive.Shadow1          = default;
                    directive.CoverShadowCount = 1;
                }
            }

            return true; // Displacement demotion never triggers F5 alone.
        }

        private static bool CheckShadowDisplacement(
            PressingSnapshot snapshot,
            ref CoverShadow shadow)
        {
            // Find baseline slot for the defender.
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId != shadow.DefenderId)
                    continue;

                float dx = shadow.TargetPosition.x - a.BaselineSlot.x;
                float dy = shadow.TargetPosition.y - a.BaselineSlot.y;
                float distSq = dx * dx + dy * dy;
                float maxSq  = PressingAIConstants.MaxPressDisplacementM
                             * PressingAIConstants.MaxPressDisplacementM;
                return distSq <= maxSq;
            }

            return true; // Agent not found — do not demote (safety).
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static Vector2 GetAgentPosition(PressingSnapshot snapshot, int entityId)
        {
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId == entityId)
                    return a.Position;
            }

            return Vector2.zero;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 H-1: added IsActive guard in EnforceMinBackline. |
#endregion
