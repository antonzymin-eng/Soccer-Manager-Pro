// File:     src/match-client-core/tests/MatchRosterTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Testing Strategy #19, Code Standards #20
// Purpose:  Locks the roster's shirt-numbering rule (per-team, 1-based, keeper on 1), its copy
//           semantics, and its bounds guards.

using System;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;

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
        public void ShirtNumbers_AreOneBasedWithinEachTeam()
        {
            var roster = new MatchRoster(EngineOrderTeamIds());

            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
                {
                    int i = team * MatchEngineConstants.PLAYERS_PER_TEAM + k;

                    Assert.AreEqual(team, roster.TeamId(i));
                    Assert.AreEqual(k + 1, roster.ShirtNumber(i),
                        "slot " + i + " should wear " + (k + 1) + " within team " + team);
                }
            }
        }

        [Test]
        public void EachTeamsKeeperSlot_WearsNumberOne()
        {
            // The engine seeds each team's keeper at local index 0 (MatchEngine boot), so the
            // slot-ordinal rule gives the keeper the 1 shirt without special-casing. Asserted for
            // BOTH teams, not just home.
            var roster = new MatchRoster(EngineOrderTeamIds());

            Assert.AreEqual(1, roster.ShirtNumber(0), "home keeper");
            Assert.AreEqual(1, roster.ShirtNumber(MatchEngineConstants.PLAYERS_PER_TEAM), "away keeper");
        }

        [Test]
        public void ShirtNumbers_AreUniqueWithinATeam()
        {
            var roster = new MatchRoster(EngineOrderTeamIds());
            var seen = new bool[MatchEngineConstants.TEAM_COUNT][];
            for (int t = 0; t < seen.Length; t++)
            {
                seen[t] = new bool[MatchEngineConstants.SQUAD_SIZE + 1];
            }

            for (int i = 0; i < roster.AgentCount; i++)
            {
                int team = roster.TeamId(i);
                int shirt = roster.ShirtNumber(i);

                Assert.IsFalse(seen[team][shirt], "team " + team + " has two players wearing " + shirt);
                seen[team][shirt] = true;
            }
        }

        [Test]
        public void InterleavedTeamOrder_StillNumbersPerTeam()
        {
            // Nothing guarantees the roster stays block-ordered; the rule is per-team-sequential, not
            // "index / 11". Interleaving is the cheapest way to tell those two rules apart.
            var roster = new MatchRoster(new[] { 0, 1, 0, 1, 0 });

            Assert.AreEqual(1, roster.ShirtNumber(0));
            Assert.AreEqual(1, roster.ShirtNumber(1));
            Assert.AreEqual(2, roster.ShirtNumber(2));
            Assert.AreEqual(2, roster.ShirtNumber(3));
            Assert.AreEqual(3, roster.ShirtNumber(4));
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): numbering rule at both ends, per-team   |
// |         |            |        | uniqueness, interleaved-order discrimination, copy semantics,   |
// |         |            |        | and the argument guards.                                        |
#endregion
