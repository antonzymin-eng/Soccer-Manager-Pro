// File:     src/season-save/Club.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md) KD-2/KD-3/
//           KD-5/KD-9; Code Standards #20
// Purpose:  One club's bootstrap identity — id, display name, and the strength delta its roster was
//           shifted by. A read-only value type; the roster itself lives on the League.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// One club in a bootstrapped league (design KD-9). Identity only — the 25-player
    /// <c>PlayerDatabase.Squad</c> is resolved through <see cref="League.ResolveByClubId"/>.
    /// <para>
    /// <b>Not serialized.</b> The season blob persists <c>ClubId</c>s (in
    /// <see cref="SeasonState"/>); the name and the strength delta are bootstrap-side presentation and
    /// generation detail, re-derivable from the world seed. This type therefore has no codec and no
    /// format version.
    /// </para>
    /// </summary>
    public readonly struct Club
    {
        /// <summary>The club's stable identifier — the value <see cref="SeasonState"/> and every
        /// fixture/table row key on. Contiguous <c>0 .. N-1</c> in a bootstrapped league (KD-2).</summary>
        public readonly int ClubId;

        /// <summary>The club's display name, from <see cref="ClubNameCatalogue"/> entry
        /// <see cref="ClubId"/> (KD-3 — assigned by id, never drawn).</summary>
        public readonly string Name;

        /// <summary>
        /// The <c>[1,20]</c>-scale attribute shift applied to every player on this club's roster
        /// (KD-5). Negative for the weaker half of the league, positive for the stronger; the ramp
        /// spans <c>±LeagueStrengthSpread</c> over the seeded rank.
        /// </summary>
        public readonly int StrengthDelta;

        /// <summary>Constructs a club identity.</summary>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="clubId"/> is negative.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="name"/> is null or empty.</exception>
        public Club(int clubId, string name, int strengthDelta)
        {
            if (clubId < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(clubId), clubId, "ClubId must be non-negative.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentException("A club needs a name.", nameof(name));
            }

            ClubId = clubId;
            Name = name;
            StrengthDelta = strengthDelta;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (roadmap A3).                               |
#endregion
