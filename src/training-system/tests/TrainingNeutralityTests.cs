// File:     src/training-system/tests/TrainingNeutralityTests.cs
// Created:  2026-07-31
// Author:   —
// Modified: 2026-07-31
// Spec:     Training System #29 §5.2 (T-TR-NEU-001 / T-TR-NEU-002) + Appendix C; §3.2; Code Standards #20
// Purpose:  The KD-8 behaviour-neutral identity locks over the ComputeTrainingInput growth seam —
//           the pair the shipped landing could not have (ComputeTrainingInput did not exist).

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;

namespace TacticalDirector.TrainingSystem.Tests
{
    /// <summary>
    /// KD-8: with #29's own <c>deepTrainingEnabled</c> dial off, training must be invisible to #28 —
    /// the growth step has to be byte-identical to a career with no training system at all. These are
    /// the locks that keep the Stage-2 landing from quietly changing progression.
    /// </summary>
    [TestFixture]
    public sealed class TrainingNeutralityTests
    {
        private const uint BaseDay = 100000; // large, so BirthWorldDay stays non-negative (the #28 convention)

        // T-TR-NEU-001, part 1 — for EVERY focus, the dial-off growth input is exactly Neutral.
        [Test]
        public void ComputeTrainingInput_DialOff_IsNeutralForEveryFocus()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            foreach (TrainingFocus focus in TrainingStepTests.AllFocuses())
            {
                TrainingState state = TrainingState.Create(focus);

                TrainingInput input = TrainingStep.ComputeTrainingInput(
                    in state, in attributes, in coach, deepTrainingEnabled: false);

                Assert.AreEqual(TrainingInput.Neutral, input, $"focus {focus} must contribute nothing at Stage-2");
            }
        }

        // T-TR-NEU-001, part 2 — the property that actually matters downstream: feeding #29's computed
        // input into #28's growth step must leave the player byte-identical to the no-training path.
        // Comparing against TrainingInput.Neutral is the "no training" reference #28 itself uses
        // (T-PG-ID-002), and this run stays meaningful once TrainingInput gains Stage-3 fields: it then
        // fails the moment the dial-off path stops being the identity.
        [Test]
        public void GrowthOverAComputedDialOffInput_IsByteIdenticalToTheNoTrainingPath()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            foreach (TrainingFocus focus in TrainingStepTests.AllFocuses())
            {
                TrainingState trainingState = TrainingState.Create(focus);
                (PlayerRecord trained, PlayerLifecycle trainedLife) = NewPlayer();
                (PlayerRecord untrained, PlayerLifecycle untrainedLife) = NewPlayer();

                for (uint day = BaseDay; day <= BaseDay + 400; day++)
                {
                    // slot-1: #29's growth-input read (never mutates the training state, FR-TR-006) …
                    TrainingInput input = TrainingStep.ComputeTrainingInput(
                        in trainingState, in attributes, in coach, deepTrainingEnabled: false);
                    GrowthProjection.AdvanceDayForPlayer(ref trained, ref trainedLife, day, input, curveEnabled: false);

                    // … versus the same day with no training system in the picture at all.
                    GrowthProjection.AdvanceDayForPlayer(
                        ref untrained, ref untrainedLife, day, TrainingInput.Neutral, curveEnabled: false);

                    // slot-2: #29's own state still evolves — that is not supposed to reach #28.
                    TrainingStep.AdvanceTrainingDay(ref trainingState, in attributes, in coach, day);
                }

                Assert.AreNotEqual(
                    TrainingSystemConstants.ConditionStart,
                    trainingState.Condition,
                    $"non-vacuity: {focus} must actually have trained over the run");
                CollectionAssert.AreEqual(
                    untrained.Attributes.ToArray(),
                    trained.Attributes.ToArray(),
                    $"attributes after a {focus} regime must equal the no-training path");
                Assert.AreEqual(untrainedLife.CurrentAbility, trainedLife.CurrentAbility, "CA");
                Assert.AreEqual(untrainedLife.PotentialAbility, trainedLife.PotentialAbility, "PA");
                Assert.AreEqual(untrainedLife.GrowthCursor, trainedLife.GrowthCursor, "growth cursor");
                Assert.AreEqual(untrained.Age, trained.Age, "derived age");
            }
        }

        // T-TR-NEU-002 — #29 is not a second attribute writer (FR-TR-005): the daily step changes only
        // Condition / TrainingFatigue / the cursor, and never a PlayerAttributes field. The `in`
        // parameter makes that true by construction today; the lock is what keeps it true if the
        // signature is ever widened.
        [Test]
        public void AdvanceTrainingDay_NeverMutatesAPlayerAttributesField()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            int[] before = attributes.ToArray();
            int weakFootBefore = attributes.WeakFootRating;
            CoachingModifier coach = CoachingModifier.Identity;

            foreach (TrainingFocus focus in TrainingStepTests.AllFocuses())
            {
                TrainingState state = TrainingState.Create(focus);
                for (uint day = 0; day <= 30; day++)
                {
                    TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, day);
                }

                Assert.AreNotEqual(
                    TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                    state.LastAdvancedWorldDay,
                    $"non-vacuity: the {focus} run must have advanced");
                CollectionAssert.AreEqual(before, attributes.ToArray(), $"attributes after a {focus} regime");
                Assert.AreEqual(weakFootBefore, attributes.WeakFootRating, "WeakFootRating");
            }
        }

        // ComputeTrainingInput is a READ: it must leave the training state untouched (FR-TR-006), which
        // is what makes the slot-1 / slot-2 order irrelevant at #30's day-advance loop (KD-2).
        [Test]
        public void ComputeTrainingInput_NeverMutatesTheTrainingState()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;
            var state = new TrainingState
            {
                Focus = TrainingFocus.Physical,
                Condition = 6543,
                TrainingFatigue = 1234,
                LastAdvancedWorldDay = 77
            };
            TrainingState before = state;

            TrainingStep.ComputeTrainingInput(in state, in attributes, in coach, deepTrainingEnabled: false);
            TrainingStep.ComputeTrainingInput(in state, in attributes, in coach, deepTrainingEnabled: true);

            Assert.AreEqual(before.Focus, state.Focus);
            Assert.AreEqual(before.Condition, state.Condition);
            Assert.AreEqual(before.TrainingFatigue, state.TrainingFatigue);
            Assert.AreEqual(before.LastAdvancedWorldDay, state.LastAdvancedWorldDay);
        }

        // The dial-ON path is an inert identity at Stage-2 (§3.2's deep BuildTrainingInput is Stage-3):
        // it must not throw — a #30 integration that sets the dial early has to keep running — and it
        // must contribute nothing until the deep weighting lands. This lock is the tripwire that will
        // fail, deliberately, when Stage-3 makes the dial mean something.
        [Test]
        public void ComputeTrainingInput_DialOn_IsStillTheStage2Identity()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;
            TrainingState state = TrainingState.Create(TrainingFocus.Technical);

            TrainingInput input = TrainingStep.ComputeTrainingInput(
                in state, in attributes, in coach, deepTrainingEnabled: true);

            Assert.AreEqual(TrainingInput.Neutral, input, "Stage-2: the deep weighting is not built yet (§3.2)");
        }

        // An out-of-contract focus fails loud at the consuming seam, on the pure read too (F4 / FR-TR-021).
        [Test]
        public void ComputeTrainingInput_RejectsAnOutOfRangeFocus()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;
            TrainingState state = TrainingState.Create(TrainingFocus.Balanced);
            state.Focus = (TrainingFocus)99;

            Assert.Throws<System.ArgumentOutOfRangeException>(() => TrainingStep.ComputeTrainingInput(
                in state, in attributes, in coach, deepTrainingEnabled: false));
        }

        private static (PlayerRecord, PlayerLifecycle) NewPlayer()
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(1);
            var life = new PlayerLifecycle
            {
                PotentialAbility = PlayerProgressionConstants.ABILITY_MAX, // no ceiling in these runs
                CurrentAbility = 0,
                GrowthCursor = 0,
                BirthWorldDay = BaseDay - (uint)(18 * PlayerProgressionConstants.DAYS_PER_YEAR),
                RetirementFlag = false,
                RetirementDay = 0
            };
            return (rec, life);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-31 | —      | AR-1 H-1: T-TR-NEU-001 (dial-off ⇒ Neutral for every focus, and a
// |         |            |        | 401-day run through #28's GrowthProjection byte-identical to the
// |         |            |        | no-training path) + T-TR-NEU-002 (the daily step never touches a
// |         |            |        | PlayerAttributes field), plus the read-purity, dial-on Stage-2
// |         |            |        | identity, and F4 fail-loud locks on ComputeTrainingInput.          |
#endregion
