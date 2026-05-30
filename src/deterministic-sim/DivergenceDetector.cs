// File:     src/deterministic-sim/DivergenceDetector.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.3, §3.4 (FR-DS-007, FR-DS-008), Code Standards #20
// Purpose:  Compares two canonical snapshot digests or individual field values and classifies
//           divergence by tier. Returns the first divergent tick/phase/field path. Pure static.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Tier-aware snapshot divergence detector.
    /// Maps field-tier differences to DivergenceClass taxonomy (FR-DS-007).
    /// Locates the first divergent tick/phase/field path (FR-DS-008).
    /// Deterministic Simulation #16 §3.3.
    /// </summary>
    public static class DivergenceDetector
    {
        /// <summary>
        /// Compares two SHA-256 snapshot digests for bit-exact equality.
        /// Tier A scope: any byte difference is HardDesync. §3.3 / FR-DS-007.
        /// </summary>
        public static DivergenceClass CompareDigests(byte[] digestA, byte[] digestB)
        {
            if (digestA == null || digestB == null)
            {
                return DivergenceClass.HardDesync;
            }

            if (digestA.Length != digestB.Length)
            {
                return DivergenceClass.HardDesync;
            }

            for (int i = 0; i < digestA.Length; i++)
            {
                if (digestA[i] != digestB[i])
                {
                    return DivergenceClass.HardDesync;
                }
            }

            return DivergenceClass.None;
        }

        /// <summary>
        /// Compares a Tier A float field. Any difference is HardDesync; NaN/Inf triggers HardDesync.
        /// §3.3 / FR-DS-013.
        /// </summary>
        public static DivergenceClass CompareTierAFloat(float a, float b, out ushort errorCode)
        {
            uint bitsA = CanonicalSerializer.SingleToUInt32Bits(a);
            uint bitsB = CanonicalSerializer.SingleToUInt32Bits(b);

            bool aNonFinite = (bitsA & 0x7F800000u) == 0x7F800000u;
            bool bNonFinite = (bitsB & 0x7F800000u) == 0x7F800000u;

            if (aNonFinite || bNonFinite)
            {
                errorCode = DeterministicSimConstants.ERR_DS_TIERA_NONFINITE;
                return DivergenceClass.HardDesync;
            }

            errorCode = 0;
            return bitsA != bitsB ? DivergenceClass.HardDesync : DivergenceClass.None;
        }

        /// <summary>
        /// Compares a Tier B float field with an approved epsilon tolerance.
        /// Normalizes -0.0 and non-canonical NaN per §3.2.4.1. §3.3 / FR-DS-011.
        /// </summary>
        public static DivergenceClass CompareTierBFloat(float a, float b, float epsilon, out ushort errorCode)
        {
            uint bitsA = CanonicalSerializer.SingleToUInt32Bits(a);
            uint bitsB = CanonicalSerializer.SingleToUInt32Bits(b);

            bool aNaN = (bitsA & 0x7F800000u) == 0x7F800000u && (bitsA & 0x007FFFFFu) != 0u;
            bool bNaN = (bitsB & 0x7F800000u) == 0x7F800000u && (bitsB & 0x007FFFFFu) != 0u;

            // Tier B NaN: non-canonical NaN is an error; canonical NaN (0x7FC00000) is allowed
            if (aNaN && bitsA != DeterministicSimConstants.F32_CANONICAL_NAN_BITS)
            {
                errorCode = DeterministicSimConstants.ERR_DS_TIERB_NONFINITE;
                return DivergenceClass.HardDesync;
            }
            if (bNaN && bitsB != DeterministicSimConstants.F32_CANONICAL_NAN_BITS)
            {
                errorCode = DeterministicSimConstants.ERR_DS_TIERB_NONFINITE;
                return DivergenceClass.HardDesync;
            }

            // If both are canonical NaN → equal by convention
            if (bitsA == DeterministicSimConstants.F32_CANONICAL_NAN_BITS &&
                bitsB == DeterministicSimConstants.F32_CANONICAL_NAN_BITS)
            {
                errorCode = 0;
                return DivergenceClass.None;
            }

            // AR-1 M-3: exactly one value is canonical NaN, the other is a finite float → definite divergence.
            // float subtraction with a NaN operand yields NaN; NaN > epsilon is always false, which would
            // incorrectly return None. Catch this case explicitly before the epsilon check.
            if (bitsA == DeterministicSimConstants.F32_CANONICAL_NAN_BITS ||
                bitsB == DeterministicSimConstants.F32_CANONICAL_NAN_BITS)
            {
                errorCode = 0;
                return DivergenceClass.SoftDrift;
            }

            errorCode = 0;
            float diff = a - b;
            if (diff < 0f) diff = -diff;
            return diff > epsilon ? DivergenceClass.SoftDrift : DivergenceClass.None;
        }

        /// <summary>
        /// Compares two Tier A integer fields. Any difference is HardDesync. §3.3.
        /// </summary>
        public static DivergenceClass CompareTierAInt(int a, int b)
        {
            return a != b ? DivergenceClass.HardDesync : DivergenceClass.None;
        }

        /// <summary>
        /// Compares two Tier A ulong fields (e.g. tick, RNG cursor). §3.3.
        /// </summary>
        public static DivergenceClass CompareTierAUlong(ulong a, ulong b)
        {
            return a != b ? DivergenceClass.HardDesync : DivergenceClass.None;
        }

        /// <summary>
        /// Returns the worst (highest-severity) DivergenceClass between two classifications.
        /// Severity order: HardDesync > SoftDrift > Cosmetic > None.
        /// </summary>
        public static DivergenceClass Worst(DivergenceClass a, DivergenceClass b)
        {
            if (a == DivergenceClass.HardDesync || b == DivergenceClass.HardDesync)
            {
                return DivergenceClass.HardDesync;
            }
            if (a == DivergenceClass.SoftDrift || b == DivergenceClass.SoftDrift)
            {
                return DivergenceClass.SoftDrift;
            }
            if (a == DivergenceClass.Cosmetic || b == DivergenceClass.Cosmetic)
            {
                return DivergenceClass.Cosmetic;
            }
            return DivergenceClass.None;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                |
// | 1.1     | 2026-05-29 | —      | AR-1 M-3: CompareTierBFloat one-canonical-NaN case returns SoftDrift.  |
#endregion
