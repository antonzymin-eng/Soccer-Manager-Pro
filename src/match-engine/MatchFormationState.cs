// File: src/match-engine/MatchFormationState.cs
// Created: 2026-11-09
// Modified: 2026-11-09
// Author: —
// Spec: #12 FR-PA-039; #21 formation routing; owner decisions P1/P2 (#434).
// Purpose: Read-only live observation of requested and effective formations.

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Formation requests and the fixed kickoff selections; copies, never mutable state.</summary>
    public readonly struct MatchFormationState
    {
        /// <summary>Home team's latest request.</summary>
        public readonly TacticFormation HomeRequested;
        /// <summary>Home team's effective positioning formation.</summary>
        public readonly TacticFormation HomeEffective;
        /// <summary>Away team's latest request.</summary>
        public readonly TacticFormation AwayRequested;
        /// <summary>Away team's effective positioning formation.</summary>
        public readonly TacticFormation AwayEffective;

        /// <summary>Copies the two teams' formation observations.</summary>
        public MatchFormationState(TacticFormation homeRequested, TacticFormation homeEffective,
            TacticFormation awayRequested, TacticFormation awayEffective)
        {
            HomeRequested = homeRequested;
            HomeEffective = homeEffective;
            AwayRequested = awayRequested;
            AwayEffective = awayEffective;
        }
    }
}

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-09 | — | Requested/effective observation for fixed kickoff formation. |
#endregion
