// File:     src/discipline/tests/DisciplineStateTests.cs
// Created:  2026-08-13
// Modified: 2026-08-16 (reviewed-findings pass, L1 — v1.2)
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.2 (data structures) / §2.3 F2 / FR-DC-008 / FR-DC-012 /
//           FR-DC-017 / FR-DC-020/021; §5 T-DC-SAV-002 / T-DC-DET-001; Code Standards #20
// Purpose:  Unit tests for DisciplineState — the canonical ascending (PlayerId, CompetitionId) order,
//           binary-search lookup, the FR-DC-017 empty-row drop on Upsert, FromEntries' three
//           restore-door refusals (non-ascending, duplicate, all-zero) plus its copy-not-borrow rule,
//           and EntryFor's negative-playerId absent case (L1).

using System;

using NUnit.Framework;

namespace TacticalDirector.Discipline.Tests
{
    /// <summary>
    /// Tests for <see cref="DisciplineState"/>. <see cref="DisciplineState.Upsert"/> and
    /// <see cref="DisciplineState.Remove"/> are <c>internal</c> — reachable here only because
    /// <c>AssemblyInfo.cs</c> grants this test assembly <c>InternalsVisibleTo</c>.
    /// </summary>
    [TestFixture]
    internal sealed class DisciplineStateTests
    {
        private static DisciplineEntry Row(int playerId, int competitionId, int yellows, int ban) =>
            new DisciplineEntry(playerId, competitionId, yellows, ban);

        // ── Canonical order ─────────────────────────────────────────────────────────

        [Test]
        public void Upsert_MaintainsCanonicalAscendingOrder_RegardlessOfInsertOrder()
        {
            var state = new DisciplineState();

            // Deliberately scrambled: descending player id, then a competition-id tie-break case.
            state.Upsert(Row(30, 0, 1, 0));
            state.Upsert(Row(10, 1, 2, 0));
            state.Upsert(Row(10, 0, 3, 0));
            state.Upsert(Row(20, 0, 4, 0));

            Assert.AreEqual(4, state.Count);
            // Canonical order is (PlayerId, CompetitionId) ascending: (10,0) < (10,1) < (20,0) < (30,0).
            Assert.AreEqual(10, state.EntryAt(0).PlayerId);
            Assert.AreEqual(0, state.EntryAt(0).CompetitionId);
            Assert.AreEqual(10, state.EntryAt(1).PlayerId);
            Assert.AreEqual(1, state.EntryAt(1).CompetitionId);
            Assert.AreEqual(20, state.EntryAt(2).PlayerId);
            Assert.AreEqual(30, state.EntryAt(3).PlayerId);
        }

        [Test]
        public void Upsert_OverwritesAnExistingRow_RatherThanInsertingASecondOne()
        {
            var state = new DisciplineState();
            state.Upsert(Row(5, 0, 1, 0));
            state.Upsert(Row(5, 0, 2, 0));

            Assert.AreEqual(1, state.Count, "a second Upsert at the same key must overwrite, not append");
            Assert.AreEqual(2, state.EntryFor(5, 0).Yellows);
        }

        [Test]
        public void BinarySearchLookup_FindsEveryInsertedKey()
        {
            var state = new DisciplineState();
            var keys = new (int player, int comp)[]
            {
                (100, 0), (1, 0), (50, 1), (50, 0), (2, 5), (999, 0), (3, 3),
            };
            foreach ((int player, int comp) in keys)
            {
                state.Upsert(Row(player, comp, 1, 0));
            }

            foreach ((int player, int comp) in keys)
            {
                Assert.IsTrue(state.HasEntry(player, comp), $"({player},{comp}) must be found");
                Assert.AreEqual(1, state.EntryFor(player, comp).Yellows);
            }
        }

        [Test]
        public void EntryFor_AbsentKey_ReturnsACleanZeroRow()
        {
            var state = new DisciplineState();
            state.Upsert(Row(1, 0, 3, 2));

            DisciplineEntry absent = state.EntryFor(999, 0);

            Assert.AreEqual(999, absent.PlayerId);
            Assert.AreEqual(0, absent.CompetitionId);
            Assert.AreEqual(0, absent.Yellows);
            Assert.AreEqual(0, absent.BanMatchesRemaining);
            Assert.IsTrue(absent.IsEmpty);
        }

        [Test]
        public void HasEntry_DistinguishesAbsentFromCarried()
        {
            var state = new DisciplineState();
            state.Upsert(Row(1, 0, 1, 0));

            Assert.IsTrue(state.HasEntry(1, 0));
            Assert.IsFalse(state.HasEntry(1, 1), "same player, different competition — no row exists there");
            Assert.IsFalse(state.HasEntry(2, 0), "a different player entirely");
        }

        [Test]
        public void EntryFor_NegativePlayerId_ReturnsTheZeroRow_WithoutThrowing()
        {
            // L1: EntryFor's doc says the absent case is not an error, and no row can ever exist for a
            // negative playerId (the validating constructor refuses one on every write path) — so this
            // must not route through that constructor and throw ArgumentOutOfRangeException.
            var state = new DisciplineState();

            DisciplineEntry entry = state.EntryFor(-1, 0);

            Assert.AreEqual(0, entry.Yellows);
            Assert.AreEqual(0, entry.BanMatchesRemaining);
            Assert.IsTrue(entry.IsEmpty);
            Assert.IsFalse(entry.IsSuspended);
        }

        [Test]
        public void HasEntry_NegativePlayerId_ReturnsFalse()
        {
            // L1: must agree with EntryFor(-1, 0) not throwing — both read a negative key as absent.
            var state = new DisciplineState();

            Assert.IsFalse(state.HasEntry(-1, 0));
        }

        // ── FR-DC-017: Upsert with an empty entry removes the row ────────────────────

        [Test]
        public void Upsert_WithAnEmptyEntry_RemovesTheExistingRow()
        {
            var state = new DisciplineState();
            state.Upsert(Row(7, 0, 3, 1));
            Assert.IsTrue(state.HasEntry(7, 0));

            state.Upsert(Row(7, 0, 0, 0));   // (0,0) — the canonical-minimality drop

            Assert.IsFalse(state.HasEntry(7, 0), "FR-DC-017: an all-zero row must not be stored");
            Assert.AreEqual(0, state.Count);
        }

        [Test]
        public void Upsert_WithAnEmptyEntryForAnAbsentKey_IsANoOp()
        {
            var state = new DisciplineState();
            state.Upsert(Row(1, 0, 5, 0));

            state.Upsert(Row(999, 0, 0, 0));   // never existed — dropping it must not disturb anything

            Assert.AreEqual(1, state.Count);
            Assert.IsTrue(state.HasEntry(1, 0));
        }

        // ── FromEntries ────────────────────────────────────────────────────────────

        [Test]
        public void FromEntries_AcceptsAStrictlyAscendingArray()
        {
            var entries = new[]
            {
                Row(1, 0, 1, 0),
                Row(1, 1, 0, 2),
                Row(2, 0, 3, 0),
            };

            DisciplineState state = DisciplineState.FromEntries(entries);

            Assert.AreEqual(3, state.Count);
            Assert.AreEqual(1, state.EntryAt(0).PlayerId);
            Assert.AreEqual(0, state.EntryAt(0).CompetitionId);
            Assert.AreEqual(1, state.EntryAt(1).PlayerId);
            Assert.AreEqual(1, state.EntryAt(1).CompetitionId);
            Assert.AreEqual(2, state.EntryAt(2).PlayerId);
        }

        [Test]
        public void FromEntries_RefusesNonAscendingOrder()
        {
            var entries = new[]
            {
                Row(5, 0, 1, 0),
                Row(3, 0, 1, 0),   // player id goes backward
            };

            Assert.Throws<ArgumentException>(() => DisciplineState.FromEntries(entries));
        }

        [Test]
        public void FromEntries_RefusesDuplicateKeys()
        {
            var entries = new[]
            {
                Row(5, 0, 1, 0),
                Row(5, 0, 2, 0),   // same (PlayerId, CompetitionId) twice
            };

            Assert.Throws<ArgumentException>(() => DisciplineState.FromEntries(entries));
        }

        [Test]
        public void FromEntries_RefusesAnAllZeroRow()
        {
            var entries = new[]
            {
                Row(5, 0, 0, 0),   // (0,0) must never be stored — FR-DC-017
            };

            Assert.Throws<ArgumentException>(() => DisciplineState.FromEntries(entries));
        }

        [Test]
        public void FromEntries_RefusesNull()
        {
            Assert.Throws<ArgumentNullException>(() => DisciplineState.FromEntries(null));
        }

        [Test]
        public void FromEntries_CopiesTheArray_CallerMutationDoesNotReachTheState()
        {
            // L21: this cannot fail for any implementation FromEntries could plausibly take. _entries
            // is a List<DisciplineEntry> over a readonly struct, and FromEntries populates it via
            // copy.Add(entry) reading a VALUE out of the caller's array element-by-element — there is
            // no code shape reachable from this signature that aliases the caller's array instead
            // (List<T> never borrows the array backing it). Kept as documentation of that field/method
            // choice, not as a guard against a reachable failure — do not read a pass here as locking
            // anything.
            var entries = new[] { Row(1, 0, 3, 0) };

            DisciplineState state = DisciplineState.FromEntries(entries);
            entries[0] = Row(1, 0, 99, 0);   // mutate the caller's array AFTER construction

            Assert.AreEqual(3, state.EntryFor(1, 0).Yellows,
                "DisciplineState.FromEntries must copy — a caller holding the source array must not " +
                "become a second writer of the state.");
        }

        // ── EntryAt bounds ─────────────────────────────────────────────────────────

        [Test]
        public void EntryAt_NegativeIndex_Throws()
        {
            var state = new DisciplineState();
            state.Upsert(Row(1, 0, 1, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => state.EntryAt(-1));
        }

        [Test]
        public void EntryAt_IndexEqualToCount_Throws()
        {
            var state = new DisciplineState();
            state.Upsert(Row(1, 0, 1, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => state.EntryAt(1));
        }

        [Test]
        public void EntryAt_OnEmptyState_Throws()
        {
            var state = new DisciplineState();

            Assert.Throws<ArgumentOutOfRangeException>(() => state.EntryAt(0));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial suite (#44 T-phase tests): canonical order across        |
// |         |            |        | arbitrary insert orders, binary-search lookup, EntryFor's clean  |
// |         |            |        | zero row, the FR-DC-017 Upsert drop, and FromEntries' three       |
// |         |            |        | restore-door refusals plus its copy-not-borrow guarantee.        |
// | 1.1     | 2026-08-15 | —      | Reviewed-findings fix (L21): FromEntries_CopiesTheArray_          |
// |         |            |        | CallerMutationDoesNotReachTheState cannot fail for any            |
// |         |            |        | implementation this signature admits — List<DisciplineEntry>      |
// |         |            |        | populated via copy.Add(entry) always reads a VALUE, never aliases |
// |         |            |        | the caller's array. Annotated as documentation of the field/      |
// |         |            |        | method choice rather than a reachable-failure guard; not deleted, |
// |         |            |        | assertion unchanged.                                              |
// | 1.2     | 2026-08-16 | —      | Reviewed-findings fix (L1): two new cases lock                    |
// |         |            |        | DisciplineState.EntryFor(-1, 0) returning the zero row without    |
// |         |            |        | throwing, and HasEntry(-1, 0) agreeing at false — the pairing the |
// |         |            |        | production fix (DisciplineState.cs v1.1) restores.                |
#endregion
