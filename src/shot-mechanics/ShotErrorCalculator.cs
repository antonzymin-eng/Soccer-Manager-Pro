// File:     src/shot-mechanics/ShotErrorCalculator.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §3.6, Code Standards #20
// Purpose:  Computes deterministic angular error: base error × power penalty × pressure penalty
//           × fatigue penalty × body-shape penalty × weak-foot multiplier. Applies error
//           in goal-relative (u, v) space. Deterministic hash per KD-4. Pure static.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Computes the deterministic error offset for a shot. §3.6.
    /// All error sources are deterministic — no System.Random. KD-4, NFR-01.
    /// Shot Mechanics #6 §3.6.
    /// </summary>
    public static class ShotErrorCalculator
    {
        /// <summary>
        /// Computes error magnitude (degrees) from all error sources. §3.6.
        /// Clamped to [MinErrorAngle, MaxErrorAngle].
        /// </summary>
        /// <param name="finishing">Agent Finishing attribute [1–20].</param>
        /// <param name="longShots">Agent LongShots attribute [1–20].</param>
        /// <param name="composure">Agent Composure attribute [1–20]; governs pressure resistance. §3.6.5.</param>
        /// <param name="distanceToGoal">Distance to goal (metres); drives Finishing/LongShots blend.</param>
        /// <param name="powerIntent">Shot power [0, 1]; quadratic power penalty. §3.6.4.</param>
        /// <param name="pressureScalar">Pressure scalar [0, 1] from nearby opponents. §3.6.5.</param>
        /// <param name="fatigue">Agent fatigue [0, 1]; 0=rested, 1=fatigued. §3.6.6.</param>
        /// <param name="bodyMechanicsScore">BMS [0, 1] from BodyMechanicsEvaluator. §3.6.7.</param>
        /// <param name="weakFootErrorMultiplier">From WeakFootPenaltyApplier.ComputeErrorMultiplier. §3.8.</param>
        public static float ComputeErrorMagnitude(
            float finishing,
            float longShots,
            float composure,
            float distanceToGoal,
            float powerIntent,
            float pressureScalar,
            float fatigue,
            float bodyMechanicsScore,
            float weakFootErrorMultiplier)
        {
            // §3.6.3 — Base error: inversely proportional to effective attribute
            float w = 1.0f / (1.0f + Mathf.Exp(-(distanceToGoal - ShotMechanicsConstants.DMid)
                                                  / ShotMechanicsConstants.DScale));
            float effAttr = (1.0f - w) * finishing + w * longShots;
            float normAttr = Mathf.Clamp(effAttr, ShotMechanicsConstants.ATTR_MIN,
                                          ShotMechanicsConstants.ATTR_MAX)
                             / ShotMechanicsConstants.ATTR_MAX;

            float baseError = ShotMechanicsConstants.BaseErrorMax * (1.0f - normAttr)
                            + ShotMechanicsConstants.BaseErrorMin * normAttr;

            // §3.6.4 — Power penalty: quadratic (FR-03)
            float powerPenalty = 1.0f + ShotMechanicsConstants.PowerPenaltyCoefficient
                                        * powerIntent * powerIntent;

            // §3.6.5 — Pressure penalty: modulated by Composure
            float normComposure    = Mathf.Clamp(composure, ShotMechanicsConstants.ATTR_MIN,
                                                  ShotMechanicsConstants.ATTR_MAX)
                                     / ShotMechanicsConstants.ATTR_MAX;
            float pressurePenalty  = 1.0f + pressureScalar
                                            * ShotMechanicsConstants.PressureMaxPenalty
                                            * (1.0f - normComposure);

            // §3.6.6 — Fatigue penalty
            float fatiguePenalty = 1.0f + fatigue * ShotMechanicsConstants.FatigueMaxPenalty;

            // §3.6.7 — Body shape penalty
            float bmsDeficit    = 1.0f - bodyMechanicsScore;
            float bodyShapeError = 1.0f + ShotMechanicsConstants.BodyShapeErrorCoefficient
                                          * bmsDeficit * bmsDeficit;

            float errorMag = baseError * powerPenalty * pressurePenalty * fatiguePenalty
                           * bodyShapeError * weakFootErrorMultiplier;

            return Mathf.Clamp(errorMag,
                               ShotMechanicsConstants.MinErrorAngle,
                               ShotMechanicsConstants.MaxErrorAngle);
        }

        /// <summary>
        /// Computes a deterministic error direction in goal-relative (u, v) space using
        /// a hash of matchSeed + agentId + frameNumber. KD-4, §3.6.9.
        /// Returns a normalised Vector2 direction.
        /// </summary>
        public static Vector2 ComputeErrorDirection(int matchSeed, int agentId, int frameNumber)
        {
            // Deterministic hash per KD-4. Pure-static 32-bit SplitMix-style mix combining
            // three seeds; chosen over DeterministicRngService.DrawReserved() because this
            // calculator is a pure-static utility consumed without constructor injection
            // (Stage 1 RNG-service wiring is tracked alongside the spec-error-log entry for
            // §3.6.9). Multipliers 2654435761u and 2246822519u are the Knuth/Fibonacci
            // constants ⌊2^32 · φ⌋ and ⌊2^32 · (√5−1)⌋; final mix 0x45d9f3b7u is the canonical
            // 32-bit SplitMix32 variant. NFR-01: no System.Random.
            uint h = unchecked((uint)matchSeed
                               ^ ((uint)agentId   * 2654435761u)
                               ^ ((uint)frameNumber * 2246822519u));

            // SplitMix32 final mix
            h ^= h >> 16;
            h = unchecked(h * 0x45d9f3b7u);
            h ^= h >> 16;

            // Map to angle in [0, 2π)
            float angle = (h / (float)uint.MaxValue) * 2.0f * Mathf.PI;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// Converts an error magnitude (degrees) and direction in goal-relative space to a
        /// (Δu, Δv) offset vector applied to PlacementTarget before world-space conversion. §3.6.9.
        /// </summary>
        public static Vector2 ComputeErrorOffset(float errorMagnitudeDeg, Vector2 errorDirection)
        {
            // Scale degrees to goal-relative displacement. §3.6.9.
            // Mathf.Deg2Rad (= π/180 ≈ 0.01745) converts angular error to radian measure used as UV scale.
            float scale = errorMagnitudeDeg * Mathf.Deg2Rad;
            return errorDirection * scale;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-28 | —      | H-3: Updated D_Mid→DMid, D_Scale→DScale constant references.  |
// | 1.2     | 2026-05-28 | —      | M-3: Magic literal 0.0175f replaced with Mathf.Deg2Rad.        |
// | 1.3     | 2026-06-01 | —      | AR-2 M-3: documented Knuth/Fibonacci multipliers + SplitMix32  |
// |         |            |        |   final mix rationale; pure-static utility justification for   |
// |         |            |        |   not consuming DeterministicRngService.DrawReserved().        |
#endregion
