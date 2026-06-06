// File:     src/first-touch/PressureEvaluator.cs
// Created:  2026-05-25
// Modified: 2026-06-06
// Author:   —
// Spec:     First Touch Mechanics #4 §3.5, Code Standards #20
// Purpose:  Evaluates opponent pressure on a receiving agent's position.

using System;

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Computes aggregated pressure from nearby opponents using inverse-square contribution.
    /// First Touch Mechanics #4 §3.5.
    /// </summary>
    internal static class PressureEvaluator
    {
        /// <summary>
        /// Evaluates pressure at agentPositionXY from all entries in opponentPositions.
        /// Iterates without LINQ; produces zero allocations. First Touch Mechanics #4 §3.5.
        /// </summary>
        /// <param name="agentPositionXY">XY world position of the receiving agent (m).</param>
        /// <param name="opponentPositions">XY world positions of all opposing outfield agents.</param>
        internal static PressureResult Evaluate(
            Vector2 agentPositionXY,
            ReadOnlySpan<Vector2> opponentPositions)
        {
            float totalPressure = 0.0f;
            float nearestDistance = float.PositiveInfinity;
            bool hasNearby = false;

            float pressureRadiusSq = FirstTouchConstants.PressureRadius * FirstTouchConstants.PressureRadius;

            for (int i = 0; i < opponentPositions.Length; i++)
            {
                Vector2 delta = opponentPositions[i] - agentPositionXY;
                float distSq = delta.sqrMagnitude;

                if (distSq > pressureRadiusSq)
                {
                    continue;
                }

                float dist = Mathf.Sqrt(distSq);
                hasNearby = true;

                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                }

                // §3.5.2 — normalised inverse-square: contribution = (MIN/dist)²; guard caps at 1.0.
                float guardedDist = Mathf.Max(dist, FirstTouchConstants.MinPressureDistance);
                float ratio = FirstTouchConstants.MinPressureDistance / guardedDist;
                totalPressure += ratio * ratio;
            }

            // §3.5.3 — normalise by saturation value then clamp to [0,1].
            float pressureScalar = Mathf.Clamp01(totalPressure / FirstTouchConstants.PressureSaturation);

            return new PressureResult
            {
                PressureScalar = pressureScalar,
                HasNearbyOpponent = hasNearby,
                NearestOpponentDistance = nearestDistance
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                             |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                    |
// | 1.1     | 2026-05-26 | —      | H-6/H-7 fix: contribution formula corrected to (MIN/dist)²; normalisation corrected to /Saturation. |
// | 1.2     | 2026-06-06 | —      | AR-5 L-3: squared-distance gate against PressureRadius² before sqrt — Vector2.Distance only computed for opponents inside the radius. |
#endregion
