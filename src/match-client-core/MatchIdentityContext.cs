// File:     src/match-client-core/MatchIdentityContext.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Resolves immutable content against the accepted frame and engine boot descriptor.

using System;
using System.Collections.Generic;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>Authored identity policy for a distinct-squad match; never selects a lineup.</summary>
    public sealed class MatchIdentityContext
    {
        private readonly Dictionary<int, MatchPlayerIdentity> _players;
        /// <summary>Match-constant slot/team policy.</summary>
        public MatchRoster Roster { get; }
        /// <summary>Engine-assigned starter and bench origins, copied before playback.</summary>
        public MatchBootRoster Boot { get; }

        /// <summary>Admits a complete engine XI/bench and uniquely numbered immutable content.</summary>
        public MatchIdentityContext(MatchRoster roster, MatchBootRoster boot, MatchPlayerIdentity[] players)
        {
            Roster = roster ?? throw new ArgumentNullException(nameof(roster));
            Boot = boot ?? throw new ArgumentNullException(nameof(boot));
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (roster.AgentCount != MatchEngineConstants.SQUAD_SIZE)
                throw new ArgumentException("A distinct match needs a full pitch roster.");
            _players = new Dictionary<int, MatchPlayerIdentity>();
            var shirts = new HashSet<int>[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < shirts.Length; t++) shirts[t] = new HashSet<int>();
            foreach (MatchPlayerIdentity player in players)
            {
                // Revalidate even a default struct, not just instances constructed through its guard.
                var admitted = new MatchPlayerIdentity(player.TeamId, player.PlayerId,
                    player.FirstName, player.LastName, player.ShirtNumber);
                if (_players.ContainsKey(admitted.PlayerId) || !shirts[admitted.TeamId].Add(admitted.ShirtNumber))
                    throw new ArgumentException("Duplicate player id or team shirt.");
                _players.Add(admitted.PlayerId, admitted);
            }
            var selected = new HashSet<int>();
            for (int i = 0; i < roster.AgentCount; i++)
            {
                int id = Boot.StarterPlayerId(i);
                Lookup(roster.TeamId(i), id);
                if (!selected.Add(id)) throw new ArgumentException("Duplicate engine boot occupant.");
            }
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
                for (int b = 0; b < MatchEngineConstants.SUBSTITUTES_PER_TEAM; b++)
                {
                    int id = Boot.BenchPlayerId(t, b);
                    Lookup(t, id);
                    if (!selected.Add(id)) throw new ArgumentException("Duplicate engine boot bench origin.");
                }
            if (selected.Count != _players.Count)
                throw new ArgumentException("Fixture content and engine boot occupants differ.");
        }

        /// <summary>Current occupant from one accepted frame, never a command log or boot starter.</summary>
        public MatchPlayerIdentity Resolve(in LiveMatchFrame frame, int agentId)
        {
            if (frame.AgentCues == null || frame.AgentCues.Length != Roster.AgentCount)
                throw new ArgumentException("Identity frame has the wrong cue shape.");
            return Lookup(Roster.TeamId(agentId), frame.AgentCues[agentId].PlayerId);
        }

        /// <summary>Immutable identity at a bench origin, including origins already used in play.</summary>
        public MatchPlayerIdentity Bench(int teamId, int benchIndex) => Lookup(teamId, Boot.BenchPlayerId(teamId, benchIndex));

        /// <summary>True when the accepted frame shows that bench origin on the pitch.</summary>
        public bool IsBenchUsed(in LiveMatchFrame frame, int teamId, int benchIndex)
        {
            Bench(teamId, benchIndex);
            if (frame.AgentCues == null || frame.AgentCues.Length != Roster.AgentCount)
                throw new ArgumentException("Identity frame has the wrong cue shape.");
            for (int i = 0; i < Roster.AgentCount; i++)
                if (Roster.TeamId(i) == teamId && frame.AgentCues[i].BenchSlot == benchIndex) return true;
            return false;
        }

        private MatchPlayerIdentity Lookup(int teamId, int playerId)
        {
            if (!_players.TryGetValue(playerId, out MatchPlayerIdentity player) || player.TeamId != teamId)
                throw new ArgumentException("Frame/boot identity is absent from the admitted team content.");
            return player;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
