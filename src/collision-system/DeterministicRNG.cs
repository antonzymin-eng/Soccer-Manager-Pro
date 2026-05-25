// File:     src/collision-system/DeterministicRNG.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Collision System #3 §2.6.4, FR-08, Code Standards #20
// Purpose:  xorshift128+ RNG seeded per-frame from match seed + frame number.
//           Stage 5+: replace with Fixed64 Math Library RNG (Spec #9).

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Deterministic xorshift128+ RNG for fall/stumble probability sampling.
    /// Value type — no heap allocation. Seeded via SplitMix64 to avoid all-zero state.
    /// Collision System #3 §2.6.4 / FR-08.
    /// Reference: Vigna, S. "An experimental exploration of Marsaglia's xorshift generators" (2016).
    /// </summary>
    public struct DeterministicRNG
    {
        private ulong _state0;
        private ulong _state1;

        /// <param name="seed">Per-frame seed. Use matchSeed ^ frameNumber ^ (frameNumber &lt;&lt; 32).</param>
        public DeterministicRNG(ulong seed)
        {
            _state0 = SplitMix64(seed);
            _state1 = SplitMix64(_state0);

            if (_state0 == 0 && _state1 == 0)
            {
                _state0 = 0x853C49E6748FEA9BUL;
                _state1 = 0xDA3E39CB94B95BDBUL;
            }
        }

        /// <summary>Returns the next float in [0, 1).</summary>
        public float NextFloat()
        {
            ulong s1 = _state0;
            ulong s0 = _state1;
            ulong result = s0 + s1;
            _state0 = s0;
            s1 ^= s1 << 23;
            _state1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);
            return (result >> 40) * (1.0f / (1UL << 24));
        }

        private static ulong SplitMix64(ulong x)
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
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
