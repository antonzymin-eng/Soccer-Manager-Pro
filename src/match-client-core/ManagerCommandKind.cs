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
    /// ORDINAL STABILITY: these ordinals are not serialized today (the tick-stamped log is in-memory
    /// only, §11), but a future on-disk replay would embed them. APPEND new kinds at the end; never
    /// insert or renumber.
    /// </para>
    /// </summary>
    public enum ManagerCommandKind
    {
        /// <summary>Stage a team-level tactic change (→ <c>SetTeamTactic</c>).</summary>
        SetTeamTactic = 0,

        /// <summary>Stage a per-agent tactic change (→ <c>SetPlayerTactic</c>).</summary>
        SetPlayerTactic = 1,

        /// <summary>Swap a bench player onto the pitch (→ <c>SubstitutePlayer</c>).</summary>
        Substitute = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P2): three game-command kinds, one per live  |
// |         |            |        | engine mutator. Playback/boot mutators deliberately excluded.  |
#endregion
