// File:     src/match-analytics/MatchStatline.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §2.2 (FR-AN-015), Code Standards #20
// Purpose:  The immutable per-team basic statline a post-match report and the #38 screens bind.

using System;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// One team's basic match statistics (§2.2, KD-4 view model). Immutable, and every value is gated
    /// at construction (FR-AN-015 / FR-AN-018) — a statline is read by a report and a screen, both of
    /// which are bad places to discover a negative count.
    ///
    /// <para><b>What is NOT here is deliberate.</b> Shots, shots on target, pass completion, tackles and
    /// saves all wait on match-engine producers that do not exist yet (Appendix D: <c>ShotAttemptedEvent</c>,
    /// <c>PassCompletedEvent</c>, <c>TackleEvent</c>, a digest-committed <c>SaveAttemptedEvent</c>).
    /// FR-AN-019 forbids building a consumer for a producer that does not exist, so those fields arrive
    /// with their producers (#37 §7.1 T2) rather than as fields that silently report zero.</para>
    /// </summary>
    public readonly struct MatchStatline
    {
        /// <summary>Team this line describes (0 = home, 1 = away).</summary>
        public readonly byte TeamId;

        /// <summary>Goals scored.</summary>
        public readonly int Goals;

        /// <summary>Share of possession, percent in [0, 100]. This team's share, the other team's, and
        /// the loose-ball share sum to 100 — the ball is unowned for a large fraction of a match, so
        /// two teams' shares do NOT sum to 100 on their own.</summary>
        public readonly float PossessionSharePercent;

        /// <summary>Fouls conceded.</summary>
        public readonly int Fouls;

        /// <summary>Yellow cards received.</summary>
        public readonly int YellowCards;

        /// <summary>Red cards received (including second-yellow dismissals).</summary>
        public readonly int RedCards;

        /// <summary>Times caught offside.</summary>
        public readonly int Offsides;

        /// <summary>Corners won.</summary>
        public readonly int Corners;

        /// <summary>Throw-ins awarded.</summary>
        public readonly int ThrowIns;

        /// <summary>Goal kicks awarded.</summary>
        public readonly int GoalKicks;

        /// <summary>Substitutions used.</summary>
        public readonly int Substitutions;

        /// <summary>Constructs a statline, refusing an unknown team, any negative count, and a
        /// possession share outside [0, 100] or non-finite (FR-AN-018).</summary>
        public MatchStatline(
            byte teamId,
            int goals,
            float possessionSharePercent,
            int fouls,
            int yellowCards,
            int redCards,
            int offsides,
            int corners,
            int throwIns,
            int goalKicks,
            int substitutions)
        {
            if (teamId >= MatchAnalyticsConstants.TEAM_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(teamId), teamId, "teamId must be 0 or 1.");
            }
            RequireNonNegative(goals,         nameof(goals));
            RequireNonNegative(fouls,         nameof(fouls));
            RequireNonNegative(yellowCards,   nameof(yellowCards));
            RequireNonNegative(redCards,      nameof(redCards));
            RequireNonNegative(offsides,      nameof(offsides));
            RequireNonNegative(corners,       nameof(corners));
            RequireNonNegative(throwIns,      nameof(throwIns));
            RequireNonNegative(goalKicks,     nameof(goalKicks));
            RequireNonNegative(substitutions, nameof(substitutions));

            if (float.IsNaN(possessionSharePercent) || float.IsInfinity(possessionSharePercent) ||
                possessionSharePercent < 0f || possessionSharePercent > 100f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(possessionSharePercent), possessionSharePercent,
                    "possessionSharePercent must be a finite value in [0, 100].");
            }

            TeamId = teamId;
            Goals = goals;
            PossessionSharePercent = possessionSharePercent;
            Fouls = fouls;
            YellowCards = yellowCards;
            RedCards = redCards;
            Offsides = offsides;
            Corners = corners;
            ThrowIns = throwIns;
            GoalKicks = goalKicks;
            Substitutions = substitutions;
        }

        private static void RequireNonNegative(int value, string what)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(what, value, what + " must not be negative.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T0): the immutable per-team basic        |
// |         |            |        | statline with FR-AN-018 construction gates. Producer-gated     |
// |         |            |        | stats (shots / passes / tackles / saves) deliberately absent   |
// |         |            |        | per FR-AN-019 — they land with their producers at T2.          |
#endregion
