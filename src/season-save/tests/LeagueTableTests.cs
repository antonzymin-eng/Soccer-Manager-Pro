// File:     src/season-save/tests/LeagueTableTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §5.3 (T-SN-TAB-001..005), §3.2, Appendix D; Testing Strategy #19
// Purpose:  Unit locks for the league table: result arithmetic, GD recomputation, the FR-SN-007
//           tie-break total order, observer-neutral OrderedView, and the F2 fail-loud gates.

using System.Collections.ObjectModel;
using NUnit.Framework;
using TacticalDirector.SeasonSave;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    public sealed class LeagueTableTests
    {
        private static LeagueTable FourClubTable() => LeagueTable.Empty(new[] { 10, 11, 12, 13 });

        /// <summary>T-SN-TAB-001 — win/draw/loss arithmetic, exact for each outcome (FR-SN-006).</summary>
        [Test]
        public void ApplyResult_HomeWin_UpdatesBothRowsExactly()
        {
            LeagueTable table = FourClubTable();
            table.ApplyResult(10, 11, 3, 1);

            LeagueTableRow home = table.Row(10);
            Assert.That(home.Played, Is.EqualTo(1));
            Assert.That(home.Won, Is.EqualTo(1));
            Assert.That(home.Drawn, Is.EqualTo(0));
            Assert.That(home.Lost, Is.EqualTo(0));
            Assert.That(home.GoalsFor, Is.EqualTo(3));
            Assert.That(home.GoalsAgainst, Is.EqualTo(1));
            Assert.That(home.GoalDifference, Is.EqualTo(2));
            Assert.That(home.Points, Is.EqualTo(SeasonLoopConstants.WIN_POINTS));

            LeagueTableRow away = table.Row(11);
            Assert.That(away.Played, Is.EqualTo(1));
            Assert.That(away.Won, Is.EqualTo(0));
            Assert.That(away.Lost, Is.EqualTo(1));
            Assert.That(away.GoalsFor, Is.EqualTo(1));
            Assert.That(away.GoalsAgainst, Is.EqualTo(3));
            Assert.That(away.GoalDifference, Is.EqualTo(-2));
            Assert.That(away.Points, Is.EqualTo(SeasonLoopConstants.LOSS_POINTS));
        }

        [Test]
        public void ApplyResult_AwayWin_UpdatesBothRowsExactly()
        {
            LeagueTable table = FourClubTable();
            table.ApplyResult(10, 11, 0, 2);

            Assert.That(table.Row(10).Lost, Is.EqualTo(1));
            Assert.That(table.Row(10).Points, Is.EqualTo(SeasonLoopConstants.LOSS_POINTS));
            Assert.That(table.Row(11).Won, Is.EqualTo(1));
            Assert.That(table.Row(11).Points, Is.EqualTo(SeasonLoopConstants.WIN_POINTS));
        }

        [Test]
        public void ApplyResult_Draw_AwardsDrawPointsToBoth()
        {
            LeagueTable table = FourClubTable();
            table.ApplyResult(10, 11, 2, 2);

            Assert.That(table.Row(10).Drawn, Is.EqualTo(1));
            Assert.That(table.Row(11).Drawn, Is.EqualTo(1));
            Assert.That(table.Row(10).Points, Is.EqualTo(SeasonLoopConstants.DRAW_POINTS));
            Assert.That(table.Row(11).Points, Is.EqualTo(SeasonLoopConstants.DRAW_POINTS));
            Assert.That(table.Row(10).GoalDifference, Is.EqualTo(0));
        }

        /// <summary>T-SN-TAB-002 — GD is GF − GA after a sequence of results (recomputed, not drifted).</summary>
        [Test]
        public void GoalDifference_AfterSequence_EqualsGoalsForMinusAgainst()
        {
            LeagueTable table = FourClubTable();
            table.ApplyResult(10, 11, 3, 1);
            table.ApplyResult(12, 10, 2, 2);
            table.ApplyResult(10, 13, 0, 4);

            LeagueTableRow row = table.Row(10);
            Assert.That(row.Played, Is.EqualTo(3));
            Assert.That(row.GoalsFor, Is.EqualTo(3 + 2 + 0));
            Assert.That(row.GoalsAgainst, Is.EqualTo(1 + 2 + 4));
            Assert.That(row.GoalDifference, Is.EqualTo(row.GoalsFor - row.GoalsAgainst));
            Assert.That(row.GoalDifference, Is.EqualTo(-2));
        }

        /// <summary>
        /// T-SN-TAB-003 — the Appendix D exact three-key tie: Pts, GD and GF all equal, so only the
        /// ascending-ClubId final key separates the two clubs.
        /// </summary>
        [Test]
        public void OrderedView_ExactThreeKeyTie_SeparatesByAscendingClubId()
        {
            // Appendix D: clubs 10 and 11 both P3 W2 D0 L1, GF 5 GA 3 (GD +2), 6 pts.
            var rows = new[]
            {
                LeagueTableRow.Create(11, 3, 2, 0, 1, 5, 3, 6),
                LeagueTableRow.Create(10, 3, 2, 0, 1, 5, 3, 6),
            };
            LeagueTable table = LeagueTable.FromRows(rows);

            ReadOnlyCollection<LeagueTableRow> ordered = table.OrderedView();

            Assert.That(ordered[0].Points, Is.EqualTo(ordered[1].Points), "points tied");
            Assert.That(ordered[0].GoalDifference, Is.EqualTo(ordered[1].GoalDifference), "GD tied");
            Assert.That(ordered[0].GoalsFor, Is.EqualTo(ordered[1].GoalsFor), "GF tied");
            Assert.That(ordered[0].ClubId, Is.EqualTo(10), "club 10 orders above 11 on the final key");
            Assert.That(ordered[1].ClubId, Is.EqualTo(11));
        }

        /// <summary>The tie-break keys are applied in the FR-SN-007 order, each dominating the next.</summary>
        [Test]
        public void OrderedView_AppliesKeysInSpecifiedPrecedence()
        {
            var rows = new[]
            {
                // 4 pts, GD -6 — fewest goals, but points dominate every other key.
                LeagueTableRow.Create(10, 3, 1, 1, 1, 2, 8, 4),
                // 3 pts, GD +7 — best GD of the three tied on points.
                LeagueTableRow.Create(11, 3, 1, 0, 2, 9, 2, 3),
                // 3 pts, GD +4, GF 5 — ties 13 on GD, loses on GF.
                LeagueTableRow.Create(12, 3, 1, 0, 2, 5, 1, 3),
                // 3 pts, GD +4, GF 9 — ties 12 on GD, wins on GF.
                LeagueTableRow.Create(13, 3, 1, 0, 2, 9, 5, 3),
            };
            LeagueTable table = LeagueTable.FromRows(rows);

            ReadOnlyCollection<LeagueTableRow> ordered = table.OrderedView();

            Assert.That(ordered[0].ClubId, Is.EqualTo(10), "4 pts beats 3 pts despite the worst GD");
            Assert.That(ordered[1].ClubId, Is.EqualTo(11), "among the 3-pt clubs, GD +7 is best");
            Assert.That(ordered[2].ClubId, Is.EqualTo(13), "GD +4 tie broken by GF 9 > 5");
            Assert.That(ordered[3].ClubId, Is.EqualTo(12), "GD +4 with the lower GF orders last");
        }

        /// <summary>
        /// T-SN-TAB-004 — OrderedView is a read-only copy: the stored rows keep their canonical
        /// ascending-ClubId order and their values (observer-neutral, FR-SN-033).
        /// </summary>
        [Test]
        public void OrderedView_DoesNotMutateStoredRows()
        {
            LeagueTable table = FourClubTable();
            table.ApplyResult(13, 10, 5, 0);   // 13 goes top, 10 bottom

            ReadOnlyCollection<LeagueTableRow> beforeStored = table.RowsInClubIdOrder();
            ReadOnlyCollection<LeagueTableRow> ordered = table.OrderedView();
            ReadOnlyCollection<LeagueTableRow> afterStored = table.RowsInClubIdOrder();

            Assert.That(ordered[0].ClubId, Is.EqualTo(13), "ordering did happen");

            Assert.That(afterStored.Count, Is.EqualTo(beforeStored.Count));
            for (int i = 0; i < beforeStored.Count; i++)
            {
                Assert.That(afterStored[i].ClubId, Is.EqualTo(beforeStored[i].ClubId),
                    "stored rows must stay in ascending ClubId order");
                Assert.That(afterStored[i].Points, Is.EqualTo(beforeStored[i].Points));
                Assert.That(afterStored[i].GoalDifference, Is.EqualTo(beforeStored[i].GoalDifference));
            }
        }

        /// <summary>Reading the ordered view twice yields the same order (determinism over a total order).</summary>
        [Test]
        public void OrderedView_IsRepeatable()
        {
            LeagueTable table = FourClubTable();
            table.ApplyResult(10, 11, 1, 1);
            table.ApplyResult(12, 13, 1, 1);

            ReadOnlyCollection<LeagueTableRow> first = table.OrderedView();
            ReadOnlyCollection<LeagueTableRow> second = table.OrderedView();

            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].ClubId, Is.EqualTo(first[i].ClubId), $"position {i}");
            }
        }

        /// <summary>T-SN-TAB-005 — F2 fail-loud: unknown club, self-fixture, negative goals.</summary>
        [Test]
        public void ApplyResult_SelfFixture_Throws()
        {
            LeagueTable table = FourClubTable();
            Assert.Throws<System.ArgumentException>(() => table.ApplyResult(10, 10, 1, 0));
        }

        [Test]
        public void ApplyResult_UnknownClub_Throws()
        {
            LeagueTable table = FourClubTable();
            Assert.Throws<System.ArgumentException>(() => table.ApplyResult(10, 99, 1, 0));
            Assert.Throws<System.ArgumentException>(() => table.ApplyResult(99, 10, 1, 0));
        }

        [Test]
        public void ApplyResult_NegativeGoals_Throws()
        {
            LeagueTable table = FourClubTable();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => table.ApplyResult(10, 11, -1, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => table.ApplyResult(10, 11, 0, -1));
        }

        /// <summary>
        /// F2 says "throw; table unchanged" — a rejected result must not leave the valid club's row
        /// half-updated (the resolve-both-before-write discipline).
        /// </summary>
        [Test]
        public void ApplyResult_UnknownAwayClub_LeavesHomeRowUntouched()
        {
            LeagueTable table = FourClubTable();
            LeagueTableRow before = table.Row(10);

            Assert.Throws<System.ArgumentException>(() => table.ApplyResult(10, 99, 4, 0));

            LeagueTableRow after = table.Row(10);
            Assert.That(after.Played, Is.EqualTo(before.Played), "Played must not advance");
            Assert.That(after.GoalsFor, Is.EqualTo(before.GoalsFor), "GF must not advance");
            Assert.That(after.Points, Is.EqualTo(before.Points), "points must not advance");
        }

        [Test]
        public void Empty_FewerThanTwoClubsOrDuplicate_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => LeagueTable.Empty(new[] { 1 }));
            Assert.Throws<System.ArgumentException>(() => LeagueTable.Empty(new[] { 1, 2, 2 }));
        }

        [Test]
        public void Empty_DoesNotMutateCallerArray()
        {
            var ids = new[] { 13, 10, 12, 11 };
            var before = new int[ids.Length];
            System.Array.Copy(ids, before, ids.Length);

            LeagueTable.Empty(ids);

            Assert.That(ids, Is.EqualTo(before), "the caller's club array must not be sorted in place");
        }

        /// <summary>PositionOf is the 1-based tie-break position — the board's on-track input.</summary>
        [Test]
        public void PositionOf_ReturnsOneBasedTieBreakPosition()
        {
            LeagueTable table = FourClubTable();
            table.ApplyResult(13, 10, 3, 0);

            Assert.That(table.PositionOf(13), Is.EqualTo(1));
            Assert.That(table.PositionOf(10), Is.EqualTo(4), "0 pts, GD -3 is last");
            Assert.Throws<System.ArgumentException>(() => table.PositionOf(99));
        }

        /// <summary>Clone is deep — mutating the clone must not touch the original.</summary>
        [Test]
        public void Clone_IsDeep()
        {
            LeagueTable original = FourClubTable();
            original.ApplyResult(10, 11, 1, 0);

            LeagueTable clone = original.Clone();
            clone.ApplyResult(12, 13, 5, 0);

            Assert.That(original.Row(12).Played, Is.EqualTo(0), "original must be unaffected");
            Assert.That(clone.Row(12).Played, Is.EqualTo(1));
            Assert.That(original.FieldsEqual(clone), Is.False);
        }

        /// <summary>FieldsEqual is order-insensitive over construction but value-sensitive.</summary>
        [Test]
        public void FieldsEqual_MatchesOnValuesRegardlessOfApplyOrder()
        {
            LeagueTable a = FourClubTable();
            a.ApplyResult(10, 11, 2, 1);
            a.ApplyResult(12, 13, 0, 0);

            LeagueTable b = FourClubTable();
            b.ApplyResult(12, 13, 0, 0);
            b.ApplyResult(10, 11, 2, 1);

            Assert.That(a.FieldsEqual(b), Is.True, "same results in a different order ⇒ same table");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-25 | —      | Initial suite (#30 T0): T-SN-TAB-001..005 + precedence,   |
// |         |            |        | atomicity, aliasing, PositionOf, Clone, FieldsEqual.      |
#endregion
