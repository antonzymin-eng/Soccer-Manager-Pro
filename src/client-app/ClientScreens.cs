// File:     src/client-app/ClientScreens.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b (the four screens),
//           UI / Client Framework #38 §3.2 (FR-UI-010), Code Standards #20
// Purpose:  The client's screen catalogue: the four ScreenIds as typed, named values. This is the one
//           place a screen identity is minted; every registration and navigation site reads from here.

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The four screens of the interactive Unity client (§5-P5b): Main Menu, Tactics Setup, Match View,
    /// Post-Match Report. FR-UI-010 keeps concrete screens out of <c>ui-framework</c>; this catalogue is
    /// the client-side allocation the framework deliberately refuses to own.
    /// <para>
    /// The legal transitions between these screens are owned by <see cref="ClientScreenFlow"/> — this
    /// type answers only "which screens exist".
    /// </para>
    /// </summary>
    public static class ClientScreens
    {
        /// <summary>Main Menu — the root screen; the shell is never without it beneath the stack.</summary>
        public static readonly ScreenId MainMenu = new ScreenId(ClientAppConstants.SCREEN_ID_MAIN_MENU);

        /// <summary>Tactics Setup — formation / team / player instructions, writing a <c>MatchSetup</c>.</summary>
        public static readonly ScreenId TacticsSetup = new ScreenId(ClientAppConstants.SCREEN_ID_TACTICS_SETUP);

        /// <summary>Match View — the live match canvas, speed controls, and in-match tactical input.</summary>
        public static readonly ScreenId MatchView = new ScreenId(ClientAppConstants.SCREEN_ID_MATCH_VIEW);

        /// <summary>Post-Match Report — final score + stats once the match has ended.</summary>
        public static readonly ScreenId PostMatchReport = new ScreenId(ClientAppConstants.SCREEN_ID_POST_MATCH_REPORT);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation: the four screens as typed ScreenIds.         |
#endregion
