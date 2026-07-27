// File:     src/match-viewer/Scoreline.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1,
//           Code Standards #20
// Purpose:  A match score as one value, so the two goal counts cannot be passed the wrong way round.

using System;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// Home and away goals.
    ///
    /// <para>This exists to make a swap impossible rather than merely unlikely: as two loose <c>int</c>
    /// parameters in a wide constructor, <c>homeScore</c> and <c>awayScore</c> are transposable with no
    /// compile error and no test failure until someone reads a scoreboard with the sides reversed. As
    /// one parameter there is nothing to transpose.</para>
    ///
    /// <para><c>default(Scoreline)</c> is 0–0, which is a genuine state (kickoff) — unlike the restart
    /// banner, this type needs no derived-from-a-discriminator trick.</para>
    /// </summary>
    public readonly struct Scoreline
    {
        /// <summary>Home (team 0) goals.</summary>
        public readonly int Home;

        /// <summary>Away (team 1) goals.</summary>
        public readonly int Away;

        /// <summary>Constructs a scoreline. Goal counts cannot be negative.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Either count is negative.</exception>
        public Scoreline(int home, int away)
        {
            if (home < 0) { throw new ArgumentOutOfRangeException(nameof(home), home, "home goals cannot be negative."); }
            if (away < 0) { throw new ArgumentOutOfRangeException(nameof(away), away, "away goals cannot be negative."); }

            Home = home;
            Away = away;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P1 AR-1 M-6): the score as one value, so the |
// |         |            |        | two counts cannot be transposed in a wide frame constructor.   |
// |         |            |        | Non-negativity is now enforced by the type rather than         |
// |         |            |        | re-checked by each consumer.                                   |
#endregion
