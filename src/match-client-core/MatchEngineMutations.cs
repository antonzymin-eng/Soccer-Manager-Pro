// File:     src/match-client-core/MatchEngineMutations.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §3-2/§4),
//           Code Standards #20
// Purpose:  Adapts a live MatchEngine to ILiveMatchMutations, forwarding each call verbatim to the
//           engine's pre-existing public live mutators — no new mutator, no raw state poke. Owned by
//           MatchSession and only ever invoked on the sim thread via the streamer's pre-tick hook.

using System;

using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The one <see cref="ILiveMatchMutations"/> implementation over a real
    /// <see cref="TacticalDirector.MatchEngine.MatchEngine"/>. A pure pass-through: every method maps
    /// onto exactly one pre-existing public engine mutator, and the two read signals onto the engine's
    /// public observation properties. Because it is passed to the driver as a parameter (never held by
    /// the driver), the driver keeps no off-thread-callable engine reference (§4).
    /// </summary>
    public sealed class MatchEngineMutations : ILiveMatchMutations
    {
        private readonly MatchEngine.MatchEngine _engine;

        /// <summary>Wraps <paramref name="engine"/>. Must not be null.</summary>
        public MatchEngineMutations(MatchEngine.MatchEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <inheritdoc/>
        public ulong CurrentTick => _engine.CurrentTick;

        /// <inheritdoc/>
        public bool MatchEnded => _engine.MatchEnded;

        /// <inheritdoc/>
        public void SetTeamTactic(int teamId, in TeamTactic tactic) => _engine.SetTeamTactic(teamId, in tactic);

        /// <inheritdoc/>
        public void SetPlayerTactic(int agentId, in PlayerTactic tactic) => _engine.SetPlayerTactic(agentId, in tactic);

        /// <inheritdoc/>
        public void SubstitutePlayer(int teamId, int outSlotIndex, int benchIndex, SubstitutionReason reason)
            => _engine.SubstitutePlayer(teamId, outSlotIndex, benchIndex, reason);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P0): pass-through adapter from a live         |
// |         |            |        | MatchEngine to the ILiveMatchMutations command surface.        |
#endregion
