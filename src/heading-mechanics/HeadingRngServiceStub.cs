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
        private ulong _state;

        /// <summary>
        /// Initialises the stub with a deterministic seed.
        /// </summary>
        /// <param name="seed">Deterministic seed (e.g. matchSeed XOR frameNumber).</param>
        public HeadingRngServiceStub(ulong seed)
        {
            _state = SplitMix64Step(seed);
            if (_state == 0)
            {
                _state = 0x853C49E6748FEA9BUL;
            }
        }

        /// <inheritdoc/>
        public float NextFloat(int drawSiteId)
        {
            ulong raw = SplitMix64Step(_state);
            _state = raw;
            return (raw >> 40) * (1.0f / (1UL << 24));
        }

        /// <inheritdoc/>
        public float NextGaussian(int drawSiteId)
        {
            // Box-Muller: two uniform samples → one standard-normal sample.
            float u1 = NextFloat(drawSiteId);
            float u2 = NextFloat(drawSiteId);
            // Guard against log(0).
            if (u1 < 1e-7f)
            {
                u1 = 1e-7f;
            }
            return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
        }

        private static ulong SplitMix64Step(ulong x)
        {
            unchecked // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                x += 0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
