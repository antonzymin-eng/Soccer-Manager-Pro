// File:     src/positioning-ai/Tests/RotationCatalogueTests.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Positional Rotations #25 Appendix A/D, FR-RO-002/003/007/016, §5 test plan
//           (T-RO-U-010/013 class), Code Standards #20
// Purpose:  Locks the #25 T0 data: the F1 catalogue invariants (GK-free, index-valid, distinct,
//           row cap) over every FormationFamily, the pinned Appendix A v0.3 row contents, the
//           FR-RO-007 [DERIVED] hold-vs-line-dwell bound, and the RotationPair constructor gates.

using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace TacticalDirector.PositioningAI.Tests
{
    [TestFixture]
    internal class RotationCatalogueTests
    {
        // ---------- F1 invariants (FR-RO-016), every family ----------

        [Test]
        public void EveryFamilyTable_SatisfiesF1Invariants()
        {
            foreach (FormationFamily family in (FormationFamily[])Enum.GetValues(typeof(FormationFamily)))
            {
                IReadOnlyList<RotationPair> pairs = RotationAdjacencyCatalogue.GetPairs(family);

                Assert.LessOrEqual(pairs.Count, PositioningAIConstants.ROTATION_MAX_PAIRS_PER_FAMILY,
                    $"{family}: row cap (ROTATION_MAX_PAIRS_PER_FAMILY)");

                var seen = new HashSet<(int, int)>();
                for (int i = 0; i < pairs.Count; i++)
                {
                    RotationPair p = pairs[i];
                    Assert.Greater(p.SlotA, 0, $"{family} row {i}: GK-free (FR-RO-003)");
                    Assert.Greater(p.SlotB, p.SlotA, $"{family} row {i}: normalized SlotA < SlotB");
                    Assert.Less(p.SlotB, 11, $"{family} row {i}: index-valid (11-slot family tables)");
                    Assert.IsTrue(seen.Add((p.SlotA, p.SlotB)), $"{family} row {i}: rows distinct");
                }
            }
        }

        [Test]
        public void FamilyTables_NeverReferenceAGoalkeeperSlot()
        {
            // Cross-check against the actual formation tables, not just the slot-0 convention.
            foreach (FormationFamily family in (FormationFamily[])Enum.GetValues(typeof(FormationFamily)))
            {
                FormationSlotRecord[] table = family switch
                {
                    FormationFamily.F433 => PositioningAIConstants.Family433,
                    FormationFamily.F4231 => PositioningAIConstants.Family4231,
                    _ => PositioningAIConstants.Family442,
                };
                foreach (RotationPair p in RotationAdjacencyCatalogue.GetPairs(family))
                {
                    Assert.IsFalse(table[p.SlotA].IsGoalkeeper, $"{family} slot {p.SlotA}");
                    Assert.IsFalse(table[p.SlotB].IsGoalkeeper, $"{family} slot {p.SlotB}");
                }
            }
        }

        // ---------- Pinned Appendix A v0.3 contents ----------

        [Test]
        public void F442_RowsMatchAppendixA1()
        {
            IReadOnlyList<RotationPair> p = RotationAdjacencyCatalogue.GetPairs(FormationFamily.F442);
            Assert.AreEqual(5, p.Count);
            AssertPair(p[0], 1, 5);   // LB ↔ LM
            AssertPair(p[1], 4, 8);   // RB ↔ RM
            AssertPair(p[2], 6, 7);   // LCM ↔ RCM
            AssertPair(p[3], 5, 9);   // LM ↔ LST
            AssertPair(p[4], 8, 10);  // RM ↔ RST
        }

        [Test]
        public void F433_RowsMatchAppendixA2_AndExcludeTheSinglePivot()
        {
            IReadOnlyList<RotationPair> p = RotationAdjacencyCatalogue.GetPairs(FormationFamily.F433);
            Assert.AreEqual(5, p.Count);
            AssertPair(p[0], 1, 8);   // LB ↔ LW
            AssertPair(p[1], 4, 10);  // RB ↔ RW
            AssertPair(p[2], 6, 7);   // CM1 ↔ CM2
            AssertPair(p[3], 8, 9);   // LW ↔ ST
            AssertPair(p[4], 9, 10);  // RW ↔ ST
            foreach (RotationPair pair in p)
            {
                Assert.IsFalse(pair.Contains(5), "the single pivot (DM, slot 5) anchors rest-defence — excluded (A.2)");
            }
        }

        [Test]
        public void F4231_RowsMatchAppendixA3()
        {
            IReadOnlyList<RotationPair> p = RotationAdjacencyCatalogue.GetPairs(FormationFamily.F4231);
            Assert.AreEqual(6, p.Count);
            AssertPair(p[0], 1, 7);   // LB ↔ LAM
            AssertPair(p[1], 4, 9);   // RB ↔ RAM
            AssertPair(p[2], 5, 6);   // DM1 ↔ DM2
            AssertPair(p[3], 8, 10);  // CAM ↔ ST
            AssertPair(p[4], 7, 8);   // LAM ↔ CAM
            AssertPair(p[5], 8, 9);   // RAM ↔ CAM
        }

        // ---------- FR-RO-007 [DERIVED] bound (T-RO-U-013) ----------

        [Test]
        public void HoldTicks_SatisfiesLineDwellLowerBound()
        {
            // Appendix D: ROTATION_HOLD_TICKS ≥ LINE_DWELL_TICKS or the analyzer and the controller
            // chase each other's transients. Locks the inequality against future retuning of either side.
            Assert.GreaterOrEqual(
                PositioningAIConstants.ROTATION_HOLD_TICKS,
                PositioningAIConstants.LINE_DWELL_TICKS);
        }

        [Test]
        public void AdvantageScalars_ConservativeNarrowsRelativeToFree()
        {
            // §3.1 shape: Conservative demands a wider margin than Free; both positive.
            Assert.Greater(PositioningAIConstants.ROTATION_ADVANTAGE_SCALAR_CONSERVATIVE,
                PositioningAIConstants.ROTATION_ADVANTAGE_SCALAR_FREE);
            Assert.Greater(PositioningAIConstants.ROTATION_ADVANTAGE_SCALAR_FREE, 0f);
            Assert.Greater(PositioningAIConstants.ROTATION_ADVANTAGE_M, 0f);
        }

        // ---------- RotationPair constructor gates (F1) ----------

        [Test]
        public void Pair_Normalizes_AndRefusesDegenerateInput()
        {
            RotationPair p = new RotationPair(8, 5);
            Assert.AreEqual(5, p.SlotA);
            Assert.AreEqual(8, p.SlotB);
            Assert.IsTrue(p.Contains(5));
            Assert.IsTrue(p.Contains(8));
            Assert.IsFalse(p.Contains(6));

            Assert.Throws<ArgumentException>(() => new RotationPair(4, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RotationPair(0, 4), "GK slot refused");
            Assert.Throws<ArgumentOutOfRangeException>(() => new RotationPair(-1, 4));
        }

        private static void AssertPair(RotationPair pair, int expectedA, int expectedB)
        {
            Assert.AreEqual(expectedA, pair.SlotA);
            Assert.AreEqual(expectedB, pair.SlotB);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial T0 suite (#25): F1 invariants, pinned Appendix A rows, FR-RO-007 bound. |
#endregion
