// File:     src/client-app/ClientAppConstants.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a (screen-catalogue home, owner
//           decision 2026-08-07), UI / Client Framework #38 §3.2 (FR-UI-010), Code Standards #20
// Purpose:  Constant catalogue for the client-app assembly: the four screens' [FIXED] identity values.
//           Presentation-only — nothing here reaches the simulation, the snapshot digest, or any
//           determinism surface.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Constant catalogue for the client application. Every value here is screen identity, never a sim
    /// constant: this assembly registers no RNG stream, no domain tag, no <c>SubsystemOrdinal</c>, and
    /// holds no persistent sim state — the same posture as <c>UiFrameworkConstants</c> (FR-UI-022).
    /// <para>
    /// The ids live in this assembly rather than <c>ui-framework</c> because FR-UI-010 is explicit that
    /// the framework hard-codes no screen — a concrete screen catalogue is client composition, not
    /// framework. They are <c>[FIXED]</c>, not <c>[GT]</c>: a screen's identity is an allocation, like
    /// a save-format magic, not a designer dial.
    /// </para>
    /// </summary>
    public static class ClientAppConstants
    {
        #region Fixed

        /// <summary>
        /// [FIXED] Main Menu screen identity. Ids start at 1 — <b>0 is deliberately never allocated</b>,
        /// so <c>default(ScreenId)</c> aliases no real screen (the <c>ManagerCommandKind.None</c>
        /// zero-value-safety convention <c>NavigationShell</c>'s un-rooted <c>Current</c> guard cites).
        /// </summary>
        public const int SCREEN_ID_MAIN_MENU = 1;

        /// <summary>[FIXED] Tactics Setup screen identity (writes the <c>MatchSetup</c> pre-match).</summary>
        public const int SCREEN_ID_TACTICS_SETUP = 2;

        /// <summary>[FIXED] Match View screen identity (the live match canvas + controls).</summary>
        public const int SCREEN_ID_MATCH_VIEW = 3;

        /// <summary>[FIXED] Post-Match Report screen identity (final score + stats).</summary>
        public const int SCREEN_ID_POST_MATCH_REPORT = 4;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation: the four [FIXED] screen ids, 0 deliberately  |
// |         |            |        | unallocated (zero-value safety).                               |
#endregion
