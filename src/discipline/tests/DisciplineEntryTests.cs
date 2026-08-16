// File:     src/discipline/tests/DisciplineEntryTests.cs
// Created:  2026-08-15
// Modified: 2026-08-16 (reviewed findings pass, L2 — v1.1: six new tests for
//           YellowsPlusOne(int)/BanMatchesPlus(int,int) — the overflow-named throw at the guard's
//           boundary, the exact-boundary non-throw one step inside it, ordinary sums, and the
//           zero-matches never-throws case.)
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.2 (data structures) / §2.3 F2 / FR-DC-012 / FR-DC-017 /
//           FR-DC-020; Code Standards #20
// Purpose:  Unit tests for DisciplineEntry — the three constructor guards (playerId < 0, yellows < 0,
//           banMatchesRemaining < 0), IsEmpty/IsSuspended, value equality, and the guarded
//           YellowsPlusOne/BanMatchesPlus accumulator arithmetic. Reviewed finding M25: DisciplineEntry
//           had no test file at all, and mutation-verified that neutering all three constructor guards
//           left the whole 105-test discipline suite green.

using System;

using NUnit.Framework;

namespace TacticalDirector.Discipline.Tests
{
    /// <summary>Tests for <see cref="DisciplineEntry"/>.</summary>
    [TestFixture]
    internal sealed class DisciplineEntryTests
    {
        // ── Constructor guards (M25) ──────────────────────────────────────────────
        //
        // Mutation-verified: replacing all three constructor guards with `if (false)` left the whole
        // 105-test discipline suite green — nothing exercised any of the three directly.

        [Test]
        public void Constructor_NegativePlayerId_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DisciplineEntry(-1, 0, 0, 0));
        }

        [Test]
        public void Constructor_NegativeYellows_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DisciplineEntry(1, 0, -1, 0));
        }

        [Test]
        public void Constructor_NegativeBanMatchesRemaining_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DisciplineEntry(1, 0, 0, -1));
        }

        [Test]
        public void Constructor_AllNonNegative_Succeeds()
        {
            var entry = new DisciplineEntry(1, 2, 3, 4);

            Assert.AreEqual(1, entry.PlayerId);
            Assert.AreEqual(2, entry.CompetitionId);
            Assert.AreEqual(3, entry.Yellows);
            Assert.AreEqual(4, entry.BanMatchesRemaining);
        }

        // ── IsEmpty / IsSuspended ──────────────────────────────────────────────────

        [Test]
        public void IsEmpty_TrueOnlyWhenBothYellowsAndBanAreZero()
        {
            Assert.IsTrue(new DisciplineEntry(1, 0, 0, 0).IsEmpty);
            Assert.IsFalse(new DisciplineEntry(1, 0, 1, 0).IsEmpty);
            Assert.IsFalse(new DisciplineEntry(1, 0, 0, 1).IsEmpty);
            Assert.IsFalse(new DisciplineEntry(1, 0, 1, 1).IsEmpty);
        }

        [Test]
        public void IsSuspended_TrueOnlyWhenBanMatchesRemainingIsPositive()
        {
            Assert.IsFalse(new DisciplineEntry(1, 0, 5, 0).IsSuspended, "yellows alone is not a suspension");
            Assert.IsTrue(new DisciplineEntry(1, 0, 0, 1).IsSuspended);
        }

        // ── L2 (reviewed findings pass): guarded accumulator arithmetic ─────────────

        [Test]
        public void YellowsPlusOne_AtIntMaxValue_ThrowsOverflowNamedMessage()
        {
            OverflowException ex = Assert.Throws<OverflowException>(
                () => DisciplineEntry.YellowsPlusOne(int.MaxValue));
            Assert.That(ex.Message, Does.Contain("overflow"),
                "the refusal must name the cause as overflow, not a counting bug");
        }

        [Test]
        public void YellowsPlusOne_BelowIntMaxValue_ReturnsIncrementedValue()
        {
            Assert.AreEqual(6, DisciplineEntry.YellowsPlusOne(5));
            Assert.AreEqual(int.MaxValue, DisciplineEntry.YellowsPlusOne(int.MaxValue - 1),
                "the boundary case, one below the guard, must still succeed");
        }

        [Test]
        public void BanMatchesPlus_WouldOverflow_ThrowsOverflowNamedMessage()
        {
            OverflowException ex = Assert.Throws<OverflowException>(
                () => DisciplineEntry.BanMatchesPlus(int.MaxValue - 1, 5));
            Assert.That(ex.Message, Does.Contain("overflow"),
                "the refusal must name the cause as overflow, not a counting bug");
        }

        [Test]
        public void BanMatchesPlus_ExactlyReachesIntMaxValue_DoesNotThrow()
        {
            // int.MaxValue - 5 + 5 == int.MaxValue exactly — the guard's own boundary, isolating
            // `>` from `>=` in the headroom check.
            Assert.AreEqual(int.MaxValue, DisciplineEntry.BanMatchesPlus(int.MaxValue - 5, 5));
        }

        [Test]
        public void BanMatchesPlus_WithinRange_ReturnsSum()
        {
            Assert.AreEqual(9, DisciplineEntry.BanMatchesPlus(4, 5));
        }

        [Test]
        public void BanMatchesPlus_ZeroMatches_NeverThrowsRegardlessOfExistingTotal()
        {
            // A zero-length ban is legal and a no-op (DisciplineRules.AddBan); it must never be refused
            // as an overflow no matter how close the existing total already is to int.MaxValue.
            Assert.AreEqual(int.MaxValue, DisciplineEntry.BanMatchesPlus(int.MaxValue, 0));
        }

        // ── Value equality ─────────────────────────────────────────────────────────

        [Test]
        public void Equals_TrueOnlyWhenAllFourFieldsMatch()
        {
            var a = new DisciplineEntry(1, 2, 3, 4);
            var b = new DisciplineEntry(1, 2, 3, 4);
            var differsInEachField = new[]
            {
                new DisciplineEntry(9, 2, 3, 4),
                new DisciplineEntry(1, 9, 3, 4),
                new DisciplineEntry(1, 2, 9, 4),
                new DisciplineEntry(1, 2, 3, 9),
            };

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            foreach (DisciplineEntry other in differsInEachField)
            {
                Assert.IsFalse(a.Equals(other), $"must differ from {other.PlayerId},{other.CompetitionId},{other.Yellows},{other.BanMatchesRemaining}");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-15 | —      | Initial suite (reviewed-findings pass, M25): DisciplineEntry had |
// |         |            |        | no test file at all. The three constructor guards (playerId < 0, |
// |         |            |        | yellows < 0, banMatchesRemaining < 0) each get an isolating case, |
// |         |            |        | mutation-verified — replacing all three with `if (false)` left   |
// |         |            |        | the whole 105-test discipline suite green before this file       |
// |         |            |        | existed. Also covers IsEmpty, IsSuspended, and value equality.   |
// | 1.1     | 2026-08-16 | —      | Reviewed findings pass, L2. Six new tests for                    |
// |         |            |        | YellowsPlusOne/BanMatchesPlus: overflow-named throws at the      |
// |         |            |        | guard boundary (int.MaxValue for Yellows; a sum that would       |
// |         |            |        | exceed it for BanMatchesRemaining), the exact-boundary case one  |
// |         |            |        | step inside each guard succeeding, an ordinary sum, and the      |
// |         |            |        | zero-matches case never throwing regardless of the existing      |
// |         |            |        | total.                                                             |
#endregion
