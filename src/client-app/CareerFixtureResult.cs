// File:     src/client-app/CareerFixtureResult.cs
// Created:  2026-11-19
// Modified: 2026-11-19
// Author:   —
// Spec:     Interactive Unity client §5-P7, Season & Competition Loop #30 FR-SN-016
// Purpose:  A completed fixture and its club names for Career Home.

using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp
{
    /// <summary>A scoreline with display names in fixture order.</summary>
    public readonly struct CareerFixtureResult
    {
        /// <summary>The season loop's authoritative fixture outcome.</summary>
        public readonly MatchResult Result;

        /// <summary>The home club's display name.</summary>
        public readonly string HomeClubName;

        /// <summary>The away club's display name.</summary>
        public readonly string AwayClubName;

        /// <summary>Pairs the scoreline with the two club names.</summary>
        public CareerFixtureResult(in MatchResult result, string homeClubName, string awayClubName)
        {
            Result = result;
            HomeClubName = homeClubName;
            AwayClubName = awayClubName;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                      |
// | 1.0     | 2026-11-19 | —      | Named fixture result for the career view.   |
#endregion
