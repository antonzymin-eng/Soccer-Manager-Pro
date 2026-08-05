// File:     src/training-system/tests/TrainingStepTests.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §3.1–§3.4 + Appendices B/C; Code Standards #20
// Purpose:  T-TR-DET-001/003/004/005, T-TR-NEU-001/002, T-TR-FAT-001/003, T-TR-CON-001/002,
//           T-TR-COA-001, T-TR-INJ-001 — the Appendix B worked example day by day, the idempotency
//           and day-gap gates, the behaviour-neutral identity, and the two own-attribute terms.

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;

namespace TacticalDirector.TrainingSystem.Tests
{
    [TestFixture]
    public sealed class TrainingStepTests
    {
        // Appendix B's seed: Fitness focus, mid-career cursors, last advanced on world day 100.
        private const uint AppendixBLastDay = 100;
        private const int AppendixBCondition = 7000;
        private const int AppendixBFatigue = 2000;

        private static PlayerAttributes NeutralAttributes() => PlayerAttributes.CreateDefault();

        private static TrainingState AppendixBSeed()
        {
            TrainingState s = TrainingState.Create(TrainingFocus.Fitness);
            s.Condition = AppendixBCondition;
            s.TrainingFatigue = AppendixBFatigue;
            s.LastAdvancedWorldDay = AppendixBLastDay;
            return s;
        }

        private static void Advance(ref TrainingState s, uint worldDay)
        {
            PlayerAttributes a = NeutralAttributes();
            TrainingStep.AdvanceTrainingDay(ref s, a, CoachingModifier.Identity, worldDay);
        }

        // ── Appendix B — the worked example, day by day ──────────────────────────────

        [Test]
        public void AppendixB_FitnessWeek_ReproducesTheWorkedExample()
        {
            TrainingState s = AppendixBSeed();

            // Neutral attributes ⇒ AttributeConditioningBonus = mean(WorkRate 10, Stamina 10) × 2 = 20,
            // so the daily conditioning delta is FocusConditionDelta[Fitness] 120 + 20 = 140, and the
            // net daily fatigue is load 300 − recovery 200 = +100.
            Advance(ref s, 101);
            Assert.AreEqual(7140, s.Condition);
            Assert.AreEqual(2100, s.TrainingFatigue);
            Assert.AreEqual(101u, s.LastAdvancedWorldDay);

            Advance(ref s, 102);
            Assert.AreEqual(7280, s.Condition);
            Assert.AreEqual(2200, s.TrainingFatigue);

            Advance(ref s, 103);
            Assert.AreEqual(7420, s.Condition);
            Assert.AreEqual(2300, s.TrainingFatigue);

            Assert.AreEqual(0.23f, TrainingStep.ProjectMatchEntryFatigue(s), 1e-6f,
                "Appendix B pins the projection at 2300 / 10000 × 1.0 after day 103.");
        }

        [Test]
        public void SaveRestore_AcrossMidWeek_ContinuesIdentically_TTRDET001()
        {
            TrainingState uninterrupted = AppendixBSeed();
            Advance(ref uninterrupted, 101);
            Advance(ref uninterrupted, 102);
            Advance(ref uninterrupted, 103);

            // At T0 there is no codec yet, so the "save" is a value-copy of the whole state — value
            // semantics stand in for the T1 round-trip, which T-TR-FAIL-001/002 lock at T1 (the #28 T0
            // precedent). What this pins is that NOTHING outside TrainingState carries continuation
            // state: if the step depended on any ambient cursor, the copy would diverge.
            TrainingState restored = uninterrupted;

            Advance(ref uninterrupted, 104);
            Advance(ref restored, 104);

            Assert.AreEqual(uninterrupted.Condition, restored.Condition);
            Assert.AreEqual(uninterrupted.TrainingFatigue, restored.TrainingFatigue);
            Assert.AreEqual(uninterrupted.Focus, restored.Focus);
            Assert.AreEqual(uninterrupted.LastAdvancedWorldDay, restored.LastAdvancedWorldDay);
            Assert.AreEqual(7560, restored.Condition, "Appendix B's post-restore day 104 value.");
            Assert.AreEqual(2400, restored.TrainingFatigue);
        }

        // ── Idempotency, day-0, day-gap ──────────────────────────────────────────────

        [Test]
        public void AlreadyAdvancedDay_IsANoOp_TTRDET003()
        {
            TrainingState s = AppendixBSeed();
            Advance(ref s, 101);
            TrainingState afterFirst = s;

            Advance(ref s, 101);
            Advance(ref s, 100);   // an older day is equally a no-op

            Assert.AreEqual(afterFirst.Condition, s.Condition, "re-running an advanced day must not double-accrue (F6).");
            Assert.AreEqual(afterFirst.TrainingFatigue, s.TrainingFatigue);
            Assert.AreEqual(afterFirst.LastAdvancedWorldDay, s.LastAdvancedWorldDay);
        }

        [Test]
        public void FreshState_AdvancesWorldDayZero_TTRDET004()
        {
            TrainingState s = TrainingState.Create(TrainingFocus.Balanced);
            Assert.AreEqual(TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL, s.LastAdvancedWorldDay);

            Advance(ref s, 0);

            // The whole point of the sentinel: world day 0 is a legitimate first day. With a 0 sentinel
            // this call would have read as "already advanced" and the player would never train.
            Assert.AreEqual(0u, s.LastAdvancedWorldDay);
            Assert.AreNotEqual(TrainingSystemConstants.ConditionStart, s.Condition,
                "day 0 must actually accrue, not be swallowed by the idempotency guard.");
        }

        [Test]
        public void DayGap_FailsLoud_TTRDET005()
        {
            TrainingState s = AppendixBSeed();
            PlayerAttributes a = NeutralAttributes();

            Assert.Throws<ArgumentException>(
                () => TrainingStep.AdvanceTrainingDay(ref s, a, CoachingModifier.Identity, AppendixBLastDay + 2),
                "#29 does not batch-replay a gap — a skipped day is a caller bug (F7 / FR-TR-026).");

            Assert.AreEqual(AppendixBCondition, s.Condition, "a refused advance mutates nothing.");
            Assert.AreEqual(AppendixBLastDay, s.LastAdvancedWorldDay);
        }

        [Test]
        public void AdvancingTheSentinelDay_FailsLoud()
        {
            TrainingState s = AppendixBSeed();
            PlayerAttributes a = NeutralAttributes();

            Assert.Throws<ArgumentException>(
                () => TrainingStep.AdvanceTrainingDay(
                    ref s, a, CoachingModifier.Identity, TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL),
                "storing the sentinel as a real advanced day would re-arm the day-0 trap on the next call.");
        }

        [Test]
        public void UndefinedFocusOnState_FailsLoud_F4()
        {
            TrainingState s = AppendixBSeed();
            s.Focus = (TrainingFocus)200;
            PlayerAttributes a = NeutralAttributes();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => TrainingStep.AdvanceTrainingDay(ref s, a, CoachingModifier.Identity, 101));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TrainingStep.ComputeTrainingInput(s, a, CoachingModifier.Identity, deepTrainingEnabled: false));
        }

        // ── Behaviour-neutral identity (Appendix C) ──────────────────────────────────

        [Test]
        public void ComputeTrainingInput_DialOff_IsExactlyNeutral_TTRNEU001()
        {
            PlayerAttributes a = NeutralAttributes();

            foreach (TrainingFocus focus in Enum.GetValues(typeof(TrainingFocus)))
            {
                TrainingState s = TrainingState.Create(focus);
                s.TrainingFatigue = 4321;
                s.Condition = 1234;

                TrainingInput result =
                    TrainingStep.ComputeTrainingInput(s, a, CoachingModifier.Identity, deepTrainingEnabled: false);

                Assert.AreEqual(TrainingInput.Neutral, result,
                    "with #29's own dial off, #28's growth must be byte-identical to the no-training path (KD-8).");
            }

            // NOT TESTED, and deliberately not faked: FR-TR-006's field-independence invariant — that
            // the slot-1 read touches only Focus/attributes/coach and never a field slot-2 mutates.
            // #28's TrainingInput has no fields yet, so BOTH branches return Neutral and any comparison
            // of two results is vacuously true no matter what the deep branch reads. A test shaped like
            // that lock would enforce nothing while claiming the id. Nor is "ComputeTrainingInput does
            // not mutate the state" worth asserting: the parameter is `in`, so the compiler already
            // forbids it. The real lock lands with #29 T3, when BuildTrainingInput has an output that
            // can differ.
        }

        [Test]
        public void AdvanceTrainingDay_NeverWritesAttributes_TTRNEU002()
        {
            TrainingState s = AppendixBSeed();
            PlayerAttributes a = NeutralAttributes();
            PlayerAttributes copy = a;

            TrainingStep.AdvanceTrainingDay(ref s, a, CoachingModifier.Identity, 101);

            // #28's GrowthProjection is the sole attribute writer (FR-TR-005 / FR-PG-008). #29 supplies
            // a TrainingInput; it never becomes a second mutation path.
            // This is REDUNDANT with the compiler today and says so deliberately: `attributes` is an `in`
            // parameter, so the callee cannot assign to it and the assertion cannot fail. It is kept as a
            // guard on the SIGNATURE — it starts failing if `in` is ever relaxed to `ref` — not as
            // evidence about the body.
            Assert.AreEqual(copy.ToArray(), a.ToArray());
            Assert.AreEqual(copy.WeakFootRating, a.WeakFootRating);
        }

        // ── Conditioning, fatigue, clamps ────────────────────────────────────────────

        [Test]
        public void Condition_ClampsAtBothBounds_AndRestLowersFatigue_TTRCON001()
        {
            TrainingState high = TrainingState.Create(TrainingFocus.Fitness);
            high.Condition = TrainingSystemConstants.ConditionMax;
            high.LastAdvancedWorldDay = 500;
            Advance(ref high, 501);
            Assert.AreEqual(TrainingSystemConstants.ConditionMax, high.Condition, "F1 ceiling.");

            TrainingState rested = TrainingState.Create(TrainingFocus.Rest);
            rested.TrainingFatigue = 150;
            rested.LastAdvancedWorldDay = 500;
            Advance(ref rested, 501);
            Assert.AreEqual(0, rested.TrainingFatigue, "F1 floor — Rest nets negative and clamps at 0, never below.");

            TrainingState saturating = TrainingState.Create(TrainingFocus.Physical);
            saturating.TrainingFatigue = TrainingSystemConstants.TrainingFatigueMax;
            saturating.LastAdvancedWorldDay = 500;
            Advance(ref saturating, 501);
            Assert.AreEqual(TrainingSystemConstants.TrainingFatigueMax, saturating.TrainingFatigue, "F1 ceiling.");
        }

        [Test]
        public void AttributeConditioningBonus_IsDeterministicAndMonotone_TTRCON002()
        {
            PlayerAttributes low = NeutralAttributes();
            low.WorkRate = 4;
            low.Stamina = 4;

            PlayerAttributes high = NeutralAttributes();
            high.WorkRate = 18;
            high.Stamina = 18;

            Assert.AreEqual(
                TrainingStep.AttributeConditioningBonus(low),
                TrainingStep.AttributeConditioningBonus(low),
                "identical inputs must give identical output — the term is deterministic, never RNG (FR-TR-009).");
            Assert.Greater(
                TrainingStep.AttributeConditioningBonus(high),
                TrainingStep.AttributeConditioningBonus(low));
        }

        [Test]
        public void IdentityCoach_YieldsTheExactStage2Delta_TTRCOA001()
        {
            PlayerAttributes a = NeutralAttributes();
            int expectedConditionDelta = TrainingSystemConstants.ConditionDeltaFor(TrainingFocus.Fitness)
                                         + TrainingStep.AttributeConditioningBonus(a);
            int expectedFatigueDelta = TrainingSystemConstants.FatigueLoadFor(TrainingFocus.Fitness)
                                       - TrainingSystemConstants.FatigueDailyRecovery;

            TrainingState s = AppendixBSeed();
            Advance(ref s, AppendixBLastDay + 1);

            // Asserted through the DAY STEP, on the state it produces — not against ApplyCoach itself.
            // ApplyCoach is the identity function today, so `ApplyCoach(x, Identity) == x` holds for
            // every possible modifier and could never fail; this version starts failing the moment #34
            // makes the seam non-identity without keeping the identity path exact, which is the whole
            // content of KD-3.
            Assert.AreEqual(AppendixBCondition + expectedConditionDelta, s.Condition,
                "KD-3: the identity modifier is ×1.0, so a no-staff game runs the raw Stage-2 deltas.");
            Assert.AreEqual(AppendixBFatigue + expectedFatigueDelta, s.TrainingFatigue);
        }

        // ── The match-entry projection ───────────────────────────────────────────────

        [Test]
        public void MatchEntryFatigue_IsMonotoneAndBounded_TTRFAT003()
        {
            TrainingState rested = TrainingState.Create(TrainingFocus.Balanced);
            TrainingState tired = TrainingState.Create(TrainingFocus.Balanced);
            tired.TrainingFatigue = TrainingSystemConstants.TrainingFatigueMax;

            float restedValue = TrainingStep.ProjectMatchEntryFatigue(rested);
            float tiredValue = TrainingStep.ProjectMatchEntryFatigue(tired);

            // The project-wide convention: 0 = fully rested, 1 = fully fatigued. An inversion here
            // would hand the match engine a rested player's fatigue as 1.0.
            Assert.AreEqual(0f, restedValue, 1e-6f);
            Assert.GreaterOrEqual(tiredValue, restedValue);
            Assert.LessOrEqual(tiredValue, 1f);
            Assert.GreaterOrEqual(tiredValue, 0f);
        }

        [Test]
        public void MatchEntryFatigue_IsRecomputed_NotStored_TTRFAT001()
        {
            TrainingState s = AppendixBSeed();
            Advance(ref s, 101);

            TrainingState restored = s;

            Assert.AreEqual(TrainingStep.ProjectMatchEntryFatigue(s),
                            TrainingStep.ProjectMatchEntryFatigue(restored), 1e-6f,
                "the projection is derived from the serialized accumulator, so it cannot drift across a " +
                "save boundary — there is nothing extra to persist (KD-1).");
        }

        // ── The injury-risk output #41 consumes ──────────────────────────────────────

        [Test]
        public void InjuryRisk_MonotoneInFatigue_InverseInCondition_Clamped_TTRINJ001()
        {
            PlayerAttributes a = NeutralAttributes();

            TrainingState fresh = TrainingState.Create(TrainingFocus.Balanced);
            fresh.Condition = TrainingSystemConstants.ConditionMax;
            fresh.TrainingFatigue = 0;

            TrainingState worn = TrainingState.Create(TrainingFocus.Balanced);
            worn.Condition = TrainingSystemConstants.ConditionMax;
            worn.TrainingFatigue = 3000;

            TrainingState unfit = TrainingState.Create(TrainingFocus.Balanced);
            unfit.Condition = TrainingSystemConstants.ConditionMin;
            unfit.TrainingFatigue = 0;

            int freshRisk = TrainingStep.ComputeInjuryRisk(fresh, a).RiskScore;
            int wornRisk = TrainingStep.ComputeInjuryRisk(worn, a).RiskScore;
            int unfitRisk = TrainingStep.ComputeInjuryRisk(unfit, a).RiskScore;

            Assert.Greater(wornRisk, freshRisk, "risk rises with the training-fatigue accumulator.");
            Assert.Greater(unfitRisk, freshRisk, "risk rises as conditioning falls.");
            Assert.GreaterOrEqual(freshRisk, 0, "the clamp floor — a robustness mitigation larger than the raw risk must not go negative.");
            Assert.LessOrEqual(unfitRisk, TrainingSystemConstants.InjuryRiskMax, "the clamp ceiling.");
        }

        [Test]
        public void InjuryRisk_IsMitigatedByOwnRobustnessAttributes()
        {
            TrainingState s = AppendixBSeed();

            PlayerAttributes frail = NeutralAttributes();
            frail.Strength = 3;
            frail.Stamina = 3;
            frail.Balance = 3;

            PlayerAttributes robust = NeutralAttributes();
            robust.Strength = 18;
            robust.Stamina = 18;
            robust.Balance = 18;

            Assert.Greater(
                TrainingStep.ComputeInjuryRisk(s, frail).RiskScore,
                TrainingStep.ComputeInjuryRisk(s, robust).RiskScore,
                "FR-TR-009: the mitigation is a deterministic function of the player's own attributes.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0).                                   |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (M): T-TR-COA-001 re-pointed at the day step (it had     |
// |         |            |        | asserted the identity function is the identity); the NEU-001       |
// |         |            |        | field-independence assertion replaced with the purity check that   |
// |         |            |        | IS assertable today, and the untestable half named in the test.    |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): v1.1's second clause describes a state this file    |
// |         |            |        | never shipped in — AR pass 2 deleted that purity check as          |
// |         |            |        | tautological in its turn (`in` forbids the mutation it asserted    |
// |         |            |        | against), leaving only the comment naming what cannot yet be       |
// |         |            |        | tested. Corrected by appending, not by editing the row.            |
#endregion
