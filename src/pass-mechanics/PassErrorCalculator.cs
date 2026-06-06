// File:     src/pass-mechanics/PassErrorCalculator.cs
// Created:  2026-05-26
// Modified: 2026-06-06
// Author:   —
// Spec:     Pass Mechanics #5 §3.5, §3.7, Code Standards #20
// Purpose:  Pure static calculator for the multiplicative error chain (§3.5),
//           deterministic error direction hash (§3.5.7), and weak foot modifiers
//           (§3.7). No side effects. No System.Random — deterministic per replay.

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Computes the deterministic pass error angle and direction, and weak foot
    /// modifiers. All methods are pure functions. Pass Mechanics #5 §3.5, §3.7.
    /// </summary>
    internal static class PassErrorCalculator
    {
        // ── §3.5 — Error Angle ──────────────────────────────────────────────────────

        /// <summary>
        /// Computes the deterministic error angle (degrees) from the multiplicative
        /// modifier chain. Called at CONTACT state with fresh pressureScalar.
        /// Formula: BASE_ERROR × PassingModifier × PressureModifier × FatigueModifier
        ///          × OrientationModifier × UrgencyModifier × WeakFootModifier,
        /// clamped to [MIN_ERROR_ANGLE, MAX_ERROR_ANGLE].
        /// Pass Mechanics #5 §3.5.3, §3.5.8.
        /// </summary>
        /// <param name="passType">Determines BASE_ERROR.</param>
        /// <param name="crossSubType">Sub-type for base error lookup (Cross only).</param>
        /// <param name="passing">Agent Passing attribute [1, 20].</param>
        /// <param name="pressureScalar">Pressure from spatial hash query [0, 1].</param>
        /// <param name="fatigue">Agent fatigue [0, 1]. 0=rested.</param>
        /// <param name="bodyAngleDeg">Angle between facing direction and pass direction [0°, 90°].</param>
        /// <param name="urgency">Pass urgency from PassRequest [0, 1].</param>
        /// <param name="isWeakFoot">True if non-preferred foot used.</param>
        /// <param name="weakFootRating">Weak foot quality [1, 5]. 5 = ambidextrous.</param>
        /// <returns>Error angle in degrees clamped to [MIN_ERROR_ANGLE, MAX_ERROR_ANGLE].</returns>
        public static float ComputeErrorAngle(
            PassType passType,
            CrossSubType crossSubType,
            float passing,
            float pressureScalar,
            float fatigue,
            float bodyAngleDeg,
            float urgency,
            bool isWeakFoot,
            int weakFootRating)
        {
            float P = Mathf.Clamp(passing,       PassMechanicsConstants.ATTR_MIN, PassMechanicsConstants.ATTR_MAX);
            float S = Mathf.Clamp01(pressureScalar);
            float F = Mathf.Clamp01(fatigue);
            float alpha = Mathf.Clamp(bodyAngleDeg, 0f, 90f);
            float U = Mathf.Clamp01(urgency);

            float passingMod = PassMechanicsConstants.PassingErrorMax
                             - ((P - 1f) / (PassMechanicsConstants.ATTR_MAX - 1f))
                             * (PassMechanicsConstants.PassingErrorMax - PassMechanicsConstants.PassingErrorMin);

            float pressureMod    = 1.0f + S * PassMechanicsConstants.PressureWeight;
            float fatigueMod     = 1.0f + F * PassMechanicsConstants.FatigueAccuracyReduction;
            float orientationMod = 1.0f + (alpha / 90.0f) * PassMechanicsConstants.OrientationMaxPenalty;
            float urgencyMod     = 1.0f + U * PassMechanicsConstants.UrgencyErrorScale;
            float weakFootMod    = ComputeWeakFootAccuracyModifier(isWeakFoot, weakFootRating);

            float baseError = PassMechanicsConstants.GetBaseError(passType, crossSubType);
            float rawError  = baseError * passingMod * pressureMod * fatigueMod
                            * orientationMod * urgencyMod * weakFootMod;

            float errorAngle = Mathf.Clamp(rawError,
                PassMechanicsConstants.MinErrorAngle,
                PassMechanicsConstants.MaxErrorAngle);

            if (float.IsNaN(errorAngle))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[PassError] FM-04: ErrorAngle is NaN. Returning MinErrorAngle.");
#endif
                return PassMechanicsConstants.MinErrorAngle;
            }

            return errorAngle;
        }

        // ── §3.5.7 — Error Direction ────────────────────────────────────────────────

        /// <summary>
        /// Computes a deterministic error-direction signed fraction in [-1, +1] using a
        /// SplitMix64-style mixer over (agentId, frameNumber, passTypeIndex). Identical
        /// inputs always produce identical output — replay-safe. No System.Random.
        ///
        /// The output is the rotation FRACTION applied to errorAngleDeg by
        /// <see cref="PassTargetResolver.ApplyErrorToDirection"/>; mapping to a uniform
        /// signed scalar (rather than an angle on [0, 2π) that is then composed through
        /// sin()) preserves uniform distribution of the final deflection.
        /// §3.5.7.
        /// </summary>
        /// <param name="agentId">Unique ID of the passing agent.</param>
        /// <param name="frameNumber">Simulation frame from PassRequest.</param>
        /// <param name="passTypeIndex">PassType cast to int.</param>
        /// <returns>Signed fraction in [-1, +1] — uniform distribution.</returns>
        public static float ComputeErrorDirection(int agentId, int frameNumber, int passTypeIndex)
        {
            // SplitMix64 Stafford-variant 13 finalizer (Mix13) over a non-zero-salted
            // (agentId, frameNumber, passTypeIndex) tuple. The 0x9E3779B97F4A7C15 golden-ratio
            // seed (used as the canonical SplitMix64 state-update gamma in Spec #16 §3.4.4)
            // is added BEFORE the first multiply so the (0, 0, 0) input cannot land on the
            // mixer's fixed point (every step on h=0 stays 0, defeating avalanche).
            // The 0xBF58476D1CE4E5B9 / 0x94D049BB133111EB constants below are the Stafford
            // Mix13 finalizer multipliers — not the same as the Spec #16 §3.4.4 state-update
            // constant, but a well-known SplitMix64 derivative used for hash quality.
            ulong h;
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug.
            {
                h = 0x9E3779B97F4A7C15UL
                    + ((ulong)(uint)agentId)
                    + ((ulong)(uint)frameNumber * 0xBF58476D1CE4E5B9UL)
                    + ((ulong)(uint)passTypeIndex * 0x94D049BB133111EBUL);
                h ^= h >> 30;
                h *= 0xBF58476D1CE4E5B9UL;
                h ^= h >> 27;
                h *= 0x94D049BB133111EBUL;
                h ^= h >> 31;
            }

            // Take the top 24 bits as the uniform random word (avalanche-cleanest end);
            // map to [0, 1) then to the signed fraction [-1, +1).
            uint top24 = (uint)(h >> 40) & 0x00FFFFFFu;
            float normalised = (float)top24 / (float)0x01000000u;
            return (normalised * 2.0f) - 1.0f;
        }

        // ── §3.7 — Weak Foot Modifiers ──────────────────────────────────────────────

        /// <summary>
        /// Returns the weak foot accuracy modifier for the error chain.
        /// Returns 1.0 if IsWeakFoot is false (no penalty for preferred foot).
        /// Formula: 1.0 + PenaltyFraction × WEAK_FOOT_BASE_PENALTY where
        /// PenaltyFraction = 1 − ((rating − 1) / 4). Pass Mechanics #5 §3.7.3.
        /// </summary>
        public static float ComputeWeakFootAccuracyModifier(bool isWeakFoot, int weakFootRating)
        {
            if (!isWeakFoot)
                return 1.0f;

            int R = Mathf.Clamp(weakFootRating, 1, 5);
            float penaltyFraction = 1.0f - ((float)(R - 1) / 4.0f);
            return 1.0f + penaltyFraction * PassMechanicsConstants.WeakFootBasePenalty;
        }

        /// <summary>
        /// Returns the weak foot power penalty applied to kick speed.
        /// Returns 1.0 if IsWeakFoot is false (no reduction for preferred foot).
        /// Formula: 1.0 − PenaltyFraction × WEAK_FOOT_POWER_PENALTY.
        /// Pass Mechanics #5 §3.7.4.
        /// </summary>
        public static float ComputeWeakFootPowerPenalty(bool isWeakFoot, int weakFootRating)
        {
            if (!isWeakFoot)
                return 1.0f;

            int R = Mathf.Clamp(weakFootRating, 1, 5);
            float penaltyFraction = 1.0f - ((float)(R - 1) / 4.0f);
            return 1.0f - penaltyFraction * PassMechanicsConstants.WeakFootPowerPenalty;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                     |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                                   |
// | 1.1     | 2026-05-26 | —      | H2: unchecked block added to ComputeErrorDirection hash for intentional   |
// |         |            |        |     32-bit wrap-around (coding guide determinism rule).                   |
// | 1.2     | 2026-05-27 | —      | AR-1 M-1: class changed public → internal (implementation detail).        |
// |         |            |        | AR-1 L-1: NaN fallback returns MinErrorAngle (0.1°) instead of 0f;        |
// |         |            |        |     0f violated the clamp contract [MinErrorAngle, MaxErrorAngle].        |
// | 1.3     | 2026-06-06 | —      | AR-2 M-1: ComputeErrorDirection replaced prime-XOR mixer with             |
// |         |            |        |     SplitMix64-style avalanche (Spec #16 §3.4.4 constants); 64-bit ulong  |
// |         |            |        |     state eliminates 32-bit collision modes on close (agentId, frame)     |
// |         |            |        |     pairs that the prior `h * P1 ^ h * P2 ^ h * P3` form was susceptible   |
// |         |            |        |     to.                                                                   |
// |         |            |        | AR-2 M-2: return type semantics changed [0, 2π) radians → [-1, +1] signed |
// |         |            |        |     fraction so the deflection magnitude distributes UNIFORMLY across     |
// |         |            |        |     [-errorAngle, +errorAngle]. Previous return mapped uniform [0,2π) to  |
// |         |            |        |     rotation degrees via sin(), which produced a non-uniform Arcsine      |
// |         |            |        |     distribution heavily weighted near ±errorAngle (sin'(±π/2) → 0).      |
// |         |            |        |     PassTargetResolver.ApplyErrorToDirection signature follows.            |
// |         |            |        | AR-2 L-13: NaN diagnostic Debug.LogError gated by                         |
// |         |            |        |     #if UNITY_EDITOR || DEVELOPMENT_BUILD (FR-CS-031 hot-path carve-out;   |
// |         |            |        |     restores symmetry with sibling files' build-guard pattern).            |
// | 1.4     | 2026-06-06 | —      | AR-3 M-2: SplitMix64 mixer now adds 0x9E3779B97F4A7C15 seed BEFORE the    |
// |         |            |        |     first multiply so the (0, 0, 0) input no longer lands on the mixer's |
// |         |            |        |     fixed point. AR-3 L-1: inline comment corrected to Stafford "Mix13"   |
// |         |            |        |     finalizer naming; explicit note that 0xBF584... / 0x94D04... are NOT  |
// |         |            |        |     the Spec #16 §3.4.4 state-update constant.                            |
#endregion
