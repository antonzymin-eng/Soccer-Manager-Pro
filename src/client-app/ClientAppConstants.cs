// File:     src/client-app/ClientAppConstants.cs
// Created:  2026-08-07
// Modified: 2026-11-05 (H1: [CROSS] client wage guard)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a (screen-catalogue home, owner
//           decision 2026-08-07), §5-P7 (career screens, save slots), UI / Client Framework #38
//           §3.2 (FR-UI-010), Code Standards #20
// Purpose:  Constant catalogue for the client-app assembly: the screens' [FIXED] identity values and the
//           [CROSS] client-side offer guard. Presentation-only — nothing here reaches the simulation, the
//           snapshot digest, or any determinism surface.

using TacticalDirector.ClubFinances;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Constant catalogue for the client application. Every value here is screen identity or a client input
    /// guard, never a sim constant: this assembly registers no RNG stream, no domain tag, no <c>SubsystemOrdinal</c>, and
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

        /// <summary>[FIXED] Career Home / Season screen identity — the career context root (§5-P7).</summary>
        public const int SCREEN_ID_CAREER_HOME = 5;

        /// <summary>[FIXED] New Career screen identity — club choice over a generated league (§5-P7 D-P7-1).</summary>
        public const int SCREEN_ID_NEW_CAREER = 6;

        /// <summary>[FIXED] Load Career screen identity — the save-slot picker (§5-P7 D-P7-2).</summary>
        public const int SCREEN_ID_LOAD_CAREER = 7;

        /// <summary>[FIXED] Transfers screen identity — buy and sell offers through #31 (§5-P8).</summary>
        public const int SCREEN_ID_TRANSFERS = 8;

        /// <summary>[FIXED] Club Finances screen identity — the read-only #40 view (§5-P8).</summary>
        public const int SCREEN_ID_CLUB_FINANCES = 9;

        /// <summary>
        /// [FIXED] Number of manual career save slots, beside the one autosave slot (owner product
        /// decision D-P7-2, October 18, 2026). A product promise, not a balance dial.
        /// </summary>
        public const int CAREER_MANUAL_SLOT_COUNT = 3;

        /// <summary>
        /// [FIXED] The <c>WorldStore</c> manager id a client-started career is created with. Early
        /// Access has exactly one human manager per career (§5-P7); the value only needs to be stable,
        /// because it is persisted inside the world blob and a resumed career carries its own.
        /// </summary>
        public const int CAREER_MANAGER_ID = 1;

        /// <summary>
        /// [FIXED] Version of the client-owned career slot sidecar (<c>.slot</c>) text format. v2 adds the
        /// nested sub-blob format stamp; a v1 sidecar reads as Incompatible (D-10).
        /// </summary>
        public const int CAREER_SLOT_META_FORMAT_VERSION = 2;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Highest wage per period the client lets an offer carry: #40's [GT] sanity ceiling for
        /// projected transfer and wage budgets (<c>ClubFinancesConstants.ClubFinancesBudgetCeilingMax</c>,
        /// Spec #40 Appendix A), consumed read-only. A client input guard only, not a game rule: #31 itself
        /// accepts any non-negative wage and compares none against #40's wage budget (H1; ERR-031 gap open
        /// for an owner decision). The guard keeps an unbounded wage out of #31, so committed contract wages
        /// stay far from the long range every finance projection and money string relies on.
        /// </summary>
        public static readonly long MAX_OFFER_WAGE_PER_PERIOD = ClubFinancesConstants.ClubFinancesBudgetCeilingMax;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation: the four [FIXED] screen ids, 0 deliberately  |
// |         |            |        | unallocated (zero-value safety).                               |
// | 1.1     | 2026-10-18 | —      | §5-P7: career screen ids 5–7, manual slot count, career        |
// |         |            |        | manager id, slot sidecar format version.                       |
// | 1.2     | 2026-10-27 | —      | PR 519 review: CAREER_SLOT_META_FORMAT_VERSION 1 → 2 (sidecar  |
// |         |            |        | records the nested sub-blob format stamp).                     |
// | 1.3     | 2026-11-05 | —      | §5-P8: SCREEN_ID_TRANSFERS (8), SCREEN_ID_CLUB_FINANCES (9).   |
// | 1.4     | 2026-11-05 | —      | H1: [CROSS] MAX_OFFER_WAGE_PER_PERIOD = #40                    |
// |         |            |        | ClubFinancesBudgetCeilingMax (client offer guard).             |
#endregion
