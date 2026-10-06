// File:     src/match-client-core/MatchBootRoster.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Private boot copy of engine-assigned pitch and bench-origin identities.

using System;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>Immutable boot descriptor. Bench origins remain valid after their players enter play.</summary>
    public sealed class MatchBootRoster
    {
        private readonly int[] _playerIds;
        /// <summary>Captures the canonical id-space mapping, copying caller storage.</summary>
        public MatchBootRoster(int[] playerIds)
        {
            if (playerIds == null) throw new ArgumentNullException(nameof(playerIds));
            if (playerIds.Length != MatchEngineConstants.AgentIdSpace)
                throw new ArgumentException("Boot roster has the wrong agent id space.", nameof(playerIds));
            _playerIds = (int[])playerIds.Clone();
        }
        /// <summary>Original occupant of a pitch slot.</summary>
        public int StarterPlayerId(int agentId)
        {
            if (agentId < 0 || agentId >= MatchEngineConstants.SQUAD_SIZE)
                throw new ArgumentOutOfRangeException(nameof(agentId));
            return _playerIds[agentId];
        }
        /// <summary>Identity selected by the engine for a zero-based team bench origin.</summary>
        public int BenchPlayerId(int teamId, int benchIndex)
        {
            if (teamId < 0 || teamId >= MatchEngineConstants.TEAM_COUNT)
                throw new ArgumentOutOfRangeException(nameof(teamId));
            if (benchIndex < 0 || benchIndex >= MatchEngineConstants.SUBSTITUTES_PER_TEAM)
                throw new ArgumentOutOfRangeException(nameof(benchIndex));
            return _playerIds[MatchEngineConstants.SQUAD_SIZE +
                teamId * MatchEngineConstants.SUBSTITUTES_PER_TEAM + benchIndex];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
