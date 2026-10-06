// File:     src/client-app/tests/ClientMatchCoordinatorTests.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Exercises real session/engine composition, stable registrations and lifecycle cleanup.

using System;
using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class ClientMatchCoordinatorTests
    {
        private sealed class Renderer : IMatchRendererBinding
        {
            internal MatchSession Session;
            internal MatchIdentityContext Identity;
            internal ILiveFrameSource Frames;
            internal Action Failure;
            internal int AttachCount;
            internal int DetachCount;
            internal Action DuringAttach;
            internal bool ThrowOnAttach;
            internal bool ThrowOnDetach;
            public void Attach(MatchSession session, MatchIdentityContext identity, ILiveFrameSource frames, Action onFailure)
            {
                AttachCount++;
                Session = session;
                Identity = identity;
                Frames = frames;
                Failure = onFailure;
                Assert.AreEqual(0UL, session.CurrentTick, "attachment precedes any playback tick");
                DuringAttach?.Invoke();
                if (ThrowOnAttach) throw new InvalidOperationException("partial attachment");
            }
            public void Detach()
            {
                DetachCount++;
                Session = null;
                Identity = null;
                Frames = null;
                Failure = null;
                if (ThrowOnDetach) throw new InvalidOperationException("detach failure");
            }
        }

        [Test]
        public void CancelAndReentryResetBalancedWithoutSession()
        {
            var renderer = new Renderer();
            using (var shell = new ClientMatchCoordinator(renderer, S0DemoFixture.CreateApproved()))
            {
                shell.OpenTacticsSetup();
                shell.SelectMentality(Mentality.VeryAttacking);
                shell.CancelTacticsSetup();
                shell.OpenTacticsSetup();
                Assert.AreEqual(Mentality.Balanced, shell.DraftMentality);
                Assert.IsFalse(shell.HasMatch);
                Assert.AreEqual(0, renderer.AttachCount);
            }
        }

        [Test]
        public void StartTransactionIsSingleAndObserverPrecedesFirstTick()
        {
            var renderer = new Renderer();
            using (var shell = new ClientMatchCoordinator(renderer, S0DemoFixture.CreateApproved()))
            {
                shell.OpenTacticsSetup();
                renderer.DuringAttach = () => Assert.IsFalse(shell.StartMatchForTests());
                Assert.IsTrue(shell.StartMatchForTests());
                Assert.IsFalse(shell.StartMatchForTests());
                Assert.AreEqual(1, renderer.AttachCount);
                Assert.AreEqual(ClientScreens.MatchView, shell.Flow.Current);
                Assert.IsFalse(renderer.Frames.TryGetLatestFrame(out _));
                shell.Session.TickOnce();
                shell.Refresh(0f);
                Assert.AreEqual(1UL, shell.MatchView.Project().Tick);
                Assert.Throws<InvalidOperationException>(() => shell.Session.AttachTickObserver(engine => () => { }));
            }
        }

        private static ClientMatchReport End(ClientMatchCoordinator shell)
        {
            // Exercise the existing real engine end transition, then real streamer frame capture.
            // No fabricated ended frame bypasses the session's command-drop authority.
            EventBus.BeginTick((uint)shell.Session.CurrentTick);
            EventBus.BeginPhase(PhaseId.Input);
            shell.Session.TestOnly_Engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL);
            shell.Session.TickOnce();
            shell.Session.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            ulong tick = shell.Session.CurrentTick;
            shell.Refresh(1f);
            shell.Refresh(2f);
            Assert.AreEqual(tick, shell.Session.CurrentTick, "Stop/ServiceOnce end barrier advances no tick");
            Assert.AreEqual(0, shell.Session.Commands.Count, "end residue is drained/dropped once");
            Assert.AreEqual(ClientScreens.MatchView, shell.Flow.Current, "full time requires an explicit report action");
            Assert.Throws<InvalidOperationException>(() => shell.MatchView.Dispatch(default));
            ClientMatchReport report = shell.Report.Project();
            Assert.IsTrue(report.IsAvailable);
            Assert.IsTrue(report.FinalFrame.MatchEnded);
            Assert.IsFalse(report.Analytics.IsIncomplete);
            Assert.AreEqual(tick, report.Analytics.CompletedTick);
            return report;
        }

        [Test]
        public void FullTimeBarrierRetainsAppliedAndRefusedEvidenceWithoutLoggingDroppedResidue()
        {
            var renderer = new Renderer();
            using (var shell = new ClientMatchCoordinator(renderer, S0DemoFixture.CreateApproved()))
            {
                shell.OpenTacticsSetup();
                shell.StartMatchForTests();
                Assert.Throws<InvalidOperationException>(() => shell.ShowPostMatchReport());
                shell.Session.TickOnce(); shell.Refresh(0f);
                ManagerIntent valid = ManagerIntent.SetTeamTactic(0, TeamTactic.Balanced);
                ManagerIntent refused = ManagerIntent.Substitute(0, -1, 0, SubstitutionReason.Tactical);
                shell.MatchView.Dispatch(in valid);
                shell.MatchView.Dispatch(in refused);
                shell.Session.TickOnce();
                ClientMatchReport report = End(shell);
                Assert.AreEqual(1, report.Applied.Count);
                Assert.AreEqual(1, report.Refused.Count);
                shell.ShowPostMatchReport();
                Assert.AreEqual(ClientScreens.PostMatchReport, shell.Flow.Current);
                shell.ReturnToMainMenu();
                Assert.IsFalse(shell.HasMatch);
                Assert.IsFalse(shell.Report.Project().IsAvailable);
                Assert.IsTrue(report.IsAvailable, "retained immutable report survives context clear");
            }
        }

        [Test]
        public void StableHandlesAndOldCallbacksCannotReachSecondSession()
        {
            var renderer = new Renderer();
            using (var shell = new ClientMatchCoordinator(renderer, S0DemoFixture.CreateApproved()))
            {
                ClientMatchScreenHandle handle = shell.MatchView;
                var registration = shell.Flow.GetRegistration(ClientScreens.MatchView);
                shell.OpenTacticsSetup();shell.StartMatchForTests();
                MatchSession first = shell.Session;
                Action oldFailure = renderer.Failure;
                ILiveFrameSource oldFrames = renderer.Frames;
                first.TickOnce();shell.Refresh(0f);
                End(shell);shell.ShowPostMatchReport();shell.ReturnToMainMenu();
                Assert.IsFalse(oldFrames.TryGetLatestFrame(out _));
                Assert.Throws<InvalidOperationException>(() => handle.Dispatch(default));
                shell.OpenTacticsSetup();shell.StartMatchForTests();
                Assert.AreSame(registration.ViewModelSource, shell.Flow.GetRegistration(ClientScreens.MatchView).ViewModelSource);
                oldFailure();
                Assert.IsFalse(shell.IsRejected);
                Assert.AreNotSame(first, shell.Session);
                shell.Session.TickOnce();shell.Refresh(1f);
                ManagerIntent intent = ManagerIntent.SetTeamTactic(0, TeamTactic.Balanced);
                handle.Dispatch(in intent);
                shell.Session.TickOnce();
                Assert.AreEqual(1, shell.Session.Driver.Log.Count);
                Assert.AreEqual(0, first.Driver.Log.Count);
                Assert.AreEqual(1UL, handle.Project().Tick, "source uses accepted frame, not direct session polling");
            }
        }

        [Test]
        public void PartialAttachAndDetachFailureClearAllOwnedReferences()
        {
            var renderer = new Renderer { ThrowOnAttach = true, ThrowOnDetach = true };
            var shell = new ClientMatchCoordinator(renderer, S0DemoFixture.CreateApproved());
            shell.OpenTacticsSetup();
            Assert.Throws<InvalidOperationException>(() => shell.StartMatchForTests());
            Assert.IsTrue(shell.IsRejected);
            Assert.IsFalse(shell.HasMatch);
            Assert.IsNull(renderer.Session);
            Assert.IsFalse(shell.Report.Project().IsAvailable);
            Assert.Throws<InvalidOperationException>(() => shell.MatchView.Dispatch(default));
            renderer.ThrowOnDetach = false;
            shell.Dispose();shell.Dispose();
        }

        [Test]
        public void ActiveRendererFailureStopsPacedSession()
        {
            var renderer = new Renderer();
            var shell = new ClientMatchCoordinator(renderer, S0DemoFixture.CreateApproved());
            shell.OpenTacticsSetup();shell.StartMatch();
            MatchSession session = renderer.Session;
            renderer.Failure();
            Assert.IsTrue(shell.IsRejected);
            Assert.IsFalse(shell.HasMatch);
            Assert.Throws<InvalidOperationException>(() => session.Start(), "stopped paced stream is single-use");
            shell.Dispose();
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RealSubstitutionIdentityArrivesOnlyInReplacingFrame(int teamId)
        {
            var fixture = S0DemoFixture.CreateApproved();
            var session = new MatchSession(fixture.BuildSetup(Mentality.Balanced));
            MatchIdentityContext identity = fixture.Bind(session);
            LiveMatchFrame before = session.TickOnce();
            int slot = -1;
            int bench = -1;
            for (int i = 0; i < identity.Roster.AgentCount; i++)
                if (identity.Roster.TeamId(i) == teamId && before.AgentCues[i].IsGoalkeeper) slot = i;
            for (int b = 0; b < MatchEngineConstants.SUBSTITUTES_PER_TEAM; b++)
                if (identity.Bench(teamId, b).ShirtNumber == 12) bench = b;
            Assert.GreaterOrEqual(slot, 0);
            Assert.GreaterOrEqual(bench, 0);
            MatchPlayerIdentity old = identity.Resolve(in before, slot);
            MatchPlayerIdentity incoming = identity.Bench(teamId, bench);
            session.Commands.Enqueue(ManagerCommand.Substitute(teamId, slot, bench, SubstitutionReason.Tactical));
            session.ServiceOnce();
            Assert.AreEqual(1, session.Driver.Log.Count);
            Assert.AreEqual(old.PlayerId, identity.Resolve(in before, slot).PlayerId, "earlier log never patches accepted identity");
            LiveMatchFrame after = session.TickOnce();
            Assert.AreEqual(incoming.PlayerId, identity.Resolve(in after, slot).PlayerId);
            Assert.AreEqual(incoming.PlayerId, session.BootRoster.BenchPlayerId(teamId, bench), "origin is deliberately retained");
            Assert.IsTrue(identity.IsBenchUsed(in after, teamId, bench));
            Assert.IsTrue(after.AgentCues[slot].IsGoalkeeper);
            Assert.AreEqual(1, old.ShirtNumber, "old retained identity is immutable");
            var positions = new Vector2[identity.Roster.AgentCount];
            before.AgentPositions[slot] = after.AgentPositions[slot] - Vector2.one;
            FrameInterpolator.AgentsAt(in before, in after, 0.5f, positions);
            Assert.AreEqual(after.AgentPositions[slot], positions[slot], "even a small displacement snaps on replacement");
            var models = new AgentRenderModel[positions.Length];
            MatchRenderProjection.ProjectAgents(positions, in after, identity.Roster, models, identity);
            Assert.AreEqual(12, models[slot].ShirtNumber);
            MatchRenderProjection.ProjectAgents(positions, in after, identity.Roster, models);
            Assert.AreEqual(identity.Roster.ShirtNumber(slot), models[slot].ShirtNumber, "explicit neutral policy remains slot-owned");
        }

        [Test]
        public void ApprovedFixtureBootIsCompleteRepeatableAndObserverNeutral()
        {
            var fixture = S0DemoFixture.CreateApproved();
            Assert.AreEqual(fixture.ContentSha256, S0DemoFixture.CreateApproved().ContentSha256);
            MatchSetup setup = fixture.BuildSetup(Mentality.Balanced);
            var baseline = new MatchSession(setup);
            for (int i = 0; i < 120; i++) baseline.TickOnce();
            byte[] digest = baseline.CurrentSnapshotDigest;
            var observed = new MatchSession(setup);
            var identity = fixture.Bind(observed);
            var analytics = SessionMatchAnalytics.Attach(observed);
            for (int i = 0; i < 120; i++)
            {
                LiveMatchFrame frame = observed.TickOnce();
                for (int a = 0; a < identity.Roster.AgentCount; a++)
                    Assert.AreEqual(observed.BootRoster.StarterPlayerId(a), identity.Resolve(in frame, a).PlayerId);
            }
            CollectionAssert.AreEqual(digest, observed.CurrentSnapshotDigest);
            Assert.AreEqual(120L, analytics.Publish().ObservedTicks);
            var neutral = new MatchSession(new MatchSetup(setup.Seed, homeTactic: setup.HomeTactic,
                awayTactic: setup.AwayTactic, awayManagerMode: setup.AwayManagerMode));
            for (int i = 0; i < 120; i++) neutral.TickOnce();
            Assert.IsFalse(System.Linq.Enumerable.SequenceEqual(digest, neutral.CurrentSnapshotDigest),
                "distinct squads deliberately change setup; no former no-squad identity is claimed");
            TestContext.WriteLine("fixture=" + S0DemoConstants.FIXTURE_REVISION + " sha256=" + fixture.ContentSha256 +
                " observed120=" + BitConverter.ToString(digest).Replace("-", "") +
                " noSquad120=" + BitConverter.ToString(neutral.CurrentSnapshotDigest).Replace("-", ""));
        }

        [Test]
        public void PlayerZeroIsRealIdentityWhileNeutralSlotsUseExplicitMissingSentinel()
        {
            MatchSetup original = S0DemoFixture.CreateApproved().BuildSetup(Mentality.Balanced);
            var home = new PlayerRecord[S0DemoConstants.PLAYERS_PER_SQUAD];
            var identities = new MatchPlayerIdentity[home.Length * MatchEngineConstants.TEAM_COUNT];
            for (int i = 0; i < home.Length; i++)
            {
                home[i] = original.HomeSquad.GetPlayer(i);
                home[i].PlayerId = i;
                PlayerRecord away = original.AwaySquad.GetPlayer(i);
                identities[i] = new MatchPlayerIdentity(0, i, home[i].FirstName, home[i].LastName, i + 1);
                identities[home.Length + i] = new MatchPlayerIdentity(1, away.PlayerId, away.FirstName, away.LastName, i + 1);
            }
            var fixture = new S0DemoFixture(new Squad(0, home), original.AwaySquad, identities);
            var session = new MatchSession(fixture.BuildSetup(Mentality.Balanced));
            MatchIdentityContext identity = fixture.Bind(session);
            LiveMatchFrame frame = session.TickOnce();
            bool foundZero = false;
            for (int i = 0; i < identity.Roster.AgentCount; i++)
                if (frame.AgentCues[i].PlayerId == 0)
                {
                    foundZero = true;
                    Assert.AreEqual(0, identity.Resolve(in frame, i).PlayerId);
                }
            Assert.IsTrue(foundZero);
            var neutral = new MatchSession(MatchSetup.NeutralDemo(1));
            LiveMatchFrame neutralFrame = neutral.TickOnce();
            foreach (LiveAgentCue cue in neutralFrame.AgentCues)
                Assert.AreEqual(MatchEngineConstants.NO_PLAYER_ID, cue.PlayerId);
            identities[0] = default;
            Assert.Throws<ArgumentException>(() => new S0DemoFixture(new Squad(0, home), original.AwaySquad, identities));
        }

        [TestCase(Mentality.VeryDefensive)]
        [TestCase(Mentality.Defensive)]
        [TestCase(Mentality.Cautious)]
        [TestCase(Mentality.Balanced)]
        [TestCase(Mentality.Positive)]
        [TestCase(Mentality.Attacking)]
        [TestCase(Mentality.VeryAttacking)]
        public void SetupChangesOnlyHomeMentality(Mentality mentality)
        {
            MatchSetup setup = S0DemoFixture.CreateApproved().BuildSetup(mentality);
            Assert.AreEqual(mentality, setup.HomeTactic.Mentality);
            foreach (var property in typeof(TeamTactic).GetProperties())
                if (property.Name != nameof(TeamTactic.Mentality) && property.Name != nameof(TeamTactic.Balanced))
                    Assert.AreEqual(property.GetValue(TeamTactic.Balanced), property.GetValue(setup.HomeTactic), property.Name);
            Assert.AreEqual(TeamTactic.Balanced, setup.AwayTactic);
            Assert.AreEqual(ManagerMode.Human, setup.HomeManagerMode);
            Assert.AreEqual(ManagerMode.AI, setup.AwayManagerMode);
            Assert.AreEqual(0, setup.AwayManagerProfile);
            Assert.AreEqual(1UL, setup.Seed);
            Assert.IsFalse(setup.GkHeadingEnabled);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
