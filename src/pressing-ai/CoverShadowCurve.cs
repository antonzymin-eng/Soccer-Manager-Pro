// File:     src/pressing-ai/CoverShadowCurve.cs
// Created:  2026-07-07
// Modified: 2026-07-07
// Author:   —
// Spec:     Pressing AI #13 §3.3–§3.5, new §7.12 (cheap-item addition, redesigned), Code Standards #20
// Purpose:  Pure static: curves the primary presser's approach path toward a nearby passing option
//           while pursuing the ball carrier, so pressure and cover-shadow denial happen together —
//           rather than the presser running straight at the carrier and leaving a pass open behind
//           them. Curve strength is gated by the presser's own attributes.

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Cover-shadow curving (new §7.12, redesigned from the original "blind-side approach" cheap-item
    /// pass after user review): curving runs adjust COVER SHADOW to deny a passing option during
    /// pursuit — they are not about approaching from the carrier's blind side, and their strength is
    /// NOT a flat bonus; it is gated by the presser's own defensive positioning, physical effort
    /// capacity (work rate / pace / stamina), and mental sharpness (decisions / anticipation). A poor,
    /// unfit, low-effort defender will not curve the run correctly — they close down in a straight
    /// line and leave the pass open.
    /// </summary>
    public static class CoverShadowCurve
    {
        /// <summary>
        /// Returns the presser's execution quality for this technique, in [0, 1]. Averages
        /// <see cref="PressingAgentSnapshot.DefensivePositioningAttribute"/>,
        /// <see cref="PressingAgentSnapshot.PhysicalEffortAttribute"/>, and
        /// <see cref="PressingAgentSnapshot.MentalSharpnessAttribute"/> (each raw 1–20), then
        /// normalises by <see cref="PressingAIConstants.ATTRIBUTE_SCALE_MAX"/>.
        /// </summary>
        public static float ComputeCurveEffectiveness(in PressingAgentSnapshot presser)
        {
            float avg = (presser.DefensivePositioningAttribute
                       + presser.PhysicalEffortAttribute
                       + presser.MentalSharpnessAttribute) / 3f;
            return Mathf.Clamp01(avg / PressingAIConstants.ATTRIBUTE_SCALE_MAX);
        }

        /// <summary>
        /// Curves <paramref name="rawTarget"/> (the primary presser's straight-line interception
        /// target) toward the shadow-lane point between the ball carrier and the nearest eligible
        /// opponent receiver — the same lane concept <see cref="CoverShadowSelector"/> uses — so a
        /// capable presser denies a nearby pass while still closing the ball down. The blend weight
        /// is <see cref="PressingAIConstants.CoverCurveBlendWeightMax"/> scaled by
        /// <see cref="ComputeCurveEffectiveness"/>; an ineffective presser (low attributes) blends
        /// near zero and the target stays effectively a straight line. Returns the input unchanged
        /// when there is no ball carrier or no eligible nearby receiver to shadow.
        /// </summary>
        public static Vector2 ApplyCurve(Vector2 rawTarget, PressingSnapshot snapshot, in PressingAgentSnapshot presser)
        {
            int carrierId = snapshot.BallCarrierEntityId;
            if (carrierId < 0)
            {
                return rawTarget;
            }

            Vector2 carrierPos = Vector2.zero;
            bool carrierFound = false;
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
                carrierPos = a.Position;
                carrierFound = true;
                break;
            }
            if (!carrierFound)
            {
                return rawTarget;
            }

            // Nearest eligible opponent receiver to the carrier (not the carrier, not the GK, active,
            // within CoverShadowCandidateRadiusM) — the passing option a curved run would shadow.
            float radiusSq = PressingAIConstants.CoverShadowCandidateRadiusM
                           * PressingAIConstants.CoverShadowCandidateRadiusM;
            int bestReceiverId = -1;
            float bestDistSq = float.MaxValue;
            Vector2 bestReceiverPos = Vector2.zero;

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot r = ref snapshot.Agents[i];
                if (r.TeamId == presser.TeamId)
                {
                    continue;
                }
                if (!r.IsActive || r.IsGoalkeeper || r.EntityId == carrierId)
                {
                    continue;
                }

                float dx = r.Position.x - carrierPos.x;
                float dy = r.Position.y - carrierPos.y;
                float distSq = dx * dx + dy * dy;
                if (distSq > radiusSq)
                {
                    continue;
                }

                if (bestReceiverId < 0 || distSq < bestDistSq
                    || (distSq == bestDistSq && r.EntityId < bestReceiverId))
                {
                    bestReceiverId = r.EntityId;
                    bestDistSq = distSq;
                    bestReceiverPos = r.Position;
                }
            }

            if (bestReceiverId < 0)
            {
                return rawTarget;
            }

            Vector2 shadowLane = Vector2.Lerp(carrierPos, bestReceiverPos, PressingAIConstants.CoverShadowLaneFraction);
            float blend = ComputeCurveEffectiveness(in presser) * PressingAIConstants.CoverCurveBlendWeightMax;
            return Vector2.Lerp(rawTarget, shadowLane, blend);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-07 | —      | Redesign of the original BlindSideApproach cheap-item addition after user  |
// |         |            |        |   review: curving runs adjust cover shadow to deny a nearby pass during     |
// |         |            |        |   pursuit, gated by the presser's own attributes — not a flat blind-side    |
// |         |            |        |   position bias. Replaces BlindSideApproach.cs (deleted).                   |
#endregion
