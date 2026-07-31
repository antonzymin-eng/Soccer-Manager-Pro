// File:     src/training-system/tests/TrainingStepTests.cs
// Created:  2026-07-30
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §5.1 / §5.3 / §5.4 / §5.5 + Appendix B; Code Standards #20
// Purpose:  Locks the Stage-2 daily cursor, projection, clamp, and failure-mode contracts —
//           T-TR-DET-001/003/004/005, T-TR-FAT-001/003, T-TR-CON-001/002, T-TR-COA-001, T-TR-INJ-001.

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
            TrainingState state = AppendixBSeedState();
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 101);

            Assert.AreEqual(7140, state.Condition);
            Assert.AreEqual(2100, state.TrainingFatigue);
            Assert.AreEqual(101u, state.LastAdvancedWorldDay);
        }

        // T-TR-DET-001 — the Appendix B three-day sequence, then a value-copy "save" (standing in for
        // the T1 codec, the #28 T-PG-DET-002 precedent) whose continuation must equal the uninterrupted
        // run. The shipped suite never advanced more than one consecutive day, so neither multi-day
        // accrual nor restore-equality had a lock.
        [Test]
        public void AdvanceTrainingDay_ThreeDaySequence_ThenSaveRestore_EqualsTheUninterruptedRun()
        {
            TrainingState live = AppendixBSeedState();
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            TrainingStep.AdvanceTrainingDay(ref live, in attributes, in coach, 101);
            Assert.AreEqual(7140, live.Condition, "Appendix B day 101 condition");
            Assert.AreEqual(2100, live.TrainingFatigue, "Appendix B day 101 fatigue");

            TrainingStep.AdvanceTrainingDay(ref live, in attributes, in coach, 102);
            Assert.AreEqual(7280, live.Condition, "Appendix B day 102 condition");
            Assert.AreEqual(2200, live.TrainingFatigue, "Appendix B day 102 fatigue");

            TrainingStep.AdvanceTrainingDay(ref live, in attributes, in coach, 103);
            Assert.AreEqual(7420, live.Condition, "Appendix B day 103 condition");
            Assert.AreEqual(2300, live.TrainingFatigue, "Appendix B day 103 fatigue");
            Assert.AreEqual(103u, live.LastAdvancedWorldDay);

            // "Save" after day 103: every field of the struct, copied.
            TrainingState restored = live;
            Assert.AreEqual(live.Focus, restored.Focus, "Focus restores field-identical");
            Assert.AreEqual(live.Condition, restored.Condition, "Condition restores field-identical");
            Assert.AreEqual(live.TrainingFatigue, restored.TrainingFatigue, "TrainingFatigue restores field-identical");
            Assert.AreEqual(live.LastAdvancedWorldDay, restored.LastAdvancedWorldDay, "the cursor restores field-identical");

            // T-TR-FAT-001: the projection is recomputed from the accumulator, never stored.
            Assert.AreEqual(
                TrainingStep.ProjectMatchEntryFatigue(in live),
                TrainingStep.ProjectMatchEntryFatigue(in restored),
                "the match-entry projection is identical before and after the save (KD-1)");

            // Advancing both onward must agree, and must match Appendix B's day-104 figures.
            TrainingStep.AdvanceTrainingDay(ref live, in attributes, in coach, 104);
            TrainingStep.AdvanceTrainingDay(ref restored, in attributes, in coach, 104);

            Assert.AreEqual(7560, live.Condition, "Appendix B day 104 condition");
            Assert.AreEqual(2400, live.TrainingFatigue, "Appendix B day 104 fatigue");
            Assert.AreEqual(live.Condition, restored.Condition);
            Assert.AreEqual(live.TrainingFatigue, restored.TrainingFatigue);
            Assert.AreEqual(live.LastAdvancedWorldDay, restored.LastAdvancedWorldDay);
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

        // AR-1 M-1 regression (the defect was proven by execution before the fix): advancing WITH the
        // sentinel as the world day wrote it into LastAdvancedWorldDay, leaving the state
        // indistinguishable from fresh — so the same day accrued twice and any later, smaller day
        // rewound the cursor and accrued again. Locked from a FRESH state (the sentinel path) …
        [Test]
        public void AdvanceTrainingDay_SentinelWorldDay_FailsLoudFromAFreshState()
        {
            TrainingState state = TrainingState.Create(TrainingFocus.Fitness);
            TrainingState before = state;
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            Assert.Throws<ArgumentOutOfRangeException>(() => TrainingStep.AdvanceTrainingDay(
                ref state, in attributes, in coach, TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL));

            Assert.AreEqual(before.Condition, state.Condition, "a refused call mutates nothing");
            Assert.AreEqual(before.TrainingFatigue, state.TrainingFatigue);
            Assert.AreEqual(
                TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                state.LastAdvancedWorldDay,
                "the state is still 'never advanced'");
        }

        // … and from an ALREADY-ADVANCED state, where the same write would have re-armed the fresh-state
        // marker on a player who has genuinely trained (reachable as last == uint.MaxValue − 1, +1).
        [Test]
        public void AdvanceTrainingDay_SentinelWorldDay_FailsLoudFromAnAdvancedState()
        {
            var state = new TrainingState
            {
                Focus = TrainingFocus.Fitness,
                Condition = 7000,
                TrainingFatigue = 2000,
                LastAdvancedWorldDay = TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL - 1u
            };
            TrainingState before = state;
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            Assert.Throws<ArgumentOutOfRangeException>(() => TrainingStep.AdvanceTrainingDay(
                ref state, in attributes, in coach, TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL));

            Assert.AreEqual(before.Condition, state.Condition);
            Assert.AreEqual(before.TrainingFatigue, state.TrainingFatigue);
            Assert.AreEqual(before.LastAdvancedWorldDay, state.LastAdvancedWorldDay, "the cursor never re-arms");
        }

        [Test]
        public void AdvanceTrainingDay_RestFocusClampsFatigueAtZero()
        {
            var state = new TrainingState
            {
                Focus = TrainingFocus.Rest,
                Condition = TrainingSystemConstants.ConditionMax,
                TrainingFatigue = 50,
                LastAdvancedWorldDay = 10
            };
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 11);

            Assert.AreEqual(TrainingSystemConstants.ConditionMax, state.Condition);
            Assert.AreEqual(0, state.TrainingFatigue);
        }

        // T-TR-COA-001 — CoachingModifier.Identity yields EXACTLY the Stage-2 catalogue deltas for every
        // focus (×1.0, KD-3). Named as its own lock rather than left as a side effect of the Appendix B
        // figures, so #34's first non-identity producer has an explicit record of what identity meant.
        [Test]
        public void AdvanceTrainingDay_IdentityCoach_YieldsExactlyTheCatalogueDeltas()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;
            int bonus = (attributes.WorkRate * TrainingSystemConstants.ConditioningBonusWorkRateWeight)
                + (attributes.Stamina * TrainingSystemConstants.ConditioningBonusStaminaWeight);

            foreach (TrainingFocus focus in AllFocuses())
            {
                var state = new TrainingState
                {
                    Focus = focus,
                    Condition = 5000,
                    TrainingFatigue = 5000,
                    LastAdvancedWorldDay = 40
                };

                TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 41);

                int expectedCondition = 5000 + TrainingSystemConstants.FocusConditionDelta[(int)focus] + bonus;
                int expectedFatigue = 5000 + TrainingSystemConstants.FocusFatigueDelta[(int)focus]
                    - TrainingSystemConstants.FatigueDailyRecovery;

                Assert.AreEqual(expectedCondition, state.Condition, $"condition delta for {focus}");
                Assert.AreEqual(expectedFatigue, state.TrainingFatigue, $"fatigue delta for {focus}");
            }
        }

        // T-TR-CON-002 — the own-attribute conditioning bonus is deterministic (identical inputs ⇒
        // identical delta) and genuinely attribute-driven, never an RNG draw (FR-TR-009). The second
        // half is what keeps the first non-vacuous: a constant bonus would also be "deterministic".
        [Test]
        public void AttributeConditioningBonus_IsDeterministicAndAttributeDriven()
        {
            CoachingModifier coach = CoachingModifier.Identity;
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();

            TrainingState first = TrainingState.Create(TrainingFocus.Technical);
            TrainingState second = TrainingState.Create(TrainingFocus.Technical);
            TrainingStep.AdvanceTrainingDay(ref first, in attributes, in coach, 7);
            TrainingStep.AdvanceTrainingDay(ref second, in attributes, in coach, 7);
            Assert.AreEqual(first.Condition, second.Condition, "identical inputs ⇒ identical delta");

            PlayerAttributes fitter = PlayerAttributes.CreateDefault();
            fitter.WorkRate += 5;
            fitter.Stamina += 5;
            TrainingState fitterState = TrainingState.Create(TrainingFocus.Technical);
            TrainingStep.AdvanceTrainingDay(ref fitterState, in fitter, in coach, 7);

            Assert.Greater(
                fitterState.Condition,
                first.Condition,
                "a higher WorkRate/Stamina conditions faster — the bonus reads the player's own attributes");
        }

        // T-TR-FAT-003 — monotone in TrainingFatigue and clamped to [0,1], asserted at an INTERIOR point
        // (the shipped test touched only the two clamped endpoints, so it locked neither the
        // monotonicity its name claimed nor Appendix B's worked 0.23).
        [Test]
        public void ProjectMatchEntryFatigue_IsMonotonicAndClamped()
        {
            TrainingState low = TrainingState.Create(TrainingFocus.Balanced);
            TrainingState mid = low;
            TrainingState higher = low;
            TrainingState overflowing = low;

            mid.TrainingFatigue = 2300; // Appendix B, after day 103
            higher.TrainingFatigue = 4600;
            overflowing.TrainingFatigue = TrainingSystemConstants.TrainingFatigueMax * 2;

            float lowProjection = TrainingStep.ProjectMatchEntryFatigue(in low);
            float midProjection = TrainingStep.ProjectMatchEntryFatigue(in mid);
            float higherProjection = TrainingStep.ProjectMatchEntryFatigue(in higher);
            float overflowProjection = TrainingStep.ProjectMatchEntryFatigue(in overflowing);

            Assert.AreEqual(0.0f, lowProjection, "an unfatigued player enters at zero offset");
            Assert.AreEqual(0.23f, midProjection, 1e-6f, "Appendix B: 2300 / 10000 × 1.0 = 0.23");
            Assert.Less(lowProjection, midProjection, "monotone: more training fatigue ⇒ higher offset");
            Assert.Less(midProjection, higherProjection, "monotone at an interior point, not just at the clamps");
            Assert.AreEqual(1.0f, overflowProjection, "clamped to [0,1]");
        }

        [Test]
        public void ComputeInjuryRisk_IncreasesWithFatigueAndLowCondition()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            var rested = new TrainingState
            {
                Focus = TrainingFocus.Balanced,
                Condition = TrainingSystemConstants.ConditionMax,
                TrainingFatigue = 0
            };
            var fatigued = rested;
            fatigued.Condition = TrainingSystemConstants.ConditionMin;
            fatigued.TrainingFatigue = TrainingSystemConstants.TrainingFatigueMax;

            InjuryRiskContribution restedRisk = TrainingStep.ComputeInjuryRisk(in rested, in attributes);
            InjuryRiskContribution fatiguedRisk = TrainingStep.ComputeInjuryRisk(in fatigued, in attributes);

            Assert.Less(restedRisk.RiskScore, fatiguedRisk.RiskScore);
            Assert.LessOrEqual(fatiguedRisk.RiskScore, TrainingSystemConstants.InjuryRiskMax);
        }

        // AR-1 L-3 — the state's fields are public-mutable, so an out-of-contract fatigue must not wrap
        // the risk sum negative and clamp to 0 ("the most fatigued representable player is at zero risk").
        [Test]
        public void ComputeInjuryRisk_DoesNotWrapOnAnOutOfContractFatigue()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            var corrupt = new TrainingState
            {
                Focus = TrainingFocus.Balanced,
                Condition = TrainingSystemConstants.ConditionMin,
                TrainingFatigue = int.MaxValue
            };

            InjuryRiskContribution risk = TrainingStep.ComputeInjuryRisk(in corrupt, in attributes);

            Assert.AreEqual(
                TrainingSystemConstants.InjuryRiskMax,
                risk.RiskScore,
                "clamps UP to the ceiling — never wraps negative and reports zero risk");
        }

        // The name claims both entry points, so both are driven: Create refuses the value, and
        // AdvanceTrainingDay's own ValidateFocus branch is reached through a hand-corrupted state (the
        // fields are public-mutable, and a corrupt persisted focus will arrive exactly this way at T1).
        [Test]
        public void CreateAndAdvance_RejectOutOfRangeFocusValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TrainingState.Create((TrainingFocus)99));

            TrainingState state = TrainingState.Create(TrainingFocus.Balanced);
            state.Focus = (TrainingFocus)99;
            TrainingState before = state;
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, 0));

            Assert.AreEqual(before.Condition, state.Condition, "a refused advance mutates nothing");
            Assert.AreEqual(before.TrainingFatigue, state.TrainingFatigue);
            Assert.AreEqual(before.LastAdvancedWorldDay, state.LastAdvancedWorldDay);
        }

        // Appendix B's seed: Condition 7000, TrainingFatigue 2000, Fitness, last-advanced day 100.
        private static TrainingState AppendixBSeedState() => new TrainingState
        {
            Focus = TrainingFocus.Fitness,
            Condition = 7000,
            TrainingFatigue = 2000,
            LastAdvancedWorldDay = 100
        };

        internal static TrainingFocus[] AllFocuses() => (TrainingFocus[])Enum.GetValues(typeof(TrainingFocus));
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
// | 1.1     | 2026-07-31 | —      | AR-1 M-1/M-2/M-5/L-3/L-4. +T-TR-DET-001 (the Appendix B three-day
// |         |            |        | sequence + a value-copy save whose continuation equals the
// |         |            |        | uninterrupted run, including the T-TR-FAT-001 projection identity),
// |         |            |        | +T-TR-COA-001 (Identity ⇒ exactly the catalogue deltas, every focus),
// |         |            |        | +T-TR-CON-002 (the conditioning bonus is deterministic AND genuinely
// |         |            |        | attribute-driven), +2 M-1 sentinel regression locks (fresh and
// |         |            |        | advanced state), +1 L-3 overflow lock. Two misleadingly-named tests
// |         |            |        | fixed: the focus-rejection test now also drives AdvanceTrainingDay's
// |         |            |        | own ValidateFocus branch through a hand-corrupted state, and the
// |         |            |        | projection test asserts monotonicity at an INTERIOR point plus
// |         |            |        | Appendix B's worked 0.23 instead of only the two clamped endpoints.
// |         |            |        | Constant references renamed for the M-2 catalogue migration.
#endregion
