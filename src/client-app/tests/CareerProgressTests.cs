// File:     src/client-app/tests/CareerProgressTests.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-3), Season & Competition Loop
//           #30 §3.3 (KD-4) / §3.5 (ERR-030-053), Code Standards #20
// Purpose:  Locks the day-granular progress rule against #30's own guards: every offered action is
//           accepted, the actions NOT offered are the ones #30 refuses, and a whole season plus the roll
//           and the next season's first matchday are reachable by Progress alone.

using System;

using NUnit.Framework;

using TacticalDirector.SeasonSave;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class CareerProgressTests
    {
        [Test]
        public void NewCareer_BeforeTheFirstFixtureDay_OffersAdvanceDay()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);

            Assert.AreEqual(CareerProgressAction.AdvanceDay, session.NextAction);
            Assert.Greater(CareerProgress.DaysUntilNextMatchday(session.Loop), 0);
        }

        [Test]
        public void OnMatchday_PlayIsOffered_AndTheWithheldDayAdvanceIsExactlyWhatSeasonLoopRefuses()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);

            int days = CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);

            Assert.Greater(days, 0, "the first fixture day is in the future at day 0.");
            Assert.AreEqual(0, CareerProgress.DaysUntilNextMatchday(session.Loop));
            Assert.AreEqual(session.Loop.State.Calendar.NextFixtureDay(), session.Loop.CurrentWorldDay);
            // Positive control for the rule: the action the client withholds on matchday is refused by #30.
            Assert.Throws<InvalidOperationException>(() => session.Loop.AdvanceDays(1));
        }

        [Test]
        public void BeforeMatchday_ThePlayTheClientWithholds_IsWhatSeasonLoopRefuses()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);

            Assert.AreEqual(CareerProgressAction.AdvanceDay, session.NextAction);
            Assert.Throws<InvalidOperationException>(() => session.Loop.AdvanceAndPlayNextRound());
            Assert.Throws<InvalidOperationException>(() => session.Execute(CareerProgressAction.PlayMatchday),
                "the session refuses an action that is not the legal one.");
        }

        [Test]
        public void AWholeSeason_TheRoll_AndTheNextSeasonsFirstMatchday_AreReachableByProgressAlone()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            int roundCount = session.Loop.State.Calendar.RoundCount;
            int rounds = 0;
            int rolls = 0;

            for (int step = 0; step < 5000 && rolls == 0; step++)
            {
                CareerProgressAction action = session.NextAction;
                Assert.AreNotEqual(CareerProgressAction.None, action);
                CareerChange change = session.Progress();
                if (action == CareerProgressAction.PlayMatchday)
                {
                    rounds++;
                    Assert.AreEqual(CareerChangeKind.RoundPlayed, change.Kind);
                    Assert.IsTrue(change.HasManagedResult, "every round of a round-robin includes the managed club.");
                }
                else if (action == CareerProgressAction.StartNextSeason)
                {
                    rolls++;
                    Assert.AreEqual(CareerChangeKind.SeasonRolled, change.Kind);
                    Assert.AreEqual(0, change.Roll.CompletedSeasonNumber, "#30 counts seasons from 0.");
                }
            }

            Assert.AreEqual(roundCount, rounds, "every round is played exactly once before the roll.");
            Assert.AreEqual(1, rolls);
            Assert.AreEqual(1, session.Loop.State.SeasonNumber);

            CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);
            Assert.AreEqual(CareerChangeKind.RoundPlayed, session.Progress().Kind, "season 2 plays.");
        }

        [Test]
        public void AfterTheLastRound_OneDayAdvanceComesBeforeTheRoll_AsErr030053Requires()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            int roundCount = session.Loop.State.Calendar.RoundCount;

            while (session.Loop.State.Calendar.NextRoundIndex < roundCount)
            {
                CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);
                session.Progress();
            }

            Assert.IsTrue(session.Loop.IsSeasonComplete);
            Assert.AreEqual(CareerProgressAction.AdvanceDay, session.NextAction);
            Assert.Throws<InvalidOperationException>(() => session.Loop.RollToNextSeason(),
                "positive control: #30 refuses the roll the client withholds.");
            Assert.AreEqual(-1, CareerProgress.DaysUntilNextMatchday(session.Loop));

            session.Progress();
            Assert.AreEqual(CareerProgressAction.StartNextSeason, session.NextAction);
        }

        [Test]
        public void IntentMapping_RoundTripsEveryAction()
        {
            foreach (CareerProgressAction action in new[]
            {
                CareerProgressAction.AdvanceDay, CareerProgressAction.PlayMatchday, CareerProgressAction.StartNextSeason,
            })
            {
                ManagerIntent intent = CareerProgress.IntentFor(action);
                Assert.IsTrue(IntentRouting.IsSeasonIntent(intent.Kind));
                Assert.AreEqual(action, CareerProgress.ActionFor(intent.Kind));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => CareerProgress.IntentFor(CareerProgressAction.None));
            Assert.AreEqual(CareerProgressAction.None, CareerProgress.ActionFor(IntentKind.SetTeamTactic));
            Assert.Throws<ArgumentNullException>(() => CareerProgress.NextAction(null));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): 6 progress-rule locks with #30       |
// |         |            |        | positive controls.                                             |
#endregion
