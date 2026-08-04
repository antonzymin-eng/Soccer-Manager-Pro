// File:     src/match-viewer/tests/RosterShirtNumbersTests.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Testing Strategy #19, Code Standards #20
// Purpose:  Locks the shirt-numbering rule where the rule now lives — per-team, 1-based, in roster
//           order, with the engine's keeper slot falling on 1 at BOTH ends.

using System;

using NUnit.Framework;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchViewer.Tests
{
    [TestFixture]
    public sealed class RosterShirtNumbersTests
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
        public void NumbersAreOneBasedWithinEachTeam()
        {
            int[] shirts = RosterShirtNumbers.Assign(EngineOrderTeamIds());

            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
                {
                    int i = team * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                    Assert.AreEqual(k + 1, shirts[i],
                        "slot " + i + " should wear " + (k + 1) + " within team " + team);
                }
            }
        }

        [Test]
        public void EachTeamsKeeperSlot_WearsNumberOne()
        {
            // The engine seeds each team's keeper at local index 0, so the slot-ordinal rule gives
            // the keeper the 1 shirt without special-casing. Asserted for BOTH teams, not just home.
            int[] shirts = RosterShirtNumbers.Assign(EngineOrderTeamIds());

            Assert.AreEqual(1, shirts[0], "home keeper");
            Assert.AreEqual(1, shirts[MatchEngineConstants.PLAYERS_PER_TEAM], "away keeper");
        }

        [Test]
        public void NumbersAreUniqueWithinATeam()
        {
            int[] teamIds = EngineOrderTeamIds();
            int[] shirts  = RosterShirtNumbers.Assign(teamIds);

            var seen = new bool[MatchEngineConstants.TEAM_COUNT][];
            for (int t = 0; t < seen.Length; t++)
            {
                seen[t] = new bool[MatchEngineConstants.SQUAD_SIZE + 1];
            }

            for (int i = 0; i < shirts.Length; i++)
            {
                Assert.IsFalse(seen[teamIds[i]][shirts[i]],
                    "team " + teamIds[i] + " has two players wearing " + shirts[i]);
                seen[teamIds[i]][shirts[i]] = true;
            }
        }

        [Test]
        public void InterleavedTeamOrder_StillNumbersPerTeam()
        {
            // Nothing guarantees the roster stays block-ordered; the rule is per-team-sequential, not
            // "index / 11". Interleaving is the cheapest way to tell those two rules apart.
            Assert.AreEqual(
                new[] { 1, 1, 2, 2, 3 },
                RosterShirtNumbers.Assign(new[] { 0, 1, 0, 1, 0 }));
        }

        [Test]
        public void TheSourceArrayIsNotMutated()
        {
            var teamIds = new[] { 0, 0, 1, 1 };
            RosterShirtNumbers.Assign(teamIds);

            Assert.AreEqual(new[] { 0, 0, 1, 1 }, teamIds, "Assign must not write through its input");
        }

        [Test]
        public void ANullOrInvalidTeamId_IsRefused()
        {
            Assert.Throws<ArgumentNullException>(() => RosterShirtNumbers.Assign(null));
            Assert.Throws<ArgumentException>(
                () => RosterShirtNumbers.Assign(new[] { 0, MatchEngineConstants.TEAM_COUNT }));
            Assert.Throws<ArgumentException>(() => RosterShirtNumbers.Assign(new[] { 0, -1 }));
        }

        [Test]
        public void TheStreamerServesTheSameNumbersItWouldComputeDirectly()
        {
            // The browser viewer reads these off the /frame payload rather than recomputing them,
            // which is the whole point of the rule having one implementation. This is the assertion
            // that the streamer's boot-time cache IS that implementation's output.
            var engine   = new MatchEngine.MatchEngine(20260804UL);
            var streamer = new LiveMatchStreamer(engine);

            var teamIds = new int[streamer.AgentCount];
            for (int i = 0; i < teamIds.Length; i++)
            {
                teamIds[i] = streamer.TeamId(i);
            }

            int[] expected = RosterShirtNumbers.Assign(teamIds);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], streamer.ShirtNumber(i), "slot " + i);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-04 | —      | Initial creation (P4a AR pass, M-6): the numbering rule's own   |
// |         |            |        | tests, moved down with the rule from MatchRosterTests so the    |
// |         |            |        | rule and its coverage stay in one place. Adds the streamer      |
// |         |            |        | accessor the browser viewer's payload now reads.                |
#endregion
