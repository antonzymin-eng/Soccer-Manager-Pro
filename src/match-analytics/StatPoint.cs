// File:     src/match-analytics/StatPoint.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §2.2 (FR-AN-015), Code Standards #20
// Purpose:  One entry in a location map (goals / fouls / offsides): which team, and where on the pitch.

using System;

using UnityEngine;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// A single (team, x, y) map entry — the unit the goal / foul / offside maps are built from.
    /// Coordinates are metres in the corner-origin frame (Ball Physics #1 §1.2).
    /// </summary>
    public readonly struct StatPoint
    {
        /// <summary>Team the event is attributed to (0 = home, 1 = away).</summary>
        public readonly byte TeamId;

        /// <summary>Pitch-plane x (goal-to-goal), metres.</summary>
        public readonly float X;

        /// <summary>Pitch-plane y (touchline-to-touchline), metres.</summary>
        public readonly float Y;

        /// <summary>Constructs a map entry, gating the team id and both coordinates (F1 / F2).</summary>
        /// <exception cref="ArgumentOutOfRangeException">An unknown team id.</exception>
        /// <exception cref="ArgumentException">A non-finite coordinate.</exception>
        public StatPoint(byte teamId, float x, float y)
        {
            if (teamId >= MatchAnalyticsConstants.TEAM_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(teamId), teamId, "teamId must be 0 or 1.");
            }
            if (float.IsNaN(x) || float.IsInfinity(x)) { throw new ArgumentException("x is not finite.", nameof(x)); }
            if (float.IsNaN(y) || float.IsInfinity(y)) { throw new ArgumentException("y is not finite.", nameof(y)); }

            TeamId = teamId;
            X = x;
            Y = y;
        }

        /// <summary>The point as a pitch-plane vector.</summary>
        public Vector2 Position => new Vector2(X, Y);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T0): the (team, x, y) location-map entry |
// |         |            |        | with F1 team-id and F2 finite-coordinate gates.                |
#endregion
