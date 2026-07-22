// File:     src/player-database/tests/SquadTests.cs
// Created:  2026-07-22
// Modified: 2026-07-22
// Author:   —
// Spec:     Squad/Player Data Layer Specification #27 §2.2.4 / §2.4 (F3), §5.2 (FR-SQ-009)
// Purpose:  Direct Squad-constructor coverage — the F3 null/empty/oversize guards and the
//           snapshot-copy invariant. Both production callers (RosterGenerator, SquadFileLoader)
//           pre-guard size, so the Squad ctor's own guards had no direct test (PASS-1 M-1).

using System;

using NUnit.Framework;

namespace TacticalDirector.PlayerDatabase.Tests
{
    [TestFixture]
    public sealed class SquadTests
    {
        private static PlayerRecord[] MakePlayers(int count)
        {
            var players = new PlayerRecord[count];
            for (int i = 0; i < count; i++)
            {
                players[i] = PlayerRecord.CreateDefault(i);
            }
            return players;
        }

        [Test]
        public void Constructor_NullPlayers_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new Squad(0, null));
        }

        [Test]
        public void Constructor_EmptyPlayers_ThrowsArgument()
        {
            Assert.Throws<ArgumentException>(() => new Squad(0, Array.Empty<PlayerRecord>()));
        }

        [Test]
        public void Constructor_ExceedsClubSquadSize_ThrowsArgument()
        {
            PlayerRecord[] tooMany = MakePlayers(PlayerDatabaseConstants.CLUB_SQUAD_SIZE + 1);
            Assert.Throws<ArgumentException>(() => new Squad(0, tooMany));
        }

        [Test]
        public void Constructor_AtClubSquadSize_Builds()
        {
            var squad = new Squad(0, MakePlayers(PlayerDatabaseConstants.CLUB_SQUAD_SIZE));
            Assert.AreEqual(PlayerDatabaseConstants.CLUB_SQUAD_SIZE, squad.Count);
        }

        [Test]
        public void Constructor_SnapshotsPlayers_PostConstructionMutationDoesNotReachInstance()
        {
            PlayerRecord[] players = MakePlayers(2);
            players[0].FirstName = "Original";
            var squad = new Squad(7, players);

            // Mutate the caller's array after construction — the snapshot copy must be unaffected.
            players[0].FirstName = "Mutated";

            Assert.AreEqual("Original", squad.GetPlayer(0).FirstName);
        }

        [Test]
        public void GetPlayer_OutOfRange_Throws()
        {
            var squad = new Squad(0, MakePlayers(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => squad.GetPlayer(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => squad.GetPlayer(3));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-22 | —      | PASS-1 v0.2: direct Squad-ctor F3 guards (null/empty/oversize) |
// |         |            |        | + at-cap build, snapshot-copy invariant, GetPlayer bounds.     |
#endregion
