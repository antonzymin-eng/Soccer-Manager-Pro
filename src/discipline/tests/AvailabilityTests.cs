// File:     src/discipline/tests/AvailabilityTests.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.3 / FR-DC-008/009/010; ERR-044-003 (F5 vs #30 §2.3 F9 —
//           viability is #30's, #44 contributes removals only); §5 T-DC-VIEW-001/002, T-DC-BAN-004/005;
//           Code Standards #20
// Purpose:  Unit tests for Availability — the pure IsAvailable predicate, the additive MarkSuspended
//           mask contribution, FilterAvailable's same-instance pass-through and reduced-copy behaviour,
//           and the deliberate non-viability-gate property (ERR-044-003) that distinguishes #44's
//           filter from a fail-loud one.

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Discipline.Tests
{
    /// <summary>Tests for <see cref="Availability"/>.</summary>
    [TestFixture]
    internal sealed class AvailabilityTests
    {
        private const int Competition = DisciplineConstants.LEAGUE_COMPETITION_KEY;

        private static Squad MakeSquad(int clubId, int count)
        {
            var players = new PlayerRecord[count];
            for (int i = 0; i < count; i++)
            {
                players[i] = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + i);
            }
            return new Squad(clubId, players);
        }

        // ── IsAvailable ────────────────────────────────────────────────────────────

        [Test]
        public void IsAvailable_AbsentRow_IsTrue()
        {
            var state = new DisciplineState();
            Assert.IsTrue(Availability.IsAvailable(state, 5, Competition));
        }

        [Test]
        public void IsAvailable_YellowsButNoBan_IsTrue()
        {
            var rules = new DisciplineRules(new DisciplineState());
            rules.ApplyCard(5, Competition, DisciplineConstants.CARD_KIND_YELLOW);

            Assert.IsTrue(Availability.IsAvailable(rules.State, 5, Competition),
                "yellows alone (below the ban threshold) must not restrict availability");
        }

        [Test]
        public void IsAvailable_ActiveBan_IsFalse()
        {
            var rules = new DisciplineRules(new DisciplineState());
            rules.AddBan(5, Competition, 1);

            Assert.IsFalse(Availability.IsAvailable(rules.State, 5, Competition));
        }

        [Test]
        public void IsAvailable_NullState_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Availability.IsAvailable(null, 5, Competition));
        }

        // ── MarkSuspended ──────────────────────────────────────────────────────────

        [Test]
        public void MarkSuspended_IsAdditive_NeverClearsAFlagAnotherContributorSet()
        {
            Squad squad = MakeSquad(0, 3);
            var rules = new DisciplineRules(new DisciplineState());
            rules.AddBan(squad.GetPlayer(1).PlayerId, Competition, 1);   // only index 1 is actually banned

            var removed = new[] { true, false, false };   // index 0 pre-marked by a DIFFERENT contributor (e.g. #41)

            int newlySet = Availability.MarkSuspended(squad, rules.State, Competition, removed);

            Assert.IsTrue(removed[0], "a flag set by another contributor must survive untouched");
            Assert.IsTrue(removed[1], "the actually-suspended player must be marked");
            Assert.IsFalse(removed[2]);
            Assert.AreEqual(1, newlySet, "only index 1 was NEWLY set by this call");
        }

        [Test]
        public void MarkSuspended_ReturnsZero_WhenEveryoneIsAlreadyMarked()
        {
            Squad squad = MakeSquad(0, 2);
            var rules = new DisciplineRules(new DisciplineState());
            rules.AddBan(squad.GetPlayer(0).PlayerId, Competition, 1);

            var removed = new[] { true, true };   // both already marked by prior contributors

            int newlySet = Availability.MarkSuspended(squad, rules.State, Competition, removed);

            Assert.AreEqual(0, newlySet, "nothing NEW was set even though index 0 is genuinely suspended");
        }

        [Test]
        public void MarkSuspended_MaskLengthMismatch_Throws()
        {
            Squad squad = MakeSquad(0, 3);
            var state = new DisciplineState();

            Assert.Throws<ArgumentException>(
                () => Availability.MarkSuspended(squad, state, Competition, new bool[2]));
        }

        [Test]
        public void MarkSuspended_NullArguments_Throw()
        {
            Squad squad = MakeSquad(0, 2);
            var state = new DisciplineState();
            var mask = new bool[2];

            Assert.Throws<ArgumentNullException>(() => Availability.MarkSuspended(null, state, Competition, mask));
            Assert.Throws<ArgumentNullException>(() => Availability.MarkSuspended(squad, null, Competition, mask));
            Assert.Throws<ArgumentNullException>(() => Availability.MarkSuspended(squad, state, Competition, null));
        }

        // ── FilterAvailable ────────────────────────────────────────────────────────

        [Test]
        public void FilterAvailable_NobodySuspended_ReturnsTheSameInstance()
        {
            Squad squad = MakeSquad(0, 5);
            var state = new DisciplineState();

            Squad result = Availability.FilterAvailable(squad, state, Competition);

            Assert.That(result, Is.SameAs(squad), "FR-DC-009: pass-through with no active ban must be the SAME instance");
        }

        [Test]
        public void FilterAvailable_SomeoneSuspended_ReturnsAReducedCopy_PreservingClubIdAndOrder()
        {
            Squad squad = MakeSquad(3, 5);
            var rules = new DisciplineRules(new DisciplineState());
            int bannedId = squad.GetPlayer(2).PlayerId;
            rules.AddBan(bannedId, Competition, 1);

            Squad result = Availability.FilterAvailable(squad, rules.State, Competition);

            Assert.That(result, Is.Not.SameAs(squad), "a reduction must be a distinct copy");
            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(3, result.ClubId, "ClubId must be preserved");

            // Roster order preserved: original indices {0,1,3,4} survive in that relative order.
            Assert.AreEqual(squad.GetPlayer(0).PlayerId, result.GetPlayer(0).PlayerId);
            Assert.AreEqual(squad.GetPlayer(1).PlayerId, result.GetPlayer(1).PlayerId);
            Assert.AreEqual(squad.GetPlayer(3).PlayerId, result.GetPlayer(2).PlayerId);
            Assert.AreEqual(squad.GetPlayer(4).PlayerId, result.GetPlayer(3).PlayerId);
            for (int i = 0; i < result.Count; i++)
            {
                Assert.AreNotEqual(bannedId, result.GetPlayer(i).PlayerId, "the banned player must not appear");
            }
        }

        [Test]
        public void FilterAvailable_NullArguments_Throw()
        {
            Squad squad = MakeSquad(0, 2);
            var state = new DisciplineState();

            Assert.Throws<ArgumentNullException>(() => Availability.FilterAvailable(null, state, Competition));
            Assert.Throws<ArgumentNullException>(() => Availability.FilterAvailable(squad, null, Competition));
        }

        // ── ERR-044-003: NO viability gate — the non-tautological, deliberate property ──

        [Test]
        public void FilterAvailable_ReducingBelowEighteen_DoesNotThrow_AndDoesNotBackFill()
        {
            // #44 §2.3's own F5 would have this fail loud below 18 (ConfigureSquads' floor). ERR-044-003
            // records that #30 §3.4/§2.3 F9 — approved AFTER #44 was written — holds authority instead:
            // #44 contributes REMOVALS ONLY and leaves viability to the composed seam one layer up, which
            // alone can back-fill from the whole-squad picture. So this method must neither throw NOR
            // pad the result back up to 18 — it just returns whatever is left, however small.
            Squad squad = MakeSquad(0, 20);
            var rules = new DisciplineRules(new DisciplineState());
            // Suspend 15 of 20 — leaves 5, deep below the 18-player viability floor.
            for (int i = 0; i < 15; i++)
            {
                rules.AddBan(squad.GetPlayer(i).PlayerId, Competition, 1);
            }

            Squad result = null;
            Assert.DoesNotThrow(
                () => result = Availability.FilterAvailable(squad, rules.State, Competition),
                "ERR-044-003: #44's FilterAvailable must NOT fail loud below the viability floor — " +
                "that is #30's rule (§2.3 F9), not #44's F5, which is deliberately unimplemented here.");

            Assert.AreEqual(5, result.Count,
                "no back-fill either — the method returns exactly what remains, small or not.");
        }

        // ── T-DC-VIEW-001 class: the source Squad is left byte-identical ─────────────

        [Test]
        public void FilterAvailable_LeavesTheSourceSquadUntouched()
        {
            Squad squad = MakeSquad(0, 4);
            var rules = new DisciplineRules(new DisciplineState());
            int bannedId = squad.GetPlayer(1).PlayerId;
            rules.AddBan(bannedId, Competition, 1);

            var beforeIds = new int[squad.Count];
            for (int i = 0; i < squad.Count; i++)
            {
                beforeIds[i] = squad.GetPlayer(i).PlayerId;
            }

            Availability.FilterAvailable(squad, rules.State, Competition);

            for (int i = 0; i < squad.Count; i++)
            {
                Assert.AreEqual(beforeIds[i], squad.GetPlayer(i).PlayerId,
                    "the original squad — including the banned player — must still be fully readable");
            }
            Assert.AreEqual(4, squad.Count, "the source squad's own Count must not shrink");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial suite (#44 T-phase tests): IsAvailable's pure predicate, |
// |         |            |        | MarkSuspended's additive-mask contract, FilterAvailable's        |
// |         |            |        | same-instance pass-through / reduced-copy shape, the ERR-044-003 |
// |         |            |        | deliberate non-viability-gate property, and the T-DC-VIEW-001    |
// |         |            |        | source-untouched guarantee.                                      |
#endregion
