// File:     src/training-system/tests/TrainingSystemConstantsTests.cs
// Created:  2026-07-31
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 Appendix A; §3.1; Code Standards #20 §3.2.3 (FR-CS-019)
// Purpose:  Coverage and shape locks over the Appendix A catalogue — chiefly that the two per-focus
//           tables carry exactly one row per TrainingFocus member (the POSITION_COUNT precedent).

using System;

using NUnit.Framework;

namespace TacticalDirector.TrainingSystem.Tests
{
    [TestFixture]
    public sealed class TrainingSystemConstantsTests
    {
        // The coverage lock the switch statements did not need and the tables do: adding a focus without
        // its two rows must fail here rather than throw IndexOutOfRange from inside the daily step.
        [Test]
        public void FocusTables_HaveExactlyOneRowPerFocus()
        {
            int focusCount = Enum.GetValues(typeof(TrainingFocus)).Length;

            Assert.AreEqual(
                focusCount,
                TrainingSystemConstants.FocusConditionDelta.Length,
                "FocusConditionDelta must carry one row per TrainingFocus member");
            Assert.AreEqual(
                focusCount,
                TrainingSystemConstants.FocusFatigueDelta.Length,
                "FocusFatigueDelta must carry one row per TrainingFocus member");
        }

        // Appendix A's recorded magnitudes, indexed by ordinal — the values the Appendix B worked
        // example and every conditioning figure in the suite are derived from.
        [Test]
        public void FocusTables_MatchTheAppendixAMagnitudes()
        {
            CollectionAssert.AreEqual(
                new[] { 70, 30, 120, 80, 100, 70 },
                TrainingSystemConstants.FocusConditionDelta,
                "Balanced / Rest / Fitness / Technical / Physical / Tactical conditioning deltas");
            CollectionAssert.AreEqual(
                new[] { 220, 0, 300, 200, 280, 180 },
                TrainingSystemConstants.FocusFatigueDelta,
                "…and the daily training loads (before recovery)");
        }

        // The Appendix A shapes are the reviewed contract, the magnitudes are tunable — so the shapes
        // are what gets locked: Fitness conditions hardest, Rest is the lightest load, and recovery is
        // strong enough that Rest nets negative (the §3.1 sub-max-equilibrium property).
        [Test]
        public void FocusTables_KeepTheAppendixAShapes()
        {
            int[] condition = TrainingSystemConstants.FocusConditionDelta;
            int[] fatigue = TrainingSystemConstants.FocusFatigueDelta;

            Assert.Greater(
                condition[(int)TrainingFocus.Fitness],
                condition[(int)TrainingFocus.Balanced],
                "Fitness conditions faster than Balanced");
            Assert.Less(
                condition[(int)TrainingFocus.Rest],
                condition[(int)TrainingFocus.Balanced],
                "Rest still conditions, but least");

            for (int i = 0; i < condition.Length; i++)
            {
                Assert.GreaterOrEqual(condition[i], 0, $"no focus de-conditions: {(TrainingFocus)i}");
                Assert.GreaterOrEqual(fatigue[i], 0, $"load is never negative — recovery is the separate term: {(TrainingFocus)i}");
            }

            Assert.AreEqual(0, fatigue[(int)TrainingFocus.Rest], "Rest carries no training load");
            Assert.Greater(
                TrainingSystemConstants.FatigueDailyRecovery,
                fatigue[(int)TrainingFocus.Rest],
                "so a Rest day nets strongly negative (§3.1)");
            Assert.Greater(
                fatigue[(int)TrainingFocus.Fitness],
                TrainingSystemConstants.FatigueDailyRecovery,
                "…while a Fitness day nets positive");
        }

        [Test]
        public void CursorBounds_AreCoherent()
        {
            Assert.Less(TrainingSystemConstants.ConditionMin, TrainingSystemConstants.ConditionMax);
            Assert.GreaterOrEqual(TrainingSystemConstants.ConditionStart, TrainingSystemConstants.ConditionMin);
            Assert.LessOrEqual(TrainingSystemConstants.ConditionStart, TrainingSystemConstants.ConditionMax);
            Assert.Greater(TrainingSystemConstants.TrainingFatigueMax, 0);
            Assert.Greater(TrainingSystemConstants.InjuryRiskMax, 0);
        }

        // The sentinel must not be a value the conditioning/fatigue model can produce as a world day,
        // and it must be the extreme of the domain — the day-0 collision argument in reverse.
        [Test]
        public void NotAdvancedSentinel_IsTheTopOfTheWorldDayDomain()
        {
            Assert.AreEqual(uint.MaxValue, TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL);
            Assert.AreNotEqual(0u, TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL, "day 0 must be a legitimate world day");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-31 | —      | AR-1 M-3: coverage lock (table length == TrainingFocus member count),
// |         |            |        | the Appendix A magnitudes, the reviewed shapes, cursor-bound
// |         |            |        | coherence, and the sentinel-domain property.                      |
#endregion
