// File:     src/match-client-core/MatchPlayerIdentity.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Immutable authored player metadata; contains no mutable player record.

using System;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>Session content identity; independent of the pitch slot its player occupies.</summary>
    public readonly struct MatchPlayerIdentity
    {
        /// <summary>Home/away team id.</summary>
        public readonly int TeamId;
        /// <summary>Stable player id; zero is valid.</summary>
        public readonly int PlayerId;
        /// <summary>Authored given name.</summary>
        public readonly string FirstName;
        /// <summary>Authored family name.</summary>
        public readonly string LastName;
        /// <summary>Authored positive shirt number.</summary>
        public readonly int ShirtNumber;

        /// <summary>Validates authored content before any match exists.</summary>
        public MatchPlayerIdentity(int teamId, int playerId, string firstName, string lastName, int shirtNumber)
        {
            if (teamId < 0 || teamId >= MatchEngineConstants.TEAM_COUNT || playerId < 0 || shirtNumber <= 0 ||
                string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Invalid authored match identity.");
            TeamId = teamId;
            PlayerId = playerId;
            FirstName = firstName;
            LastName = lastName;
            ShirtNumber = shirtNumber;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
