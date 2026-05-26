// File:     src/first-touch/TouchRadiusCalculator.cs
// Created:  2026-05-25
// Modified: 2026-05-26
// Author:   —
// Spec:     First Touch Mechanics #4 §3.2, Code Standards #20
// Purpose:  Computes ball displacement radius from control quality and ball speed.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Computes the touch displacement radius (r, metres) using a piecewise linear
    /// interpolation over control quality bands plus a velocity modifier.
    /// First Touch Mechanics #4 §3.2.
    /// </summary>
    internal static class TouchRadiusCalculator
    {
        /// <summary>
        /// Calculates displacement radius for a touch. First Touch Mechanics #4 §3.2.
        /// Returns a value in [RadiusMin, RadiusHeavy]; the velocity modifier can inflate the
        /// base radius but a hard cap at RadiusHeavy is always applied (§3.2.3).
        /// </summary>
        /// <param name="q">Control quality scalar [0,1] from ControlQualityCalculator.</param>
        /// <param name="ballSpeed">Speed of the incoming ball (m/s), used for velocity modifier §3.2.3.</param>
        internal static float Calculate(float q, float ballSpeed)
        {
            // Piecewise linear interpolation over the four quality bands (§3.2).
            // Band boundaries use named constants from FirstTouchConstants to avoid magic literals.
            //   q ∈ [QualityBandPerfect, 1.0]           → r ∈ [RadiusMin,    RadiusPerfect]
            //   q ∈ [ControlledThreshold, QualityBandPerfect) → r ∈ [RadiusPerfect, RadiusGood]
            //   q ∈ [QualityBandPoor, ControlledThreshold)   → r ∈ [RadiusGood,    RadiusPoor]
            //   q ∈ [0.0, QualityBandPoor)                   → r ∈ [RadiusPoor,    RadiusHeavy]
            float baseRadius;
            if (q >= FirstTouchConstants.QualityBandPerfect)
            {
                float t = (q - FirstTouchConstants.QualityBandPerfect)
                        / (1.0f - FirstTouchConstants.QualityBandPerfect);
                baseRadius = Mathf.Lerp(FirstTouchConstants.RadiusPerfect, FirstTouchConstants.RadiusMin, t);
            }
            else if (q >= FirstTouchConstants.ControlledThreshold)
            {
                float t = (q - FirstTouchConstants.ControlledThreshold)
                        / (FirstTouchConstants.QualityBandPerfect - FirstTouchConstants.ControlledThreshold);
                baseRadius = Mathf.Lerp(FirstTouchConstants.RadiusGood, FirstTouchConstants.RadiusPerfect, t);
            }
            else if (q >= FirstTouchConstants.QualityBandPoor)
            {
                float t = (q - FirstTouchConstants.QualityBandPoor)
                        / (FirstTouchConstants.ControlledThreshold - FirstTouchConstants.QualityBandPoor);
                baseRadius = Mathf.Lerp(FirstTouchConstants.RadiusPoor, FirstTouchConstants.RadiusGood, t);
            }
            else
            {
                float t = q / FirstTouchConstants.QualityBandPoor;
                baseRadius = Mathf.Lerp(FirstTouchConstants.RadiusHeavy, FirstTouchConstants.RadiusPoor, t);
            }

            // §3.2.3 — velocity modifier: multiplicative; only applied for speed above reference.
            // Ball at reference speed → mod = 1.0 (no change). Ball at 30 m/s → mod = 1.25.
            float velocityExcess = Mathf.Max(0.0f, ballSpeed - FirstTouchConstants.VelocityReference);
            float velocityMod = 1.0f + (velocityExcess / FirstTouchConstants.VelocityReference)
                                     * FirstTouchConstants.VelocityRadiusFactor;

            // Hard cap at RadiusHeavy (2.0m); velocity modifier cannot push radius past max.
            return Mathf.Min(baseRadius * velocityMod, FirstTouchConstants.RadiusHeavy);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                                |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                                       |
// | 1.1     | 2026-05-26 | —      | H-2/H-3 fix: band thresholds corrected via constant updates; velocity modifier changed to multiplicative excess-only. |
#endregion
