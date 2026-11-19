// File:     src/client-app/CareerFixtureView.cs
// Created:  2026-11-19
// Modified: 2026-11-19
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2, UI / Client Framework #38 §3.1, Code Standards #20
// Purpose:  One dated match in the managed club's fixture list and calendar.

namespace TacticalDirector.ClientApp
{
    /// <summary>A scheduled match from the managed club's perspective.</summary>
    public readonly struct CareerFixtureView
    {
        /// <summary>The fixture's zero-based league round.</summary>
        public readonly int RoundIndex;

        /// <summary>The scheduled day on the career's world calendar.</summary>
        public readonly uint WorldDay;

        /// <summary>The opposing club's id.</summary>
        public readonly int OpponentId;

        /// <summary>The opposing club's display name.</summary>
        public readonly string OpponentName;

        /// <summary>True when the managed club is at home.</summary>
        public readonly bool IsHome;

        /// <summary>True when the fixture has been resolved.</summary>
        public readonly bool IsPlayed;

        internal CareerFixtureView(
            int roundIndex, uint worldDay, int opponentId, string opponentName, bool isHome, bool isPlayed)
        {
            RoundIndex = roundIndex;
            WorldDay = worldDay;
            OpponentId = opponentId;
            OpponentName = opponentName;
            IsHome = isHome;
            IsPlayed = isPlayed;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                          |
// | 1.0     | 2026-11-19 | —      | Initial managed-club fixture projection.        |
#endregion
