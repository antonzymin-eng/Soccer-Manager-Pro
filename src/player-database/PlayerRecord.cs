// File:     src/player-database/PlayerRecord.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §3
// Purpose:  One player's identity + attributes. PlayerId is club-scoped (KD-3), not the match-scoped
//           agent roster index MatchEngine assigns per match.

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// One player: a stable club-scoped identity plus canonical attributes. Design doc §3.
    /// </summary>
    public struct PlayerRecord
    {
        /// <summary>Club-scoped unique identifier. Design doc KD-3: <c>clubId * CLUB_SQUAD_SIZE + localIndex</c>.</summary>
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
#endregion
