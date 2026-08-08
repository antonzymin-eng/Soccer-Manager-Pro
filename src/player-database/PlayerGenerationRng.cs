// File:     src/player-database/PlayerGenerationRng.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Squad/Player Data Layer #27 §3 / Player Progression #28 §3.3; Deterministic Simulation #16 (RNG); Code Standards #20
// Purpose:  Shared deterministic player-generation RNG helpers used by RosterGenerator (#27) and
//           RegenGenerator (#28) — the single source for the bounded-draw modulo mapping + its rationale,
//           so the two generators cannot drift out of sync.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// Shared deterministic-generation RNG helpers for the roster (#27) and regen (#28) generators. The
    /// bounded-draw modulo mapping is deliberately biased-but-accepted for player generation ONLY — it is
    /// intentionally not a general <c>DeterministicRngService</c> primitive, so a consumer that needs a
    /// uniform bounded draw is not tempted to reach for a biased one.
    /// </summary>
    public static class PlayerGenerationRng
    {
        /// <summary>
        /// Maps one reserved draw (at <paramref name="drawIndex"/> within the stream's open reservation)
        /// to a value in [0, <paramref name="bound"/>). <paramref name="bound"/> must be &gt; 0
        /// (caller-guaranteed — every generation call site uses a fixed positive catalogue length or span).
        /// The plain <c>value % bound</c> modulo bias is deliberately accepted: over a u64 draw the bias
        /// for the small bounds generation uses (≤ 32) is &lt; 2^-59, generation is not a statistically
        /// load-bearing surface, and the mapping is deterministic. Do NOT "fix" this with rejection
        /// sampling — a variable draw count per field would break the fixed per-player reservation budget
        /// both generators depend on.
        /// </summary>
        /// <exception cref="InvalidOperationException">The draw failed — corrupt reservation state (internal invariant).</exception>
        public static int DrawBounded(DeterministicRngService rng, int streamIndex, int drawIndex, int bound)
        {
            ushort err = rng.DrawReserved(streamIndex, drawIndex, out ulong value);
            if (err != 0)
            {
                throw new InvalidOperationException(
                    "PlayerGenerationRng.DrawBounded: draw failed — corrupt reservation state (internal invariant).");
            }
            return (int)(value % (ulong)bound);
        }

        /// <summary>Clamps <paramref name="value"/> to the inclusive range [<paramref name="min"/>, <paramref name="max"/>].</summary>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Extracted from RosterGenerator / RegenGenerator (adversarial- |
// |         |            |        | review Low: the bounded-draw mapping + its rationale were     |
// |         |            |        | duplicated across the two generators). Byte-identical logic.  |
#endregion
