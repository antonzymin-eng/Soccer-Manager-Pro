// File:     src/match-client-core/MatchRoster.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Code Standards #20
// Purpose:  The match-constant half of what a renderer needs per agent — team id and the shirt number
//           drawn on the marker — sampled once and held for the match. Everything that can change
//           mid-match lives on the per-frame LiveAgentCue instead.

using System;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Per-slot roster data that does not change while a match is played.
    ///
    /// <para><b>The split this type defines is the load-bearing part.</b> A roster slot's <b>team</b>
    /// is fixed — a bench player belongs to the team whose bench they sit on, so a substitution can
    /// never move a slot across the halfway line of the roster. A slot's <b>goalkeeper flag</b> is
    /// not fixed: the engine rewrites it when a substitution fills the slot. So team ids are cached
    /// here and the goalkeeper flag is deliberately absent — a renderer reads it from
    /// <c>LiveAgentCue.IsGoalkeeper</c> on the current frame. Caching both together is what made the
    /// browser viewer draw the keeper ring on the wrong player after a goalkeeper substitution.</para>
    ///
    /// <para><b>Shirt numbers are slot ordinals, not player identity.</b> Within each team, slots are
    /// numbered 1..n in roster order. Stage 0 carries no shirt-number field on a player, and the
    /// engine seeds each team's keeper at local index 0, so this gives the keeper the 1 shirt and
    /// everyone else a stable, unique number — which is all a marker label needs. When player records
    /// gain real numbers this is the one place that changes.</para>
    ///
    /// <para>Built once at scene boot and held; it allocates, and is not on the render path.</para>
    /// </summary>
    public sealed class MatchRoster
    {
        private readonly int[] _teamIds;
        private readonly int[] _shirtNumbers;

        /// <summary>Number of roster slots.</summary>
        public int AgentCount => _teamIds.Length;

        /// <summary>
        /// Builds a roster from per-slot team ids, copying the array.
        /// </summary>
        /// <param name="teamIds">Team id per roster slot; every entry must be in [0, TEAM_COUNT).</param>
        /// <exception cref="ArgumentNullException"><paramref name="teamIds"/> is null.</exception>
        /// <exception cref="ArgumentException">An entry is not a valid team id. A renderer that
        /// silently treated an out-of-range id as "home" would draw one team in two colours.</exception>
        public MatchRoster(int[] teamIds)
        {
            if (teamIds == null) { throw new ArgumentNullException(nameof(teamIds)); }

            _teamIds      = new int[teamIds.Length];
            _shirtNumbers = new int[teamIds.Length];

            var assigned = new int[MatchEngineConstants.TEAM_COUNT];
            for (int i = 0; i < teamIds.Length; i++)
            {
                int teamId = teamIds[i];
                if (teamId < 0 || teamId >= MatchEngineConstants.TEAM_COUNT)
                {
                    throw new ArgumentException(
                        "teamIds[" + Inv(i) + "] is " + Inv(teamId) + "; expected [0, " +
                        Inv(MatchEngineConstants.TEAM_COUNT) + ").", nameof(teamIds));
                }

                _teamIds[i]      = teamId;
                _shirtNumbers[i] = ++assigned[teamId];
            }
        }

        /// <summary>
        /// Samples the roster from a streamer. The streamer's boot-time goalkeeper flags are
        /// deliberately not read — see the class doc.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="streamer"/> is null.</exception>
        public static MatchRoster FromStreamer(LiveMatchStreamer streamer)
        {
            if (streamer == null) { throw new ArgumentNullException(nameof(streamer)); }

            var teamIds = new int[streamer.AgentCount];
            for (int i = 0; i < teamIds.Length; i++)
            {
                teamIds[i] = streamer.TeamId(i);
            }

            return new MatchRoster(teamIds);
        }

        /// <summary>Team id (0 = home, 1 = away) of roster slot <paramref name="index"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a roster slot.</exception>
        public int TeamId(int index)
        {
            Guard(index);
            return _teamIds[index];
        }

        /// <summary>Shirt number drawn on roster slot <paramref name="index"/>'s marker (1-based within its team).</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a roster slot.</exception>
        public int ShirtNumber(int index)
        {
            Guard(index);
            return _shirtNumbers[index];
        }

        private void Guard(int index)
        {
            if (index < 0 || index >= _teamIds.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, "index must be a roster slot in [0, AgentCount).");
            }
        }

        private static string Inv(int value) =>
            value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): match-constant per-slot render data —   |
// |         |            |        | team id + slot-ordinal shirt number. Holds no goalkeeper flag   |
// |         |            |        | on purpose; that one changes under substitution and rides the   |
// |         |            |        | per-frame cue. The shirt-numbering rule moves here out of the   |
// |         |            |        | browser viewer's inline JavaScript.                             |
#endregion
