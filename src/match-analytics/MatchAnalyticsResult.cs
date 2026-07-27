// File:     src/match-analytics/MatchAnalyticsResult.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §2.2 (FR-AN-015), Code Standards #20
// Purpose:  The full per-match analytics result — both teams' statlines plus the location maps. What a
//           post-match report binds, and the only thing the aggregator hands out.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// The complete analytics result for one match (§2.2): both teams' basic and advanced statlines, and
    /// the three location maps. Immutable — the map arrays are copied at construction and exposed only
    /// through read-only collections, so a consumer cannot write back through the result and the
    /// aggregator can keep reusing its own buffers.
    /// </summary>
    public readonly struct MatchAnalyticsResult
    {
        /// <summary>Home (team 0) basic statline.</summary>
        public readonly MatchStatline Home;

        /// <summary>Away (team 1) basic statline.</summary>
        public readonly MatchStatline Away;

        /// <summary>Home advanced statline.</summary>
        public readonly AdvancedStatline HomeAdvanced;

        /// <summary>Away advanced statline.</summary>
        public readonly AdvancedStatline AwayAdvanced;

        private readonly ReadOnlyCollection<StatPoint> _goalMap;
        private readonly ReadOnlyCollection<StatPoint> _foulMap;
        private readonly ReadOnlyCollection<StatPoint> _offsideMap;

        /// <summary>Where goals were scored from (the ball's crossing point, per the ledger).</summary>
        public IReadOnlyList<StatPoint> GoalMap => _goalMap ?? EmptyPoints;

        /// <summary>Where fouls were committed.</summary>
        public IReadOnlyList<StatPoint> FoulMap => _foulMap ?? EmptyPoints;

        /// <summary>Where offsides were called.</summary>
        public IReadOnlyList<StatPoint> OffsideMap => _offsideMap ?? EmptyPoints;

        private static readonly ReadOnlyCollection<StatPoint> EmptyPoints =
            new ReadOnlyCollection<StatPoint>(new StatPoint[0]);

        /// <summary>
        /// Constructs the result, copying all three maps. Refuses statlines whose team ids are not
        /// exactly {home = 0, away = 1} — a result with two home lines would silently mis-attribute
        /// every number a report shows.
        /// </summary>
        public MatchAnalyticsResult(
            in MatchStatline home,
            in MatchStatline away,
            in AdvancedStatline homeAdvanced,
            in AdvancedStatline awayAdvanced,
            StatPoint[] goalMap,
            StatPoint[] foulMap,
            StatPoint[] offsideMap)
        {
            RequireTeam(home.TeamId,         0, "home");
            RequireTeam(away.TeamId,         1, "away");
            RequireTeam(homeAdvanced.TeamId, 0, "homeAdvanced");
            RequireTeam(awayAdvanced.TeamId, 1, "awayAdvanced");

            Home = home;
            Away = away;
            HomeAdvanced = homeAdvanced;
            AwayAdvanced = awayAdvanced;

            _goalMap    = CopyMap(goalMap,    nameof(goalMap));
            _foulMap    = CopyMap(foulMap,    nameof(foulMap));
            _offsideMap = CopyMap(offsideMap, nameof(offsideMap));
        }

        private static void RequireTeam(byte actual, byte expected, string what)
        {
            if (actual != expected)
            {
                throw new ArgumentException(
                    what + " carries TeamId " + actual + "; expected " + expected + ".", what);
            }
        }

        private static ReadOnlyCollection<StatPoint> CopyMap(StatPoint[] source, string what)
        {
            if (source == null)
            {
                throw new ArgumentNullException(what);
            }
            var copy = new StatPoint[source.Length];
            Array.Copy(source, copy, source.Length);
            return new ReadOnlyCollection<StatPoint>(copy);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T0): the per-match result — both teams'  |
// |         |            |        | statlines plus copied goal / foul / offside maps, with a       |
// |         |            |        | home-is-0 / away-is-1 coherence gate.                          |
#endregion
