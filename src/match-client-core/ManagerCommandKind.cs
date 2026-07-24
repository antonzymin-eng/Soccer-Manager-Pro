// File:     src/match-client-core/ManagerCommandKind.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P2/§6.4),
//           Code Standards #20
// Purpose:  Discriminates the closed set of typed manager GAME commands the deterministic command
//           channel carries. Playback pause/speed are deliberately absent — they are presentation
//           pacing on the streamer's own surface, never commands (§6.4).

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The closed set of manager game-command kinds. Each maps onto exactly one live, stride-safe
    /// engine mutator (<see cref="ILiveMatchMutations"/>). There is no pause/speed kind — playback is
    /// not a game command (§6.4) — and no boot mutator kind (those belong to <see cref="MatchSetup"/>).
    /// <para>
    /// <see cref="None"/> is the zero value on purpose: a <c>default(ManagerCommand)</c> (an unfilled
    /// array slot, <c>new ManagerCommand()</c>) therefore reads as <see cref="None"/> and fails loud at
    /// enqueue / apply, rather than silently mapping onto a real mutator with a malformed payload — the
    /// project's zero-value-safety convention.
    /// </para>
    /// <para>
    /// ORDINAL STABILITY: these ordinals are not serialized today (the tick-stamped log is in-memory
    /// only, §11), but a future on-disk replay would embed them. APPEND new kinds at the end; never
    /// insert or renumber (keep <see cref="None"/> at 0).
    /// </para>
    /// </summary>
    public enum ManagerCommandKind
    {
        /// <summary>The uninitialized sentinel (zero value). Not a real command — refused at enqueue and apply.</summary>
        None = 0,

        /// <summary>Stage a team-level tactic change (→ <c>SetTeamTactic</c>).</summary>
        SetTeamTactic = 1,

        /// <summary>Stage a per-agent tactic change (→ <c>SetPlayerTactic</c>).</summary>
        SetPlayerTactic = 2,

        /// <summary>Swap a bench player onto the pitch (→ <c>SubstitutePlayer</c>).</summary>
        Substitute = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P2): three game-command kinds, one per live  |
// |         |            |        | engine mutator. Playback/boot mutators deliberately excluded.  |
// | 1.1     | 2026-07-24 | —      | AR pass-1 Medium: added the None=0 zero-value sentinel (game    |
// |         |            |        | kinds shifted to 1/2/3) so a default(ManagerCommand) fails      |
// |         |            |        | loud instead of silently staging a malformed team tactic.      |
#endregion
