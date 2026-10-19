// File:     src/player-database/PlayerRecord.cs
// Created:  2026-07-15
// Modified: 2026-10-19 (ERR-031-005: stable ids; the formula is genesis-only — v1.4)
//           Prior: 2026-08-08 (AR pass 12 L3: the draw-key spelling — v1.3)
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
        /// Career-unique, STABLE identifier. Genesis allocates <c>clubId * CLUB_SQUAD_SIZE + localIndex</c>
        /// (design doc KD-3), but that formula is a genesis allocation only, never an identity rule
        /// (ERR-031-005, owner decision October 19, 2026): a transfer re-homes the player under the
        /// same id, and every post-genesis id (a #28 regen) comes from <c>ProgressionEngine.NextPlayerId</c>,
        /// the career's single allocator. Nothing may derive a player's club from his id.
        /// <para>
        /// <b>A career needs more than KD-3 promises (ERR-041-019):</b> #41's armed occurrence draw is
        /// keyed on <c>(worldSeed, playerId, actionOrdinal = worldDay × RADIX + purpose)</c> with no club term, so ids must be GLOBALLY
        /// unique across every club a career carries — two players sharing an id would draw identical
        /// injury luck forever. The genesis formula plus the single monotonic post-genesis allocator
        /// satisfy that by construction (ERR-031-005); a future allocator (#42 youth intake) MUST draw
        /// from the same cursor, and <c>PlayerCareerStates</c> enforces
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
// | 1.4     | 2026-10-19 | —      | ERR-031-005: PlayerId doc — stable across transfers; the KD-3 formula is genesis allocation only; ProgressionEngine.NextPlayerId is the single post-genesis allocator; club is never derived from the id. |
#endregion
