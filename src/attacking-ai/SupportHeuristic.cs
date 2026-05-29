// File:     src/attacking-ai/SupportHeuristic.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.5, FR-AT-013, Code Standards #20
// Purpose:  Computes effective support radius and determines per-agent SUPPORT_BALL eligibility.
//           The MinEffectiveRadiusM floor prevents radius collapse under aggressive supportMult values.

using UnityEngine;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Support-radius heuristic for SUPPORT_BALL role eligibility. Pure static.
    /// Effective radius = max(<see cref="AttackingAIConstants.MinEffectiveRadiusM"/>,
    /// <see cref="AttackingAIConstants.SupportRadiusM"/> × supportMult).
    /// The floor ensures a minimum zone always exists around the carrier (§3.5 / FR-AT-013).
    /// Attacking AI #15 §3.5.
    /// </summary>
    internal static class SupportHeuristic
    {
        /// <summary>
        /// Returns true when <paramref name="agentPosition"/> is within the effective support
        /// radius of <paramref name="ballCarrierPosition"/> for the given profile. §3.5.
        /// </summary>
        public static bool IsWithinSupportRadius(
            Vector2 agentPosition, Vector2 ballCarrierPosition, StyleProfile styleProfile)
        {
            float r   = ComputeEffectiveRadius(styleProfile);
            float dx  = agentPosition.x - ballCarrierPosition.x;
            float dy  = agentPosition.y - ballCarrierPosition.y;
            float dSq = dx * dx + dy * dy;
            return dSq <= r * r;
        }

        /// <summary>
        /// Computes the effective support radius after applying the profile multiplier and
        /// the <see cref="AttackingAIConstants.MinEffectiveRadiusM"/> floor. Units: m. §3.5.
        /// </summary>
        public static float ComputeEffectiveRadius(StyleProfile styleProfile)
        {
            float raw = AttackingAIConstants.SupportRadiusM * styleProfile.SupportMult;
            return raw < AttackingAIConstants.MinEffectiveRadiusM
                ? AttackingAIConstants.MinEffectiveRadiusM
                : raw;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 H-1: removed local MinEffectiveRadiusM const; reference AttackingAIConstants.MinEffectiveRadiusM (FR-CS-016). AR-2 L-1: updated class doc and Purpose to reference constant name. |
#endregion
