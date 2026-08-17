// File:     src/player-database/PlayerRecord.cs
// Created:  2026-07-15
// Modified: 2026-08-08 (AR pass 12 L3: the draw-key spelling — v1.3)
// Author:   —
// Spec:     Squad / Player Data Layer #27 §2.2.3 / FR-SQ-008/010 (as amended by ERR-027-004);
//           its pre-promotion design supplement §3 is history only (the spec folder wins)
// Purpose:  One player's identity + attributes. PlayerId is club-scoped (KD-3) and globally unique
//           across a career's clubs (ERR-027-004 / ERR-041-019); not the match-scoped agent roster
//           index MatchEngine assigns per match.

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// One player: a stable club-scoped identity plus canonical attributes. Design doc §3.
    /// </summary>
    public struct PlayerRecord
    {
        /// <summary>
        /// Club-scoped unique identifier (design doc KD-3: <c>clubId * CLUB_SQUAD_SIZE + localIndex</c>).
        /// <para>
        /// <b>A career needs more than KD-3 promises (ERR-041-019):</b> #41's armed occurrence draw is
        /// keyed on <c>(worldSeed, playerId, actionOrdinal = worldDay × RADIX + purpose)</c> with no club term, so ids must be GLOBALLY
        /// unique across every club a career carries — two players sharing an id would draw identical
        /// injury luck forever. Today's KD-3 formula happens to satisfy that; any future allocator
        /// (#42 youth intake, #31 transfers) MUST preserve it, and <c>PlayerCareerStates</c> enforces
        /// it fail-loud at construction and roster sync.
        /// </para>
        /// </summary>
        public int PlayerId;

        /// <summary>Given name.</summary>
        public string FirstName;

        /// <summary>Family name.</summary>
        public string LastName;

        /// <summary>Age in years.</summary>
        public int Age;

        /// <summary>Coarse squad-management position.</summary>
        public PlayerPosition Position;

        /// <summary>Canonical player attributes.</summary>
        public PlayerAttributes Attributes;

        /// <summary>Creates an identity record: mid-range attributes, Midfielder, age 25, "Player {playerId}".</summary>
        public static PlayerRecord CreateDefault(int playerId)
        {
            return new PlayerRecord
            {
                PlayerId = playerId,
                FirstName = "Player",
                LastName = playerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Age = 25,
                Position = PlayerPosition.Midfielder,
                Attributes = PlayerAttributes.CreateDefault()
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                    |
// | 1.0     | 2026-07-15 | —      | Initial implementation.  |
// | 1.1     | 2026-08-08 | —      | Balance-pass AR pass 3 (H1, doc only — ERR-041-019): PlayerId's   |
// |         |            |        | summary states the career-level GLOBAL-uniqueness requirement     |
// |         |            |        | #41's club-less draw key imposes on top of KD-3's club scope,     |
// |         |            |        | and where it is enforced. No code change.                         |
// | 1.2     | 2026-08-08 | —      | AR pass 4 (M3, doc only — ERR-027-004): the header Purpose line   |
// |         |            |        | still said club-scoped full stop; now cites the back-prop that    |
// |         |            |        | landed the requirement in #27 FR-SQ-010 itself, where the future  |
// |         |            |        | allocators will read it.                                          |
// | 1.3     | 2026-08-08 | —      | Balance-pass AR pass 12 (L3, doc): the ERR-041-019 key spelled     |
// |         |            |        | per #41 SS3.1.1 — this doc had named three components in an order  |
// |         |            |        | matching neither the code nor the spec.                            |
#endregion
