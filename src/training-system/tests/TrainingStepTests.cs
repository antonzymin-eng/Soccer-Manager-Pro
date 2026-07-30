// File:     src/training-system/tests/TrainingStepTests.cs
// Created:  2026-07-30
// Modified: 2026-07-30
// Spec:     Training System #29 §5.1 / §5.3 / §5.4 / §5.5; Code Standards #20
// Purpose:  Locks the Stage-2 daily cursor, projection, clamp, and failure-mode contracts.

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.TrainingSystem.Tests
{
    [TestFixture]
    public sealed class TrainingStepTests
    {
        [Test]
        public void AdvanceTrainingDay_DayZeroAdvancesOnceAndRerunIsANoOp()
        {
            TrainingState state = TrainingState.Create(TrainingFocus.Fitness);
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 0);

            Assert.AreEqual(7140, state.Condition);
            Assert.AreEqual(100, state.TrainingFatigue);
            Assert.AreEqual(0u, state.LastAdvancedWorldDay);

            TrainingState afterFirstAdvance = state;
            TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 0);

            Assert.AreEqual(afterFirstAdvance.Condition, state.Condition);
            Assert.AreEqual(afterFirstAdvance.TrainingFatigue, state.TrainingFatigue);
            Assert.AreEqual(afterFirstAdvance.LastAdvancedWorldDay, state.LastAdvancedWorldDay);
        }

        [Test]
        public void AdvanceTrainingDay_FitnessMatchesTheAppendixWorkedExample()
        {
            var state = new TrainingState
            {
                Focus = TrainingFocus.Fitness,
                Condition = 7000,
                TrainingFatigue = 2000,
                LastAdvancedWorldDay = 100
            };
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 101);

            Assert.AreEqual(7140, state.Condition);
            Assert.AreEqual(2100, state.TrainingFatigue);
            Assert.AreEqual(101u, state.LastAdvancedWorldDay);
        }

        [Test]
        public void AdvanceTrainingDay_DayGapFailsLoudWithoutMutatingState()
        {
            var state = new TrainingState
            {
                Focus = TrainingFocus.Balanced,
                Condition = 7000,
                TrainingFatigue = 1000,
                LastAdvancedWorldDay = 5
            };
            TrainingState before = state;
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            Assert.Throws<ArgumentException>(() => TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 7));

            Assert.AreEqual(before.Condition, state.Condition);
            Assert.AreEqual(before.TrainingFatigue, state.TrainingFatigue);
            Assert.AreEqual(before.LastAdvancedWorldDay, state.LastAdvancedWorldDay);
        }

        [Test]
        public void AdvanceTrainingDay_RestFocusClampsFatigueAtZero()
        {
            var state = new TrainingState
            {
                Focus = TrainingFocus.Rest,
                Condition = TrainingSystemConstants.CONDITION_MAX,
                TrainingFatigue = 50,
                LastAdvancedWorldDay = 10
            };
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 11);

            Assert.AreEqual(TrainingSystemConstants.CONDITION_MAX, state.Condition);
            Assert.AreEqual(0, state.TrainingFatigue);
        }

        [Test]
        public void ProjectMatchEntryFatigue_IsMonotonicAndClamped()
        {
            TrainingState low = TrainingState.Create(TrainingFocus.Balanced);
            TrainingState high = low;
            high.TrainingFatigue = TrainingSystemConstants.TRAINING_FATIGUE_MAX * 2;

            Assert.AreEqual(0.0f, TrainingStep.ProjectMatchEntryFatigue(in low));
            Assert.AreEqual(1.0f, TrainingStep.ProjectMatchEntryFatigue(in high));
        }

        [Test]
        public void ComputeInjuryRisk_IncreasesWithFatigueAndLowCondition()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            var rested = new TrainingState
            {
                Focus = TrainingFocus.Balanced,
                Condition = TrainingSystemConstants.CONDITION_MAX,
                TrainingFatigue = 0
            };
            var fatigued = rested;
            fatigued.Condition = TrainingSystemConstants.CONDITION_MIN;
            fatigued.TrainingFatigue = TrainingSystemConstants.TRAINING_FATIGUE_MAX;

            InjuryRiskContribution restedRisk = TrainingStep.ComputeInjuryRisk(in rested, in attributes);
            InjuryRiskContribution fatiguedRisk = TrainingStep.ComputeInjuryRisk(in fatigued, in attributes);

            Assert.Less(restedRisk.RiskScore, fatiguedRisk.RiskScore);
            Assert.LessOrEqual(fatiguedRisk.RiskScore, TrainingSystemConstants.INJURY_RISK_MAX);
        }

        [Test]
        public void CreateAndAdvance_RejectOutOfRangeFocusValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TrainingState.Create((TrainingFocus)99));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
#endregion