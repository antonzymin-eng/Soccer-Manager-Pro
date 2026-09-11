// File:     src/match-client-core/tests/MatchSessionLifecycleTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5b/§8),
//           Code Standards #20
// Purpose:  Locks the host-free serial-session lifecycle needed before P5b may hand Tactics Setup to
//           Match View: empty/current semantics, null refusal, replacement, repeat-match freshness,
//           running-session shutdown, stale-reference boundary, and idempotent teardown.

using System;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class MatchSessionLifecycleTests
    {
        private const ulong FirstSeed = 0xA11CE001UL;
        private const ulong SecondSeed = 0xA11CE002UL;

        [Test]
        public void NewLifecycle_HasNoCurrent_AndCurrentFailsLoud()
        {
            var lifecycle = new MatchSessionLifecycle();

            Assert.IsFalse(lifecycle.HasCurrent);
            Assert.Throws<InvalidOperationException>(() => _ = lifecycle.Current);
        }

        [Test]
        public void CreateSession_InstallsFreshUntickedSession_WithoutStartingPlayback()
        {
            var lifecycle = new MatchSessionLifecycle();

            MatchSession session = lifecycle.CreateSession(MatchSetup.NeutralDemo(FirstSeed));

            Assert.IsTrue(lifecycle.HasCurrent);
            Assert.AreSame(session, lifecycle.Current);
            Assert.AreEqual(0UL, session.CurrentTick);
            Assert.AreEqual(0, session.Driver.Log.Count);
            Assert.IsFalse(session.TryGetLatestFrame(out _),
                "creation must not start paced playback before the Unity host attaches");
        }

        [Test]
        public void CreateSession_NullSetup_Throws_AndPreservesCurrent()
        {
            var lifecycle = new MatchSessionLifecycle();
            MatchSession first = lifecycle.CreateSession(MatchSetup.NeutralDemo(FirstSeed));

            Assert.Throws<ArgumentNullException>(() => lifecycle.CreateSession(null));

            Assert.IsTrue(lifecycle.HasCurrent);
            Assert.AreSame(first, lifecycle.Current);
        }

        [Test]
        public void CreateSession_RepeatMatch_ReplacesWithFreshComposition()
        {
            var lifecycle = new MatchSessionLifecycle();
            MatchSetup setup = MatchSetup.NeutralDemo(FirstSeed);

            MatchSession first = lifecycle.CreateSession(setup);
            first.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            first.ServiceOnce();
            Assert.AreEqual(1, first.Driver.Log.Count, "positive control: first session has live state");

            MatchSession second = lifecycle.CreateSession(setup);

            Assert.AreNotSame(first, second, "repeat match must be a new single-match composition");
            Assert.AreSame(second, lifecycle.Current);
            Assert.AreEqual(0UL, second.CurrentTick);
            Assert.AreEqual(0, second.Driver.Log.Count, "the prior match command log must not leak");
            Assert.AreEqual(0, second.Commands.Count, "the prior match queue must not leak");
            Assert.IsFalse(second.TryGetLatestFrame(out _), "the replacement starts frameless");
        }

        [Test]
        public void CreateSession_NeverStartedPredecessor_DoesNotRevokeStaleRawReference()
        {
            var lifecycle = new MatchSessionLifecycle();
            MatchSession first = lifecycle.CreateSession(MatchSetup.NeutralDemo(FirstSeed));

            MatchSession second = lifecycle.CreateSession(MatchSetup.NeutralDemo(SecondSeed));

            Assert.AreSame(second, lifecycle.Current);
            Assert.DoesNotThrow(() => first.TickOnce(),
                "lifecycle ownership does not revoke a caller-held raw reference to a never-started session");
            Assert.AreEqual(1UL, first.CurrentTick,
                "positive control: the stale raw reference remains independently usable");
            Assert.AreEqual(0UL, second.CurrentTick,
                "using the stale predecessor must not mutate the lifecycle's replacement");
        }

        [Test]
        public void CreateSession_WhenCurrentPlaybackIsRunning_StopsOldSessionBeforeReplacement()
        {
            var lifecycle = new MatchSessionLifecycle();
            MatchSession first = lifecycle.CreateSession(MatchSetup.NeutralDemo(FirstSeed));
            first.Start();

            MatchSession second = lifecycle.CreateSession(MatchSetup.NeutralDemo(SecondSeed));

            Assert.AreSame(second, lifecycle.Current);
            Assert.Throws<InvalidOperationException>(() => first.Start(),
                "replacement must have stopped the old single-use streamer");

            lifecycle.ClearSession();
        }

        [Test]
        public void ClearSession_WhenPlaybackIsRunning_StopsAndClears()
        {
            var lifecycle = new MatchSessionLifecycle();
            MatchSession session = lifecycle.CreateSession(MatchSetup.NeutralDemo(FirstSeed));
            session.Start();

            lifecycle.ClearSession();

            Assert.IsFalse(lifecycle.HasCurrent);
            Assert.Throws<InvalidOperationException>(() => _ = lifecycle.Current);
            Assert.Throws<InvalidOperationException>(() => session.Start(),
                "clearing must stop the running single-use streamer");
        }

        [Test]
        public void ClearSession_WhenEmpty_IsIdempotent()
        {
            var lifecycle = new MatchSessionLifecycle();

            Assert.DoesNotThrow(() => lifecycle.ClearSession());
            Assert.DoesNotThrow(() => lifecycle.ClearSession());
            Assert.IsFalse(lifecycle.HasCurrent);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.1     | 2026-09-11 | —      | Review: lock stale never-started raw-reference boundary;       |
// |         |            |        | lifecycle coverage now 8 tests.                                |
// | 1.0     | 2026-09-11 | —      | Initial P5b host-free lifecycle coverage (7 tests).            |
#endregion
