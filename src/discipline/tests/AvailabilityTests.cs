// File:     src/discipline/tests/AvailabilityTests.cs
// Created:  2026-08-13
// Modified: 2026-08-15 (reviewed-findings pass — v1.3: the Spec line's §5 test-id list still named
//           T-DC-VIEW-001, withdrawn today (ERR-044-006) — this is the file whose own test for it
//           (FilterAvailable_LeavesTheSourceSquadUntouched, L4(a)) was deleted at v1.1. Removed the id
//           from the Spec line only; the v1.0 row naming the deleted test stays as history, unchanged.)
//           Prior: 2026-08-13 (#44 C1/C2 adversarial review round 5, L17 — DisciplineConstants.CardKindYellow
//           reference updated for the CARD_KIND_YELLOW rename — v1.2)
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.3 / FR-DC-008/009/010; ERR-044-003 (F5 vs #30 §2.3 F9 —
//           viability is #30's, #44 contributes removals only); §5 T-DC-VIEW-002, T-DC-BAN-004/005;
//           Code Standards #20
// Purpose:  Unit tests for Availability — the pure IsAvailable predicate, MarkSuspended's mask-owning
//           (non-additive) contract, FilterAvailable's same-instance pass-through, reduced-copy and
//           all-suspended-returns-null behaviour, and the deliberate non-viability-gate property
//           (ERR-044-003) that distinguishes #44's filter from a fail-loud one.

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
            rules.ApplyCard(5, Competition, DisciplineConstants.CardKindYellow);

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
        public void MarkSuspended_OwnsTheMask_OverwritesAPreSetFlagForAPlayerWhoIsNotSuspended()
        {
            // M3: MarkSuspended is NOT additive — it writes suspension status unconditionally for
            // every index, regardless of what the caller passed in. A pre-set `true` for a player who
            // is NOT suspended (e.g. stale data, or a caller mistakenly reusing a shared mask) must be
            // cleared, not preserved — the old additive-and-skip contract is exactly what let a
            // suspended player reach the pitch if a caller ever shared a mask across contributors.
            Squad squad = MakeSquad(0, 3);
            var rules = new DisciplineRules(new DisciplineState());
            rules.AddBan(squad.GetPlayer(1).PlayerId, Competition, 1);   // only index 1 is actually banned

            var removed = new[] { true, false, false };   // index 0 pre-set to true, but player 0 is NOT suspended

            int suspendedCount = Availability.MarkSuspended(squad, rules.State, Competition, removed);

            Assert.IsFalse(removed[0], "M3: a pre-set flag for a player who is NOT suspended must be cleared — this method owns the whole mask");
            Assert.IsTrue(removed[1], "the actually-suspended player must be marked");
            Assert.IsFalse(removed[2]);
            Assert.AreEqual(1, suspendedCount, "the return value is the count of suspended players, not a delta");
        }

        [Test]
        public void MarkSuspended_ReturnsTheFullSuspendedCount_EvenWhenTheMaskWasAlreadyAllTrue()
        {
            Squad squad = MakeSquad(0, 2);
            var rules = new DisciplineRules(new DisciplineState());
            rules.AddBan(squad.GetPlayer(0).PlayerId, Competition, 1);   // index 0 genuinely suspended, index 1 not

            var removed = new[] { true, true };   // both pre-set true by a stale/shared mask

            int suspendedCount = Availability.MarkSuspended(squad, rules.State, Competition, removed);

            Assert.IsTrue(removed[0], "genuinely suspended");
            Assert.IsFalse(removed[1], "M3: NOT suspended, so the pre-set true must be overwritten to false");
            Assert.AreEqual(1, suspendedCount, "the count reflects who is ACTUALLY suspended, independent of the input mask");
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

        // ── M2: every player suspended — no reduced copy is constructible ────────────

        [Test]
        public void FilterAvailable_EveryPlayerSuspended_ReturnsNull()
        {
            // Squad's own constructor refuses a zero-player squad, so FilterAvailable cannot return a
            // "reduced value copy" when nothing survives the reduction — it returns null instead of
            // letting a cross-assembly ArgumentException from Squad's constructor escape (M2). This is
            // FR-DC-009's own surface, not #44's production path: AvailabilityComposition consumes
            // MarkSuspended's mask directly and never reaches this case via Squad's constructor.
            Squad squad = MakeSquad(0, 3);
            var rules = new DisciplineRules(new DisciplineState());
            for (int i = 0; i < 3; i++)
            {
                rules.AddBan(squad.GetPlayer(i).PlayerId, Competition, 1);
            }

            Squad result = null;
            Assert.DoesNotThrow(
                () => result = Availability.FilterAvailable(squad, rules.State, Competition),
                "M2: an all-suspended squad must not let Squad's constructor throw uncaught.");
            Assert.IsNull(result, "M2: no reduced copy exists when every player is removed.");
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
// | 1.1     | 2026-08-13 | —      | AR fixes. M3: MarkSuspended tests rewritten for the mask-OWNING  |
// |         |            |        | (non-additive) contract — a pre-set flag for a NON-suspended     |
// |         |            |        | player must now be cleared, not preserved. M2: added the         |
// |         |            |        | all-suspended-returns-null case. L4(a): deleted                  |
// |         |            |        | FilterAvailable_LeavesTheSourceSquadUntouched — Squad is         |
// |         |            |        | immutable and exposes no mutator, so no implementation could     |
// |         |            |        | ever fail that assertion; it had no real failure mode.           |
// | 1.2     | 2026-08-13 | —      | AR round 5 fix (L17): DisciplineConstants.CARD_KIND_YELLOW        |
// |         |            |        | reference updated to CardKindYellow ([FIXED] -> [CROSS] rename); |
// |         |            |        | no behaviour change.                                              |
// | 1.3     | 2026-08-15 | —      | Reviewed-findings fix: the Spec line still listed T-DC-VIEW-001,  |
// |         |            |        | withdrawn today (ERR-044-006) — its own test                     |
// |         |            |        | (FilterAvailable_LeavesTheSourceSquadUntouched) was deleted at    |
// |         |            |        | v1.1 (L4(a)). Removed the id from the Spec line; the v1.0 row     |
// |         |            |        | naming the deleted test is correct AS HISTORY and is unchanged.  |
#endregion
