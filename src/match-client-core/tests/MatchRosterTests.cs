// File:     src/match-client-core/tests/MatchRosterTests.cs
// Created:  2026-08-03
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Testing Strategy #19, Code Standards #20
// Purpose:  Locks what MatchRoster itself owns: that it exposes RosterShirtNumbers' output against
//           the right slot, its copy semantics, its bounds guards, and the FromStreamer sampling path.

using System;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class MatchRosterTests
    {
        private static int[] EngineOrderTeamIds()
        {
            // The engine's boot layout: i = team * PLAYERS_PER_TEAM + localIndex.
            var teamIds = new int[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < teamIds.Length; i++)
            {
                teamIds[i] = i / MatchEngineConstants.PLAYERS_PER_TEAM;
            }
            return teamIds;
        }

        [Test]
        public void TeamIdsAndShirtNumbers_AreExposedPerSlot()
        {
            // The numbering RULE is owned and tested by RosterShirtNumbers (match-viewer), which the
            // browser viewer shares — one rule, one fixture. What belongs here is that this type
            // exposes that rule's output against the right slot, so the two cannot drift.
            int[] teamIds = EngineOrderTeamIds();
            var roster    = new MatchRoster(teamIds);
            int[] expected = RosterShirtNumbers.Assign(teamIds);

            Assert.AreEqual(teamIds.Length, roster.AgentCount);
            for (int i = 0; i < teamIds.Length; i++)
            {
                Assert.AreEqual(teamIds[i], roster.TeamId(i), "slot " + i + " team");
                Assert.AreEqual(expected[i], roster.ShirtNumber(i), "slot " + i + " shirt");
            }

            // Named explicitly because it is the property a marker label depends on and the one an
            // "index / 11" reimplementation would break: the keeper slot at BOTH ends wears 1.
            Assert.AreEqual(1, roster.ShirtNumber(0), "home keeper");
            Assert.AreEqual(1, roster.ShirtNumber(MatchEngineConstants.PLAYERS_PER_TEAM), "away keeper");
        }

        [Test]
        public void TheConstructorCopies_SoALaterMutationOfTheSourceIsNotSeen()
        {
            var source = new[] { 0, 0, 1, 1 };
            var roster = new MatchRoster(source);

            source[0] = 1;

            Assert.AreEqual(0, roster.TeamId(0), "the roster must not alias the caller's array");
        }

        [Test]
        public void ANullOrInvalidTeamId_IsRefused()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchRoster(null));
            Assert.Throws<ArgumentException>(() => new MatchRoster(new[] { 0, MatchEngineConstants.TEAM_COUNT }));
            Assert.Throws<ArgumentException>(() => new MatchRoster(new[] { 0, -1 }));
        }

        [Test]
        public void AccessorsGuardTheirIndex()
        {
            var roster = new MatchRoster(new[] { 0, 1 });

            Assert.Throws<ArgumentOutOfRangeException>(() => roster.TeamId(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => roster.TeamId(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => roster.ShirtNumber(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => roster.ShirtNumber(2));
        }

        [Test]
        public void FromStreamer_RefusesANullStreamer()
        {
            Assert.Throws<ArgumentNullException>(() => MatchRoster.FromStreamer(null));
        }

        [Test]
        public void FromStreamer_SamplesTheRealRosterFromARealEngine()
        {
            // The only production path into this type, and it was previously covered by its null
            // guard alone — the sampling loop itself never ran under test.
            var engine   = new MatchEngine.MatchEngine(20260804UL);
            var streamer = new LiveMatchStreamer(engine);

            MatchRoster roster = MatchRoster.FromStreamer(streamer);

            Assert.AreEqual(streamer.AgentCount, roster.AgentCount);
            Assert.AreEqual(MatchEngineConstants.SQUAD_SIZE, roster.AgentCount);

            for (int i = 0; i < roster.AgentCount; i++)
            {
                Assert.AreEqual(streamer.TeamId(i), roster.TeamId(i), "slot " + i + " team");
                Assert.AreEqual(streamer.ShirtNumber(i), roster.ShirtNumber(i), "slot " + i + " shirt");
            }

            // Both teams are represented — a sampling loop that read team 0 for every slot would
            // satisfy every assertion above.
            Assert.AreEqual(0, roster.TeamId(0), "home");
            Assert.AreEqual(1, roster.TeamId(MatchEngineConstants.PLAYERS_PER_TEAM), "away");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): numbering rule at both ends, per-team   |
// |         |            |        | uniqueness, interleaved-order discrimination, copy semantics,   |
// |         |            |        | and the argument guards.                                        |
// | 1.1     | 2026-08-04 | —      | AR pass M-6/L-8: the numbering RULE's tests move down to       |
// |         |            |        | RosterShirtNumbersTests with the rule itself, leaving this     |
// |         |            |        | fixture asserting what MatchRoster owns — that it exposes that |
// |         |            |        | rule's output against the right slot. + the FromStreamer happy |
// |         |            |        | path, which was previously covered by its null guard alone, so |
// |         |            |        | the only production path into this type never ran under test.  |
#endregion
