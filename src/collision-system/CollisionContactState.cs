// File:     src/collision-system/CollisionContactState.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Collision System #3 §3.4.1; Match Engine design note §5.Z.13; Code Standards #20
// Purpose:  Typed carrier for the collision system's only cross-tick state — the set of agent pairs
//           in contact as of the last frame, which the contact-ONSET emit gate compares against.
//           Follows the PressingTickState convention: the subsystem returns a typed value carrier
//           and the match-engine composition root owns 100% of the byte layout.

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// The 256-bit <see cref="CollisionPairBitfield"/> pair set, as four words. Every bit pattern is
    /// structurally valid (bits above the reachable pair index simply never match a query), so a
    /// restore needs no validation gate.
    /// </summary>
    public readonly struct CollisionContactState
    {
        /// <summary>Pair-index bits 0-63.</summary>
        public readonly ulong Word0;

        /// <summary>Pair-index bits 64-127.</summary>
        public readonly ulong Word1;

        /// <summary>Pair-index bits 128-191.</summary>
        public readonly ulong Word2;

        /// <summary>Pair-index bits 192-255.</summary>
        public readonly ulong Word3;

        /// <summary>Constructs the carrier from the four backing words, in ascending bit order.</summary>
        public CollisionContactState(ulong word0, ulong word1, ulong word2, ulong word3)
        {
            Word0 = word0;
            Word1 = word1;
            Word2 = word2;
            Word3 = word3;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial. Carries the contact-onset pair set introduced with the    |
// |         |            |        | §5.Z.13 contact-rate fix (one event per contact, not per tick).    |
#endregion
