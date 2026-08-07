// File:     src/match-client-core/MatchControlLockReason.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5, §6.2,
//           §12 rule 1), Code Standards #20
// Purpose:  Why a match-view control is unavailable, so the shell can say so instead of presenting a
//           button that silently does nothing.

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Why a control is locked (§5-P5). Carried alongside the availability flags so the UI can
    /// explain a disabled control rather than leaving the viewer to guess.
    ///
    /// <para><see cref="None"/> is the zero value on purpose: a <c>default</c> reason reads as "not
    /// locked", matching <c>ManagerCommandKind.None</c> and <c>IntentKind.None</c>, whose absence was
    /// a real defect in the first of those types.</para>
    /// </summary>
    public enum MatchControlLockReason
    {
        /// <summary>Not locked — the control is available.</summary>
        None = 0,

        /// <summary>
        /// The streamer has published no frame yet, so there is no match state to act on. Distinct
        /// from <see cref="MatchEnded"/> because it is transient: the same control becomes available
        /// a frame later, and a UI may prefer to show it busy rather than refused.
        /// </summary>
        AwaitingFirstFrame = 1,

        /// <summary>
        /// The match has finished. Tactical input and substitutions can no longer change anything —
        /// the engine freezes its AI, Physics and Resolve phases at full time.
        /// </summary>
        MatchEnded = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation (§5-P5 host-free half): the two reasons a      |
// |         |            |        | match-view control is locked, with None=0 so a default reads    |
// |         |            |        | as unlocked.                                                    |
#endregion
