// File:     src/client-app/tests/S0ScreenPresentationTests.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 I-Q01–14/16/18, Code Standards #20
// Purpose:  Exercise real coordinator/session/driver and admitted content through the shipping presenter path.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TacticalDirector.MatchAnalytics;
using TacticalDirector.EventSystem;
using TacticalDirector.DeterministicSim;
using TacticalDirector.Localization;
using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class S0ScreenPresentationTests
    {
        private sealed class Renderer : IMatchRendererBinding
        {
            public void Attach(MatchSession s, MatchIdentityContext i, ILiveFrameSource f, Action failed)
            {
            }

            public void Detach()
            {
            }
        }

        private ClientMatchCoordinator _shell;
        private S0ScreenPresenter _view;
        [SetUp]
        public void SetUp()
        {
            _shell = new ClientMatchCoordinator(new Renderer(), S0DemoFixture.CreateApproved());
            _view = new S0ScreenPresenter(_shell, new S0TextFormatter(), new S0PresentationConfiguration());
        }

        [TearDown]
        public void TearDown() => _shell.Dispose();
        private void Start()
        {
            _view.OpenSetup();
            _shell.StartMatchForTests();
            _view.Refresh();
        }

        private void Tick()
        {
            _shell.Session.TickOnce();
            _shell.Refresh(0f);
            _view.Refresh();
        }

        private void End()
        {
            EventBus.BeginTick((uint)_shell.Session.CurrentTick);
            EventBus.BeginPhase(PhaseId.Input);
            _shell.Session.TestOnly_Engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL);
            Tick();
        }

        [Test]
        public void WaitingWithholdsScoreAndClockAndRefreshNeverAdvances()
        {
            Start();
            Assert.AreEqual("—", _view.Score);
            Assert.AreEqual("Clock awaiting first frame", _view.Clock);
            Assert.IsFalse(_view.CanPause);
            Assert.IsFalse(_view.CanChangeMentality);
            Assert.IsFalse(_view.CanReport);
            for (int i = 0; i < 10; i++)
                _view.Refresh();
            Assert.AreEqual(0UL, _shell.Session.CurrentTick);
            Tick();
            Assert.AreEqual("0 – 0", _view.Score);
            Assert.AreEqual("Minute 0", _view.Clock);
            Assert.IsTrue(_view.CanPause);
            Assert.IsTrue(_view.CanChangeMentality);
        }

        [Test]
        public void DraftCancelDoesNotDispatchAndSetupReentryIsBalanced()
        {
            _view.OpenSetup();
            _view.SelectSetupMentality(Mentality.Attacking);
            _view.CancelSetup();
            _view.OpenSetup();
            Assert.AreEqual(Mentality.Balanced, _shell.DraftMentality);
            Assert.IsFalse(_shell.HasMatch);
            _shell.StartMatchForTests();
            Tick();
            _view.OpenMentality();
            _view.SelectRequestedMentality(Mentality.Defensive);
            _view.CancelDialog();
            Assert.AreEqual(0, _shell.Session.Commands.Count);
            Assert.AreEqual(Mentality.Balanced, _view.CurrentMentality);
        }

        [Test]
        public void PausedDistinctRequestsStayPendingThenSettleWithoutReusingOldEvidence()
        {
            Start();
            Tick();
            _view.TogglePause();
            _view.OpenMentality();
            _view.SelectRequestedMentality(Mentality.Attacking);
            ClientChangeRecord first = _view.SubmitMentality();
            _view.OpenSubstitution();
            _view.SelectOutgoing(_view.Outgoing[0].Index);
            _view.SelectIncoming(_view.Incoming[0].Index);
            ClientChangeRecord sub = _view.SubmitSubstitution();
            ulong tick = _shell.Session.CurrentTick;
            _view.Refresh();
            _view.Refresh();
            Assert.AreEqual(tick, _shell.Session.CurrentTick);
            Assert.AreEqual(2, _shell.Session.Commands.Count);
            Assert.AreEqual(ClientChangeStatus.Pending, first.Status);
            Assert.AreEqual(ClientChangeStatus.Pending, sub.Status);
            Assert.AreEqual(Mentality.Balanced, _view.CurrentMentality);
            StringAssert.Contains("resume", _view.Feedback[0]);
            Assert.IsFalse(_view.CanChangeMentality);
            Assert.IsFalse(_view.CanSubstitute);
            _view.TogglePause();
            Tick();
            Assert.AreEqual(ClientChangeStatus.Applied, first.Status);
            Assert.AreEqual(ClientChangeStatus.Applied, sub.Status);
            Assert.AreEqual(Mentality.Attacking, _view.CurrentMentality);
            _view.OpenMentality();
            _view.SelectRequestedMentality(Mentality.Attacking);
            ClientChangeRecord second = _view.SubmitMentality();
            Assert.AreEqual(ClientChangeStatus.Pending, second.Status);
            _view.Refresh();
            Assert.AreEqual(ClientChangeStatus.Pending, second.Status);
            Tick();
            Assert.AreEqual(ClientChangeStatus.Applied, second.Status);
            Assert.AreEqual(3, _view.Feedback.Count);
            StringAssert.Contains(sub.Outgoing.FirstName, _view.Feedback[1]);
            StringAssert.Contains(sub.Incoming.FirstName, _view.Feedback[1]);
        }

        [Test]
        public void FullTimeSettlesOnlyUnmatchedRequestsRetainsStatisticsAndRequiresExplicitReport()
        {
            Start();
            Tick();
            _view.ToggleStatistics();
            _view.OpenMentality();
            ClientChangeRecord pending = _view.SubmitMentality();
            End();
            Assert.AreEqual(ClientChangeStatus.NotApplied, pending.Status);
            Assert.IsTrue(_view.IsStatisticsOpen);
            Assert.IsFalse(_view.CanPause);
            Assert.IsFalse(_view.CanChangeMentality);
            Assert.IsTrue(_view.CanReport);
            Assert.AreEqual(ClientScreens.MatchView, _view.Screen);
            _view.ToggleStatistics();
            Assert.IsTrue(_view.IsStatisticsOpen);
            _view.ShowReport();
            Assert.AreEqual(ClientScreens.PostMatchReport, _view.Screen);
            _view.ReturnToMenu();
            Assert.IsFalse(_shell.HasMatch);
            Assert.AreEqual(0, _view.Feedback.Count);
            Assert.IsFalse(_view.IsStatisticsOpen);
            Start();
            Tick();
            Assert.AreEqual(0, _view.Feedback.Count);
            Assert.AreEqual(Mentality.Balanced, _view.CurrentMentality);
        }

        [Test]
        public void AppliedBeforeWhistleStaysAppliedAndChoiceHistoryDoesNotChange()
        {
            Start();
            Tick();
            _view.OpenMentality();
            _view.SelectRequestedMentality(Mentality.Positive);
            ClientChangeRecord request = _view.SubmitMentality();
            Tick();
            End();
            Assert.AreEqual(ClientChangeStatus.Applied, request.Status);
            StringAssert.Contains("Applied", _view.Feedback[0]);
        }

        [Test]
        public void KeeperAndBenchChoicesUseAuthoredIdentityAndStalePairSendsNothing()
        {
            Start();
            Tick();
            Assert.AreEqual(MatchEngineConstants.PLAYERS_PER_TEAM, _view.Outgoing.Count);
            Assert.AreEqual(MatchEngineConstants.SUBSTITUTES_PER_TEAM, _view.Incoming.Count);
            Assert.That(_view.Outgoing[0].Label, Does.Contain("shirt"));
            bool keeper = false;
            foreach (var c in _view.Outgoing)
                keeper |= c.Label.Contains("goalkeeper");
            Assert.IsTrue(keeper);
            _view.OpenSubstitution();
            _view.SelectOutgoing(_view.Outgoing[0].Index);
            _view.SelectIncoming(_view.Incoming[0].Index);
            // An accepted replacement invalidates a previously staged slot/identity pair.
            _shell.Session.TestOnly_Engine.SubstitutePlayer(0, _view.SelectedOutgoing, _view.SelectedIncoming, SubstitutionReason.Tactical);
            Tick();
            Assert.IsNull(_view.SubmitSubstitution());
            Assert.AreEqual(0, _shell.Session.Commands.Count);
            StringAssert.Contains("no longer available", _view.ChoiceNotice);
            Assert.AreEqual(MatchEngineConstants.SUBSTITUTES_PER_TEAM - 1, _view.Incoming.Count);
        }

        [Test]
        public void PlaybackLadderClampsAndPausePreservesSelectedRung()
        {
            Start();
            Tick();
            Assert.IsFalse(_view.CanSlower);
            for (int i = 0; i < 8; i++)
                _view.Faster();
            Assert.IsFalse(_view.CanFaster);
            StringAssert.Contains("10×", _view.Speed);
            _view.TogglePause();
            StringAssert.Contains("Paused", _view.Speed);
            _view.TogglePause();
            StringAssert.Contains("10×", _view.Speed);
            for (int i = 0; i < 8; i++)
                _view.Slower();
            Assert.IsFalse(_view.CanSlower);
            StringAssert.Contains("1×", _view.Speed);
        }

        [Test]
        public void StatisticsUseOneRealPublicationAndDoNotOfferUnavailableXgOrShots()
        {
            Start();
            Tick();
            Assert.IsTrue(_view.HasStatistics);
            Assert.AreEqual(11, _view.Statistics.Count);
            bool substitutions = false;
            foreach (var row in _view.Statistics)
            {
                Assert.AreNotEqual("Shots", row.Label);
                Assert.AreNotEqual("Expected goals (xG)", row.Label);
                substitutions |= row.Label == "Substitutions";
            }

            Assert.IsTrue(substitutions);
            ulong tick = _shell.Session.CurrentTick;
            _view.ToggleStatistics();
            _view.ToggleStatistics();
            Assert.AreEqual(tick, _shell.Session.CurrentTick);
        }

        private sealed class FaultingSample : IWorldStateSample
        {
            private readonly IWorldStateSample _inner;
            internal FaultingSample(IWorldStateSample inner)
            {
                _inner = inner;
            }

            public Vector2 BallPosition => throw new ArgumentException("controlled observer failure");
            public int AgentCount => _inner.AgentCount;

            public Vector2 AgentPosition(int i) => _inner.AgentPosition(i);
            public int AgentTeamId(int i) => _inner.AgentTeamId(i);
            public bool AgentIsActive(int i) => _inner.AgentIsActive(i);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActualObserverFaultKeepsScoreAndReportUsableWithInitiallyHiddenPartials(bool beforeFirst)
        {
            Start();
            if (!beforeFirst)
                Tick();
            SessionMatchAnalytics analytics = _shell.Context.Analytics;
            FieldInfo sample = typeof(SessionMatchAnalytics).GetField("_sample", BindingFlags.Instance | BindingFlags.NonPublic);
            sample.SetValue(analytics, new FaultingSample((IWorldStateSample)sample.GetValue(analytics)));
            Tick();
            Assert.IsTrue(_view.IsStatisticsIncomplete);
            StringAssert.Contains("incomplete", _view.StatisticsNotice);
            Assert.IsFalse(_view.IsStatisticsOpen);
            Assert.IsTrue(_view.CanPause);
            ulong previous = _view.Frame.Tick;
            Tick();
            Assert.Greater(_view.Frame.Tick, previous);
            Assert.AreEqual(!beforeFirst, _view.HasStatistics);
            End();
            _view.ShowReport();
            Assert.IsFalse(_view.IsPartialReportOpen);
            StringAssert.Contains("Final score remains available", _view.StatisticsNotice);
            _view.TogglePartialReport();
            Assert.AreEqual(!beforeFirst, _view.IsPartialReportOpen);
            _view.ReturnToMenu();
            Assert.AreEqual(ClientScreens.MainMenu, _view.Screen);
        }

        [Test]
        public void RealDrainRefusalShowsGenericPersistentOutcomeAndNoPredictedReplacement()
        {
            Start();
            Tick();
            _view.OpenSubstitution();
            _view.SelectOutgoing(_view.Outgoing[0].Index);
            _view.SelectIncoming(_view.Incoming[0].Index);
            ClientChangeRecord request = _view.SubmitSubstitution();
            // The accepted frame was legal at Submit; the engine now owns a different legal state.
            _shell.Session.TestOnly_Engine.SubstitutePlayer(0, request.Command.OutSlotIndex, request.Command.BenchIndex, SubstitutionReason.Tactical);
            Tick();
            Assert.AreEqual(ClientChangeStatus.Refused, request.Status);
            StringAssert.Contains("Refused", _view.Feedback[0]);
            Assert.IsFalse(_view.Feedback[0].Contains("private"));
            _view.Refresh();
            StringAssert.Contains(request.Outgoing.FirstName, _view.Feedback[0]);
        }

        [TestCase(0UL, 0)]
        [TestCase(3599UL, 0)]
        [TestCase(3600UL, 1)]
        [TestCase(324000UL, 90)]
        public void ClockUsesPhysicsTicks(ulong tick, int minute) => Assert.AreEqual(minute, S0ScreenPresenter.Minute(tick));
        [Test]
        public void AllMentalitiesHaveLabelsAndEffects()
        {
            Assert.AreEqual(7, S0ScreenPresenter.Mentalities.Count);
            foreach (Mentality m in S0ScreenPresenter.Mentalities)
                StringAssert.Contains("line", _view.MentalityChoice(m));
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Real composed screen and driver scenarios; no Unity visual claim. |
#endregion
