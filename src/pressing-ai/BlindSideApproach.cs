// File:     src/pressing-ai/BlindSideApproach.cs
// Created:  2026-07-07
// Modified: 2026-07-07
// Author:   —
// Spec:     Pressing AI #13 §3.3, new §7.12 (cheap-item addition), Code Standards #20
// Purpose:  Pure static: biases the primary presser's approach target toward the ball carrier's
//           blind side (opposite the carrier's facing direction), so the press closes down while
//           staying outside the carrier's peripheral vision longer ("curving the press").

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Curving-press blind-side bias (new §7.12). Pure static; no side effects. Applied AFTER
    /// <see cref="PrimaryPressSelector.Select"/> — biases only the final movement target, never the
    /// eligibility/cost computation, so who is selected as primary presser is unaffected.
    /// </summary>
    public static class BlindSideApproach
    {
        /// <summary>
        /// Returns <paramref name="targetPosition"/> nudged toward the ball carrier's blind side
        /// (the direction opposite <see cref="PressingAgentSnapshot.Facing"/>) by
        /// <see cref="PressingAIConstants.BlindSideApproachBiasM"/>. Returns the input unchanged when
        /// there is no ball carrier, the carrier is inactive, or the carrier's facing vector is
        /// degenerate (near-zero — cannot determine a blind side).
        /// </summary>
        public static Vector2 ApplyBias(Vector2 targetPosition, PressingSnapshot snapshot)
        {
            int carrierId = snapshot.BallCarrierEntityId;
            if (carrierId < 0)
            {
                return targetPosition;
            }

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId != carrierId)
                {
                    continue;
                }
                if (!a.IsActive)
                {
                    break;
                }

                float facingMagSq = a.Facing.sqrMagnitude;
                if (facingMagSq < 1e-6f)
                {
                    return targetPosition;
                }

                Vector2 blindDir = -a.Facing / Mathf.Sqrt(facingMagSq);
                return targetPosition + blindDir * PressingAIConstants.BlindSideApproachBiasM;
            }

            return targetPosition;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-07 | —      | Initial implementation (cheap-item addition). |
#endregion
