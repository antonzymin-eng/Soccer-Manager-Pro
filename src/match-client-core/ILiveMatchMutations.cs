// File:     src/match-client-core/ILiveMatchMutations.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §3-2/§4/§6),
//           Code Standards #20
// Purpose:  The exact, closed set of LIVE (stride-committed) MatchEngine mutators the deterministic
//           command channel is allowed to drive, plus the two read-only signals the sim-side drain
//           needs (current tick for stamping, match-ended for the post-full-time drop). Both sides
//           are specified — producer is MatchClientDriver, consumer is MatchEngineMutations — so this
//           is not a phantom interface (CLAUDE.md "Interface Design Principle").

using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The mutation surface the pre-tick command drain applies commands through, on the sim thread.
    /// It exposes ONLY the three live, stride-safe mutators (§3-2) — there is deliberately no
    /// pre-kickoff/boot mutator (<c>ConfigureSquads</c>/<c>ConfigureManager</c>/<c>EnableGkHeading</c>,
    /// which belong to <see cref="MatchSetup"/>) and no raw poke into engine state. The driver
    /// receives this as a method parameter, so it retains no engine reference it could call off the
    /// sim thread (§4).
    /// </summary>
    public interface ILiveMatchMutations
    {
        /// <summary>The engine's current (last completed) tick — the value a drained command is stamped with.</summary>
        ulong CurrentTick { get; }

        /// <summary>True once full time has fired. The drain drops any command while this is set — a finished match is never mutated (§6.2).</summary>
        bool MatchEnded { get; }

        /// <summary>Live, stride-committed: stages a team tactic change (FR-TI-027).</summary>
        void SetTeamTactic(int teamId, in TeamTactic tactic);

        /// <summary>Live, stride-committed: stages a per-agent tactic change (FR-TI-027).</summary>
        void SetPlayerTactic(int agentId, in PlayerTactic tactic);

        /// <summary>Live: swaps a bench player onto the pitch (guarded against a finished match inside the engine).</summary>
        void SubstitutePlayer(int teamId, int outSlotIndex, int benchIndex, SubstitutionReason reason);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P0/P2): closed live-mutator surface for the  |
// |         |            |        | deterministic command channel + tick/match-ended read signals. |
#endregion
