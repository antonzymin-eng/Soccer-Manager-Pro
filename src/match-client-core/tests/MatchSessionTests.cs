// File:     src/match-client-core/tests/MatchSessionTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P0/§6.3), Code Standards #20
// Purpose:  Head-less locks for the composition root: a neutral session builds and wires the engine +
//           streamer + driver, an off-tick ServiceOnce() drains a queued command through the real
//           engine and stamps it in the log, and MatchSetup enforces the both-or-neither squad rule.
//           No Unity host and no background pacing thread — ServiceOnce runs on the test thread.

using System;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class MatchSessionTests
    {
        private const ulong Seed = 0xA11CE5EEDUL;

        [Test]
        public void NeutralDemo_Builds_AndExposesSurfaces()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));

            Assert.IsNotNull(session.Streamer);
            Assert.IsNotNull(session.Driver);
            Assert.IsNotNull(session.Commands);
            Assert.AreEqual(0, session.Driver.Log.Count, "nothing serviced yet");
            Assert.IsFalse(session.TryGetLatestFrame(out _), "no frame before the first tick");
        }

        [Test]
        public void ServiceOnce_DrainsQueuedCommand_ThroughRealEngine_AndLogsAtTickZero()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));

            session.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            session.ServiceOnce();

            Assert.AreEqual(1, session.Driver.Log.Count);
            Assert.AreEqual(0UL, session.Driver.Log[0].AppliedTick, "no tick has advanced");
            Assert.AreEqual(ManagerCommandKind.SetTeamTactic, session.Driver.Log[0].Command.Kind);
            Assert.AreEqual(0, session.Commands.Count, "queue drained");
        }

        [Test]
        public void ServiceOnce_EmptyQueue_IsNoOp()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            session.ServiceOnce();
            Assert.AreEqual(0, session.Driver.Log.Count);
        }

        [Test]
        public void GkHeadingEnabled_Setup_Builds_AndServicesACommand()
        {
            var setup = new MatchSetup(Seed, gkHeadingEnabled: true);
            var session = new MatchSession(setup);

            session.Commands.Enqueue(ManagerCommand.SetPlayerTactic(4, PlayerTactic.Default(PlayerRole.Default)));
            session.ServiceOnce();

            Assert.AreEqual(1, session.Driver.Log.Count);
            Assert.AreEqual(ManagerCommandKind.SetPlayerTactic, session.Driver.Log[0].Command.Kind);
        }

        [Test]
        public void Constructor_NullSetup_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchSession(null));
        }

        [Test]
        public void MatchSetup_SingleSquad_Throws_BothOrNeither()
        {
            var oneSquad = new Squad(1, new[] { PlayerRecord.CreateDefault(0) });

            Assert.Throws<ArgumentException>(() => new MatchSetup(Seed, homeSquad: oneSquad, awaySquad: null));
            Assert.Throws<ArgumentException>(() => new MatchSetup(Seed, homeSquad: null, awaySquad: oneSquad));
        }
    }
}
