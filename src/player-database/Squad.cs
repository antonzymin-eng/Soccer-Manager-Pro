// File:     src/player-database/Squad.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §3, KD-6
// Purpose:  One club's roster. Plain class over an array — this layer runs at club-setup time,
//           never per-tick, so the zero-allocation game-loop rules do not apply (KD-6, same
//           carve-out as TeamTacticFileLoader/PlayerTacticFileLoader).

using System;

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// One club's roster of up to <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/> players.
    /// Design doc §3.
    /// </summary>
    public sealed class Squad
    {
        private readonly PlayerRecord[] _players;

        /// <summary>Caller-assigned stable club identifier. NOT a match <c>teamId</c> (design doc KD-3).</summary>
        public int ClubId { get; }

        /// <summary>Number of players on this roster.</summary>
        public int Count => _players.Length;

        /// <summary>
        /// Builds a squad. Snapshot-copies <paramref name="players"/> so post-construction mutation
        /// of the caller's array cannot reach this instance (the FR-TP-014 / T0-AR-1-M-1 precedent).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="players"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="players"/> is empty or exceeds <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/>.</exception>
        public Squad(int clubId, PlayerRecord[] players)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }
            if (players.Length == 0 || players.Length > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new ArgumentException(
                    $"Squad must have between 1 and {PlayerDatabaseConstants.CLUB_SQUAD_SIZE} players; got {players.Length}.",
                    nameof(players));
            }

            ClubId = clubId;
            _players = new PlayerRecord[players.Length];
            Array.Copy(players, _players, players.Length);
        }

        /// <summary>Returns the player at roster index <paramref name="index"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of [0, Count).</exception>
        public PlayerRecord GetPlayer(int index)
        {
            if ((uint)index >= (uint)_players.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _players[index];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                    |
// | 1.0     | 2026-07-15 | —      | Initial implementation.  |
#endregion
