// File:     src/ui-framework/IntentRouting.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     UI / Client Framework #38 Appendix C (dispatcher split by owning surface), §3.3 (F3),
//           docs/tracking/interactive-unity-client-design.md §5-P7, Code Standards #20
// Purpose:  The single classifier of which dispatcher owns an IntentKind. Appendix C splits the kinds
//           between a match-tactics dispatcher and a season dispatcher; this type is where that split
//           is written down once, so the two dispatchers and their drift guards cannot disagree.

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// Classifies an <see cref="IntentKind"/> by owning dispatcher (#38 Appendix C). A kind is exactly
    /// one of: a <b>match</b> intent (live-match channel, marshaled per FR-UI-023), a <b>season</b>
    /// intent (#30's single-threaded turn-based seams, called directly), or neither
    /// (<see cref="IntentKind.None"/> and any unallocated ordinal) — which every dispatcher refuses (F3).
    /// </summary>
    public static class IntentRouting
    {
        /// <summary>True for the three kinds the match-tactics dispatcher owns.</summary>
        public static bool IsMatchIntent(IntentKind kind) =>
            kind == IntentKind.SetTeamTactic
            || kind == IntentKind.SetPlayerTactic
            || kind == IntentKind.Substitute;

        /// <summary>True for the three kinds a season dispatcher owns.</summary>
        public static bool IsSeasonIntent(IntentKind kind) =>
            kind == IntentKind.AdvanceRound
            || kind == IntentKind.AdvanceDay
            || kind == IntentKind.StartNextSeason;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): match vs season ownership of every   |
// |         |            |        | IntentKind, the one classifier both dispatchers' guards read.  |
#endregion
