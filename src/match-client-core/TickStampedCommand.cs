// File:     src/match-client-core/TickStampedCommand.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §6.1/§6.3),
//           Code Standards #20
// Purpose:  One entry in the tick-stamped command log: the applied command paired with the engine
//           tick it was applied at. The log of these is the record a live match is byte-identically
//           reproducible from (MatchSetup + this log), since wall-clock click timing is not itself
//           reproducible (§6.1).

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// A <see cref="ManagerCommand"/> paired with the engine tick it was applied at. Reproducibility is
    /// defined against the sequence of these (not human intent): the same <see cref="MatchSetup"/> plus
    /// the same tick-stamped sequence re-ticks byte-identically (§6.1). In-memory only at Stage 0 (§11).
    /// </summary>
    public readonly struct TickStampedCommand
    {
        /// <summary>The engine tick the command was applied at (read at the top of the applying tick).</summary>
        public readonly ulong AppliedTick;

        /// <summary>The applied command.</summary>
        public readonly ManagerCommand Command;

        /// <summary>Pairs <paramref name="command"/> with the <paramref name="appliedTick"/> it was applied at.</summary>
        public TickStampedCommand(ulong appliedTick, in ManagerCommand command)
        {
            AppliedTick = appliedTick;
            Command     = command;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P2): (appliedTick, command) log entry.       |
#endregion
