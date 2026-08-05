// File:     src/training-system/tests/TrainingSystemConstantsTests.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 Appendix A; Code Standards #20
// Purpose:  Catalogue invariants — the per-focus tables cover every TrainingFocus ordinal, the cursor
//           bounds are ordered, and the undefined-ordinal accessors fail loud (F4).

using System;

using NUnit.Framework;

namespace TacticalDirector.TrainingSystem.Tests
{
    [TestFixture]
    public sealed class TrainingSystemConstantsTests
    {
        [Test]
        public void FocusTables_CoverEveryDefinedOrdinal()
        {
            Array ordinals = Enum.GetValues(typeof(TrainingFocus));

            Assert.AreEqual(ordinals.Length, TrainingSystemConstants.FocusCount,
                "every TrainingFocus ordinal must have a row in both [GT] tables — a missing row would " +
                "fail loud at the first advance of a player on that focus, which is a save-time bug " +
                "surfaced at run time.");

            foreach (TrainingFocus focus in ordinals)
            {
                Assert.DoesNotThrow(() => TrainingSystemConstants.ConditionDeltaFor(focus));
                Assert.DoesNotThrow(() => TrainingSystemConstants.FatigueLoadFor(focus));
                Assert.IsTrue(TrainingSystemConstants.IsDefinedFocus(focus));
            }
        }

        [Test]
        public void UndefinedOrdinal_FailsLoud_F4()
        {
            var undefined = (TrainingFocus)200;

            Assert.IsFalse(TrainingSystemConstants.IsDefinedFocus(undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => TrainingSystemConstants.ConditionDeltaFor(undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => TrainingSystemConstants.FatigueLoadFor(undefined));
        }

        [Test]
        public void CursorBounds_AreOrdered_AndStartSitsInside()
        {
            Assert.Less(TrainingSystemConstants.ConditionMin, TrainingSystemConstants.ConditionMax);
            Assert.GreaterOrEqual(TrainingSystemConstants.ConditionStart, TrainingSystemConstants.ConditionMin);
            Assert.LessOrEqual(TrainingSystemConstants.ConditionStart, TrainingSystemConstants.ConditionMax);
            Assert.Greater(TrainingSystemConstants.TrainingFatigueMax, 0,
                "a non-positive ceiling makes the match-entry projection a 0/0 NaN (§3.3 guard).");
            Assert.Greater(TrainingSystemConstants.InjuryRiskMax, 0);
        }

        [Test]
        public void RestIsTheLightestLoad_AndFitnessOutConditionsBalanced()
        {
            // The reviewed CONTRACT is the shape, not the magnitudes (Appendix A: the [GT] values are
            // illustrative pending the balance pass). These two orderings are the shape.
            Assert.LessOrEqual(
                TrainingSystemConstants.FatigueLoadFor(TrainingFocus.Rest),
                TrainingSystemConstants.FatigueLoadFor(TrainingFocus.Balanced),
                "Rest is a recovery emphasis — its daily load must not exceed Balanced's.");

            Assert.Less(
                TrainingSystemConstants.FatigueLoadFor(TrainingFocus.Rest),
                TrainingSystemConstants.FatigueDailyRecovery,
                "Rest must net negative against the passive daily recovery, or 'rest' does not rest.");

            Assert.Greater(
                TrainingSystemConstants.ConditionDeltaFor(TrainingFocus.Fitness),
                TrainingSystemConstants.ConditionDeltaFor(TrainingFocus.Balanced),
                "Fitness is the conditioning emphasis.");
        }

        [Test]
        public void NotAdvancedSentinel_IsNotADay0Collision()
        {
            Assert.AreEqual(uint.MaxValue, TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                "the sentinel must not be 0 — a legitimate world day 0 would then read as 'already " +
                "advanced' and skip that player's accrual forever (the day-0 trap, F6).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0).       |
#endregion
