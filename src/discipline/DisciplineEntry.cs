// File:     src/discipline/DisciplineEntry.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.2 (data structures) / FR-DC-012 / FR-DC-017 / FR-DC-020;
//           Code Standards #20
// Purpose:  One player's discipline tally within one competition — the (PlayerId, CompetitionId) →
//           (Yellows, BanMatchesRemaining) row DisciplineState stores and DisciplineSaveCodec writes.

using System;

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// One <c>(PlayerId, CompetitionId)</c> discipline row: the yellows accumulated toward the next
    /// accumulation ban, and the club fixtures still to be served (#44 §2.2).
    /// <para>
    /// <b>All integer</b> (FR-DC-020) and immutable — every mutation produces a new row, so no holder
    /// of an entry can become a second writer of <see cref="DisciplineState"/>.
    /// </para>
    /// <para>
    /// <b>An all-zero row is never stored</b> (FR-DC-017's canonical-minimality rule): a
    /// <c>(0, 0)</c> entry and an absent entry would otherwise both be encodable states for the same
    /// situation, and two equivalent runs would serialize different bytes. <see cref="IsEmpty"/> is
    /// the predicate <see cref="DisciplineState"/> drops on, immediately and wherever it arises — at
    /// the season-boundary sweep or mid-season, the tick a ban is served out with no residual yellows.
    /// </para>
    /// </summary>
    public readonly struct DisciplineEntry : IEquatable<DisciplineEntry>
    {
        /// <summary>The player this row belongs to. Globally unique (#27 FR-SQ-010, ERR-027-004).</summary>
        public readonly int PlayerId;

        /// <summary>
        /// The competition partition (FR-DC-012). <see cref="DisciplineConstants.LEAGUE_COMPETITION_KEY"/>
        /// at minimal; a plain <c>int</c> so #44 needs no #43 reference.
        /// </summary>
        public readonly int CompetitionId;

        /// <summary>Yellows accumulated toward the next accumulation ban. Never negative; always below the threshold after a crossing (§3.2's residual rule).</summary>
        public readonly int Yellows;

        /// <summary>Club fixtures still to be served. Never negative. Carries across <c>RollToNextSeason</c> (FR-DC-017).</summary>
        public readonly int BanMatchesRemaining;

        /// <summary>Builds a row.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="yellows"/> or
        /// <paramref name="banMatchesRemaining"/> is negative — a negative tally is a counting bug, and
        /// the codec refuses one on the way back in (F3), so it must never be constructible either.</exception>
        public DisciplineEntry(int playerId, int competitionId, int yellows, int banMatchesRemaining)
        {
            if (yellows < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(yellows), yellows,
                    "DisciplineEntry: Yellows must be >= 0 (FR-DC-020 / F3) — a negative tally is a counting bug.");
            }
            if (banMatchesRemaining < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(banMatchesRemaining), banMatchesRemaining,
                    "DisciplineEntry: BanMatchesRemaining must be >= 0 (FR-DC-020 / F3) — serving past zero is a counting bug.");
            }

            PlayerId = playerId;
            CompetitionId = competitionId;
            Yellows = yellows;
            BanMatchesRemaining = banMatchesRemaining;
        }

        /// <summary>
        /// True when this row carries nothing — the state FR-DC-017 requires be dropped rather than
        /// stored, so that an absent entry is the ONE representation of "this player is clean".
        /// </summary>
        public bool IsEmpty => Yellows == 0 && BanMatchesRemaining == 0;

        /// <summary>True while the player is suspended (FR-DC-008): a pure read, no threshold arithmetic.</summary>
        public bool IsSuspended => BanMatchesRemaining > 0;

        /// <summary>Value equality across all four fields.</summary>
        public bool Equals(DisciplineEntry other) =>
            PlayerId == other.PlayerId
            && CompetitionId == other.CompetitionId
            && Yellows == other.Yellows
            && BanMatchesRemaining == other.BanMatchesRemaining;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DisciplineEntry other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerId;
                hash = (hash * 397) ^ CompetitionId;
                hash = (hash * 397) ^ Yellows;
                hash = (hash * 397) ^ BanMatchesRemaining;
                return hash;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T0, roadmap C1): the immutable       |
// |         |            |        | (PlayerId, CompetitionId) tally row, with the FR-DC-017          |
// |         |            |        | canonical-minimality predicate and the F3 non-negative gates     |
// |         |            |        | enforced at construction, not only at decode.                    |
#endregion
