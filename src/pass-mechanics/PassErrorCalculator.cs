// File:     src/pass-mechanics/PassErrorCalculator.cs
// Created:  2026-05-26
// Modified: 2026-06-08
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
        /// Computes a deterministic error-direction signed fraction in [-1, +1) using a
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
        /// <returns>Signed fraction in [-1, +1) — uniform distribution. Upper bound is open
        /// because the 24-bit mantissa quantisation never produces normalised == 1.0f.</returns>
        public static float ComputeErrorDirection(int agentId, int frameNumber, int passTypeIndex)
        {
            // SplitMix64 Stafford-variant 13 finalizer (Mix13) over a non-zero-salted
            // (agentId, frameNumber, passTypeIndex) tuple. The 0x9E3779B97F4A7C15 golden-ratio
            // seed (used as the canonical SplitMix64 state-update gamma in Spec #16 §3.4.4)
            // is added BEFORE the first multiply so the (0, 0, 0) input cannot land on the
            // mixer's fixed point (every step on h=0 stays 0, defeating avalanche).
            //
            // agentId is folded ADDITIVELY into the seed (seed + agentId * P), while
            // frameNumber and passTypeIndex are folded via XOR after distinct prime
            // multiplications. The asymmetry is intentional (AR-5 M-1): the additive
            // path on agentId guarantees a unique pre-finalizer base for adjacent
            // agents within the same frame even before avalanche kicks in.
            //
            // Each input multiplier is an xxHash64 family prime (PRIME64_2 / PRIME64_3 /
            // PRIME64_5). The finalizer multipliers 0xBF58476D1CE4E5B9 and
            // 0x94D049BB133111EB are the Stafford Mix13 constants. Input-mix primes are
            // kept DISJOINT from the finalizer primes (AR-6 M-1) so the finalizer cannot
            // partially undo input-mix structure on adjacent (frame, passIdx) tuples by
            // multiplying through the same prime twice. None of the input-mix primes
            // are the Spec #16 §3.4.4 state-update gamma.
            ulong h;
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug.
            {
                h  = 0x9E3779B97F4A7C15UL + ((ulong)(uint)agentId)        * 0xC2B2AE3D27D4EB4FUL; // xxHash64 PRIME64_2
                h ^= ((ulong)(uint)frameNumber)    * 0x165667B19E3779F9UL;                         // xxHash64 PRIME64_3
                h ^= ((ulong)(uint)passTypeIndex)  * 0x27D4EB2F165667C5UL;                         // xxHash64 PRIME64_5
                h ^= h >> 30;
                h *= 0xBF58476D1CE4E5B9UL;                                                         // Stafford Mix13 #1
                h ^= h >> 27;
                h *= 0x94D049BB133111EBUL;                                                         // Stafford Mix13 #2
                h ^= h >> 31;
            }

            // Take the top 24 bits as the uniform random word (avalanche-cleanest end);
            // the 24-bit window matches float mantissa precision so the divisor is exactly
            // representable and the [0, 1) → [-1, +1) mapping introduces no rounding bias.
            const uint  Mantissa24Mask  = 0x00FFFFFFu;   // 2^24 − 1
            const float Mantissa24Scale = 16777216.0f;   // 2^24, exactly representable in float
            uint  top24      = (uint)(h >> 40) & Mantissa24Mask;
            float normalised = (float)top24 / Mantissa24Scale;
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
// | 1.5     | 2026-06-06 | —      | AR-4 M-1: restored XOR-combinator orthogonality between frameNumber and  |
// |         |            |        |     passTypeIndex contributions (AR-3 had folded all three inputs into a |
// |         |            |        |     single ADD). Form is now (seed + agentId) XOR (frame*P1) XOR         |
// |         |            |        |     (passIdx*P2), eliminating the (a=0, f=k*P2, p=0) / (a=0, f=0,        |
// |         |            |        |     p=k*P1) aliasing.                                                    |
// | 1.6     | 2026-06-06 | —      | AR-5 M-1: agentId now multiplied by the xxHash64 prime                  |
// |         |            |        |     0xC2B2AE3D27D4EB4F before the seed addition so adjacent agentIds    |
// |         |            |        |     within the same frame produce divergent pre-finalizer values rather |
// |         |            |        |     than differing by 1 in the low bits (Stafford-13 still recovers but |
// |         |            |        |     the multiplier-spread input is strictly better hash quality).        |
// | 1.7     | 2026-06-08 | —      | AR-6 fix pass (1M+3L; completes the AR-5 cycle-stop punt).               |
// |         |            |        | M-1: input-mix primes for frameNumber / passTypeIndex replaced with     |
// |         |            |        |     xxHash64 PRIME64_3 (0x165667B19E3779F9) and PRIME64_5               |
// |         |            |        |     (0x27D4EB2F165667C5). AR-5 fixed the same hazard for agentId but   |
// |         |            |        |     left the other two axes multiplying by the SAME primes the         |
// |         |            |        |     Stafford Mix13 finalizer then multiplies through again              |
// |         |            |        |     (0xBF58476D1CE4E5B9 / 0x94D049BB133111EB). With the prime reuse,    |
// |         |            |        |     adjacent (frame, passIdx) tuples at a fixed agentId let the         |
// |         |            |        |     finalizer partially undo input-mix structure it was designed       |
// |         |            |        |     against uniform input. Input-mix primes are now disjoint from      |
// |         |            |        |     finalizer primes on all three axes.                                |
// |         |            |        | L-1: <returns> + <summary> upper bound corrected [-1, +1] → [-1, +1).  |
// |         |            |        |     EC-010 enforces Assert.Less(dir, 1.0f) — the implementation       |
// |         |            |        |     produces top24 < 2^24, so normalised < 1.0f. Doc-only.            |
// |         |            |        | L-2: inline comment block rewritten to call out the additive vs XOR    |
// |         |            |        |     asymmetry on agentId (AR-5 M-1 intent) and to record the AR-6      |
// |         |            |        |     input-mix / finalizer prime disjointness invariant.                |
// |         |            |        | L-3: bit-extraction literals 0x00FFFFFFu / 0x01000000u promoted to     |
// |         |            |        |     named local consts Mantissa24Mask / Mantissa24Scale; comment        |
// |         |            |        |     records that the 24-bit window matches float mantissa precision.    |
#endregion
