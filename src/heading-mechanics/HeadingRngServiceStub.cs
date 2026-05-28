// File:     src/heading-mechanics/HeadingRngServiceStub.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §4.4, Deterministic Simulation #16 §4.1, Code Standards #20
// Purpose:  Stage 0 stub implementation of IHeadingRngService using SplitMix64.
//           Replace — do not remove — at Stage 1 with real DeterministicRngService (#16 §4.1).

using UnityEngine;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Stage 0 stub for IHeadingRngService. Uses SplitMix64 internally; ignores draw-site IDs
    /// (draw-site registry wiring is a Stage 1 deliverable per Deterministic Simulation #16 §4.5).
    /// Box-Muller transform for Gaussian draws.
    /// Replace — do not remove — at Stage 1 with real DeterministicRngService wiring.
    /// Heading Mechanics #10 §4.4 / KD-10.
    /// </summary>
    public sealed class HeadingRngServiceStub : IHeadingRngService
    {
        // Raw SplitMix64 counter: incremented by the golden-ratio constant each call.
        // The counter is never stored in mixed form — mixing produces output only.
        private ulong _state;

        /// <summary>
        /// Initialises the stub with a deterministic seed.
        /// </summary>
        /// <param name="seed">Deterministic seed (e.g. matchSeed XOR frameNumber).</param>
        public HeadingRngServiceStub(ulong seed)
        {
            _state = seed;
        }

        /// <inheritdoc/>
        public float NextFloat(int drawSiteId)
        {
            unchecked // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                _state += 0x9E3779B97F4A7C15UL;
            }
            return (Mix(_state) >> 40) * (1.0f / (1UL << 24));
        }

        /// <inheritdoc/>
        public float NextGaussian(int drawSiteId)
        {
            // Box-Muller: two uniform samples → one standard-normal sample.
            float u1 = NextFloat(drawSiteId);
            float u2 = NextFloat(drawSiteId);
            if (u1 < HeadingMechanicsConstants.RNG_GUARD_EPSILON)
            {
                u1 = HeadingMechanicsConstants.RNG_GUARD_EPSILON;
            }
            return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
        }

        private static ulong Mix(ulong x)
        {
            unchecked // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-28 | —      | AR-1 H-3: SplitMix64 corrected — raw counter in _state (incremented each call); |
// |         |            |        | Mix() produces output only; constructor stores seed directly, no initial step.  |
// |         |            |        | Box-Muller guard uses RNG_GUARD_EPSILON constant (was 1e-7f literal).             |
#endregion
