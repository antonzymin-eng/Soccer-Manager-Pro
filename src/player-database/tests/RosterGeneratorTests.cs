// File:     src/player-database/tests/RosterGeneratorTests.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §5
// Purpose:  RosterGenerator determinism, PlayerId club-scoping (KD-3), attribute bound/clamp
//           coverage, and the exact RNG-budget-per-player lock (FIELDS_PER_PLAYER).

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.PlayerDatabase.Tests
{
    [TestFixture]
    public sealed class RosterGeneratorTests
    {
        private const string SiteId = "player-database.roster-generation";

        private static DeterministicRngService NewRng(ulong seed, int clubId, out int streamIndex)
        {
            var rng = new DeterministicRngService(seed);
            streamIndex = rng.RegisterStream(SiteId, SubsystemOrdinals.PlayerDatabase, clubId, streamVersion: 1);
            return rng;
        }

        [Test]
        public void Generate_SameSeed_ProducesIdenticalSquad()
        {
            var rngA = NewRng(12345UL, clubId: 3, out int idxA);
            var rngB = NewRng(12345UL, clubId: 3, out int idxB);

            Squad squadA = RosterGenerator.Generate(rngA, idxA, clubId: 3, count: 10);
            Squad squadB = RosterGenerator.Generate(rngB, idxB, clubId: 3, count: 10);

            for (int i = 0; i < 10; i++)
            {
                PlayerRecord a = squadA.GetPlayer(i);
                PlayerRecord b = squadB.GetPlayer(i);
                Assert.AreEqual(a.PlayerId, b.PlayerId, $"player {i} PlayerId");
                Assert.AreEqual(a.FirstName, b.FirstName, $"player {i} FirstName");
                Assert.AreEqual(a.LastName, b.LastName, $"player {i} LastName");
                Assert.AreEqual(a.Age, b.Age, $"player {i} Age");
                Assert.AreEqual(a.Position, b.Position, $"player {i} Position");
                CollectionAssert.AreEqual(a.Attributes.ToArray(), b.Attributes.ToArray(), $"player {i} attributes");
                Assert.AreEqual(a.Attributes.WeakFootRating, b.Attributes.WeakFootRating, $"player {i} WeakFootRating");
            }
        }

        [Test]
        public void Generate_DifferentSeed_ProducesDifferentSquad()
        {
            var rngA = NewRng(1UL, clubId: 0, out int idxA);
            var rngB = NewRng(2UL, clubId: 0, out int idxB);

            Squad squadA = RosterGenerator.Generate(rngA, idxA, clubId: 0, count: 10);
            Squad squadB = RosterGenerator.Generate(rngB, idxB, clubId: 0, count: 10);

            bool anyDifference = false;
            for (int i = 0; i < 10 && !anyDifference; i++)
            {
                PlayerRecord a = squadA.GetPlayer(i);
                PlayerRecord b = squadB.GetPlayer(i);
                if (a.FirstName != b.FirstName || a.LastName != b.LastName || a.Age != b.Age
                    || a.Position != b.Position || !ArraysEqual(a.Attributes.ToArray(), b.Attributes.ToArray()))
                {
                    anyDifference = true;
                }
            }
            Assert.IsTrue(anyDifference, "two different seeds produced byte-identical squads.");
        }

        [Test]
        public void Generate_PlayerIds_AreClubScopedAndSequential()
        {
            var rng = NewRng(99UL, clubId: 2, out int idx);
            Squad squad = RosterGenerator.Generate(rng, idx, clubId: 2, count: 5);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(2 * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + i, squad.GetPlayer(i).PlayerId);
            }
        }

        [Test]
        public void Generate_PlayerIds_NoCollisionAcrossClubs()
        {
            var rngA = NewRng(1UL, clubId: 0, out int idxA);
            var rngB = NewRng(1UL, clubId: 1, out int idxB);

            Squad squadA = RosterGenerator.Generate(rngA, idxA, clubId: 0, count: PlayerDatabaseConstants.CLUB_SQUAD_SIZE);
            Squad squadB = RosterGenerator.Generate(rngB, idxB, clubId: 1, count: PlayerDatabaseConstants.CLUB_SQUAD_SIZE);

            for (int i = 0; i < squadA.Count; i++)
            {
                for (int j = 0; j < squadB.Count; j++)
                {
                    Assert.AreNotEqual(squadA.GetPlayer(i).PlayerId, squadB.GetPlayer(j).PlayerId);
                }
            }
        }

        [Test]
        public void Generate_AllGeneratedValues_WithinDeclaredBounds()
        {
            var rng = NewRng(7UL, clubId: 4, out int idx);
            Squad squad = RosterGenerator.Generate(rng, idx, clubId: 4, count: PlayerDatabaseConstants.CLUB_SQUAD_SIZE);

            for (int i = 0; i < squad.Count; i++)
            {
                PlayerRecord p = squad.GetPlayer(i);
                Assert.IsTrue(Enum.IsDefined(typeof(PlayerPosition), p.Position), $"player {i} position");
                Assert.GreaterOrEqual(p.Age, PlayerDatabaseConstants.AgeMin, $"player {i} age");
                Assert.LessOrEqual(p.Age, PlayerDatabaseConstants.AgeMax, $"player {i} age");
                Assert.GreaterOrEqual(p.Attributes.WeakFootRating, PlayerDatabaseConstants.WEAK_FOOT_MIN, $"player {i} weakFoot");
                Assert.LessOrEqual(p.Attributes.WeakFootRating, PlayerDatabaseConstants.WEAK_FOOT_MAX, $"player {i} weakFoot");

                int[] attrs = p.Attributes.ToArray();
                for (int a = 0; a < attrs.Length; a++)
                {
                    Assert.GreaterOrEqual(attrs[a], PlayerDatabaseConstants.ATTRIBUTE_MIN, $"player {i} attr {a}");
                    Assert.LessOrEqual(attrs[a], PlayerDatabaseConstants.ATTRIBUTE_MAX, $"player {i} attr {a}");
                }
            }
        }

        [Test]
        public void Generate_RngCursor_AdvancesByExactBudgetPerPlayer()
        {
            var rng = NewRng(42UL, clubId: 0, out int idx);
            RosterGenerator.Generate(rng, idx, clubId: 0, count: 6);

            ulong expectedCursor = 6UL * (ulong)PlayerDatabaseConstants.FIELDS_PER_PLAYER;
            Assert.AreEqual(expectedCursor, rng.GetStreamState(idx).RngCursor);
        }

        [Test]
        public void Generate_InvalidCount_Throws()
        {
            var rng = NewRng(1UL, clubId: 0, out int idx);
            Assert.Throws<ArgumentException>(() => RosterGenerator.Generate(rng, idx, 0, 0));
            Assert.Throws<ArgumentException>(() => RosterGenerator.Generate(rng, idx, 0, -1));
            Assert.Throws<ArgumentException>(() => RosterGenerator.Generate(rng, idx, 0, PlayerDatabaseConstants.CLUB_SQUAD_SIZE + 1));
        }

        [Test]
        public void Generate_NullRng_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RosterGenerator.Generate(null, 0, 0, 1));
        }

        private static bool ArraysEqual(int[] a, int[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                    |
// | 1.0     | 2026-07-15 | —      | Initial implementation.  |
#endregion
